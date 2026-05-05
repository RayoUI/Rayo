namespace Arkanoid.Engine;

using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// Base class for every game object in the 2D engine.
/// Holds position, velocity, size, and visibility.
/// Subclasses implement <see cref="Update"/> and <see cref="Render"/>.
/// </summary>
public abstract class GameObject
{
    // ── Position & motion ─────────────────────────────────────────────────────
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Width      { get; set; }
    public float Height     { get; set; }

    // ── State ─────────────────────────────────────────────────────────────────
    public bool IsActive    { get; set; } = true;

    // ── Computed bounds ───────────────────────────────────────────────────────
    public Rect2D Bounds => new(Position.X, Position.Y, Width, Height);

    /// <summary>
    /// Advances the object's simulation by <paramref name="deltaTime"/> seconds.
    /// </summary>
    public abstract void Update(float deltaTime);

    /// <summary>
    /// Draws the object using the Rayo renderer.
    /// Coordinates are in canvas-local space (world space = canvas space here).
    /// </summary>
    public abstract void Render(IRenderer renderer, float offsetX, float offsetY);
}
