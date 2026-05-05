using System.Numerics;

namespace Rayo.Gestures.Events;

/// <summary>
/// Event arguments for tap and double-tap gestures.
/// </summary>
public class TapEventArgs : GestureEventArgs
{
    /// <summary>
    /// Number of taps (1 for single tap, 2 for double tap).
    /// </summary>
    public int TapCount { get; set; }

    public TapEventArgs(Vector2 position, int tapCount = 1) : base(position)
    {
        TapCount = tapCount;
    }
}
