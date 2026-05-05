namespace Rayo.Reactivity;

/// <summary>
/// Mutable list signal optimized for UI state and collection-driven computed values.
/// </summary>
public class SignalList<T> : IList<T>, IReadableSignal<IReadOnlyList<T>>
{
    private readonly List<T> _items = new();
    private readonly List<Action<SignalListChange<T>>> _changeSubscribers = new();
    private readonly List<Action<IReadOnlyList<T>>> _typedSubscribers = new();
    private readonly List<Action> _subscribers = new();

    public SignalList()
    {
    }

    public SignalList(IEnumerable<T> items)
    {
        _items.AddRange(items);
    }

    public IReadOnlyList<T> Value
    {
        get
        {
            SignalTrackingContext.Track(this);
            return _items;
        }
    }

    public int Count
    {
        get
        {
            SignalTrackingContext.Track(this);
            return _items.Count;
        }
    }

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get
        {
            SignalTrackingContext.Track(this);
            return _items[index];
        }
        set => SetItem(index, value);
    }

    public void Add(T item)
    {
        _items.Add(item);
        NotifyChange(new SignalListChange<T>(SignalListChangeType.Added, _items.Count - 1, item, default));
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        NotifyChange(new SignalListChange<T>(SignalListChangeType.Cleared, -1, default, default));
    }

    public bool Contains(T item) => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator()
    {
        SignalTrackingContext.Track(this);
        return _items.GetEnumerator();
    }

    public int IndexOf(T item) => _items.IndexOf(item);

    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        NotifyChange(new SignalListChange<T>(SignalListChangeType.Added, index, item, default));
    }

    public bool Remove(T item)
    {
        var index = _items.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        var oldValue = _items[index];
        _items.RemoveAt(index);
        NotifyChange(new SignalListChange<T>(SignalListChangeType.Removed, index, default, oldValue));
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public IDisposable Subscribe(Action<IReadOnlyList<T>> callback)
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

    public IDisposable Subscribe(Action<SignalListChange<T>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _changeSubscribers.Add(callback);
        return new Subscription(() => _changeSubscribers.Remove(callback));
    }

    public Computed<TResult> Map<TResult>(Func<IReadOnlyList<T>, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new Computed<TResult>(() => selector(Value));
    }

    private void SetItem(int index, T value)
    {
        var oldValue = _items[index];
        if (EqualityComparer<T>.Default.Equals(oldValue, value))
        {
            return;
        }

        _items[index] = value;
        NotifyChange(new SignalListChange<T>(SignalListChangeType.Modified, index, value, oldValue));
    }

    private void NotifyChange(SignalListChange<T> change)
    {
        foreach (var subscriber in _changeSubscribers.ToArray())
        {
            subscriber(change);
        }

        foreach (var subscriber in _typedSubscribers.ToArray())
        {
            subscriber(_items);
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

public enum SignalListChangeType
{
    Added,
    Removed,
    Modified,
    Cleared
}

public readonly record struct SignalListChange<T>(
    SignalListChangeType Type,
    int Index,
    T? NewValue,
    T? OldValue);
