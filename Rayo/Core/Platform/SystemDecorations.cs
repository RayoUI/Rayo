namespace Rayo.Core.Platform;

/// <summary>
/// Specifies the system decorations (chrome) to use for the window.
/// </summary>
public enum SystemDecorations
{
    /// <summary>
    /// No decorations.
    /// </summary>
    None,

    /// <summary>
    /// Only the window border without title bar.
    /// </summary>
    BorderOnly,

    /// <summary>
    /// Full system decorations (title bar, borders, etc.).
    /// </summary>
    Full
}
