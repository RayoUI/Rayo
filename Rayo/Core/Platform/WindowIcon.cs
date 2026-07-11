namespace Rayo.Core.Platform;

/// <summary>
/// Raw RGBA icon pixels used for desktop window icons.
/// </summary>
public sealed class WindowIcon
{
    public WindowIcon(int width, int height, byte[] pixels)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Icon width must be greater than zero.");

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Icon height must be greater than zero.");

        ArgumentNullException.ThrowIfNull(pixels);

        var expectedLength = checked(width * height * 4);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Icon pixel data must contain exactly {expectedLength} RGBA bytes.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    /// <summary>
    /// The icon width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The icon height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Tightly packed RGBA pixel data.
    /// </summary>
    public byte[] Pixels { get; }
}
