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

    public int SchemaVersion { get; set; } = 2;
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

    public static SpriteAssetDocument Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SpriteAssetDocument>(json, SerializerOptions)
                ?? throw new InvalidDataException("The sprite asset is empty or invalid.");
        }
        catch (JsonException)
        {
            return DeserializeLegacy(json);
        }
    }

    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);

    public void Validate()
    {
        ValidateDimensions(Width, Height);
        if (Frames.Count == 0)
            throw new InvalidDataException("A sprite must contain at least one frame.");
        if (Frames.Any(frame => frame.Pixels.Length != Width * Height * 4))
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

    private static SpriteAssetDocument DeserializeLegacy(string json)
    {
        using var source = JsonDocument.Parse(json);
        var root = source.RootElement;
        var width = root.GetProperty("width").GetInt32();
        var height = root.GetProperty("height").GetInt32();
        var document = new SpriteAssetDocument
        {
            Width = width,
            Height = height,
            Palette = root.TryGetProperty("palette", out var palette)
                ? JsonSerializer.Deserialize<List<SpriteColor>>(palette.GetRawText(), SerializerOptions) ?? []
                : DefaultPalette(),
            Animations = root.TryGetProperty("animations", out var animations)
                ? JsonSerializer.Deserialize<List<SpriteAnimationDocument>>(animations.GetRawText(), SerializerOptions) ?? []
                : []
        };

        foreach (var legacyFrame in root.GetProperty("frames").EnumerateArray())
        {
            var rgba = new byte[width * height * 4];
            var offset = 0;
            foreach (var pixel in legacyFrame.GetProperty("pixels").EnumerateArray())
            {
                rgba[offset++] = pixel.GetProperty("r").GetByte();
                rgba[offset++] = pixel.GetProperty("g").GetByte();
                rgba[offset++] = pixel.GetProperty("b").GetByte();
                rgba[offset++] = pixel.TryGetProperty("a", out var alpha) ? alpha.GetByte() : (byte)255;
            }

            document.Frames.Add(new SpriteFrameDocument
            {
                DurationMs = legacyFrame.TryGetProperty("durationMs", out var duration)
                    ? duration.GetInt32()
                    : 100,
                Pixels = rgba
            });
        }

        document.Validate();
        return document;
    }
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
    /// <summary>
    /// Raw row-major RGBA pixels. System.Text.Json stores byte arrays as Base64,
    /// keeping large sprite assets compact while preserving the alpha channel.
    /// </summary>
    public byte[] Pixels { get; set; } = [];

    public static SpriteFrameDocument FromFrame(SpriteFrame frame) =>
        new() { Pixels = SpriteCanvas.CreateRgbaPixels(frame) };

    public SpriteFrame ToFrame(int width, int height)
    {
        if (Pixels.Length != width * height * 4)
            throw new InvalidDataException("The sprite frame pixel buffer has an invalid size.");

        var frame = new SpriteFrame(width, height);
        var offset = 0;
        for (var row = 0; row < height; row++)
        {
            for (var column = 0; column < width; column++)
            {
                frame.Pixels[row, column] = new Color(
                    Pixels[offset++],
                    Pixels[offset++],
                    Pixels[offset++],
                    Pixels[offset++]);
            }
        }

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
