using System.Text.Json;
using Nano.Views.SpriteEditor.Components;
using Rayo.Rendering;

namespace Nano.Views.SpriteEditor;

/// <summary>
/// Persisted representation of a Nano sprite asset. The .sprite file is UTF-8 JSON
/// so it can safely live inside the project's .nn ZIP and remain versionable.
/// </summary>
public sealed class SpriteAssetDocument
{
    public const string Extension = ".sprite";

    public int SchemaVersion { get; set; } = 1;
    public int Width { get; set; } = 16;
    public int Height { get; set; } = 16;
    public List<SpriteColor> Palette { get; set; } = [];
    public List<SpriteFrameDocument> Frames { get; set; } = [];
    public List<SpriteAnimationDocument> Animations { get; set; } = [];

    public static SpriteAssetDocument CreateBlank(int width, int height)
    {
        ValidateDimensions(width, height);
        return new SpriteAssetDocument
        {
            Width = width,
            Height = height,
            Palette = DefaultPalette(),
            Frames = [SpriteFrameDocument.FromFrame(new SpriteFrame(width, height))],
            Animations = [new SpriteAnimationDocument { Name = "idle", FrameIndices = [0] }]
        };
    }

    public static SpriteAssetDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<SpriteAssetDocument>(json, SerializerOptions)
        ?? throw new InvalidDataException("The sprite asset is empty or invalid.");

    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);

    public void Validate()
    {
        ValidateDimensions(Width, Height);
        if (Frames.Count == 0)
            throw new InvalidDataException("A sprite must contain at least one frame.");
        if (Frames.Any(frame => frame.Pixels.Count != Width * Height))
            throw new InvalidDataException("A sprite frame does not match the declared canvas dimensions.");
    }

    public static void ValidateDimensions(int width, int height)
    {
        if (width is < 1 or > 256 || height is < 1 or > 256)
            throw new ArgumentOutOfRangeException(nameof(width), "Sprite dimensions must be between 1 and 256 pixels.");
    }

    private static List<SpriteColor> DefaultPalette() =>
    [
        new(62, 126, 214), new(34, 150, 94), new(225, 142, 38),
        new(215, 72, 72), new(137, 87, 229), new(35, 39, 47)
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public sealed record SpriteColor(byte R, byte G, byte B, byte A = 255)
{
    public Color ToColor() => new(R, G, B, A);

    public static SpriteColor FromColor(Color color) => new(
        (byte)Math.Clamp((int)MathF.Round(color.R * 255), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.G * 255), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.B * 255), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.A * 255), 0, 255));
}

public sealed class SpriteFrameDocument
{
    public int DurationMs { get; set; } = 100;
    public List<SpriteColor> Pixels { get; set; } = [];

    public static SpriteFrameDocument FromFrame(SpriteFrame frame)
    {
        var pixels = new List<SpriteColor>(frame.Width * frame.Height);
        for (var row = 0; row < frame.Height; row++)
            for (var column = 0; column < frame.Width; column++)
                pixels.Add(SpriteColor.FromColor(frame.Pixels[row, column]));
        return new SpriteFrameDocument { Pixels = pixels };
    }

    public SpriteFrame ToFrame(int width, int height)
    {
        var frame = new SpriteFrame(width, height);
        for (var row = 0; row < height; row++)
            for (var column = 0; column < width; column++)
                frame.Pixels[row, column] = Pixels[row * width + column].ToColor();
        return frame;
    }
}

public sealed class SpriteAnimationDocument
{
    public string Name { get; set; } = "idle";
    public bool Loop { get; set; } = true;
    public float Speed { get; set; } = 1f;
    public List<int> FrameIndices { get; set; } = [];
}
