namespace Rayo.Core;

/// <summary>
/// Specifies how an element is aligned horizontally within its allocated space.
/// Similar to WPF HorizontalAlignment and Flutter MainAxisAlignment.
/// </summary>
public enum HorizontalAlignment
{
    /// <summary>
    /// Aligned to the left (uses specified or minimum Width).
    /// </summary>
    Left,

    /// <summary>
    /// Centered horizontally (uses specified or minimum Width).
    /// </summary>
    Center,

    /// <summary>
    /// Aligned to the right (uses specified or minimum Width).
    /// </summary>
    Right,

    /// <summary>
    /// Expands to fill all available width (ignores Width).
    /// Similar to Flutter Expanded or CSS flex-grow: 1.
    /// </summary>
    Stretch
}

/// <summary>
/// Specifies how an element is aligned vertically within its allocated space.
/// Similar to WPF VerticalAlignment and Flutter CrossAxisAlignment.
/// </summary>
public enum VerticalAlignment
{
    /// <summary>
    /// Aligned to the top (uses specified or minimum Height).
    /// </summary>
    Top,

    /// <summary>
    /// Centered vertically (uses specified or minimum Height).
    /// </summary>
    Center,

    /// <summary>
    /// Aligned to the bottom (uses specified or minimum Height).
    /// </summary>
    Bottom,

    /// <summary>
    /// Expands to fill all available height (ignores Height).
    /// Similar to Flutter Expanded or CSS flex-grow: 1.
    /// </summary>
    Stretch
}