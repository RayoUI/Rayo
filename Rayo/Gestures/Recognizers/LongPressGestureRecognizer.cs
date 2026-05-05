using System.Numerics;
using Rayo.Gestures.Core;
using Rayo.Gestures.Events;

namespace Rayo.Gestures.Recognizers;

/// <summary>
/// Recognizes long-press gestures.
/// A long-press is defined as pressing and holding for a specified duration with minimal movement.
/// </summary>
public class LongPressGestureRecognizer : GestureRecognizer
{
    /// <summary>
    /// Event fired when a long-press is recognized.
    /// </summary>
    public event Action<Vector2>? OnLongPress;

    public override int Priority => 20; // Highest priority to prevent other gestures

    private bool _longPressTriggered = false;

    protected override void HandlePointerDown(Vector2 position, int touchId)
    {
        if (_touchTracker.TouchCount == 1)
        {
            _longPressTriggered = false;
            // Stay in Possible until duration met
        }
    }

    protected override void HandlePointerMove(Vector2 position, int touchId)
    {
        var touch = _touchTracker.PrimaryTouch;
        if (touch == null) return;

        // Fail if movement exceeds threshold
        if (touch.TotalDistance > GestureConfig.LongPressMaxDistance)
        {
            TransitionToState(GestureState.Failed);
        }
    }

    protected override void HandlePointerUp(Vector2 position, int touchId)
    {
        var touch = _touchTracker.GetTouch(touchId);
        if (touch == null)
        {
            TransitionToState(GestureState.Failed);
            return;
        }

        if (!_longPressTriggered &&
            touch.Duration.TotalMilliseconds >= GestureConfig.LongPressDurationMs &&
            touch.TotalDistance <= GestureConfig.LongPressMaxDistance)
        {
            _longPressTriggered = true;
            TransitionToState(GestureState.Ended);
            OnLongPress?.Invoke(touch.Position);
            return;
        }

        if (_longPressTriggered)
        {
            TransitionToState(GestureState.Ended);
        }
        else
        {
            TransitionToState(GestureState.Failed);
        }
    }

    public override void ProcessFrame()
    {
        if (_state != GestureState.Possible) return;

        var touch = _touchTracker.PrimaryTouch;
        if (touch == null) return;

        // Check if duration threshold is met
        if (touch.Duration.TotalMilliseconds >= GestureConfig.LongPressDurationMs &&
            touch.TotalDistance <= GestureConfig.LongPressMaxDistance)
        {
            _longPressTriggered = true;
            // Claim the win
            TransitionToState(GestureState.Began);
            OnLongPress?.Invoke(touch.Position);
        }
    }

    public override void Reset()
    {
        base.Reset();
        _longPressTriggered = false;
    }
}
