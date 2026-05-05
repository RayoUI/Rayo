namespace Rayo.Reactivity;

/// <summary>
/// Base contract for all signal sources that can notify subscribers.
/// </summary>
public interface ISignal
{
    /// <summary>
    /// Subscribes to invalidation notifications without receiving the current value.
    /// </summary>
    IDisposable Subscribe(Action callback);
}

/// <summary>
/// Read-only signal contract.
/// </summary>
public interface IReadableSignal<out T> : ISignal
{
    /// <summary>
    /// Gets the current value while participating in dependency tracking.
    /// </summary>
    T Value { get; }

    /// <summary>
    /// Subscribes to typed value notifications.
    /// </summary>
    IDisposable Subscribe(Action<T> callback);
}

/// <summary>
/// Mutable signal contract.
/// </summary>
public interface IWritableSignal<T> : IReadableSignal<T>
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    new T Value { get; set; }
}

/// <summary>
/// Tracks dependencies while computed signals and effects are evaluated.
/// </summary>
public static class SignalTrackingContext
{
    [ThreadStatic]
    private static Stack<HashSet<ISignal>>? _dependencyStack;

    internal static void Enter(HashSet<ISignal> dependencies)
    {
        _dependencyStack ??= new();
        _dependencyStack.Push(dependencies);
    }

    internal static void Exit()
    {
        _dependencyStack?.Pop();
    }

    internal static void Track(ISignal signal)
    {
        if (_dependencyStack?.Count > 0)
        {
            _dependencyStack.Peek().Add(signal);
        }
    }
}
