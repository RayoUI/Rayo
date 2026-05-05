namespace Rayo.Core.Input.Gestures;

/// <summary>
/// Base interface for gesture recognizers.
/// Similar to MAUI's GestureRecognizer and iOS UIGestureRecognizer.
/// 
/// Gesture recognizers process pointer events and detect higher-level gestures
/// like taps, long press, swipes, pinch, etc.
/// </summary>
public interface IGestureRecognizer
{
    /// <summary>
    /// Process a pointer event and update gesture state.
    /// Returns true if the event should be consumed (not passed to other recognizers).
    /// </summary>
    bool ProcessPointerEvent(PointerEventArgs e);

    /// <summary>
    /// Reset the gesture recognizer state.
    /// </summary>
    void Reset();
}

/// <summary>
/// Interface for elements that support tap gestures (unified click/touch).
/// </summary>
public interface ITappable
{
    /// <summary>
    /// Fired when the element is tapped (click or touch tap).
    /// </summary>
    event Action<TapGestureEventArgs>? Tapped;
}

/// <summary>
/// Interface for elements that support long press gestures.
/// </summary>
public interface ILongPressable
{
    /// <summary>
    /// Fired when the element is long pressed (press and hold).
    /// </summary>
    event Action<LongPressGestureEventArgs>? LongPressed;
}

/// <summary>
/// Interface for elements that support swipe gestures.
/// </summary>
public interface ISwipeable
{
    /// <summary>
    /// Fired when a swipe gesture is detected on the element.
    /// </summary>
    event Action<SwipeGestureEventArgs>? Swiped;
}

/// <summary>
/// Interface for elements that support pan/drag gestures.
/// </summary>
public interface IPannable
{
    /// <summary>
    /// Fired when a pan gesture is updated (started, changed, ended).
    /// </summary>
    event Action<PanGestureEventArgs>? Panned;
}

/// <summary>
/// Interface for elements that support pinch gestures (two-finger zoom).
/// </summary>
public interface IPinchable
{
    /// <summary>
    /// Fired when a pinch gesture is updated (started, changed, ended).
    /// </summary>
    event Action<PinchGestureEventArgs>? Pinched;
}
