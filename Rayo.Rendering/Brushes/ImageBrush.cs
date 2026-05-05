using System.Numerics;

namespace Rayo.Rendering.Brushes;

/// <summary>
/// Specifies how an image is stretched to fill the destination area.
/// </summary>
public enum Stretch
{
    /// <summary>
    /// The image is not stretched. Original size is preserved.
    /// </summary>
    None,

    /// <summary>
    /// The image is stretched to fill the destination. Aspect ratio is not preserved.
    /// </summary>
    Fill,

    /// <summary>
    /// The image is scaled to fit the destination while preserving its aspect ratio.
    /// </summary>
    Uniform,

    /// <summary>
    /// The image is scaled to fill the destination while preserving its aspect ratio.
    /// Some parts of the image may be clipped.
    /// </summary>
    UniformToFill
}

/// <summary>
/// Specifies how an image is tiled when it doesn't fill the entire area.
/// </summary>
public enum TileMode
{
    /// <summary>
    /// The image is not tiled. Only drawn once.
    /// </summary>
    None,

    /// <summary>
    /// The image is tiled horizontally and vertically.
    /// </summary>
    Tile,

    /// <summary>
    /// The image is tiled horizontally and vertically, with alternating tiles flipped.
    /// </summary>
    FlipXY,

    /// <summary>
    /// The image is tiled with horizontal tiles flipped.
    /// </summary>
    FlipX,

    /// <summary>
    /// The image is tiled with vertical tiles flipped.
    /// </summary>
    FlipY
}

/// <summary>
/// A brush that paints an area with an image.
/// </summary>
public class ImageBrush : Brush
{
    /// <summary>
    /// The source image texture.
    /// </summary>
    public ITexture? ImageSource { get; set; }

    /// <summary>
    /// The path to the image file (alternative to ImageSource).
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// How the image is stretched to fill the destination.
    /// </summary>
    public Stretch Stretch { get; set; } = Stretch.Fill;

    /// <summary>
    /// How the image is tiled if it doesn't fill the entire area.
    /// </summary>
    public TileMode TileMode { get; set; } = TileMode.None;

    /// <summary>
    /// Horizontal alignment of the image within the destination area.
    /// 0 = left, 0.5 = center, 1 = right.
    /// </summary>
    public float AlignmentX { get; set; } = 0.5f;

    /// <summary>
    /// Vertical alignment of the image within the destination area.
    /// 0 = top, 0.5 = center, 1 = bottom.
    /// </summary>
    public float AlignmentY { get; set; } = 0.5f;

    /// <summary>
    /// Optional tint color applied to the image.
    /// </summary>
    public Color? Tint { get; set; }

    /// <summary>
    /// The viewport rectangle for the image (normalized 0-1 coordinates).
    /// Defines which portion of the destination area the image maps to.
    /// </summary>
    public (float X, float Y, float Width, float Height)? Viewport { get; set; }

    public ImageBrush() { }

    public ImageBrush(ITexture texture)
    {
        ImageSource = texture;
    }

    public ImageBrush(string imagePath)
    {
        ImagePath = imagePath;
    }

    public override Color PrimaryColor
    {
        get
        {
            // Return tint color if specified, otherwise a neutral gray
            return Tint ?? Color.Gray;
        }
    }

    public override Color GetColorAt(float normalizedX, float normalizedY)
    {
        // Image brushes can't easily sample colors without access to pixel data
        // Return the tint color or a neutral value
        var baseColor = Tint ?? Color.White;
        return baseColor.WithAlpha(baseColor.A * Opacity);
    }

    public override Brush WithOpacity(float opacity)
    {
        return new ImageBrush
        {
            ImageSource = ImageSource,
            ImagePath = ImagePath,
            Stretch = Stretch,
            TileMode = TileMode,
            AlignmentX = AlignmentX,
            AlignmentY = AlignmentY,
            Tint = Tint,
            Viewport = Viewport,
            Opacity = opacity
        };
    }

    /// <summary>
    /// Calculates the destination rectangle for the image based on stretch mode.
    /// </summary>
    public (float X, float Y, float Width, float Height) CalculateDestinationRect(
        float destX, float destY, float destWidth, float destHeight,
        int imageWidth, int imageHeight)
    {
        float x = destX, y = destY, w = destWidth, h = destHeight;

        switch (Stretch)
        {
            case Stretch.None:
                w = imageWidth;
                h = imageHeight;
                x = destX + (destWidth - w) * AlignmentX;
                y = destY + (destHeight - h) * AlignmentY;
                break;

            case Stretch.Fill:
                // Use full destination area (default)
                break;

            case Stretch.Uniform:
                {
                    float scaleX = destWidth / imageWidth;
                    float scaleY = destHeight / imageHeight;
                    float scale = Math.Min(scaleX, scaleY);
                    w = imageWidth * scale;
                    h = imageHeight * scale;
                    x = destX + (destWidth - w) * AlignmentX;
                    y = destY + (destHeight - h) * AlignmentY;
                }
                break;

            case Stretch.UniformToFill:
                {
                    float scaleX = destWidth / imageWidth;
                    float scaleY = destHeight / imageHeight;
                    float scale = Math.Max(scaleX, scaleY);
                    w = imageWidth * scale;
                    h = imageHeight * scale;
                    x = destX + (destWidth - w) * AlignmentX;
                    y = destY + (destHeight - h) * AlignmentY;
                }
                break;
        }

        return (x, y, w, h);
    }

    public override bool Equals(Brush? other)
    {
        if (other is not ImageBrush b) return false;
        return ImageSource == b.ImageSource &&
               ImagePath == b.ImagePath &&
               Stretch == b.Stretch &&
               TileMode == b.TileMode &&
               MathF.Abs(AlignmentX - b.AlignmentX) < 0.0001f &&
               MathF.Abs(AlignmentY - b.AlignmentY) < 0.0001f &&
               Tint == b.Tint &&
               Viewport == b.Viewport &&
               MathF.Abs(Opacity - b.Opacity) < 0.0001f;
    }

    public override int GetHashCode()
        => HashCode.Combine(ImageSource, ImagePath, Stretch, TileMode, AlignmentX, AlignmentY, Tint, Opacity);

    // Factory methods
    public static ImageBrush FromFile(string path) => new(path);

    public static ImageBrush FromTexture(ITexture texture) => new(texture);

    public static ImageBrush Tiled(string path) => new(path) { TileMode = TileMode.Tile, Stretch = Stretch.None };
}
