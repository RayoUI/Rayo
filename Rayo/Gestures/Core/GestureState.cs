namespace Rayo.Gestures.Core;

/// <summary>
/// Represents the state of a gesture recognizer.
/// </summary>
public enum GestureState
{
    /// <summary>
    /// Initial state. Gesture could be recognized but hasn't started yet.
    /// </summary>
    Possible,

    /// <summary>
    /// Gesture has been recognized and started.
    /// </summary>
    Began,

    /// <summary>
    /// Gesture is ongoing with continuous updates.
    /// </summary>
    Changed,

    /// <summary>
    /// Gesture completed successfully.
    /// </summary>
    Ended,

    /// <summary>
    /// Gesture was canceled by another gesture or system event.
    /// </summary>
    Canceled,

    /// <summary>
    /// Gesture failed to meet recognition criteria.
    /// </summary>
    Failed
}
