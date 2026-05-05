namespace Rayo.Reactivity;

/// <summary>
/// Provides component-scoped hook storage for signals and effects.
/// </summary>
public static class Hooks
{
    private static readonly Dictionary<string, List<object>> _storage = new();
    private static string? _currentContextKey;
    private static int _currentHookIndex;

    public static void Begin(object builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _currentContextKey = builder.GetType().FullName;
        _currentHookIndex = 0;

        if (_currentContextKey != null && !_storage.ContainsKey(_currentContextKey))
        {
            _storage[_currentContextKey] = new List<object>();
        }
    }

    public static Signal<T> UseSignal<T>(T initialValue)
    {
        EnsureContext();

        var hooks = _storage[_currentContextKey!];
        if (_currentHookIndex < hooks.Count && hooks[_currentHookIndex] is Signal<T> existingSignal)
        {
            _currentHookIndex++;
            return existingSignal;
        }

        var signal = new Signal<T>(initialValue);
        StoreHook(hooks, signal);
        return signal;
    }

    public static Computed<T> UseComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        EnsureContext();

        var hooks = _storage[_currentContextKey!];
        if (_currentHookIndex < hooks.Count && hooks[_currentHookIndex] is Computed<T> existingComputed)
        {
            _currentHookIndex++;
            return existingComputed;
        }

        var computed = new Computed<T>(compute);
        StoreHook(hooks, computed);
        return computed;
    }

    public static void UseEffect(Action effect, params object[] dependencies)
    {
        ArgumentNullException.ThrowIfNull(effect);
        EnsureContext();

        var hooks = _storage[_currentContextKey!];
        if (_currentHookIndex < hooks.Count && hooks[_currentHookIndex] is EffectHookState state)
        {
            if (DependenciesChanged(state.Dependencies, dependencies))
            {
                state.Effect.Dispose();
                state.Effect = new Effect(effect);
                state.Dependencies = dependencies;
            }

            _currentHookIndex++;
            return;
        }

        StoreHook(hooks, new EffectHookState
        {
            Effect = new Effect(effect),
            Dependencies = dependencies
        });
    }

    public static void Reset(object builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var key = builder.GetType().FullName;
        if (key == null || !_storage.Remove(key, out var hooks))
        {
            return;
        }

        DisposeHooks(hooks);
    }

    public static void ResetAll()
    {
        foreach (var entry in _storage.Values)
        {
            DisposeHooks(entry);
        }

        _storage.Clear();
    }

    private static void StoreHook(List<object> hooks, object hook)
    {
        if (_currentHookIndex < hooks.Count)
        {
            if (hooks[_currentHookIndex] is IDisposable disposable)
            {
                disposable.Dispose();
            }

            hooks[_currentHookIndex] = hook;
        }
        else
        {
            hooks.Add(hook);
        }

        _currentHookIndex++;
    }

    private static void EnsureContext()
    {
        if (_currentContextKey == null)
        {
            throw new InvalidOperationException("Hooks.Begin(this) must be called before using hooks.");
        }
    }

    private static bool DependenciesChanged(object[] previous, object[] current)
    {
        if (previous.Length != current.Length)
        {
            return true;
        }

        for (int i = 0; i < previous.Length; i++)
        {
            if (!Equals(previous[i], current[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static void DisposeHooks(IEnumerable<object> hooks)
    {
        foreach (var hook in hooks)
        {
            switch (hook)
            {
                case EffectHookState state:
                    state.Effect.Dispose();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    private sealed class EffectHookState
    {
        public required Effect Effect { get; set; }

        public object[] Dependencies { get; set; } = Array.Empty<object>();
    }
}
