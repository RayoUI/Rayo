namespace Rayo.Core.Platform;

/// <summary>
/// System safe-area insets in logical pixels (already DPI-scaled).
/// Use these to keep content clear of notches, status bars, and home indicators.
/// </summary>
public readonly struct SafeAreaInsets : IEquatable<SafeAreaInsets>
{
    public static SafeAreaInsets Empty { get; } = new(0, 0, 0, 0);

    public SafeAreaInsets(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    /// <summary>Inset from the top edge (status bar / notch / cutout).</summary>
    public float Top { get; }

    /// <summary>Inset from the right edge.</summary>
    public float Right { get; }

    /// <summary>Inset from the bottom edge (home indicator / nav bar).</summary>
    public float Bottom { get; }

    /// <summary>Inset from the left edge.</summary>
    public float Left { get; }

    public bool Equals(SafeAreaInsets other) =>
        Top.Equals(other.Top) &&
        Right.Equals(other.Right) &&
        Bottom.Equals(other.Bottom) &&
        Left.Equals(other.Left);

    public override bool Equals(object? obj) => obj is SafeAreaInsets other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Top, Right, Bottom, Left);

    public static bool operator ==(SafeAreaInsets left, SafeAreaInsets right) => left.Equals(right);

    public static bool operator !=(SafeAreaInsets left, SafeAreaInsets right) => !left.Equals(right);

    public override string ToString() =>
        $"SafeArea(Top={Top}, Right={Right}, Bottom={Bottom}, Left={Left})";
}
