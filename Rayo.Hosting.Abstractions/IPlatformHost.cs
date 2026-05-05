using System;

namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Represents a platform-specific host for running Rayo applications.
/// Implementations provide platform-specific initialization and execution logic.
/// </summary>
public interface IPlatformHost
{
    /// <summary>
    /// Runs the application on the target platform.
    /// </summary>
    /// <param name="configureApp">Action to configure the application before running.</param>
    /// <param name="configureWindow">Optional action to customize window configuration.</param>
    void Run(
        Action<IPlatformApplicationContext> configureApp,
        Action<IPlatformWindowConfiguration>? configureWindow = null);

    /// <summary>
    /// Gets platform-specific capabilities.
    /// </summary>
    IPlatformCapabilities Capabilities { get; }
}
