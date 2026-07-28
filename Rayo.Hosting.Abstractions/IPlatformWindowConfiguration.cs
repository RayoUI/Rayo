using Rayo.Core.Platform;

namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Platform-agnostic window configuration.
/// Platform-specific implementations will map these to their native equivalents.
/// </summary>
public interface IPlatformWindowConfiguration
{
    string Title { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    bool CanResize { get; set; }
    bool VSync { get; set; }
    int Samples { get; set; }

    /// <summary>
    /// Whether the window should stay above other windows.
    /// </summary>
    bool Topmost { get; set; }

    /// <summary>
    /// Initial window state (normal, maximized, minimized, fullscreen).
    /// </summary>
    WindowState WindowState { get; set; }

    /// <summary>
    /// System chrome / decorations for the window.
    /// </summary>
    SystemDecorations SystemDecorations { get; set; }
}
