namespace Rayo.Reactivity;

/// <summary>
/// Represents an object that owns disposable resources for a managed lifecycle.
/// </summary>
public interface IReactiveOwner
{
    /// <summary>
    /// Registers a disposable resource to be disposed with the owner lifecycle.
    /// </summary>
    void RegisterDisposable(IDisposable disposable);

    /// <summary>
    /// Registers a disposable in the owner lifecycle and returns it.
    /// </summary>
    T Use<T>(T disposable) where T : IDisposable;

    /// <summary>
    /// Creates a mutable signal owned by this lifecycle owner.
    /// </summary>
    Signal<T> UseSignal<T>(T initialValue);

    /// <summary>
    /// Creates an empty reactive collection owned by this lifecycle owner.
    /// </summary>
    SignalList<T> UseSignalList<T>();

    /// <summary>
    /// Creates a reactive collection seeded with the provided items and owned by this lifecycle owner.
    /// </summary>
    SignalList<T> UseSignalList<T>(IEnumerable<T> items);

    /// <summary>
    /// Creates a computed signal owned by this lifecycle owner.
    /// </summary>
    Computed<T> UseComputed<T>(Func<T> compute);

    /// <summary>
    /// Creates an effect owned by this lifecycle owner.
    /// </summary>
    Effect UseEffect(Action effect);

    /// <summary>
    /// Subscribes to a readable signal and owns the subscription in this lifecycle owner.
    /// </summary>
    IDisposable UseSubscription<T>(IReadableSignal<T> signal, Action<T> callback);

    /// <summary>
    /// Subscribes to a signal and owns the subscription in this lifecycle owner.
    /// </summary>
    IDisposable UseSubscription(ISignal signal, Action callback);
}
