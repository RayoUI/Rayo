namespace Rayo.Core.Input.Gestures;

using System.Numerics;

/// <summary>
/// Recognizes long press gestures (press and hold).
/// Works with both mouse and touch input.
/// </summary>
public class LongPressRecognizer : IGestureRecognizer
{
    private Vector2 _pressPosition;
    private DateTime _pressTime;
    private bool _isPressed;
    private bool _longPressTriggered;
    
    // Configuration
    private readonly float _maxMovementThreshold;
    private readonly TimeSpan _longPressDuration;

    public event Action<LongPressGestureEventArgs>? LongPressDetected;

    public LongPressRecognizer(
        float maxMovementThreshold = 10f,
        int longPressDurationMs = 500)
    {
        _maxMovementThreshold = maxMovementThreshold;
        _longPressDuration = TimeSpan.FromMilliseconds(longPressDurationMs);
    }

    public bool ProcessPointerEvent(PointerEventArgs e)
    {
        switch (e)
        {
            case { IsInContact: true } when !_isPressed:
                // Pointer pressed
                _isPressed = true;
                _pressPosition = e.Position;
                _pressTime = DateTime.UtcNow;
                _longPressTriggered = false;
                return false;

            case { IsInContact: false } when _isPressed:
                // Pointer released
                _isPressed = false;
                _longPressTriggered = false;
                return false;

            default:
                // Check if long press should trigger
                if (_isPressed && !_longPressTriggered)
                {
                    var duration = DateTime.UtcNow - _pressTime;
                    var movement = Vector2.Distance(e.Position, _pressPosition);

                    if (duration >= _longPressDuration && movement <= _maxMovementThreshold)
                    {
                        _longPressTriggered = true;

                        LongPressDetected?.Invoke(new LongPressGestureEventArgs
                        {
                            Position = e.Position,
                            Duration = duration,
                            Timestamp = DateTime.UtcNow
                        });

                        return true; // Consume the event
                    }
                }
                break;
        }

        return false;
    }

    public void Reset()
    {
        _isPressed = false;
        _longPressTriggered = false;
    }
}
