using Rayo.Rendering;
using Rayo.Styling;
using Rayo.Controls;

namespace Rayo.Tests;

public sealed class StateAndAccessibilityTests
{
    [Fact]
    public void State_map_prefers_exact_combination()
    {
        var map = new StateMap<string>("normal")
            .With(ControlState.Hovered, "hover")
            .With(ControlState.Pressed, "pressed")
            .With(ControlState.Hovered | ControlState.Pressed, "hover-pressed");

        Assert.Equal(
            "hover-pressed",
            map.Resolve(ControlState.Hovered | ControlState.Pressed));
    }

    [Fact]
    public void State_map_uses_documented_priority_for_ties()
    {
        var map = new StateMap<string>("normal")
            .With(ControlState.Hovered, "hover")
            .With(ControlState.Disabled, "disabled");

        Assert.Equal(
            "disabled",
            map.Resolve(ControlState.Hovered | ControlState.Disabled));
    }

    [Fact]
    public void Wcag_contrast_matches_reference_extremes()
    {
        Assert.Equal(21f, ThemeColorUtilities.ContrastRatio(Color.White, Color.Black), 3);
        Assert.True(ThemeColorUtilities.MeetsWcagAA(Color.White, Color.Black));
    }

    [Fact]
    public void Perceptual_lightness_preserves_alpha()
    {
        var source = new Color(40, 100, 180, 128);
        var adjusted = ThemeColorUtilities.AdjustLightness(source, 0.1f);

        Assert.Equal(source.A, adjusted.A);
        Assert.True(adjusted.R > source.R || adjusted.G > source.G || adjusted.B > source.B);
    }

    [Fact]
    public void Host_preferences_create_a_complete_accessible_theme()
    {
        var theme = RayoThemes.ResolveSystem(new HostThemePreferences
        {
            PrefersDark = true,
            ReduceMotion = true,
            TextScale = 1.5f,
            Density = ThemeDensity.Touch,
        });

        Assert.Equal(ThemeBrightness.Dark, theme.Brightness);
        Assert.Equal(ThemeDensity.Touch, theme.Density);
        Assert.Equal(1.5f, theme.Preferences.TextScale);
        Assert.True(theme.Preferences.ReduceMotion);
        Assert.Equal(TimeSpan.Zero, theme.Motion.Normal);
        var entry = new Entry();
        _ = new ThemeScope(theme, entry);
        Assert.Equal(21f, entry.FontSize, 3);
    }

    [Fact]
    public void Host_high_contrast_takes_priority_over_dark_mode()
    {
        var theme = RayoThemes.ResolveSystem(new HostThemePreferences
        {
            PrefersDark = true,
            HighContrast = true,
        });

        Assert.Equal(ThemeBrightness.HighContrast, theme.Brightness);
        Assert.True(theme.Preferences.HighContrast);
    }
}
