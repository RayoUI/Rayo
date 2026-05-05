using Silk.NET.OpenGL;
using System.Numerics;

namespace Rayo.Rendering.OpenGL;

/// <summary>
/// OpenGL implementation of the graphics context
/// </summary>
public class OpenGLGraphicsContext : IGraphicsContext
{
    private readonly GL _gl;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _isDisposed;

    // Cache of the current state to avoid redundant changes
    private bool _blendingEnabled = true;
    private (BlendFactor src, BlendFactor dst) _currentBlendFunc = (BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
    private bool _scissorEnabled = false;

    // Context capabilities
    public int MaxTextureSize { get; }
    public int MaxVertexAttributes { get; }
    public int MaxTextureUnits { get; }
    public string Vendor { get; }
    public string Renderer { get; }
    public string Version { get; }
    public string GLSLVersion { get; }

    public OpenGLGraphicsContext(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));

        // Query context capabilities
        MaxTextureSize = _gl.GetInteger(GetPName.MaxTextureSize);
        MaxVertexAttributes = _gl.GetInteger(GetPName.MaxVertexAttribs);
        MaxTextureUnits = _gl.GetInteger(GetPName.MaxTextureImageUnits);
        
        unsafe
        {
      Vendor = _gl.GetStringS(StringName.Vendor);
         Renderer = _gl.GetStringS(StringName.Renderer);
     Version = _gl.GetStringS(StringName.Version);
    GLSLVersion = _gl.GetStringS(StringName.ShadingLanguageVersion);
        }

    // Configure initial OpenGL state
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
   _blendingEnabled = true;

   // Enable anti-aliasing if available
      _gl.Enable(EnableCap.Multisample);

 // Disable depth test by default (2D UI)
    _gl.Disable(EnableCap.DepthTest);

