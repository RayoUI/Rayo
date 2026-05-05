namespace Rayo.Styling;

/// <summary>
/// A named dictionary of design-token values that can be referenced from style rules.
///
/// <para><b>Theme integration</b>: when a <see cref="Theme"/> is active (set via
/// <c>UIApplication.UseTheme</c>), <see cref="Get{T}"/> looks up the token in the active
/// theme first before falling back to this dictionary.</para>
///
/// <para><b>Computed tokens</b>: use the <c>Set(name, factory)</c> overload to define
/// tokens whose values are derived from other tokens at evaluation time.</para>
///
/// <code>
/// var tokens = new StyleTokens()
///     .Set("--accent",     new Color(80, 120, 220))
///     .Set("--accent-dim", t => t.Get&lt;Color&gt;("--accent").WithAlpha(0.6f))
///     .Set("--radius",     6f);
/// </code>
/// </summary>
public sealed class StyleTokens
{
    private readonly Dictionary<string, object>                       _tokens    = new();
    private readonly Dictionary<string, Func<StyleTokens, object>>   _factories = new();

    // ------------------------------------------------------------------
    // Set
    // ------------------------------------------------------------------

    /// <summary>Sets or replaces a token with a concrete value. Returns <c>this</c>.</summary>
    public StyleTokens Set<T>(string name, T value) where T : notnull
    {
        _tokens[name] = value;
        _factories.Remove(name);
        return this;
    }

    /// <summary>
    /// Sets or replaces a token with a computed factory evaluated on each <see cref="Get{T}"/>
    /// call. The factory receives <c>this</c> so it can read other tokens.
    /// </summary>
    public StyleTokens Set<T>(string name, Func<StyleTokens, T> factory) where T : notnull
    {
        _factories[name] = t => factory(t)!;
        _tokens.Remove(name);
        return this;
    }

    // ------------------------------------------------------------------
    // Get
    // ------------------------------------------------------------------

    /// <summary>
    /// Gets the value of <paramref name="name"/> as <typeparamref name="T"/>.
    /// Resolution order: active Theme → computed factory → concrete value.
    /// </summary>
    public T Get<T>(string name)
    {
        // 1. Active theme overrides
        var theme = Rayo.Core.UIApplication.Current?.ActiveTheme;
        if (theme != null && theme.TryGet<T>(name, out var themeValue))
            return themeValue!;

        // 2. Computed factory
        if (_factories.TryGetValue(name, out var factory))
        {
            var raw = factory(this);
            if (raw is T factoryTyped) return factoryTyped;
            throw new InvalidCastException(
                $"Computed token '{name}' returned {raw.GetType().Name}, expected {typeof(T).Name}.");
        }

        // 3. Concrete value
        if (!_tokens.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Style token '{name}' not found.");
        if (value is not T typed)
            throw new InvalidCastException(
                $"Token '{name}' has type {value.GetType().Name}, expected {typeof(T).Name}.");
        return typed;
    }

    /// <summary>Gets the token or returns <paramref name="fallback"/> on any failure.</summary>
    public T Get<T>(string name, T fallback)
    {
        try   { return Get<T>(name); }
        catch { return fallback;     }
    }

    /// <summary>Tries to get the value. Returns <c>false</c> if missing or wrong type.</summary>
    public bool TryGet<T>(string name, out T? value)
    {
        try   { value = Get<T>(name); return true;  }
        catch { value = default;      return false; }
    }

    /// <summary>Returns <c>true</c> if a token with the given name exists.</summary>
    public bool Contains(string name) => _tokens.ContainsKey(name) || _factories.ContainsKey(name);

    /// <summary>Removes a token. Returns <c>true</c> if it existed.</summary>
    public bool Remove(string name) => _tokens.Remove(name) | _factories.Remove(name);
}
