using Rayo.Hosting.Abstractions;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Adapter that wraps Rayo's UIApplication to implement the platform-agnostic interface.
/// </summary>
internal class DesktopApplicationContext : IPlatformApplicationContext
{
    private readonly UIApplication _app;

    public DesktopApplicationContext(UIApplication app)
    {
        _app = app;
    }

    public void ConfigureServices(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        _app.UseServiceProvider(services);
    }

    public void SetUI<TView>() where TView : class
    {
        // TView must implement IUIBuilder for UIApplication.SetUI<T>()
        if (!typeof(IUIBuilder).IsAssignableFrom(typeof(TView)))
        {
            throw new InvalidOperationException(
                $"Type {typeof(TView).Name} must implement IUIBuilder. " +
                $"Ensure your view extends ViewBase<TViewModel>, UserControl, or implements IUIBuilder directly.");
        }

        // Use reflection to call SetUI<TView> on UIApplication
        var method = typeof(UIApplication).GetMethod(nameof(UIApplication.SetUI))
            ?.MakeGenericMethod(typeof(TView));
        
        method?.Invoke(_app, null);
    }

    public bool EnableDevTools { get; set; }

    public int DevToolsPort { get; set; } = 9999;

    public IServiceProvider? Services => DependencyInjector.ServiceProvider;

    /// <summary>
    /// Gets the underlying UIApplication for desktop-specific usage.
    /// </summary>
    public UIApplication NativeApplication => _app;
}
