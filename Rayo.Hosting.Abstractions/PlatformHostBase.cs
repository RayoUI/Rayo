using System;
using Microsoft.Extensions.DependencyInjection;

namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Base class for platform hosts providing common functionality.
/// </summary>
public abstract class PlatformHostBase : IPlatformHost
{
    /// <inheritdoc/>
    public abstract void Run(
        Action<IPlatformApplicationContext> configureApp,
        Action<IPlatformWindowConfiguration>? configureWindow = null);

    /// <inheritdoc/>
    public abstract IPlatformCapabilities Capabilities { get; }

    /// <summary>
    /// Called before the application starts running.
    /// Override to perform platform-specific initialization.
    /// </summary>
    protected virtual void OnBeforeRun() { }

    /// <summary>
    /// Called after the application finishes running.
    /// Override to perform platform-specific cleanup.
    /// </summary>
    protected virtual void OnAfterRun() { }

    /// <summary>
    /// Applies platform-specific default configuration.
    /// </summary>
    protected virtual void ApplyPlatformDefaults(IPlatformWindowConfiguration config) { }
}
