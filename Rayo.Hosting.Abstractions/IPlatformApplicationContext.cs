using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Provides access to the application context during configuration.
/// This is a platform-agnostic wrapper around the underlying UI framework.
/// </summary>
public interface IPlatformApplicationContext
{
    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    void ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    /// Sets the root UI element or view.
    /// </summary>
    void SetUI<TView>() where TView : class;

    /// <summary>
    /// Sets whether DevTools inspection is enabled.
    /// When enabled, the app will listen on the specified port for DevTool connections.
    /// </summary>
    bool EnableDevTools { get; set; }

    /// <summary>
    /// Gets or sets the port for DevTools connection (default: 9999).
    /// </summary>
    int DevToolsPort { get; set; }

    /// <summary>
    /// Gets the service provider after configuration.
    /// </summary>
    IServiceProvider? Services { get; }
}
