using Silk.NET.OpenGL;

namespace Rayo.Rendering.OpenGL;

/// <summary>
/// OpenGL texture that can be used as a render target (framebuffer object).
/// </summary>
public class OpenGLRenderTargetTexture : ITexture
{
    private readonly uint _textureId;
    private readonly uint _fboId;
    private readonly int _width;
    private readonly int _height;
    private readonly GL _gl;
    private bool _disposed;

    public OpenGLRenderTargetTexture(uint textureId, uint fboId, int width, int height, GL gl)
    {
        _textureId = textureId;
        _fboId = fboId;
        _width = width;
        _height = height;
        _gl = gl;
    }

    public uint TextureId => _textureId;
    public uint FBO => _fboId;
    public int Width => _width;
    public int Height => _height;

    public void Dispose()
    {
        if (_disposed) return;

        _gl.DeleteTexture(_textureId);
        _gl.DeleteFramebuffer(_fboId);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
