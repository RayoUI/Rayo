namespace Rayo.Reactivity;

/// <summary>
/// Derived read-only signal that automatically tracks dependencies.
/// </summary>
public class Computed<T> : IReadableSignal<T>, IDisposable
{
    private readonly Func<T> _compute;
    private readonly List<Action<T>> _typedSubscribers = new();
    private readonly List<Action> _subscribers = new();
    private readonly HashSet<ISignal> _dependencies = new();
    private readonly List<IDisposable> _subscriptions = new();
    private T? _cachedValue;
    private bool _isDirty = true;
    private bool _isDisposed;

    public Computed(Func<T> compute)
    {
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        Recompute();
    }

    public T Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            SignalTrackingContext.Track(this);

            if (_isDirty)
            {
                Recompute();
            }

            return _cachedValue!;
        }
    }

    public IDisposable Subscribe(Action<T> callback)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(callback);
        _typedSubscribers.Add(callback);
        return new Subscription(() => _typedSubscribers.Remove(callback));
    }

    public IDisposable Subscribe(Action callback)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentNullException.ThrowIfNull(callback);
        _subscribers.Add(callback);
        return new Subscription(() => _subscribers.Remove(callback));
    }

    public Computed<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new Computed<TResult>(() => selector(Value));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ClearSubscriptions();
        _typedSubscribers.Clear();
        _subscribers.Clear();
        _dependencies.Clear();
    }

    private void OnDependencyChanged()
    {
        if (_isDirty || _isDisposed)
        {
            return;
        }

        _isDirty = true;
        var value = Value;
        NotifySubscribers(value);
    }

    private void Recompute()
    {
        ClearSubscriptions();
        _dependencies.Clear();

        SignalTrackingContext.Enter(_dependencies);
        try
        {
            _cachedValue = _compute();
            _isDirty = false;
        }
        finally
        {
            SignalTrackingContext.Exit();
        }

        foreach (var dependency in _dependencies)
        {
            _subscriptions.Add(dependency.Subscribe(OnDependencyChanged));
        }
    }

    private void ClearSubscriptions()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
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
