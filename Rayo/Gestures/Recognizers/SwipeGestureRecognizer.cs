using System.Numerics;
using Rayo.Gestures.Core;
using Rayo.Gestures.Events;

namespace Rayo.Gestures.Recognizers;

/// <summary>
/// Recognizes directional swipe gestures.
/// A swipe is a quick, directional movement with sufficient velocity.
/// </summary>
public class SwipeGestureRecognizer : GestureRecognizer
{
    /// <summary>
    /// Event fired when a swipe is recognized.
    /// </summary>
    public event Action<SwipeEventArgs>? OnSwipe;

    public override int Priority => 12; // Higher than pan, lower than double-tap

    protected override void HandlePointerDown(Vector2 position, int touchId)
    {
        if (_touchTracker.TouchCount == 1)
        {
            // Stay in Possible until fast enough movement or Up
        }
    }

    protected override void HandlePointerMove(Vector2 position, int touchId)
    {
        var touch = _touchTracker.PrimaryTouch;
        if (touch == null) return;

        // If it's a very fast movement, we can claim it early
        if (_state == GestureState.Possible && touch.Velocity.Length() > GestureConfig.SwipeMinVelocity * 1.5f)
        {
            // TransitionToState(GestureState.Began);
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

        // Calculate swipe metrics
        var distance = touch.TotalDistance;
        var durationSeconds = MathF.Max((float)touch.Duration.TotalSeconds, 0.001f);
        var averageVelocity = distance / durationSeconds;
        var velocity = MathF.Max(touch.Velocity.Length(), averageVelocity);
        var direction = GetSwipeDirection(touch.StartPosition, touch.Position, out var angle);

        // Validate swipe criteria
        bool isValidSwipe =
            velocity >= GestureConfig.SwipeMinVelocity &&
            distance >= GestureConfig.SwipeMinDistance &&
            direction.HasValue &&
            IsAngleValid(angle, direction.Value);

        if (isValidSwipe && direction.HasValue)
        {
            TransitionToState(GestureState.Ended);

            var args = new SwipeEventArgs(
                position: touch.Position,
                direction: direction.Value,
                velocity: velocity,
                distance: distance,
                velocityVector: touch.Velocity
            );
            OnSwipe?.Invoke(args);
        }
        else
        {
            TransitionToState(GestureState.Failed);
        }
    }

    private SwipeDirection? GetSwipeDirection(Vector2 start, Vector2 end, out float angle)
    {
        var delta = end - start;
        angle = MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI);

        // Normalize angle to 0-360
        if (angle < 0) angle += 360f;

        // Determine cardinal direction
        // Right: 315-45, Up: 45-135, Left: 135-225, Down: 225-315
        if (angle >= 315f || angle < 45f)
            return SwipeDirection.Right;
        else if (angle >= 45f && angle < 135f)
            return SwipeDirection.Up;
        else if (angle >= 135f && angle < 225f)
            return SwipeDirection.Left;
        else if (angle >= 225f && angle < 315f)
            return SwipeDirection.Down;

        return null;
    }

    private bool IsAngleValid(float angle, SwipeDirection direction)
    {
        // Get ideal angle for the direction
        float idealAngle = direction switch
        {
            SwipeDirection.Right => 0f,
            SwipeDirection.Up => 90f,
            SwipeDirection.Left => 180f,
            SwipeDirection.Down => 270f,
            _ => 0f
        };

        // Calculate angular deviation
        var deviation = MathF.Abs(NormalizeAngleDifference(angle, idealAngle));

        // Allow deviation within configured threshold
        return deviation <= GestureConfig.SwipeMaxAngleDeviation;
    }

    private float NormalizeAngleDifference(float angle1, float angle2)
    {
        var diff = angle1 - angle2;

        // Normalize to -180 to 180 range
        while (diff > 180f) diff -= 360f;
        while (diff < -180f) diff += 360f;

        return diff;
    }
}
