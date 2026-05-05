using Rayo.Reactivity;

namespace Rayo.Core;

/// <summary>
/// Base class for ViewModels with dependency injection support.
/// Properties marked with [Inject] will be automatically populated from the service provider.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// /// public partial class MyViewModel : ViewModelBase
/// {
///     [Inject]
///     public IMyService? MyService { get; private set; }
///
///     public string Title { get; set; } = "Hello";
///
///     protected override void OnInitialized()
///     {
///         // Services are available here
///         Title = MyService?.GetTitle() ?? "Default";
///     }
/// }
/// </code>
/// </remarks>
public abstract class ViewModelBase : IDisposable
{
    private bool _isInitialized;
    private bool _isDisposed;
    private readonly List<IDisposable> _disposables = new();

    protected ViewModelBase()
    {
        // Inject dependencies immediately on construction
        DependencyInjector.Inject(this);

        // Call initialization
        if (!_isInitialized)
        {
            _isInitialized = true;
            OnInitialized();
        }
    }

    /// <summary>
    /// Called after dependencies are injected and the ViewModel is initialized.
    /// Override to perform initialization logic.
    /// </summary>
    protected virtual void OnInitialized() { }

    /// <summary>
    /// Called when the ViewModel is being disposed.
    /// Override to clean up resources.
    /// </summary>
    protected virtual void OnDispose() { }

    /// <summary>
    /// Registers a disposable to be disposed when the ViewModel is disposed.
    /// Useful for subscriptions and other resources.
    /// </summary>
    protected void RegisterDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    /// <summary>
    /// Subscribes to a signal and automatically disposes on ViewModel disposal.
    /// </summary>
    protected void Subscribe<T>(IReadableSignal<T> signal, Action<T> callback)
    {
        var subscription = signal.Subscribe(callback);
        RegisterDisposable(subscription);
    }

    /// <summary>
    /// Subscribes to a signal and automatically disposes on ViewModel disposal.
    /// </summary>
    protected void Subscribe(ISignal signal, Action callback)
    {
        var subscription = signal.Subscribe(callback);
        RegisterDisposable(subscription);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        OnDispose();

        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Generic ViewModelBase that provides access to a specific service type.
/// Useful when you have a primary service dependency.
/// </summary>
/// <typeparam name="TService">The primary service type.</typeparam>
public abstract class ViewModelBase<TService> : ViewModelBase where TService : class
{
    [Inject]
    protected TService? Service { get; private set; }
}
