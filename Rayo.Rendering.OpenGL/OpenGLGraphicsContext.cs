using Silk.NET.OpenGL;

namespace Rayo.Rendering.OpenGL;

/// <summary>
/// OpenGL implementation of the graphics context.
/// Supports delayed GL attachment so hosting can configure the context
/// before the native window has created its OpenGL instance.
/// </summary>
public class OpenGLGraphicsContext : IGraphicsContext
{
    private GL? _gl;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _isDisposed;

    // Cache of the current state to avoid redundant changes.
    private bool _blendingEnabled = true;
    private (BlendFactor src, BlendFactor dst) _currentBlendFunc = (BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
    private bool _scissorEnabled;

    // Context capabilities.
    public int MaxTextureSize { get; private set; }
    public int MaxVertexAttributes { get; private set; }
    public int MaxTextureUnits { get; private set; }
    public string Vendor { get; private set; } = string.Empty;
    public string Renderer { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string GLSLVersion { get; private set; } = string.Empty;

    public OpenGLGraphicsContext()
    {
    }

    public OpenGLGraphicsContext(GL gl)
    {
        AttachGL(gl);
    }

    public void AttachGL(GL gl)
    {
        ArgumentNullException.ThrowIfNull(gl);

        if (_gl != null)
            return;

        _gl = gl;
        InitializeContextState(gl);
    }

    public void OnGraphicsDeviceCreated(object device)
    {
        if (device is GL gl)
            AttachGL(gl);
    }

    public IRenderer CreateRenderer()
    {
        return new OpenGLRenderer(RequireGL());
    }

    public ITexture CreateTexture(int width, int height, byte[] data, TextureFormat format)
    {
        var gl = RequireGL();

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Texture dimensions must be positive");

        if (width > MaxTextureSize || height > MaxTextureSize)
            throw new ArgumentException($"Texture dimensions exceed maximum size of {MaxTextureSize}");

        return new OpenGLTexture(gl, width, height, data, format);
    }

    public IShaderProgram CreateShaderProgram(string vertexShader, string fragmentShader)
    {
        var gl = RequireGL();

        if (string.IsNullOrWhiteSpace(vertexShader))
            throw new ArgumentException("Vertex shader source cannot be empty", nameof(vertexShader));

        if (string.IsNullOrWhiteSpace(fragmentShader))
            throw new ArgumentException("Fragment shader source cannot be empty", nameof(fragmentShader));

        return new OpenGLShaderProgram(gl, vertexShader, fragmentShader);
    }

    public IBuffer CreateVertexBuffer(int sizeInBytes)
    {
        var gl = RequireGL();

        if (sizeInBytes <= 0)
            throw new ArgumentException("Buffer size must be positive", nameof(sizeInBytes));

        return new OpenGLBuffer(gl, BufferTargetARB.ArrayBuffer, sizeInBytes);
    }

    public IBuffer CreateIndexBuffer(int sizeInBytes)
    {
        var gl = RequireGL();

        if (sizeInBytes <= 0)
            throw new ArgumentException("Buffer size must be positive", nameof(sizeInBytes));

        return new OpenGLBuffer(gl, BufferTargetARB.ElementArrayBuffer, sizeInBytes);
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Viewport dimensions must be positive");

        _viewportWidth = width;
        _viewportHeight = height;
        RequireGL().Viewport(x, y, (uint)width, (uint)height);
    }

    public void Clear(float r, float g, float b, float a)
    {
        var gl = RequireGL();
        gl.ClearColor(r, g, b, a);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void Clear(Color color)
    {
        Clear(color.R, color.G, color.B, color.A);
    }

    public void SetBlendingEnabled(bool enabled)
    {
        if (_blendingEnabled == enabled)
            return;

        var gl = RequireGL();
        if (enabled)
            gl.Enable(EnableCap.Blend);
        else
            gl.Disable(EnableCap.Blend);

        _blendingEnabled = enabled;
    }

    public void SetBlendFunction(BlendFactor srcFactor, BlendFactor dstFactor)
    {
        if (_currentBlendFunc.src == srcFactor && _currentBlendFunc.dst == dstFactor)
            return;

        RequireGL().BlendFunc(ToGLBlendFactor(srcFactor), ToGLBlendFactor(dstFactor));
        _currentBlendFunc = (srcFactor, dstFactor);
    }

    public void SetScissorEnabled(bool enabled)
    {
        if (_scissorEnabled == enabled)
            return;

        var gl = RequireGL();
        if (enabled)
            gl.Enable(EnableCap.ScissorTest);
        else
            gl.Disable(EnableCap.ScissorTest);

        _scissorEnabled = enabled;
    }

    public void SetScissorRect(int x, int y, int width, int height)
    {
        if (width < 0 || height < 0)
            throw new ArgumentException("Scissor dimensions cannot be negative");

        RequireGL().Scissor(x, y, (uint)width, (uint)height);
    }

    public void SetDepthTestEnabled(bool enabled)
    {
        var gl = RequireGL();
        if (enabled)
            gl.Enable(EnableCap.DepthTest);
        else
            gl.Disable(EnableCap.DepthTest);
    }

    public void SetDepthFunction(DepthFunction function)
    {
        RequireGL().DepthFunc(ToGLDepthFunction(function));
    }

    public void SetCullingEnabled(bool enabled)
    {
        var gl = RequireGL();
        if (enabled)
            gl.Enable(EnableCap.CullFace);
        else
            gl.Disable(EnableCap.CullFace);
    }

    public void SetCullFace(CullFaceMode mode)
    {
        RequireGL().CullFace(ToGLCullFace(mode));
    }

    public void SetLineWidth(float width)
    {
        if (width <= 0)
            throw new ArgumentException("Line width must be positive", nameof(width));

        RequireGL().LineWidth(width);
    }

    public void SetPointSize(float size)
    {
        if (size <= 0)
            throw new ArgumentException("Point size must be positive", nameof(size));

        RequireGL().PointSize(size);
    }

    public (int width, int height) GetViewportSize()
    {
        return (_viewportWidth, _viewportHeight);
    }

    public void ReadPixels(int x, int y, int width, int height, byte[] buffer, TextureFormat format = TextureFormat.RGBA8)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var gl = RequireGL();
        var (_, pixelFormat, pixelType) = GetGLFormat(format);

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                gl.ReadPixels(x, y, (uint)width, (uint)height, pixelFormat, pixelType, ptr);
            }
        }
    }

