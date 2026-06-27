namespace Rayo.Controls;

/// <summary>
/// Defines how picker-style components display their selection UI.
/// </summary>
public enum PickerDisplayMode
{
    /// <summary>
    /// Uses the control's default presentation.
    /// </summary>
    Auto,

    /// <summary>
    /// Displays the picker in a compact popup anchored near its trigger.
    /// </summary>
    Popup,

    /// <summary>
    /// Displays the picker inside a modal dialog overlay.
    /// </summary>
    Dialog,

    /// <summary>
    /// Legacy name for <see cref="Popup"/>.
    /// </summary>
    [System.Obsolete("Use PickerDisplayMode.Popup instead.")]
    Floating = Popup
}
