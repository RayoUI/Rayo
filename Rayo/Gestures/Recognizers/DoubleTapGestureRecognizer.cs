using System.Numerics;
using Rayo.Gestures.Core;
using Rayo.Gestures.Events;

namespace Rayo.Gestures.Recognizers;

/// <summary>
/// Recognizes double-tap gestures.
/// Requires two taps in quick succession within a small distance.
/// </summary>
public class DoubleTapGestureRecognizer : GestureRecognizer
{
    /// <summary>
    /// Event fired when a double-tap is recognized.
    /// </summary>
    public event Action<TapEventArgs>? OnDoubleTap;

    public override int Priority => 15; // Higher than single tap

    private Vector2 _firstTapPosition;
    private DateTime _firstTapTime;
    private bool _waitingForSecondTap = false;

    protected override void HandlePointerDown(Vector2 position, int touchId)
    {
        if (_touchTracker.TouchCount == 1)
        {
            // Stay in Possible until first tap is completed
        }
    }

    protected override void HandlePointerMove(Vector2 position, int touchId)
    {
        var touch = _touchTracker.PrimaryTouch;
        if (touch == null) return;

        // Fail if movement exceeds threshold
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

        if (!isValidTap)
        {
            TransitionToState(GestureState.Failed);
            _waitingForSecondTap = false;
            return;
        }

        if (!_waitingForSecondTap)
        {
            // First tap
            _firstTapPosition = position;
            _firstTapTime = DateTime.UtcNow;
            _waitingForSecondTap = true;
            
            // Move to Began state while waiting for second tap to claim the arena
            TransitionToState(GestureState.Began);
        }
        else
        {
            // Second tap
            var timeSinceFirstTap = DateTime.UtcNow - _firstTapTime;
            var distanceFromFirstTap = Vector2.Distance(_firstTapPosition, position);

            bool isValidDoubleTap =
                timeSinceFirstTap.TotalMilliseconds <= GestureConfig.DoubleTapIntervalMs &&
                distanceFromFirstTap <= GestureConfig.DoubleTapMaxDistance;

            if (isValidDoubleTap)
            {
                TransitionToState(GestureState.Ended);
            }
            else
            {
                TransitionToState(GestureState.Failed);
            }

            _waitingForSecondTap = false;
        }
    }

    public override void ProcessFrame()
    {
        // Timeout if waiting too long for second tap
        if (_waitingForSecondTap)
        {
            var elapsed = DateTime.UtcNow - _firstTapTime;
            if (elapsed.TotalMilliseconds > GestureConfig.DoubleTapIntervalMs)
            {
                TransitionToState(GestureState.Failed);
                _waitingForSecondTap = false;
            }
        }
    }

    protected override void OnEnded()
    {
        var args = new TapEventArgs(_firstTapPosition, tapCount: 2);
        OnDoubleTap?.Invoke(args);
    }

    public override void Reset()
    {
        base.Reset();
        _waitingForSecondTap = false;
        _firstTapPosition = Vector2.Zero;
        _firstTapTime = DateTime.MinValue;
    }
}
