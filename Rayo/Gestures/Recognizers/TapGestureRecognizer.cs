using System.Numerics;
using Rayo.Gestures.Core;
using Rayo.Gestures.Events;

namespace Rayo.Gestures.Recognizers;

/// <summary>
/// Recognizes single tap gestures.
/// A tap is defined as a quick press and release with minimal movement.
/// </summary>
public class TapGestureRecognizer : GestureRecognizer
{
    /// <summary>
    /// Event fired when a tap is recognized.
    /// </summary>
    public event Action<TapEventArgs>? OnTap;

    public override int Priority => 10;

    protected override void HandlePointerDown(Vector2 position, int touchId)
    {
        // Start tracking potential tap
        if (_touchTracker.TouchCount == 1)
        {
            // We stay in Possible state until Up or movement threshold
            // This allows other gestures (like Pan) to take over if they reach their threshold first
        }
    }

    protected override void HandlePointerMove(Vector2 position, int touchId)
    {
        var touch = _touchTracker.PrimaryTouch;
        if (touch == null) return;

        // Check if movement exceeds threshold
        if (touch.TotalDistance > GestureConfig.TapMaxDistance)
        {
            TransitionToState(GestureState.Failed);
        }
    }

    protected override void HandlePointerUp(Vector2 position, int touchId)
    {
        var touch = _touchTracker.GetTouch(touchId);
        if (touch == null) return;

        // Validate tap criteria
        bool isValidTap =
            touch.TotalDistance <= GestureConfig.TapMaxDistance &&
            touch.Duration.TotalMilliseconds <= GestureConfig.TapMaxDurationMs;

        if (isValidTap && (_state == GestureState.Began || _state == GestureState.Possible))
        {
            TransitionToState(GestureState.Ended);
        }
        else
        {
            TransitionToState(GestureState.Failed);
        }
    }

    protected override void OnEnded()
    {
        var touch = _touchTracker.PrimaryTouch;
        if (touch != null)
        {
            var args = new TapEventArgs(touch.Position, tapCount: 1);
            OnTap?.Invoke(args);
        }
    }
}
