namespace Rayo.Core.Platform;

/// <summary>
/// Specifies the position of the window when first shown.
/// </summary>
public enum WindowStartupLocation
{
    /// <summary>
    /// The window position is determined by the operating system.
    /// </summary>
    Manual,

    /// <summary>
    /// The window is centered on the screen.
    /// </summary>
    CenterScreen,

    /// <summary>
    /// The window is centered on its owner window.
    /// </summary>
    CenterOwner
}
