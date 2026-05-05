namespace Rayo.Core.Input.Gestures;

using System.Numerics;

/// <summary>
/// Recognizes pan/drag gestures (continuous movement).
/// Works with both mouse drags and touch drags.
/// </summary>
public class PanRecognizer : IGestureRecognizer
{
    private Vector2 _startPosition;
    private Vector2 _lastPosition;
    private bool _isPanning;
    private GestureState _state;
    
    // Configuration
    private readonly float _activationThreshold;

    public event Action<PanGestureEventArgs>? PanDetected;

    public PanRecognizer(float activationThreshold = 5f)
    {
        _activationThreshold = activationThreshold;
    }

    public bool ProcessPointerEvent(PointerEventArgs e)
    {
        switch (e)
        {
            case { IsInContact: true } when !_isPanning:
                // Potential pan start
                _startPosition = e.Position;
                _lastPosition = e.Position;
                _isPanning = false; // Will activate after threshold
                _state = GestureState.Started;
                return false;

            case { IsInContact: true } when _isPanning || Vector2.Distance(e.Position, _startPosition) >= _activationThreshold:
                // Pan is active
                if (!_isPanning)
                {
                    _isPanning = true;
                    _state = GestureState.Started;
                }
                else
                {
                    _state = GestureState.Changed;
                }

                var totalTranslation = e.Position - _startPosition;
                var deltaTranslation = e.Position - _lastPosition;

                PanDetected?.Invoke(new PanGestureEventArgs
                {
                    Position = e.Position,
                    TotalTranslation = totalTranslation,
                    DeltaTranslation = deltaTranslation,
                    State = _state,
                    Timestamp = DateTime.UtcNow
                });

                _lastPosition = e.Position;
                return true; // Consume while panning

            case { IsInContact: false } when _isPanning:
                // Pan ended
                _isPanning = false;
                _state = GestureState.Ended;

                var finalTranslation = e.Position - _startPosition;

                PanDetected?.Invoke(new PanGestureEventArgs
                {
                    Position = e.Position,
                    TotalTranslation = finalTranslation,
                    DeltaTranslation = Vector2.Zero,
                    State = _state,
                    Timestamp = DateTime.UtcNow
                });

                return true;
        }

        return false;
    }

    public void Reset()
    {
        if (_isPanning)
        {
            _isPanning = false;
            _state = GestureState.Canceled;

            PanDetected?.Invoke(new PanGestureEventArgs
            {
                Position = _lastPosition,
                TotalTranslation = Vector2.Zero,
                DeltaTranslation = Vector2.Zero,
                State = _state,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
