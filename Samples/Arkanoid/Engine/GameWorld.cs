namespace Arkanoid.Engine;

using Rayo.Rendering;

/// <summary>
/// Manages the complete list of active <see cref="GameObject"/>s.
/// Each frame: calls <see cref="Update"/> then <see cref="Render"/>.
/// Dead objects (IsActive = false) are swept at the end of each update.
/// </summary>
public class GameWorld
{
    private readonly List<GameObject> _objects = new();
    private readonly List<GameObject> _pending  = new();  // added mid-frame

    public float WorldWidth  { get; set; }
    public float WorldHeight { get; set; }

    // ── Object management ─────────────────────────────────────────────────────

    /// <summary>Adds a game object. Safe to call during Update.</summary>
    public void Add(GameObject obj)
    {
        _pending.Add(obj);
    }

    /// <summary>Removes all objects satisfying the predicate immediately.</summary>
    public void RemoveWhere(Predicate<GameObject> predicate)
    {
        _objects.RemoveAll(predicate);
        _pending.RemoveAll(predicate);
    }

    public IReadOnlyList<GameObject> Objects => _objects;

    // ── Frame loop ────────────────────────────────────────────────────────────

    /// <summary>Advances every active object, then sweeps inactive ones.</summary>
    public void Update(float deltaTime)
    {
        // Flush pending additions
        _objects.AddRange(_pending);
        _pending.Clear();

        foreach (var obj in _objects)
        {
            if (obj.IsActive)
                obj.Update(deltaTime);
        }

        // Sweep dead objects
        _objects.RemoveAll(o => !o.IsActive);
    }

    /// <summary>
    /// Flushes pending additions and sweeps inactive objects without calling
    /// Update on any object. Use when the caller drives Update manually
    /// (e.g. sub-stepped physics) and only needs housekeeping.
    /// </summary>
    public void FlushPendingAndSweep()
    {
        _objects.AddRange(_pending);
        _pending.Clear();
        _objects.RemoveAll(o => !o.IsActive);
    }

    /// <summary>Renders all active objects. <paramref name="offsetX/Y"/> is the canvas origin.</summary>
    public void Render(IRenderer renderer, float offsetX, float offsetY)
    {
        foreach (var obj in _objects)
        {
            if (obj.IsActive)
                obj.Render(renderer, offsetX, offsetY);
        }
    }

    /// <summary>Removes all game objects.</summary>
    public void Clear()
    {
        _objects.Clear();
        _pending.Clear();
    }

    /// <summary>Returns all active objects of type <typeparamref name="T"/>.</summary>
    public IEnumerable<T> ObjectsOfType<T>() where T : GameObject =>
        _objects.OfType<T>().Where(o => o.IsActive);
}
