namespace Rayo.Core.Platform;

/// <summary>
/// Specifies the state of the window.
/// </summary>
public enum WindowState
{
    /// <summary>
    /// The window is in its normal state.
    /// </summary>
    Normal,

    /// <summary>
    /// The window is minimized.
    /// </summary>
    Minimized,

    /// <summary>
    /// The window is maximized.
    /// </summary>
    Maximized,

    /// <summary>
    /// The window takes up the full screen (no decorations).
    /// </summary>
    FullScreen
}
