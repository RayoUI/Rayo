using System.Numerics;
using Rayo.Gestures.Core;
using Rayo.Gestures.Events;

namespace Rayo.Gestures.Recognizers;

/// <summary>
/// Recognizes pan (continuous drag) gestures.
/// Emits start, update, and end events with delta and velocity information.
/// </summary>
public class PanGestureRecognizer : GestureRecognizer
{
    /// <summary>
    /// Event fired when pan starts.
    /// </summary>
    public event Action<PanEventArgs>? OnPanStart;

    /// <summary>
    /// Event fired when pan updates (continuous).
    /// </summary>
    public event Action<PanEventArgs>? OnPanUpdate;

    /// <summary>
    /// Event fired when pan ends.
    /// </summary>
    public event Action<PanEventArgs>? OnPanEnd;

    public override int Priority => 5; // Lower priority, can be canceled by swipe

    private Vector2 _startPosition;
    private Vector2 _previousPosition;

    protected override void HandlePointerDown(Vector2 position, int touchId)
    {
        if (_touchTracker.TouchCount == 1)
        {
            _startPosition = position;
            _previousPosition = position;
            // Don't transition to Began yet, wait for movement threshold
        }
    }

    protected override void HandlePointerMove(Vector2 position, int touchId)
    {
        var touch = _touchTracker.PrimaryTouch;
        if (touch == null) return;

        // Activate pan after minimum distance threshold
        if (_state == GestureState.Possible && touch.TotalDistance >= GestureConfig.PanMinDistance)
        {
            TransitionToState(GestureState.Began);
            _startPosition = touch.StartPosition;
            _previousPosition = touch.StartPosition;

            var args = CreatePanArgs(touch);
            OnPanStart?.Invoke(args);
        }
        else if (_state == GestureState.Began || _state == GestureState.Changed)
        {
            TransitionToState(GestureState.Changed);

            var args = CreatePanArgs(touch);
            OnPanUpdate?.Invoke(args);

            _previousPosition = touch.Position;
        }
    }

    protected override void HandlePointerUp(Vector2 position, int touchId)
    {
        var touch = _touchTracker.GetTouch(touchId);

        if (_state == GestureState.Began || _state == GestureState.Changed)
        {
            if (touch != null)
            {
                var args = CreatePanArgs(touch);
                OnPanEnd?.Invoke(args);
            }

            TransitionToState(GestureState.Ended);
        }
        else
        {
            TransitionToState(GestureState.Failed);
        }
    }

    private PanEventArgs CreatePanArgs(TouchPoint touch)
    {
        var delta = touch.Position - _previousPosition;
        var totalDelta = touch.Position - _startPosition;

        return new PanEventArgs(
            position: touch.Position,
            delta: delta,
            totalDelta: totalDelta,
            velocity: touch.Velocity
        );
    }

    public override void Reset()
    {
        base.Reset();
        _startPosition = Vector2.Zero;
        _previousPosition = Vector2.Zero;
    }
}