        // Disable culling by default
        _gl.Disable(EnableCap.CullFace);
    }

    public IRenderer CreateRenderer()
    {
        return new OpenGLRenderer(_gl);
    }

    public ITexture CreateTexture(int width, int height, byte[] data, TextureFormat format)
    {
        if (width <= 0 || height <= 0)
      throw new ArgumentException("Texture dimensions must be positive");
        
        if (width > MaxTextureSize || height > MaxTextureSize)
            throw new ArgumentException($"Texture dimensions exceed maximum size of {MaxTextureSize}");

        return new OpenGLTexture(_gl, width, height, data, format);
    }

    public IShaderProgram CreateShaderProgram(string vertexShader, string fragmentShader)
    {
        if (string.IsNullOrWhiteSpace(vertexShader))
            throw new ArgumentException("Vertex shader source cannot be empty", nameof(vertexShader));
  
        if (string.IsNullOrWhiteSpace(fragmentShader))
         throw new ArgumentException("Fragment shader source cannot be empty", nameof(fragmentShader));

        return new OpenGLShaderProgram(_gl, vertexShader, fragmentShader);
    }

    public IBuffer CreateVertexBuffer(int sizeInBytes)
    {
        if (sizeInBytes <= 0)
        throw new ArgumentException("Buffer size must be positive", nameof(sizeInBytes));

        return new OpenGLBuffer(_gl, BufferTargetARB.ArrayBuffer, sizeInBytes);
  }

    public IBuffer CreateIndexBuffer(int sizeInBytes)
    {
    if (sizeInBytes <= 0)
         throw new ArgumentException("Buffer size must be positive", nameof(sizeInBytes));

  return new OpenGLBuffer(_gl, BufferTargetARB.ElementArrayBuffer, sizeInBytes);
    }

    public void SetViewport(int x, int y, int width, int height)
{
    if (width <= 0 || height <= 0)
         throw new ArgumentException("Viewport dimensions must be positive");

        _viewportWidth = width;
      _viewportHeight = height;
        _gl.Viewport(x, y, (uint)width, (uint)height);
    }

    public void Clear(float r, float g, float b, float a)
    {
        _gl.ClearColor(r, g, b, a);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Clears the framebuffer with a color (convenience overload)
 /// </summary>
    public void Clear(Color color)
    {
        Clear(color.R, color.G, color.B, color.A);
    }

 public void SetBlendingEnabled(bool enabled)
    {
        // Avoid redundant state changes
  if (_blendingEnabled == enabled)
    return;

        if (enabled)
       _gl.Enable(EnableCap.Blend);
        else
        _gl.Disable(EnableCap.Blend);

        _blendingEnabled = enabled;
    }

    public void SetBlendFunction(BlendFactor srcFactor, BlendFactor dstFactor)
    {
      // Avoid redundant state changes
        if (_currentBlendFunc.src == srcFactor && _currentBlendFunc.dst == dstFactor)
            return;

        _gl.BlendFunc(ToGLBlendFactor(srcFactor), ToGLBlendFactor(dstFactor));
     _currentBlendFunc = (srcFactor, dstFactor);
    }

    public void SetScissorEnabled(bool enabled)
    {
        // Avoid redundant state changes
        if (_scissorEnabled == enabled)
            return;

        if (enabled)
          _gl.Enable(EnableCap.ScissorTest);
        else
            _gl.Disable(EnableCap.ScissorTest);

        _scissorEnabled = enabled;
    }

    public void SetScissorRect(int x, int y, int width, int height)
    {
    if (width < 0 || height < 0)
   throw new ArgumentException("Scissor dimensions cannot be negative");

     _gl.Scissor(x, y, (uint)width, (uint)height);
    }

    /// <summary>
    /// Enables or disables depth testing
    /// </summary>
    public void SetDepthTestEnabled(bool enabled)
    {
        if (enabled)
            _gl.Enable(EnableCap.DepthTest);
        else
            _gl.Disable(EnableCap.DepthTest);
    }

    /// <summary>
    /// Configures the depth test function
  /// </summary>
    public void SetDepthFunction(DepthFunction function)
    {
_gl.DepthFunc(ToGLDepthFunction(function));
    }

    /// <summary>
    /// Enables or disables culling
    /// </summary>
    public void SetCullingEnabled(bool enabled)
    {
        if (enabled)
       _gl.Enable(EnableCap.CullFace);
        else
            _gl.Disable(EnableCap.CullFace);
    }

    /// <summary>
    /// Configures which faces to cull
    /// </summary>
    public void SetCullFace(CullFaceMode mode)
    {
_gl.CullFace(ToGLCullFace(mode));
    }

    /// <summary>
 /// Sets the line width for line rendering
 /// </summary>
    public void SetLineWidth(float width)
    {
   if (width <= 0)
            throw new ArgumentException("Line width must be positive", nameof(width));

        _gl.LineWidth(width);
 }

    /// <summary>
    /// Configures the point size for point rendering
    /// </summary>
    public void SetPointSize(float size)
    {
        if (size <= 0)
 throw new ArgumentException("Point size must be positive", nameof(size));

        _gl.PointSize(size);
    }

    /// <summary>
    /// Gets the current size of the viewport
    /// </summary>
    public (int width, int height) GetViewportSize()
    {
      return (_viewportWidth, _viewportHeight);
    }

    /// <summary>
    /// Reads pixels from the framebuffer
    /// </summary>
    public void ReadPixels(int x, int y, int width, int height, byte[] buffer, TextureFormat format = TextureFormat.RGBA8)
    {
        if (buffer == null)
    throw new ArgumentNullException(nameof(buffer));

        var (_, pixelFormat, pixelType) = GetGLFormat(format);

        unsafe
        {
       fixed (byte* ptr = buffer)
         {
          _gl.ReadPixels(x, y, (uint)width, (uint)height, pixelFormat, pixelType, ptr);
            }
        }
  }

    /// <summary>
    /// Synchronizes and waits for all OpenGL operations to complete
    /// </summary>
    public void Finish()
    {
      _gl.Finish();
    }

    /// <summary>
    /// Forces the execution of pending OpenGL commands
    /// </summary>
    public void Flush()
    {
    _gl.Flush();
    }

    /// <summary>
    /// Checks for OpenGL errors and reports them
    /// </summary>
    public void CheckErrors(string location = "")
    {
        var error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
       string errorMsg = $"OpenGL Error at {location}: {error}";
         System.Diagnostics.Debug.WriteLine(errorMsg);
            throw new Exception(errorMsg);
  }
    }

    /// <summary>
    /// Obtains information about the current OpenGL context
    /// </summary>
    public string GetContextInfo()
    {
        return $"""
            OpenGL Context Information:
   Vendor: {Vendor}
       Renderer: {Renderer}
            Version: {Version}
        GLSL Version: {GLSLVersion}
   Max Texture Size: {MaxTextureSize}
            Max Vertex Attributes: {MaxVertexAttributes}
         Max Texture Units: {MaxTextureUnits}
  """;
    }

    private BlendingFactor ToGLBlendFactor(BlendFactor factor)
    {
        return factor switch
        {
    BlendFactor.Zero => BlendingFactor.Zero,
 BlendFactor.One => BlendingFactor.One,
     BlendFactor.SrcAlpha => BlendingFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
            BlendFactor.DstAlpha => BlendingFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha => BlendingFactor.OneMinusDstAlpha,
            _ => BlendingFactor.One
        };
    }

    private Silk.NET.OpenGL.DepthFunction ToGLDepthFunction(DepthFunction function)
    {
        return function switch
        {
 DepthFunction.Never => Silk.NET.OpenGL.DepthFunction.Never,
            DepthFunction.Less => Silk.NET.OpenGL.DepthFunction.Less,
     DepthFunction.Equal => Silk.NET.OpenGL.DepthFunction.Equal,
        DepthFunction.LessOrEqual => Silk.NET.OpenGL.DepthFunction.Lequal,
            DepthFunction.Greater => Silk.NET.OpenGL.DepthFunction.Greater,
     DepthFunction.NotEqual => Silk.NET.OpenGL.DepthFunction.Notequal,
 DepthFunction.GreaterOrEqual => Silk.NET.OpenGL.DepthFunction.Gequal,
            DepthFunction.Always => Silk.NET.OpenGL.DepthFunction.Always,
 _ => Silk.NET.OpenGL.DepthFunction.Less
        };
    }

    private TriangleFace ToGLCullFace(CullFaceMode mode)
    {
     return mode switch
      {
      CullFaceMode.Front => TriangleFace.Front,
CullFaceMode.Back => TriangleFace.Back,
            CullFaceMode.FrontAndBack => TriangleFace.FrontAndBack,
   _ => TriangleFace.Back
        };
    }

    private (InternalFormat, PixelFormat, PixelType) GetGLFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.RGBA8 => (InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte),
      TextureFormat.RGB8 => (InternalFormat.Rgb, PixelFormat.Rgb, PixelType.UnsignedByte),
            TextureFormat.Alpha8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
            TextureFormat.R8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
            _ => (InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte)
        };
    }

    public void Dispose()
    {
     if (_isDisposed)
     return;

        // The GL context is managed externally by the window
        // We only clean up our state
        _isDisposed = true;
    GC.SuppressFinalize(this);
}
}

/// <summary>
/// Depth test function
/// </summary>
public enum DepthFunction
{
    Never,
    Less,
    Equal,
    LessOrEqual,
    Greater,
    NotEqual,
    GreaterOrEqual,
    Always
}

/// <summary>
/// Face culling mode
/// </summary>
public enum CullFaceMode
{
    Front,
    Back,
 FrontAndBack
}