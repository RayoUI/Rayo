using System.Numerics;

namespace Rayo.Gestures.Events;

/// <summary>
/// Event arguments for pan gestures.
/// </summary>
public class PanEventArgs : GestureEventArgs
{
    /// <summary>
    /// Movement delta since last update.
    /// </summary>
    public Vector2 Delta { get; set; }

    /// <summary>
    /// Total movement delta since pan started.
    /// </summary>
    public Vector2 TotalDelta { get; set; }

    /// <summary>
    /// Current velocity in pixels per second.
    /// </summary>
    public Vector2 Velocity { get; set; }

    public PanEventArgs(Vector2 position, Vector2 delta, Vector2 totalDelta, Vector2 velocity)
        : base(position)
    {
        Delta = delta;
        TotalDelta = totalDelta;
        Velocity = velocity;
    }
}
