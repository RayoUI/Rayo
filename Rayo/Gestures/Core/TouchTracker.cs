using System.Numerics;

namespace Rayo.Gestures.Core;

/// <summary>
/// Manages multiple simultaneous touch points.
/// </summary>
public class TouchTracker
{
    private readonly Dictionary<int, TouchPoint> _activeTouches = new();
    private int _nextTouchId = 0;

    /// <summary>
    /// Gets all active touches.
    /// </summary>
    public IReadOnlyDictionary<int, TouchPoint> ActiveTouches => _activeTouches;

    /// <summary>
    /// Gets the number of active touches.
    /// </summary>
    public int TouchCount => _activeTouches.Count;

    /// <summary>
    /// Gets the primary touch (first registered).
    /// </summary>
    public TouchPoint? PrimaryTouch
    {
        get
        {
            if (_activeTouches.Count == 0)
                return null;

            return _activeTouches.Values.OrderBy(t => t.Id).FirstOrDefault();
        }
    }

    /// <summary>
    /// Starts tracking a new touch at the given position.
    /// </summary>
    public TouchPoint StartTouch(Vector2 position, int? touchId = null)
    {
        int id = touchId ?? _nextTouchId++;
        var touch = new TouchPoint(id, position);
        _activeTouches[id] = touch;
        return touch;
    }

    /// <summary>
    /// Updates an existing touch with a new position.
    /// </summary>
    public bool UpdateTouch(int touchId, Vector2 position)
    {
        if (_activeTouches.TryGetValue(touchId, out var touch))
        {
            touch.Update(position);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Ends tracking of a touch.
    /// </summary>
    public bool EndTouch(int touchId)
    {
        return _activeTouches.Remove(touchId);
    }

    /// <summary>
    /// Gets a specific touch by ID.
    /// </summary>
    public TouchPoint? GetTouch(int touchId)
    {
        _activeTouches.TryGetValue(touchId, out var touch);
        return touch;
    }

    /// <summary>
    /// Clears all active touches.
    /// </summary>
    public void Clear()
    {
        _activeTouches.Clear();
    }
}
