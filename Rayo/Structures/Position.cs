using System.Diagnostics;

namespace Rayo;

[DebuggerDisplay("Position: X = {X}, Y = {Y}")]
public readonly struct Position(float x, float y)
{
    public static readonly Position Zero = new(0, 0);

    public float X { get; } = x;
    public float Y { get; } = y;

    public static Position operator +(Position a, Position b)
    {
        return new Position(a.X + b.X, a.Y + b.Y);
    }

    public static Position operator -(Position a, Position b)
    {
        return new Position(a.X - b.X, a.Y - b.Y);
    }

    public static implicit operator Position((float x, float y) tuple)
    {
        return new Position(tuple.x, tuple.y);
    }
}
