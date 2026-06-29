namespace Rayo.Styling;

/// <summary>
/// A named set of design-token values that acts as an override layer on top of
/// <see cref="StyleTokens"/>. When a theme is active (set via
/// <c>UIApplication.UseTheme</c>), any <see cref="StyleTokens.Get{T}"/> call
/// first looks up the token in the active theme before falling back to the
/// tokens dictionary itself.
///
/// <code>
/// var dark = new Theme("dark")
///     .Set("--bg",     new Color(18, 18, 18))
///     .Set("--fg",     Color.White)
///     .Set("--accent", new Color(100, 140, 255));
///
/// var light = new Theme("light")
///     .Set("--bg",     Color.White)
///     .Set("--fg",     new Color(20, 20, 20))
///     .Set("--accent", new Color(0, 120, 212));
///
/// app.UseTheme(dark);   // switches at runtime; UserControls re-apply styles
/// </code>
/// </summary>
public sealed class Theme
{
    private readonly Dictionary<string, object> _tokens = new();

    /// <summary>The name of this theme (e.g. "light", "dark", "high-contrast").</summary>
    public string Name { get; }

    /// <summary>Semantic colors shared by all controls.</summary>
    public ColorPalette Colors { get; private set; }

    /// <summary>Typed tokens used by button controls.</summary>
    public ButtonTheme Buttons { get; private set; }

    /// <summary>
    /// Creates a theme using the light palette and its derived control tokens.
    /// </summary>
    public Theme(string name) : this(name, ColorPalettes.Light)
    {
    }

    /// <summary>
    /// Creates a theme from semantic colors and optional button-specific tokens.
    /// </summary>
    public Theme(string name, ColorPalette colors, ButtonTheme? buttons = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(colors);

        Name = name;
        Colors = colors;
        Buttons = buttons ?? ButtonTheme.FromPalette(colors);
    }

    /// <summary>
    /// Replaces the semantic palette and regenerates control tokens from it.
    /// </summary>
    public Theme UseColors(ColorPalette colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        Colors = colors;
        Buttons = ButtonTheme.FromPalette(colors);
        return this;
    }

    /// <summary>Overrides the button-specific tokens for this theme.</summary>
    public Theme UseButtons(ButtonTheme buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        Buttons = buttons;
        return this;
    }

    /// <summary>Sets a token value. Returns <c>this</c> for chaining.</summary>
    public Theme Set<T>(string token, T value) where T : notnull
    {
        _tokens[token] = value;
        return this;
    }

    /// <summary>Gets a token value, or <paramref name="fallback"/> if not found.</summary>
    public T Get<T>(string token, T fallback = default!)
    {
        if (_tokens.TryGetValue(token, out var v) && v is T typed)
            return typed;
        return fallback;
    }

    /// <summary>Tries to get a token value.</summary>
    public bool TryGet<T>(string token, out T? value)
    {
        if (_tokens.TryGetValue(token, out var v) && v is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>Returns <c>true</c> if this theme contains the given token.</summary>
    public bool Contains(string token) => _tokens.ContainsKey(token);
}

/// <summary>
/// Ready-to-use global themes supplied by Rayo.
/// </summary>
public static class RayoThemes
{
    public static Theme Light { get; } = new("light", ColorPalettes.Light);
    public static Theme Dark { get; } = new("dark", ColorPalettes.Dark);

    private static Theme _current = Light;

    public static Theme Current => Rayo.Core.UIApplication.Current?.ActiveTheme ?? _current;

    /// <summary>
    /// Applies a theme through the active platform host. On mobile hosts there may be
    /// no UIApplication, so the current UITree is updated directly.
    /// </summary>
    public static void UseTheme(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (Rayo.Core.UIApplication.Current != null)
        {
            Rayo.Core.UIApplication.Current.UseTheme(theme);
            return;
        }

        SetCurrent(theme);
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

    internal static void SetCurrent(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _current = theme;
    }
}
