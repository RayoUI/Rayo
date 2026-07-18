using Silk.NET.OpenGL;
using SkiaSharp;
using StbImageSharp;
using Svg.Skia;
using System.Text;

namespace Rayo.Rendering.OpenGL;

/// <summary>
/// Manages loading and caching of OpenGL textures
/// </summary>
public class OpenGLTextureManager : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, OpenGLTexture> _textureCache = new();

    public OpenGLTextureManager(GL gl)
    {
        _gl = gl;
    }

    public OpenGLTexture? LoadTexture(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        // Check if already in cache
        if (_textureCache.TryGetValue(filePath, out var cachedTexture))
            return cachedTexture;

        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[OpenGLTextureManager] Error: File not found: {filePath}");
                return null;
            }

            using var stream = File.OpenRead(filePath);
            var prepared = PrepareTexture(stream, filePath);
            if (prepared == null)
            {
                Console.WriteLine($"[OpenGLTextureManager] Error: Could not load image: {filePath}");
                return null;
            }
            return UploadPreparedTexture(prepared);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenGLTextureManager] Error loading texture {filePath}: {ex.Message}");
            return null;
        }
    }

    public OpenGLTexture? LoadTextureFromStream(Stream stream, string cacheKey)
    {
        if (stream == null || string.IsNullOrEmpty(cacheKey))
            return null;

        // Check if already in cache
        if (_textureCache.TryGetValue(cacheKey, out var cachedTexture))
            return cachedTexture;

        try
        {
            var prepared = PrepareTexture(stream, cacheKey);
            if (prepared == null)
            {
                Console.WriteLine($"[OpenGLTextureManager] Error: Could not load image from stream: {cacheKey}");
                return null;
            }
            return UploadPreparedTexture(prepared);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenGLTextureManager] Error loading texture from stream {cacheKey}: {ex.Message}");
            return null;
        }
    }

    public void UnloadTexture(string filePath)
    {
        if (_textureCache.TryGetValue(filePath, out var texture))
        {
            _gl.DeleteTexture(texture.Id);
            _textureCache.Remove(filePath);
        }
    }

    internal OpenGLTexture? TryGetLoadedTexture(string cacheKey) =>
        _textureCache.TryGetValue(cacheKey, out var texture) ? texture : null;

    internal static OpenGLPreparedTexture? PrepareTexture(Stream stream, string cacheKey)
    {
        var image = DecodeImage(stream);
        return image == null
            ? null
            : new OpenGLPreparedTexture(cacheKey, image.Data, image.Width, image.Height);
    }

    internal OpenGLTexture UploadPreparedTexture(OpenGLPreparedTexture prepared)
    {
        if (_textureCache.TryGetValue(prepared.CacheKey, out var cachedTexture))
        {
            return cachedTexture;
        }

        uint textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        unsafe
        {
            fixed (byte* pointer = prepared.Pixels)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)prepared.Width,
                    (uint)prepared.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pointer);
            }
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
        var texture = new OpenGLTexture(
            _gl,
            textureId,
            prepared.Width,
            prepared.Height,
            TextureFormat.RGBA8);
        _textureCache[prepared.CacheKey] = texture;
        return texture;
    }

    internal static ImageResult? DecodeImage(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var data = memory.ToArray();

        StbImage.stbi_set_flip_vertically_on_load(0);
        if (!LooksLikeSvg(data))
        {
            using var rasterStream = new MemoryStream(data, writable: false);
            return ImageResult.FromStream(rasterStream, ColorComponents.RedGreenBlueAlpha);
        }

        var svgText = Encoding.UTF8.GetString(data);
        using var svg = new SKSvg();
        var picture = svg.FromSvg(svgText);
        if (picture == null)
        {
            return null;
        }

        var bounds = picture.CullRect;
        var width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
        var height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));
        var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(imageInfo);
        if (surface == null)
        {
            return null;
        }

        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(-bounds.Left, -bounds.Top);
        surface.Canvas.DrawPicture(picture);
        surface.Canvas.Flush();

        using var rasterized = surface.Snapshot();
        using var png = rasterized.Encode(SKEncodedImageFormat.Png, 100);
        if (png == null)
        {
            return null;
        }

        using var pngStream = png.AsStream();
        return ImageResult.FromStream(pngStream, ColorComponents.RedGreenBlueAlpha);
    }

    private static bool LooksLikeSvg(byte[] data)
    {
        var length = Math.Min(data.Length, 256);
        var header = Encoding.UTF8.GetString(data, 0, length)
            .TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return header.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            && header.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        foreach (var texture in _textureCache.Values)
        {
            _gl.DeleteTexture(texture.Id);
        }
        _textureCache.Clear();
    }
}

internal sealed record OpenGLPreparedTexture(
    string CacheKey,
    byte[] Pixels,
    int Width,
    int Height) : Rayo.Rendering.IPreparedTexture;