    public void Finish()
    {
        RequireGL().Finish();
    }

    public void Flush()
    {
        RequireGL().Flush();
    }

    public void CheckErrors(string location = "")
    {
        var error = RequireGL().GetError();
        if (error != GLEnum.NoError)
        {
            string errorMsg = $"OpenGL Error at {location}: {error}";
            System.Diagnostics.Debug.WriteLine(errorMsg);
            throw new Exception(errorMsg);
        }
    }

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

    private void InitializeContextState(GL gl)
    {
        MaxTextureSize = gl.GetInteger(GetPName.MaxTextureSize);
        MaxVertexAttributes = gl.GetInteger(GetPName.MaxVertexAttribs);
        MaxTextureUnits = gl.GetInteger(GetPName.MaxTextureImageUnits);

        unsafe
        {
            Vendor = gl.GetStringS(StringName.Vendor);
            Renderer = gl.GetStringS(StringName.Renderer);
            Version = gl.GetStringS(StringName.Version);
            GLSLVersion = gl.GetStringS(StringName.ShadingLanguageVersion);
        }

        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _blendingEnabled = true;

        gl.Enable(EnableCap.Multisample);
        gl.Disable(EnableCap.DepthTest);
        gl.Disable(EnableCap.CullFace);
    }

    private GL RequireGL()
    {
        return _gl ?? throw new InvalidOperationException("OpenGLGraphicsContext requires a GL instance before use");
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
