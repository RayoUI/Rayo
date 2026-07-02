namespace Rayo.Styling;

using System.Collections.ObjectModel;

/// <summary>
/// Immutable, complete description of a Rayo visual theme.
/// </summary>
public sealed record ThemeData
{
    private IReadOnlyDictionary<Type, IThemeExtension> ExtensionMap { get; init; }

    public string Name { get; init; }
    public ThemeBrightness Brightness { get; init; }
    public ColorScheme Colors { get; init; }
    public TypographyScheme Typography { get; init; }
    public SpacingScale Spacing { get; init; }
    public ShapeScheme Shapes { get; init; }
    public ElevationScheme Elevation { get; init; }
    public MotionScheme Motion { get; init; }
    public ThemeDensity Density { get; init; }
    public ComponentThemes Components { get; init; }
    public ThemeTokenSet Tokens { get; init; }
    public ThemePreferences Preferences { get; init; }

    public IReadOnlyDictionary<Type, IThemeExtension> Extensions => ExtensionMap;

    public ButtonTheme Buttons => Components.Buttons;

    public float ControlHeight => Density switch
    {
        ThemeDensity.Compact => 28,
        ThemeDensity.Touch => 44,
        _ => 36,
    };

    public Thickness ControlPadding => Density switch
    {
        ThemeDensity.Compact => new Thickness(8, 4),
        ThemeDensity.Touch => new Thickness(14, 10),
        _ => new Thickness(10, 6),
    };

    public ThemeData(
        string name,
        ColorScheme colors,
        ButtonTheme? buttons = null,
        ThemeBrightness brightness = ThemeBrightness.Light,
        TypographyScheme? typography = null,
        SpacingScale? spacing = null,
        ShapeScheme? shapes = null,
        ElevationScheme? elevation = null,
        MotionScheme? motion = null,
        ThemeDensity density = ThemeDensity.Comfortable,
        ComponentThemes? components = null,
        ThemeTokenSet? tokens = null,
        ThemePreferences? preferences = null,
        IReadOnlyDictionary<Type, IThemeExtension>? extensions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(colors);

        Name = name;
        Brightness = brightness;
        Colors = colors;
        Typography = typography ?? TypographyScheme.Default;
        Spacing = spacing ?? SpacingScale.Default;
        Shapes = shapes ?? ShapeScheme.Default;
        Elevation = elevation ?? ElevationScheme.Default;
        Motion = motion ?? MotionScheme.Default;
        Density = density;
        Preferences = preferences ?? ThemePreferences.Default;
        Components = components ?? ComponentThemes.FromScheme(
            colors,
            buttons,
            Typography,
            Spacing,
            Shapes,
            Density,
            Preferences);
        Tokens = tokens ?? ThemeTokenSet.Empty;
        ExtensionMap = extensions is null
            ? EmptyExtensions
            : new ReadOnlyDictionary<Type, IThemeExtension>(
                new Dictionary<Type, IThemeExtension>(extensions));

        Validate();
    }

    private static IReadOnlyDictionary<Type, IThemeExtension> EmptyExtensions { get; } =
        new ReadOnlyDictionary<Type, IThemeExtension>(
            new Dictionary<Type, IThemeExtension>());

    public ThemeData WithExtension<T>(T extension) where T : class, IThemeExtension
    {
        ArgumentNullException.ThrowIfNull(extension);
        var extensions = new Dictionary<Type, IThemeExtension>(ExtensionMap)
        {
            [typeof(T)] = extension,
        };
        return this with { ExtensionMap = new ReadOnlyDictionary<Type, IThemeExtension>(extensions) };
    }

    public T? Extension<T>() where T : class, IThemeExtension =>
        ExtensionMap.TryGetValue(typeof(T), out var extension) ? (T)extension : null;

    public ThemeData WithToken<T>(ThemeKey<T> key, T value) where T : notnull =>
        this with { Tokens = Tokens.Set(key, value) };

    public ThemeData WithComputedToken<T>(
        ThemeKey<T> key,
        Func<ThemeTokenResolver, T> factory) where T : notnull =>
        this with { Tokens = Tokens.Set(key, factory) };

    public ThemeData WithComponentTheme<TControl>(ComponentTheme<TControl> componentTheme) =>
        this with { Components = Components.With(componentTheme) };

    /// <summary>
    /// Regenerates all built-in component defaults from the current semantic systems.
    /// Call this after a record <c>with</c> expression changes typography, spacing,
    /// shapes, density, colors or preferences, before adding custom component overrides.
    /// </summary>
    public ThemeData RebuildComponentDefaults() =>
        this with
        {
            Components = ComponentThemes.FromScheme(
                Colors,
                ButtonTheme.FromScheme(Colors) with
                {
                    Padding = ControlPadding,
                    Radius = Shapes.Medium,
                    Typography = Typography.Label with
                    {
                        FontSize = Typography.Label.FontSize * Preferences.TextScale,
                    },
                    MinHeight = ControlHeight,
                    BorderThickness = Shapes.BorderThick,
                },
                Typography,
                Spacing,
                Shapes,
                Density,
                Preferences),
        };

