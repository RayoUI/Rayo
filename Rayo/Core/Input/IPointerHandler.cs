namespace Rayo.Core.Input;

/// <summary>
/// Modern interface for elements that handle pointer events (mouse, touch, pen).
/// Similar to Avalonia's IPointerHandler and MAUI's GestureRecognizer system.
/// 
/// This replaces the old IInputHandler/IClickable pattern with a unified approach
/// that works across all input types.
/// </summary>
public interface IPointerHandler
{
    /// <summary>
    /// Called when a pointer enters the bounds of the element.
    /// For touch, this only fires when actually touching (no hover state).
    /// </summary>
    void OnPointerEntered(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer exits the bounds of the element.
    /// </summary>
    void OnPointerExited(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer moves within the element.
    /// For touch, this fires during drag operations.
    /// </summary>
    void OnPointerMoved(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer is pressed down on the element.
    /// This is the unified "press" event for mouse down, touch start, pen down.
    /// </summary>
    void OnPointerPressed(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer is released.
    /// This is the unified "release" event for mouse up, touch end, pen up.
    /// </summary>
    void OnPointerReleased(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer is canceled (e.g., touch interrupted by system gesture).
    /// </summary>
    void OnPointerCanceled(PointerEventArgs e) { }
}
