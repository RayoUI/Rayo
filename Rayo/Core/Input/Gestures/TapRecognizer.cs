namespace Rayo.Core.Input.Gestures;

using System.Numerics;

/// <summary>
/// Recognizes tap gestures (single tap, double tap).
/// Unified for both mouse clicks and touch taps.
/// </summary>
public class TapRecognizer : IGestureRecognizer
{
    private Vector2 _pressPosition;
    private DateTime _pressTime;
    private bool _isPressed;
    private int _tapCount;
    private DateTime _lastTapTime;
    
    // Configuration
    private readonly float _maxMovementThreshold;
    private readonly TimeSpan _maxPressDuration;
    private readonly TimeSpan _doubleTapWindow;

    public event Action<TapGestureEventArgs>? TapDetected;

    public TapRecognizer(
        float maxMovementThreshold = 10f,
        int maxPressDurationMs = 500,
        int doubleTapWindowMs = 300)
    {
        _maxMovementThreshold = maxMovementThreshold;
        _maxPressDuration = TimeSpan.FromMilliseconds(maxPressDurationMs);
        _doubleTapWindow = TimeSpan.FromMilliseconds(doubleTapWindowMs);
    }

    public bool ProcessPointerEvent(PointerEventArgs e)
    {
        switch (e)
        {
            case { IsInContact: true } when !_isPressed:
                // Pointer pressed
                _isPressed = true;
                _pressPosition = e.LocalPosition; // Use local position to be scrolling-aware
                _pressTime = DateTime.UtcNow;
                return false; // Don't consume press events

            case { IsInContact: true } when _isPressed:
                // Pointer moved while pressed - early cancellation if movement exceeds threshold
                // This handles dragging to scroll without waiting for release
                var currentDragDist = Vector2.Distance(e.LocalPosition, _pressPosition);
                if (currentDragDist > _maxMovementThreshold)
                {
                    _isPressed = false;
                }
                return false;

            case { IsInContact: false } when _isPressed:
                // Pointer released
                _isPressed = false;
                
                var duration = DateTime.UtcNow - _pressTime;
                var movement = Vector2.Distance(e.LocalPosition, _pressPosition);

                // Check if it qualifies as a tap
                if (duration <= _maxPressDuration && movement <= _maxMovementThreshold)
                {
                    // Check for double tap
                    var timeSinceLastTap = DateTime.UtcNow - _lastTapTime;
                    if (timeSinceLastTap <= _doubleTapWindow && _tapCount > 0)
                    {
                        _tapCount++;
                    }
                    else
                    {
                        _tapCount = 1;
                    }

                    _lastTapTime = DateTime.UtcNow;

                    TapDetected?.Invoke(new TapGestureEventArgs
                    {
                        Position = e.Position,
                        TapCount = _tapCount,
                        PointerType = e.PointerType,
                        Timestamp = DateTime.UtcNow
                    });

                    return true; // Consume the event
                }
                break;
        }

        return false;
    }

    public void Reset()
    {
        _isPressed = false;
        _tapCount = 0;
    }
}
