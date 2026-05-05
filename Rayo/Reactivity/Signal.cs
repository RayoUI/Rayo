namespace Rayo.Reactivity;

/// <summary>
/// Mutable state signal with dependency tracking.
/// </summary>
public class Signal<T> : IWritableSignal<T>
{
    private T _value;
    private readonly List<Action<T>> _typedSubscribers = new();
    private readonly List<Action> _subscribers = new();

    public Signal(T initialValue)
    {
        _value = initialValue;
    }

    public T Value
    {
        get
        {
            SignalTrackingContext.Track(this);
            return _value;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            NotifySubscribers(value);
        }
    }

    public IDisposable Subscribe(Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _typedSubscribers.Add(callback);
        return new Subscription(() => _typedSubscribers.Remove(callback));
    }

    public IDisposable Subscribe(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _subscribers.Add(callback);
        return new Subscription(() => _subscribers.Remove(callback));
    }

    public Computed<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new Computed<TResult>(() => selector(Value));
    }

    private void NotifySubscribers(T value)
    {
        foreach (var subscriber in _typedSubscribers.ToArray())
        {
            subscriber(value);
        }

        foreach (var subscriber in _subscribers.ToArray())
        {
            subscriber();
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _isDisposed;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _dispose();
        }
    }
}
