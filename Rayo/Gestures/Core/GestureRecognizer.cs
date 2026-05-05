using System.Numerics;

namespace Rayo.Gestures.Core;

/// <summary>
/// Base class for all gesture recognizers.
/// Implements a state machine pattern similar to DragDropManager.
/// </summary>
public abstract class GestureRecognizer
{
    protected readonly TouchTracker _touchTracker = new();
    protected GestureState _state = GestureState.Possible;

    /// <summary>
    /// Current state of the gesture recognizer.
    /// </summary>
    public GestureState State => _state;

    /// <summary>
    /// Priority for conflict resolution. Higher values win.
    /// Default priorities: LongPress(20), DoubleTap(15), Tap(10), Pan(5)
    /// </summary>
    public virtual int Priority => 10;

    /// <summary>
    /// Indicates if this recognizer is currently tracking a gesture.
    /// </summary>
    public bool IsTracking => _state == GestureState.Began || _state == GestureState.Changed;

    /// <summary>
    /// Indicates if this recognizer is active and not in a terminal state.
    /// </summary>
    public bool IsActive => _state != GestureState.Failed && _state != GestureState.Canceled && _state != GestureState.Ended;

    /// <summary>
    /// Called when a pointer goes down.
    /// </summary>
    public virtual void OnPointerDown(Vector2 position, int touchId)
    {
        _touchTracker.StartTouch(position, touchId);
        HandlePointerDown(position, touchId);
    }

    /// <summary>
    /// Called when a pointer moves.
    /// </summary>
    public virtual void OnPointerMove(Vector2 position, int touchId)
    {
        _touchTracker.UpdateTouch(touchId, position);
        HandlePointerMove(position, touchId);
    }

    /// <summary>
    /// Called when a pointer goes up.
    /// </summary>
    public virtual void OnPointerUp(Vector2 position, int touchId)
    {
        _touchTracker.UpdateTouch(touchId, position);
        HandlePointerUp(position, touchId);
        _touchTracker.EndTouch(touchId);
    }

    /// <summary>
    /// Called every frame for time-based gesture recognition (e.g., long-press, double-tap timeout).
    /// </summary>
    public virtual void ProcessFrame()
    {
        // Override in derived classes if frame processing is needed
    }

    /// <summary>
    /// Resets the recognizer to initial state.
    /// </summary>
    public virtual void Reset()
    {
        _touchTracker.Clear();
        TransitionToState(GestureState.Possible);
    }

    /// <summary>
    /// Cancels the current gesture.
    /// </summary>
    public void Cancel()
    {
        if (IsActive)
        {
            TransitionToState(GestureState.Canceled);
        }
    }

    /// <summary>
    /// Transitions to a new state and calls appropriate lifecycle methods.
    /// </summary>
    protected void TransitionToState(GestureState newState)
    {
        if (_state == newState) return;

        var previousState = _state;
        _state = newState;

        // Call lifecycle hooks
        switch (newState)
        {
            case GestureState.Began:
                OnBegan();
                break;

            case GestureState.Changed:
                OnChanged();
                break;

            case GestureState.Ended:
                OnEnded();
                break;

            case GestureState.Canceled:
                OnCanceled();
                break;

            case GestureState.Failed:
                OnFailed();
                break;
        }
    }

    // ========== Abstract/Virtual Methods for Derived Classes ==========

    /// <summary>
    /// Handles pointer down event. Override in derived classes.
    /// </summary>
    protected abstract void HandlePointerDown(Vector2 position, int touchId);

    /// <summary>
    /// Handles pointer move event. Override in derived classes.
    /// </summary>
    protected abstract void HandlePointerMove(Vector2 position, int touchId);

    /// <summary>
    /// Handles pointer up event. Override in derived classes.
    /// </summary>
    protected abstract void HandlePointerUp(Vector2 position, int touchId);

    // ========== Lifecycle Hooks ==========

    /// <summary>
    /// Called when gesture recognition begins.
    /// </summary>
    protected virtual void OnBegan() { }

    /// <summary>
    /// Called when gesture continues with updates.
    /// </summary>
    protected virtual void OnChanged() { }

    /// <summary>
    /// Called when gesture completes successfully.
    /// </summary>
    protected virtual void OnEnded() { }

    /// <summary>
    /// Called when gesture is canceled.
    /// </summary>
    protected virtual void OnCanceled() { }

    /// <summary>
    /// Called when gesture fails to meet recognition criteria.
    /// </summary>
    protected virtual void OnFailed() { }
}
