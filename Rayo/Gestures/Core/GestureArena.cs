namespace Rayo.Gestures.Core;

/// <summary>
/// Manages conflict resolution between competing gesture recognizers.
/// When multiple gestures could be recognized from the same input,
/// the arena determines which gesture wins based on priority.
/// </summary>
public class GestureArena
{
    private readonly List<GestureRecognizer> _activeRecognizers = new();
    private GestureRecognizer? _winner = null;

    /// <summary>
    /// Adds a recognizer to the arena for conflict resolution.
    /// </summary>
    public void Add(GestureRecognizer recognizer)
    {
        if (!_activeRecognizers.Contains(recognizer))
        {
            _activeRecognizers.Add(recognizer);
        }
    }

    /// <summary>
    /// Removes a recognizer from the arena.
    /// </summary>
    public void Remove(GestureRecognizer recognizer)
    {
        _activeRecognizers.Remove(recognizer);
    }

    /// <summary>
    /// Resolves conflicts between active recognizers.
    /// Called after pointer events to determine which gesture should win.
    /// </summary>
    public void Resolve()
    {
        if (_activeRecognizers.Count == 0) return;

        // If we already have a winner, don't change it
        if (_winner != null && _winner.IsTracking)
            return;

        // Find tracking recognizers
        var tracking = _activeRecognizers
            .Where(r => r.IsTracking)
            .ToList();

        if (tracking.Count == 0)
        {
            // No one is tracking yet
            return;
        }

        if (tracking.Count == 1)
        {
            // Only one recognizer is tracking, it wins
            _winner = tracking[0];
            CancelLosers(_winner);
            return;
        }

        // Multiple recognizers are tracking, resolve by priority
        var winner = tracking
            .OrderByDescending(r => r.Priority)
            .First();

        _winner = winner;
        CancelLosers(winner);
    }

    /// <summary>
    /// Cancels all recognizers except the winner.
    /// </summary>
    private void CancelLosers(GestureRecognizer winner)
    {
        foreach (var recognizer in _activeRecognizers)
        {
            if (recognizer != winner && recognizer.IsActive)
            {
                recognizer.Cancel();
            }
        }
    }

    /// <summary>
    /// Clears the arena and resets all recognizers.
    /// Called when all pointers are up.
    /// </summary>
    public void Clear()
    {
        _winner = null;
        _activeRecognizers.Clear();
    }

    /// <summary>
    /// Gets the count of active recognizers.
    /// </summary>
    public int Count => _activeRecognizers.Count;

    /// <summary>
    /// Gets the current winner, if any.
    /// </summary>
    public GestureRecognizer? Winner => _winner;
}
