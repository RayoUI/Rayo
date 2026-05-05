namespace Arkanoid.Engine;

using System.Numerics;

/// <summary>
/// Axis-aligned bounding box used for 2D game physics and rendering.
/// </summary>
public readonly struct Rect2D
{
    public readonly float X;
    public readonly float Y;
    public readonly float Width;
    public readonly float Height;

    public Rect2D(float x, float y, float width, float height)
    {
        X = x; Y = y; Width = width; Height = height;
    }

    public float Left   => X;
    public float Right  => X + Width;
    public float Top    => Y;
    public float Bottom => Y + Height;
    public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);

    /// <summary>Returns true when this rect overlaps <paramref name="other"/>.</summary>
    public bool Intersects(Rect2D other) =>
        Left   < other.Right  &&
        Right  > other.Left   &&
        Top    < other.Bottom &&
        Bottom > other.Top;

    /// <summary>Returns the overlap rectangle (depth) between the two rects.</summary>
    public Rect2D Overlap(Rect2D other)
    {
        float ox = MathF.Max(Left, other.Left);
        float oy = MathF.Max(Top, other.Top);
        float ow = MathF.Min(Right, other.Right) - ox;
        float oh = MathF.Min(Bottom, other.Bottom) - oy;
        return new Rect2D(ox, oy, ow, oh);
    }

    public Rect2D Translate(float dx, float dy) =>
        new(X + dx, Y + dy, Width, Height);

    public Rect2D Translate(Vector2 delta) =>
        Translate(delta.X, delta.Y);

    public override string ToString() =>
        $"Rect2D({X:F1},{Y:F1} {Width:F1}x{Height:F1})";
}
