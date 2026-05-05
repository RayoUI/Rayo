namespace Rayo.Rendering;

/// <summary>
/// Represents a backend-independent graphics context.
/// Provides access to graphics resources and operations.
/// </summary>
public interface IGraphicsContext : IDisposable
{
    /// <summary>
    /// Returns true if this context requires a native (non-GL) window.
    /// When true, UIApplication uses GraphicsAPI.None so GLFW exposes IVkSurface
    /// instead of creating an OpenGL context.
    /// </summary>
    bool RequiresNativeWindow => false;

    /// <summary>
    /// Called after the Silk.NET window is created, before CreateRenderer().
    /// Backends that need to bootstrap from a native window handle (e.g. Vulkan)
    /// override this. The parameter is Silk.NET.Windowing.IWindow; cast as needed.
    /// </summary>
    void OnWindowCreated(object window) { }

    /// <summary>
    /// Creates a renderer for this graphics context.
    /// </summary>
    IRenderer CreateRenderer();

    /// <summary>
    /// Creates a texture from raw data.
    /// </summary>
    ITexture CreateTexture(int width, int height, byte[] data, TextureFormat format);

    /// <summary>
    /// Creates a shader program.
    /// </summary>
    IShaderProgram CreateShaderProgram(string vertexShaderSource, string fragmentShaderSource);

    /// <summary>
    /// Creates a vertex buffer.
    /// </summary>
    IBuffer CreateVertexBuffer(int sizeInBytes);

    /// <summary>
    /// Creates an index buffer.
    /// </summary>
    IBuffer CreateIndexBuffer(int sizeInBytes);

    /// <summary>
    /// Sets the viewport (rendering area).
    /// </summary>
    void SetViewport(int x, int y, int width, int height);

    /// <summary>
    /// Clears the framebuffer with a color.
    /// </summary>
    void Clear(float r, float g, float b, float a);

    /// <summary>
    /// Enables or disables blending.
    /// </summary>
    void SetBlendingEnabled(bool enabled);

    /// <summary>
    /// Configures the blending function.
    /// </summary>
    void SetBlendFunction(BlendFactor srcFactor, BlendFactor dstFactor);

    /// <summary>
    /// Enables or disables the scissor test.
    /// </summary>
    void SetScissorEnabled(bool enabled);

    /// <summary>
    /// Configures the scissor rectangle.
    /// </summary>
    void SetScissorRect(int x, int y, int width, int height);
}

/// <summary>
/// Texture format
/// </summary>
public enum TextureFormat
{
    RGBA8,
    RGB8,
    Alpha8,
    R8
}

/// <summary>
/// Blending factors
/// </summary>
public enum BlendFactor
{
    Zero,
    One,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha
}