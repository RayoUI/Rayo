using Rayo.Controls;
using Rayo.Core;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

namespace Rayo.Tests;

public sealed class ThemeDataTests
{
    public sealed record ChartTheme(Color GridColor, float LineWidth) : IThemeExtension;

    [Fact]
    public void With_expression_does_not_mutate_source_theme()
    {
        var customized = RayoThemes.Light with
        {
            Spacing = RayoThemes.Light.Spacing with { Md = 10 },
        };

        Assert.Equal(8, RayoThemes.Light.Spacing.Md);
        Assert.Equal(10, customized.Spacing.Md);
    }

    [Fact]
    public void Rebuilding_component_defaults_uses_updated_semantic_systems()
    {
        var customized = (RayoThemes.Light with
        {
            Density = ThemeDensity.Touch,
            Preferences = ThemePreferences.Default with { TextScale = 1.5f },
        }).RebuildComponentDefaults();
        var entry = new Entry();
        _ = new ThemeScope(customized, entry);

        Assert.Equal(44f, entry.MinHeight);
        Assert.Equal(21f, entry.FontSize);
        Assert.Equal(44f, customized.Buttons.MinHeight);
        Assert.Equal(21f, customized.Buttons.Typography.FontSize);
    }

    [Fact]
    public void Typed_computed_tokens_resolve_dependencies()
    {
        var accent = new ThemeKey<Color>("color.accent");
        var muted = new ThemeKey<Color>("color.accent-muted");
        var theme = RayoThemes.Light
            .WithToken(accent, new Color(10, 20, 30))
            .WithComputedToken(muted, tokens => tokens.Get(accent).WithAlpha(0.5f));

        Assert.Equal(0.5f, theme.GetToken(muted).A);
    }

    [Fact]
    public void Computed_tokens_report_cycles()
    {
        var first = new ThemeKey<float>("first");
        var second = new ThemeKey<float>("second");
        var theme = RayoThemes.Light
            .WithComputedToken(first, tokens => tokens.Get(second))
            .WithComputedToken(second, tokens => tokens.Get(first));

        var error = Assert.Throws<InvalidOperationException>(() => theme.GetToken(first));
        Assert.Contains("first -> second -> first", error.Message);
    }

    [Fact]
    public void Invalid_scales_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new ThemeData(
            "invalid",
            ColorSchemes.Light,
            spacing: SpacingScale.Default with { Sm = 20, Md = 8 }));
    }

    [Fact]
    public void Component_theme_is_compile_time_typed_and_applied()
    {
        var buttonTheme = ComponentTheme<Button>.Empty.Set(button => button.FontSize, 21f);
        var theme = RayoThemes.Dark.WithComponentTheme(buttonTheme);
        var button = new Button();
        _ = new ThemeScope(theme, button);

        Assert.Equal(21f, button.FontSize);
    }

    [Fact]
    public void Json_round_trip_preserves_complete_theme_and_tokens()
    {
        var accent = new ThemeKey<Color>("color.custom");
        var source = RayoThemes.Dark.WithToken(accent, new Color(12, 34, 56));

        var json = ThemeJson.Serialize(source);
        var restored = ThemeJson.Deserialize(json);

        Assert.Equal(source.Name, restored.Name);
        Assert.Equal(source.Brightness, restored.Brightness);
        Assert.Equal(source.Colors.Primary, restored.Colors.Primary);
        Assert.Equal(source.Shapes.Medium.TopLeft, restored.Shapes.Medium.TopLeft);
        Assert.Equal(source.Shapes.Medium.BottomRight, restored.Shapes.Medium.BottomRight);
        Assert.Equal(source.GetToken(accent), restored.GetToken(accent));
    }

    [Fact]
    public void Json_based_on_inherits_missing_systems()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "basedOn": "dark",
              "name": "derived",
              "density": "compact"
            }
            """;

        var theme = ThemeJson.Deserialize(json);

        Assert.Equal("derived", theme.Name);
        Assert.Equal(ThemeDensity.Compact, theme.Density);
        Assert.Equal(RayoThemes.Dark.Colors, theme.Colors);
        Assert.True(theme.Components.HasCustomThemes);
    }

    [Fact]
    public void Json_errors_include_the_failing_path()
    {
        const string json = """{"schemaVersion":99,"name":"future"}""";

        var error = Assert.Throws<ThemeJsonException>(() => ThemeJson.Deserialize(json));

        Assert.Equal("$.schemaVersion", error.JsonPath);
    }

    [Fact]
    public void High_contrast_is_a_complete_accessible_builtin_theme()
    {
        var theme = RayoThemes.HighContrast;

        Assert.Equal(ThemeBrightness.HighContrast, theme.Brightness);
        Assert.True(theme.Preferences.HighContrast);
        Assert.True(theme.Preferences.ReduceMotion);
        Assert.True(ThemeColorUtilities.MeetsWcagAA(
            theme.Colors.OnBackground,
            theme.Colors.Background));
        Assert.True(ThemeColorUtilities.MeetsWcagAA(
            theme.Colors.OnPrimary,
            theme.Colors.Primary));
    }

    [Fact]
    public void Json_round_trip_preserves_registered_extensions()
    {
        var registry = new ThemeJsonRegistry().Register<ChartTheme>("chart");
        var source = RayoThemes.Dark.WithExtension(
            new ChartTheme(new Color(12, 34, 56), 2.5f));

        var json = ThemeJson.Serialize(source, registry: registry);
        var restored = ThemeJson.Deserialize(json, registry: registry);

        Assert.Equal(source.Extension<ChartTheme>(), restored.Extension<ChartTheme>());
    }

    [Fact]
    public void Json_round_trip_preserves_generic_component_overrides()
    {
        var background = new Color(22, 44, 66);
        var source = RayoThemes.Light.WithComponentTheme(
            ComponentTheme<Button>.Empty
                .Set(button => button.FontSize, 21f)
                .Set(button => button.Background, (Brush)background));

        var json = ThemeJson.Serialize(source);
        var restored = ThemeJson.Deserialize(json);
        var button = new Button();
        _ = new ThemeScope(restored, button);

        Assert.Equal(21f, button.FontSize);
        Assert.Equal(background, button.Background.PrimaryColor);
    }

    [Fact]
    public void Hot_reload_keeps_last_valid_theme_after_an_invalid_file()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"rayo-theme-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "theme.json");

        try
        {
            File.WriteAllText(path, ThemeJson.Serialize(RayoThemes.Light));
            using var watcher = new ThemeHotReloadWatcher(path);
            var lastValid = watcher.Current;

            File.WriteAllText(path, """{"schemaVersion":99,"name":"invalid"}""");

            Assert.False(watcher.TryReload(out var error));
            Assert.Same(lastValid, watcher.Current);
            Assert.Contains("$.schemaVersion", error);

            File.WriteAllText(path, ThemeJson.Serialize(RayoThemes.Dark));
            Assert.True(watcher.TryReload(out error));
            Assert.Null(error);
            Assert.Equal(ThemeBrightness.Dark, watcher.Current.Brightness);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
