namespace Rayo.Core.Input.Gestures;

using System.Numerics;

/// <summary>
/// Recognizes swipe gestures (fast directional movement).
/// Works with both mouse drags and touch swipes.
/// </summary>
public class SwipeRecognizer : IGestureRecognizer
{
    private Vector2 _startPosition;
    private DateTime _startTime;
    private bool _isTracking;
    private List<(Vector2 Position, DateTime Time)> _trackingPoints = new();
    
    // Configuration
    private readonly float _minSwipeDistance;
    private readonly float _minSwipeVelocity; // pixels per second
    private readonly TimeSpan _maxSwipeDuration;

    public event Action<SwipeGestureEventArgs>? SwipeDetected;

    public SwipeRecognizer(
        float minSwipeDistance = 50f,
        float minSwipeVelocity = 200f,
        int maxSwipeDurationMs = 500)
    {
        _minSwipeDistance = minSwipeDistance;
        _minSwipeVelocity = minSwipeVelocity;
        _maxSwipeDuration = TimeSpan.FromMilliseconds(maxSwipeDurationMs);
    }

    public bool ProcessPointerEvent(PointerEventArgs e)
    {
        switch (e)
        {
            case { IsInContact: true } when !_isTracking:
                // Start tracking
                _isTracking = true;
                _startPosition = e.Position;
                _startTime = DateTime.UtcNow;
                _trackingPoints.Clear();
                _trackingPoints.Add((e.Position, DateTime.UtcNow));
                return false;

            case { IsInContact: true } when _isTracking:
                // Continue tracking
                _trackingPoints.Add((e.Position, DateTime.UtcNow));
                return false;

            case { IsInContact: false } when _isTracking:
                // End tracking and check for swipe
                _isTracking = false;
                
                var endPosition = e.Position;
                var duration = DateTime.UtcNow - _startTime;
                var delta = endPosition - _startPosition;
                var distance = delta.Length();

                if (duration <= _maxSwipeDuration && distance >= _minSwipeDistance)
                {
                    var velocity = distance / (float)duration.TotalSeconds;

                    if (velocity >= _minSwipeVelocity)
                    {
                        var direction = GetSwipeDirection(delta);

                        SwipeDetected?.Invoke(new SwipeGestureEventArgs
                        {
                            Position = endPosition,
                            Direction = direction,
                            Velocity = velocity,
                            Distance = distance,
                            Timestamp = DateTime.UtcNow
                        });

                        return true; // Consume the event
                    }
                }
                break;
        }

        return false;
    }

    private SwipeDirection GetSwipeDirection(Vector2 delta)
    {
        var absX = Math.Abs(delta.X);
        var absY = Math.Abs(delta.Y);

        if (absX > absY)
        {
            return delta.X > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            return delta.Y > 0 ? SwipeDirection.Down : SwipeDirection.Up;
        }
    }

    public void Reset()
    {
        _isTracking = false;
        _trackingPoints.Clear();
    }
}
