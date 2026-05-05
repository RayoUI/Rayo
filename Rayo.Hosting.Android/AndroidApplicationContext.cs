using Rayo.Hosting.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Rayo.Hosting.Android;

/// <summary>
/// Android application context adapter.
/// Since Android uses GLSurfaceView, the context is simplified.
/// </summary>
public class AndroidApplicationContext : IPlatformApplicationContext
{
    private readonly ServiceCollection _services = new();
    private IServiceProvider? _serviceProvider;
    private Type? _viewType;

    public void ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        _serviceProvider = _services.BuildServiceProvider();
    }

    public void SetUI<TView>() where TView : class
    {
        _viewType = typeof(TView);
    }

    public bool EnableDevTools { get; set; }

    public int DevToolsPort { get; set; } = 9999;

    public IServiceProvider? Services => _serviceProvider;

    /// <summary>
    /// Gets the configured view type.
    /// </summary>
    public Type? ViewType => _viewType;

    /// <summary>
    /// Gets the service collection for Android-specific service registration.
    /// </summary>
    public IServiceCollection ServiceCollection => _services;
}