    public T GetToken<T>(ThemeKey<T> key) => Tokens.Get(key);

    public bool RequiresMeasureComparedTo(ThemeData previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        return Typography != previous.Typography ||
            Spacing != previous.Spacing ||
            Shapes != previous.Shapes ||
            Density != previous.Density ||
            Preferences.TextScale != previous.Preferences.TextScale ||
            !EqualityComparer<Thickness>.Default.Equals(Buttons.Padding, previous.Buttons.Padding) ||
            !EqualityComparer<CornerRadius>.Default.Equals(Buttons.Radius, previous.Buttons.Radius) ||
            Buttons.Typography != previous.Buttons.Typography ||
            Buttons.MinHeight != previous.Buttons.MinHeight ||
            Buttons.BorderThickness != previous.Buttons.BorderThickness;
    }

    public void Validate()
    {
        Typography.Validate();
        Spacing.Validate();
        Shapes.Validate();
        Elevation.Validate();
        Motion.Validate();
        Components.Validate();
        Preferences.Validate();
    }
}

/// <summary>Ready-to-use built-in themes.</summary>
public static class RayoThemes
{
    public static ThemeData Light { get; } = new(
        "light",
        ColorSchemes.Light,
        brightness: ThemeBrightness.Light);

    public static ThemeData Dark { get; } = new(
        "dark",
        ColorSchemes.Dark,
        brightness: ThemeBrightness.Dark);

    public static ThemeData HighContrast { get; } = new(
        "high-contrast",
        ColorSchemes.HighContrast,
        brightness: ThemeBrightness.HighContrast,
        shapes: ShapeScheme.Default with { BorderThin = 2, BorderThick = 3 },
        motion: new MotionScheme
        {
            Fast = TimeSpan.Zero,
            Normal = TimeSpan.Zero,
            Slow = TimeSpan.Zero,
        },
        preferences: ThemePreferences.Default with
        {
            HighContrast = true,
            ReduceMotion = true,
        });

    /// <summary>Builds a complete immutable theme from preferences reported by a host.</summary>
    public static ThemeData ResolveSystem(HostThemePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        preferences.Validate();

        var source = preferences.HighContrast
            ? HighContrast
            : preferences.PrefersDark
                ? Dark
                : Light;
        var themePreferences = source.Preferences with
        {
            HighContrast = preferences.HighContrast,
            ReduceMotion = preferences.ReduceMotion,
            TextScale = preferences.TextScale,
        };
        var motion = preferences.ReduceMotion
            ? new MotionScheme
            {
                Fast = TimeSpan.Zero,
                Normal = TimeSpan.Zero,
                Slow = TimeSpan.Zero,
            }
            : source.Motion;
        var controlHeight = preferences.Density switch
        {
            ThemeDensity.Compact => 28f,
            ThemeDensity.Touch => 44f,
            _ => 36f,
        };
        var controlPadding = preferences.Density switch
        {
            ThemeDensity.Compact => new Thickness(8, 4),
            ThemeDensity.Touch => new Thickness(14, 10),
            _ => new Thickness(10, 6),
        };
        var buttons = source.Buttons with
        {
            Padding = controlPadding,
            Radius = source.Shapes.Medium,
            Typography = source.Buttons.Typography with
            {
                FontSize = source.Buttons.Typography.FontSize * preferences.TextScale,
            },
            MinHeight = controlHeight,
            BorderThickness = source.Shapes.BorderThick,
        };

        return new ThemeData(
            source.Name,
            source.Colors,
            buttons,
            source.Brightness,
            source.Typography,
            source.Spacing,
            source.Shapes,
            source.Elevation,
            motion,
            preferences.Density,
            tokens: source.Tokens,
            preferences: themePreferences,
            extensions: source.Extensions);
    }

    public static void UseTheme(ThemeData theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (Rayo.Core.UIApplication.Current is { } app)
        {
            app.UseTheme(theme);
            return;
        }

        Rayo.Core.UIApplication.SetFallbackTheme(theme);
        Rayo.Core.UIApplication.NotifyThemeChanged(theme);

        var tree = Rayo.Core.UITree.Current;
        tree?.Root?.NotifyThemeChanged(theme);
        if (tree != null)
        {
            foreach (var overlay in tree.Overlays)
                overlay.NotifyThemeChanged(theme);
            tree.MarkNeedsMeasure();
        }
    }
}
