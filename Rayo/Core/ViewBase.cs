namespace Rayo.Core;

/// <summary>
/// Base class for Views in MVVM pattern.
/// Provides typed ViewModel support with automatic injection and lifecycle management.
/// </summary>
/// <typeparam name="TViewModel">The ViewModel type for this View.</typeparam>
/// <remarks>
/// Usage:
/// <code>
/// public class PokemonView : ViewBase&lt;PokemonViewModel&gt;
/// {
///     public override UIElementBase Build()
///     {
///         return new VStack()
///             .SetChildren(
///                 new Label().BindText(ViewModel, vm => vm.Title),
///                 new Button()
///                     .SetText("Load")
///                     .OnClickHandler(() => ViewModel?.LoadData())
///             );
///     }
/// }
/// </code>
/// </remarks>
public abstract class ViewBase<TViewModel> : UserControl where TViewModel : class
{
    private TViewModel? _viewModel;
    private bool _viewModelInjected;

    /// <summary>
    /// The ViewModel for this View. Always non-null after <see cref="Build"/> starts.
    /// Can be injected via [Inject] attribute, resolved from DI automatically, or set manually.
    /// </summary>
    [Inject]
    public TViewModel ViewModel
    {
        get => _viewModel ?? throw new InvalidOperationException(
            $"ViewModel has not been initialized for {GetType().Name}. Ensure base.OnBeforeBuild() is called.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(_viewModel, value))
                return;

            var oldViewModel = _viewModel;
            _viewModel = value;

            OnViewModelChanged(oldViewModel, value);
        }
    }

    /// <summary>
    /// Called when the ViewModel changes.
    /// Override to react to ViewModel changes (e.g., setup subscriptions).
    /// </summary>
    /// <param name="oldViewModel">The previous ViewModel (may be null).</param>
    /// <param name="newViewModel">The new ViewModel (may be null).</param>
    protected virtual void OnViewModelChanged(TViewModel? oldViewModel, TViewModel? newViewModel)
    {
        // Default: trigger rebuild when ViewModel changes after initial build
        if (_viewModelInjected && newViewModel != null)
        {
            Rebuild();
        }
    }

    /// <summary>
    /// Called after ViewModel is set and before Build() is called.
    /// Override to setup ViewModel subscriptions or initial state.
    /// </summary>
    protected virtual void OnViewModelReady() { }

    protected override void OnBeforeBuild()
    {
        base.OnBeforeBuild();

        EnsureViewModel();

        _viewModelInjected = true;
        OnViewModelReady();
    }

    protected override void OnDispose()
    {
        base.OnDispose();

        // Dispose ViewModel if it implements IDisposable
        if (_viewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _viewModel = null;
    }

    /// <summary>
    /// Sets the ViewModel manually (fluent API).
    /// Use this when not using dependency injection.
    /// </summary>
    public ViewBase<TViewModel> SetViewModel(TViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        return this;
    }

    private void EnsureViewModel()
    {
        if (_viewModel != null)
        {
            return;
        }

        TViewModel? resolved = null;

        // Use DependencyInjector.ServiceProvider which checks both UIApplication.Current
        // and the global fallback (for platforms like Android that don't use UIApplication)
        var serviceProvider = DependencyInjector.ServiceProvider;
        if (serviceProvider != null)
        {
            resolved = DependencyInjector.CreateWithServices<TViewModel>(serviceProvider);
        }
        else if (typeof(TViewModel).GetConstructor(Type.EmptyTypes) != null)
        {
            resolved = Activator.CreateInstance<TViewModel>();
            if (resolved != null)
            {
                DependencyInjector.Inject(resolved);
            }
        }

        if (resolved == null)
        {
            throw new InvalidOperationException(
                $"Unable to create ViewModel of type '{typeof(TViewModel).Name}'. Register it in the service container or provide a parameterless constructor.");
        }

        // Use private setter logic to trigger change notifications without marking for rebuild during initial load
        var oldViewModel = _viewModel;
        _viewModel = resolved;
        OnViewModelChanged(oldViewModel, resolved);
    }
}

/// <summary>
/// Base class for Views with a ViewModel that requires constructor parameters.
/// The ViewModel is created using DependencyInjector.CreateWithServices.
/// </summary>
/// <typeparam name="TViewModel">The ViewModel type for this View.</typeparam>
public abstract class ViewBaseWithFactory<TViewModel> : UserControl where TViewModel : class
{
    private TViewModel? _viewModel;

    /// <summary>
    /// The ViewModel for this View.
    /// </summary>
    public TViewModel? ViewModel => _viewModel;

    /// <summary>
    /// Creates the ViewModel. Override to provide constructor parameters.
    /// Called automatically before Build() if ViewModel is null.
    /// </summary>
    protected abstract TViewModel CreateViewModel();

    /// <summary>
    /// Called when the ViewModel is created and ready.
    /// Override to setup subscriptions or initial state.
    /// </summary>
    protected virtual void OnViewModelReady() { }

    protected override void OnBeforeBuild()
    {
        base.OnBeforeBuild();

        if (_viewModel == null)
        {
            _viewModel = CreateViewModel();

            // Inject any remaining dependencies into the ViewModel
            if (_viewModel != null)
            {
                DependencyInjector.Inject(_viewModel);
                OnViewModelReady();
            }
        }
    }

    protected override void OnDispose()
    {
        base.OnDispose();

        if (_viewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _viewModel = null;
    }

    /// <summary>
    /// Sets the ViewModel manually (fluent API).
    /// </summary>
    protected void SetViewModel(TViewModel viewModel)
    {
        if (_viewModel is IDisposable oldDisposable)
        {
            oldDisposable.Dispose();
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            DependencyInjector.Inject(_viewModel);
        }
    }
}
