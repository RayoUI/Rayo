using Rayo.Rendering;
using SkiaSharp;

namespace Rayo.Rendering.SkiaSharp;

/// <summary>
/// Graphics context for SkiaSharp rendering backend
/// </summary>
public class SkiaSharpGraphicsContext : IGraphicsContext
{
    private SkiaSharpRenderer? _renderer;
    private bool _disposed;

    /// <summary>
    /// Creates a SkiaSharp graphics context
    /// </summary>
    public SkiaSharpGraphicsContext()
    {
    }

    /// <summary>
    /// Creates a new SkiaSharp renderer
    /// </summary>
    public IRenderer CreateRenderer()
    {
        if (_renderer != null)
            throw new InvalidOperationException("Renderer already created");

        _renderer = new SkiaSharpRenderer();
        return _renderer;
    }

    /// <summary>
    /// Creates a texture from raw data
    /// </summary>
    public ITexture CreateTexture(int width, int height, byte[] data, TextureFormat format)
    {
        var colorType = format switch
        {
            TextureFormat.RGBA8 => SKColorType.Rgba8888,
            TextureFormat.RGB8 => SKColorType.Rgb888x,
            TextureFormat.Alpha8 => SKColorType.Alpha8,
            TextureFormat.R8 => SKColorType.Gray8,
            _ => SKColorType.Rgba8888
        };

        var imageInfo = new SKImageInfo(width, height, colorType, SKAlphaType.Premul);

        using var pixmap = new SKPixmap(imageInfo, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
        var image = SKImage.FromPixels(pixmap);

        return new SkiaSharpTexture(image);
    }

    /// <summary>
    /// Not applicable to SkiaSharp (shader programs are internal to Skia)
    /// </summary>
    public IShaderProgram CreateShaderProgram(string vertexShader, string fragmentShader)
    {
        throw new NotSupportedException("Shader programs are not directly accessible in SkiaSharp backend");
    }

    /// <summary>
    /// Not applicable to SkiaSharp (vertex buffers are managed internally)
    /// </summary>
    public IBuffer CreateVertexBuffer(int sizeInBytes)
    {
        throw new NotSupportedException("Vertex buffers are not directly accessible in SkiaSharp backend");
    }

    /// <summary>
    /// Not applicable to SkiaSharp (index buffers are managed internally)
    /// </summary>
    public IBuffer CreateIndexBuffer(int sizeInBytes)
    {
        throw new NotSupportedException("Index buffers are not directly accessible in SkiaSharp backend");
    }

    /// <summary>
    /// Viewport is handled automatically by SkiaSharp surface size
    /// </summary>
    public void SetViewport(int x, int y, int width, int height)
    {
        // No-op: SkiaSharp handles viewport automatically through surface size
    }

    /// <summary>
    /// Clear is delegated to the renderer
    /// </summary>
    public void Clear(float r, float g, float b, float a)
    {
        _renderer?.Clear(new Color(r, g, b, a));
    }

    /// <summary>
    /// Blending is always enabled in SkiaSharp
    /// </summary>
    public void SetBlendingEnabled(bool enabled)
    {
        // No-op: SkiaSharp always uses blending with configurable blend modes per draw call
    }

    /// <summary>
    /// Blend function is handled per-draw-call in SkiaSharp
    /// </summary>
    public void SetBlendFunction(BlendFactor srcFactor, BlendFactor dstFactor)
    {
        // No-op: SkiaSharp handles blending per paint object
    }

    /// <summary>
    /// Scissor is handled through renderer's PushScissor/PopScissor
    /// </summary>
    public void SetScissorEnabled(bool enabled)
    {
        // No-op: Scissor is managed through renderer's scissor stack
    }

    /// <summary>
    /// Scissor rect is handled through renderer's PushScissor
    /// </summary>
    public void SetScissorRect(int x, int y, int width, int height)
    {
        // No-op: Use renderer's PushScissor method instead
    }

    /// <summary>
    /// Gets the current renderer instance
    /// </summary>
    public SkiaSharpRenderer? Renderer => _renderer;

    public void Dispose()
    {
        if (_disposed) return;

        _renderer?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    ~SkiaSharpGraphicsContext()
    {
        Dispose();
    }
}
