namespace Rayo.Reactivity;

/// <summary>
/// Executes a side effect whenever the signals it reads change.
/// </summary>
public sealed class Effect : IDisposable
{
    private readonly Action _effect;
    private readonly HashSet<ISignal> _dependencies = new();
    private readonly List<IDisposable> _subscriptions = new();
    private bool _isDisposed;

    public Effect(Action effect)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        Execute();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ClearSubscriptions();
        _dependencies.Clear();
    }

    private void Execute()
    {
        if (_isDisposed)
        {
            return;
        }

        ClearSubscriptions();
        _dependencies.Clear();

        SignalTrackingContext.Enter(_dependencies);
        try
        {
            _effect();
        }
        finally
        {
            SignalTrackingContext.Exit();
        }

        foreach (var dependency in _dependencies)
        {
            _subscriptions.Add(dependency.Subscribe(Execute));
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
}
