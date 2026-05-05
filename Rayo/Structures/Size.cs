using System.Diagnostics;

namespace Rayo;

[DebuggerDisplay("Size: Width = {Width}, Height = {Height}")]
public readonly struct Size(float width, float height)
{
    public static readonly Size Empty = new(0, 0);
    public float Width { get; } = width;
    public float Height { get; } = height;

    public Size(float size) : this(size, size) { }

    public static Size operator +(Size size, Thickness thickness)
    {
        return new Size(size.Width + thickness.Left + thickness.Right, size.Height + thickness.Top + thickness.Bottom);
    }

    public static Size operator +(Size a, Size b)
    {
        return new Size(a.Width + b.Height, a.Width + b.Height);
    }

    public static Size operator -(Size a, Size b)
    {
        return new Size(a.Width - b.Height, a.Width - b.Height);
    }

    public static implicit operator Size(float size)
    {
        return new Size(size, size);
    }
}


