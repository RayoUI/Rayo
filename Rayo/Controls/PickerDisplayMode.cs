namespace Rayo.Controls;

/// <summary>
/// Defines how picker-style components display their UI (floating Frame vs dialog).
/// </summary>
public enum PickerDisplayMode
{
    /// <summary>
    /// Automatically selects the best mode: floating on desktop, dialog on mobile.
    /// </summary>
    Auto,

    /// <summary>
    /// Displays the picker as a floating Frame anchored near the control.
    /// </summary>
    Floating,

    /// <summary>
    /// Displays the picker inside a modal dialog overlay.
    /// </summary>
    Dialog
}
