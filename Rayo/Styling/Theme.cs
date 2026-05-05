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

    /// <param name="name">Unique name for this theme.</param>
    public Theme(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
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
