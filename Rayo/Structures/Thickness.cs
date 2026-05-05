namespace Rayo;

public readonly struct Thickness
{
    public float Left { get; }
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }

    public Thickness(float uniformSize)
    {
        Left = Top = Right = Bottom = uniformSize;
    }

    public Thickness(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }

    public Thickness(float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    /// <summary>Allows writing Margin(6f) or Padding(4f) anywhere a Thickness is expected.</summary>
    public static implicit operator Thickness(float uniformSize) => new Thickness(uniformSize);
}