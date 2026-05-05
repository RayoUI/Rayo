namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Describes the capabilities of the platform.
/// </summary>
public interface IPlatformCapabilities
{
    /// <summary>
    /// Gets the name of the platform (Windows, Linux, macOS, Android, etc.).
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Indicates if the platform supports windowing (Desktop platforms).
    /// </summary>
    bool SupportsWindowing { get; }

    /// <summary>
    /// Indicates if the platform supports hot reload.
    /// </summary>
    bool SupportsHotReload { get; }

    /// <summary>
    /// Indicates if the platform is mobile (Android, iOS).
    /// </summary>
    bool IsMobile { get; }

    /// <summary>
    /// Gets the DPI scale factor (1.0 for standard, higher for high-DPI displays).
    /// </summary>
    float DpiScale { get; }
}
