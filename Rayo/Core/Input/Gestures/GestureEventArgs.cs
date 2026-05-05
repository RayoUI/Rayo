namespace Rayo.Core.Input.Gestures;

using System.Numerics;

/// <summary>
/// Base class for gesture event arguments.
/// </summary>
public abstract class GestureEventArgs
{
    public Vector2 Position { get; set; }
    public bool Handled { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for tap gesture (unified click/touch).
/// </summary>
public class TapGestureEventArgs : GestureEventArgs
{
    /// <summary>
    /// Number of taps (1 = single tap, 2 = double tap).
    /// </summary>
    public int TapCount { get; set; } = 1;

    /// <summary>
    /// Pointer type that triggered the tap.
    /// </summary>
    public PointerType PointerType { get; set; }
}

/// <summary>
/// Event arguments for long press gesture (press and hold).
/// </summary>
public class LongPressGestureEventArgs : GestureEventArgs
{
    /// <summary>
    /// Duration the pointer was held down.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Event arguments for swipe gesture.
/// </summary>
public class SwipeGestureEventArgs : GestureEventArgs
{
    /// <summary>
    /// Direction of the swipe.
    /// </summary>
    public SwipeDirection Direction { get; set; }

    /// <summary>
    /// Velocity of the swipe (pixels per second).
    /// </summary>
    public float Velocity { get; set; }

    /// <summary>
    /// Total distance of the swipe.
    /// </summary>
    public float Distance { get; set; }
}

/// <summary>
/// Direction of a swipe gesture.
/// </summary>
public enum SwipeDirection
{
    None,
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// Event arguments for pan/drag gesture.
/// </summary>
public class PanGestureEventArgs : GestureEventArgs
{
    /// <summary>
    /// Total translation from the start of the gesture.
    /// </summary>
    public Vector2 TotalTranslation { get; set; }

    /// <summary>
    /// Delta translation since last update.
    /// </summary>
    public Vector2 DeltaTranslation { get; set; }

    /// <summary>
    /// Current state of the pan gesture.
    /// </summary>
    public GestureState State { get; set; }
}

/// <summary>
/// Event arguments for pinch gesture (two-finger zoom).
/// </summary>
public class PinchGestureEventArgs : GestureEventArgs
{
    /// <summary>
    /// Scale factor (1.0 = no change, &gt;1.0 = zoom in, &lt;1.0 = zoom out).
    /// </summary>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// Delta scale since last update.
    /// </summary>
    public float DeltaScale { get; set; }

    /// <summary>
    /// Center point of the pinch gesture.
    /// </summary>
    public Vector2 Center { get; set; }

    /// <summary>
    /// Current state of the pinch gesture.
    /// </summary>
    public GestureState State { get; set; }
}

/// <summary>
/// State of a continuous gesture.
/// </summary>
public enum GestureState
{
    /// <summary>
    /// Gesture has started.
    /// </summary>
    Started,

    /// <summary>
    /// Gesture is ongoing (update).
    /// </summary>
    Changed,

    /// <summary>
    /// Gesture has completed.
    /// </summary>
    Ended,

    /// <summary>
    /// Gesture was canceled.
    /// </summary>
    Canceled
}
