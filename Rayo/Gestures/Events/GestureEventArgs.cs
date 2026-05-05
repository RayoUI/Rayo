using System.Numerics;

namespace Rayo.Gestures.Events;

/// <summary>
/// Base class for all gesture event arguments.
/// </summary>
public abstract class GestureEventArgs : EventArgs
{
    /// <summary>
    /// Position where the gesture occurred.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Time when the gesture occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    protected GestureEventArgs(Vector2 position)
    {
        Position = position;
        Timestamp = DateTime.UtcNow;
    }
}
