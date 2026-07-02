namespace Rayo.Styling;

using System.Collections.ObjectModel;

public sealed record ThemeKey<T>
{
    public string Name { get; }

    public ThemeKey(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public override string ToString() => Name;
}

public sealed class ThemeTokenSet
{
    private readonly IReadOnlyDictionary<string, TokenEntry> _entries;

    public static ThemeTokenSet Empty { get; } = new(
        new ReadOnlyDictionary<string, TokenEntry>(new Dictionary<string, TokenEntry>()));

    private ThemeTokenSet(IReadOnlyDictionary<string, TokenEntry> entries)
    {
        _entries = entries;
    }

    public ThemeTokenSet Set<T>(ThemeKey<T> key, T value) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);
        var entries = CopyEntries();
        entries[key.Name] = new TokenEntry(typeof(T), _ => value);
        return Create(entries);
    }

    public ThemeTokenSet Set<T>(
        ThemeKey<T> key,
        Func<ThemeTokenResolver, T> factory) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        var entries = CopyEntries();
        entries[key.Name] = new TokenEntry(typeof(T), resolver => factory(resolver));
        return Create(entries);
    }

    public bool Contains<T>(ThemeKey<T> key) => _entries.ContainsKey(key.Name);

    public T Get<T>(ThemeKey<T> key) =>
        new ThemeTokenResolver(this).Get(key);

    public bool TryGet<T>(ThemeKey<T> key, out T? value)
    {
        try
        {
            value = Get(key);
            return true;
        }
        catch (KeyNotFoundException)
        {
            value = default;
            return false;
        }
        catch (InvalidCastException)
        {
            value = default;
            return false;
        }
    }

    public IReadOnlyList<ThemeTokenValue> Snapshot()
    {
        var resolver = new ThemeTokenResolver(this);
        return _entries
            .Select(pair => new ThemeTokenValue(
                pair.Key,
                pair.Value.ValueType,
                pair.Value.Factory(resolver)))
            .ToArray();
    }

    internal object Resolve(string name, Type requestedType, ThemeTokenResolver resolver)
    {
        if (!_entries.TryGetValue(name, out var entry))
            throw new KeyNotFoundException($"ThemeData token '{name}' was not found.");
        if (entry.ValueType != requestedType)
        {
            throw new InvalidCastException(
                $"ThemeData token '{name}' has type {entry.ValueType.Name}, expected {requestedType.Name}.");
        }
        return entry.Factory(resolver);
    }

    private Dictionary<string, TokenEntry> CopyEntries() => new(_entries);

    private static ThemeTokenSet Create(Dictionary<string, TokenEntry> entries) =>
        new(new ReadOnlyDictionary<string, TokenEntry>(entries));

    private sealed record TokenEntry(Type ValueType, Func<ThemeTokenResolver, object> Factory);
}

public readonly record struct ThemeTokenValue(string Name, Type ValueType, object Value);

public sealed class ThemeTokenResolver
{
    private readonly ThemeTokenSet _tokens;
    private readonly Stack<string> _path = new();

    internal ThemeTokenResolver(ThemeTokenSet tokens)
    {
        _tokens = tokens;
    }

    public T Get<T>(ThemeKey<T> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_path.Contains(key.Name))
        {
            var path = string.Join(" -> ", _path.Reverse().Append(key.Name));
            throw new InvalidOperationException($"Circular theme token reference detected: {path}.");
        }

        _path.Push(key.Name);
        try
        {
            return (T)_tokens.Resolve(key.Name, typeof(T), this);
        }
        finally
        {
            _path.Pop();
        }
    }
}
