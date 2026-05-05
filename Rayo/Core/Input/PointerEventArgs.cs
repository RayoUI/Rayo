namespace Rayo.Core.Input;

using System.Numerics;

/// <summary>
/// Unified pointer event arguments supporting mouse, touch, and pen input.
/// Similar to Avalonia's PointerEventArgs and WinUI's PointerRoutedEventArgs.
/// </summary>
public class PointerEventArgs
{
    /// <summary>
    /// Unique identifier for this pointer (important for multi-touch).
    /// For mouse, this is typically 0. For touch, each finger gets a unique ID.
    /// </summary>
    public int PointerId { get; set; }

    /// <summary>
    /// Type of pointer device (Mouse, Touch, Pen).
    /// </summary>
    public PointerType PointerType { get; set; }

    /// <summary>
    /// Position of the pointer in window coordinates.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Position relative to the element that is handling the event.
    /// Set by the event routing system.
    /// </summary>
    public Vector2 LocalPosition { get; set; }

    /// <summary>
    /// Pressure of the pointer contact (0.0 to 1.0).
    /// - Touch: Usually 0.5 (not all devices support pressure)
    /// - Pen: Actual pressure from stylus
    /// - Mouse: Always 0.5
    /// </summary>
    public float Pressure { get; set; } = 0.5f;

    /// <summary>
    /// Indicates whether the pointer is currently in contact with the surface.
    /// - Touch/Pen: true when touching, false when hovering
    /// - Mouse: true when any button is pressed
    /// </summary>
    public bool IsInContact { get; set; }

    /// <summary>
    /// Which mouse button is involved (for mouse pointer type).
    /// 0 = Left, 1 = Middle, 2 = Right
    /// </summary>
    public int Button { get; set; }

    /// <summary>
    /// Timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this event has been handled and should not propagate further.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Delta movement since last pointer event (useful for drag operations).
    /// </summary>
    public Vector2 Delta { get; set; }

    /// <summary>
    /// Contact area size in pixels (for touch - typically ~40-50px for a finger).
    /// </summary>
    public float ContactRadius { get; set; } = 1f; // Default for mouse (single pixel)

    public PointerEventArgs(int pointerId, PointerType pointerType, Vector2 position)
    {
        PointerId = pointerId;
        PointerType = pointerType;
        Position = position;
        LocalPosition = position;
    }

    /// <summary>
    /// Creates a mouse pointer event.
    /// </summary>
    public static PointerEventArgs FromMouse(Vector2 position, int button, bool isPressed)
    {
        return new PointerEventArgs(0, PointerType.Mouse, position)
        {
            Button = button,
            IsInContact = isPressed,
            ContactRadius = 1f,
            Pressure = 0.5f
        };
    }

    /// <summary>
    /// Creates a touch pointer event.
    /// </summary>
    public static PointerEventArgs FromTouch(int touchId, Vector2 position, float pressure = 0.5f)
    {
        return new PointerEventArgs(touchId, PointerType.Touch, position)
        {
            IsInContact = true,
            Pressure = pressure,
            ContactRadius = 45f // Typical finger size
        };
    }

    /// <summary>
    /// Creates a pen/stylus pointer event.
    /// </summary>
    public static PointerEventArgs FromPen(int penId, Vector2 position, float pressure, bool isInContact)
    {
        return new PointerEventArgs(penId, PointerType.Pen, position)
        {
            IsInContact = isInContact,
            Pressure = pressure,
            ContactRadius = 2f // Pen tip is small
        };
    }
}
