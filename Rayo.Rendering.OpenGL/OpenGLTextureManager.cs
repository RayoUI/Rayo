using Silk.NET.OpenGL;
using StbImageSharp;

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

            // Load image using StbImageSharp
            StbImage.stbi_set_flip_vertically_on_load(0);

            using var stream = File.OpenRead(filePath);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            if (image == null)
            {
                Console.WriteLine($"[OpenGLTextureManager] Error: Could not load image: {filePath}");
                return null;
            }

            // Create OpenGL texture
            uint textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, textureId);

            // Set texture parameters
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            // Upload texture data to GPU
            unsafe
            {
                fixed (byte* ptr = image.Data)
                {
                    _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        InternalFormat.Rgba,
                        (uint)image.Width,
                        (uint)image.Height,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        ptr
                    );
                }
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            var texture = new OpenGLTexture(_gl, textureId, image.Width, image.Height, TextureFormat.RGBA8);

            _textureCache[filePath] = texture;

            Console.WriteLine($"[OpenGLTextureManager] Texture loaded: {filePath} ({image.Width}x{image.Height})");

            return texture;
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
            // Load image using StbImageSharp
            StbImage.stbi_set_flip_vertically_on_load(0);

            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            if (image == null)
            {
                Console.WriteLine($"[OpenGLTextureManager] Error: Could not load image from stream: {cacheKey}");
                return null;
            }

            // Create OpenGL texture
            uint textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, textureId);

            // Set texture parameters
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            // Upload texture data to GPU
            unsafe
            {
                fixed (byte* ptr = image.Data)
                {
                    _gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        InternalFormat.Rgba,
                        (uint)image.Width,
                        (uint)image.Height,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        ptr
                    );
                }
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            var texture = new OpenGLTexture(_gl, textureId, image.Width, image.Height, TextureFormat.RGBA8);

            _textureCache[cacheKey] = texture;

            Console.WriteLine($"[OpenGLTextureManager] Texture loaded from stream: {cacheKey} ({image.Width}x{image.Height})");

            return texture;
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

    public void Dispose()
    {
        foreach (var texture in _textureCache.Values)
        {
            _gl.DeleteTexture(texture.Id);
        }
        _textureCache.Clear();
    }
}