using System.Numerics;

namespace Rayo.Gestures.Events;

/// <summary>
/// Direction of a swipe gesture.
/// </summary>
public enum SwipeDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Event arguments for swipe gestures.
/// </summary>
public class SwipeEventArgs : GestureEventArgs
{
    /// <summary>
    /// Direction of the swipe.
    /// </summary>
    public SwipeDirection Direction { get; set; }

    /// <summary>
    /// Velocity of the swipe in pixels per second.
    /// </summary>
    public float Velocity { get; set; }

    /// <summary>
    /// Total distance of the swipe.
    /// </summary>
    public float Distance { get; set; }

    /// <summary>
    /// Velocity vector of the swipe.
    /// </summary>
    public Vector2 VelocityVector { get; set; }

    public SwipeEventArgs(Vector2 position, SwipeDirection direction, float velocity, float distance, Vector2 velocityVector)
        : base(position)
    {
        Direction = direction;
        Velocity = velocity;
        Distance = distance;
        VelocityVector = velocityVector;
    }
}
