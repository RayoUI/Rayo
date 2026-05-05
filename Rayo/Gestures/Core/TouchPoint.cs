using System.Numerics;

namespace Rayo.Gestures.Core;

/// <summary>
/// Represents a single touch or mouse point with position, velocity, and timing data.
/// </summary>
public class TouchPoint
{
    /// <summary>
    /// Unique identifier for this touch (0 for mouse, 0+ for touch).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Current position of the touch.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Initial position when touch started.
    /// </summary>
    public Vector2 StartPosition { get; set; }

    /// <summary>
    /// Time when touch started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Time of last update.
    /// </summary>
    public DateTime LastUpdateTime { get; set; }

    /// <summary>
    /// Movement delta since last update.
    /// </summary>
    public Vector2 Delta { get; set; }

    /// <summary>
    /// Current velocity in pixels per second.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Total distance traveled from start position.
    /// </summary>
    public float TotalDistance { get; private set; }

    public TouchPoint(int id, Vector2 position)
    {
        Id = id;
        Position = position;
        StartPosition = position;
        StartTime = DateTime.UtcNow;
        LastUpdateTime = DateTime.UtcNow;
        Delta = Vector2.Zero;
        Velocity = Vector2.Zero;
        TotalDistance = 0;
    }

    /// <summary>
    /// Updates the touch position and calculates velocity with smoothing.
    /// </summary>
    public void Update(Vector2 newPosition)
    {
        var now = DateTime.UtcNow;
        var dt = (float)(now - LastUpdateTime).TotalSeconds;

        if (dt > 0)
        {
            Delta = newPosition - Position;

            // Calculate instantaneous velocity
            var instantVelocity = Delta / dt;

            // Smooth velocity using weighted average (0.3 new, 0.7 old)
            Velocity = Velocity * 0.7f + instantVelocity * 0.3f;
        }

        Position = newPosition;
        LastUpdateTime = now;

        // Update total distance
        TotalDistance = Vector2.Distance(StartPosition, Position);
    }

    /// <summary>
    /// Gets the duration since touch started.
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - StartTime;
}
