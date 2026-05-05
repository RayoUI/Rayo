using Silk.NET.OpenGL;
using Rayo.Rendering.SkiaSharp;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Owns the OpenGL resources needed to blit a SkiaSharp CPU surface onto the screen as a
/// fullscreen quad. Created by <see cref="DesktopPlatformHost"/> once the GL context is ready
/// and wired to <see cref="Rayo.Core.UIApplication.WindowPresenter"/>.
/// </summary>
internal sealed class SkiaSharpGLPresenter : IDisposable
{
    private readonly GL _gl;
    private readonly SkiaSharpRenderer _renderer;

    private uint _texture;
    private int _textureWidth;
    private int _textureHeight;
    private uint _vao;
    private uint _vbo;
    private uint _shader;

    public SkiaSharpGLPresenter(GL gl, SkiaSharpRenderer renderer)
    {
        _gl = gl;
        _renderer = renderer;
        Initialize();
    }

    /// <summary>
    /// Uploads the current SkiaSharp surface pixels to the GPU and draws a fullscreen quad.
    /// Called every rendered frame via <see cref="Rayo.Core.UIApplication.WindowPresenter"/>.
    /// </summary>
    public unsafe void Present(int windowWidth, int windowHeight)
    {
        if (!_renderer.TryGetPixels(out IntPtr pixels, out int width, out int height, out _))
            return;

        // Set viewport to the full window before blitting.
        _gl.Viewport(0, 0, (uint)windowWidth, (uint)windowHeight);

        // Create or resize the presentation texture when dimensions change.
        if (_texture == 0 || _textureWidth != width || _textureHeight != height)
        {
            if (_texture != 0)
            {
                _gl.DeleteTexture(_texture);
            }

            _texture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _texture);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            _textureWidth = width;
            _textureHeight = height;
        }
        else
        {
            _gl.BindTexture(TextureTarget.Texture2D, _texture);
        }

        // Upload pixels from SkiaSharp surface to the GPU texture.
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba,
            (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte,
            (void*)pixels);

        // Draw fullscreen quad using the compiled shader.
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.Blend);

        _gl.UseProgram(_shader);
        _gl.BindVertexArray(_vao);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.UseProgram(0);
    }

    private unsafe void Initialize()
    {
        // Vertex shader — passes NDC positions and flips the Y axis so the image is right-side up.
        string vertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec2 aPos;
            layout (location = 1) in vec2 aTexCoord;
            out vec2 TexCoord;
            void main()
            {
                gl_Position = vec4(aPos, 0.0, 1.0);
                TexCoord = aTexCoord;
            }
        ";

        // Fragment shader — samples the SkiaSharp surface texture.
        string fragmentShaderSource = @"
            #version 330 core
            in vec2 TexCoord;
            out vec4 FragColor;
            uniform sampler2D uTexture;
            void main()
            {
                FragColor = texture(uTexture, TexCoord);
            }
        ";

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _shader = _gl.CreateProgram();
        _gl.AttachShader(_shader, vertexShader);
        _gl.AttachShader(_shader, fragmentShader);
        _gl.LinkProgram(_shader);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        // Two triangles forming a fullscreen quad in NDC space.
        // TexCoords are arranged so row 0 of the bitmap lands at the top of the screen.
        float[] vertices =
        [
            // Positions    // TexCoords
            -1.0f,  1.0f,   0.0f, 0.0f,  // Top-left
            -1.0f, -1.0f,   0.0f, 1.0f,  // Bottom-left
             1.0f, -1.0f,   1.0f, 1.0f,  // Bottom-right

            -1.0f,  1.0f,   0.0f, 0.0f,  // Top-left
             1.0f, -1.0f,   1.0f, 1.0f,  // Bottom-right
             1.0f,  1.0f,   1.0f, 0.0f   // Top-right
        ];

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (float* v = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
        }

        // Position attribute (location 0)
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);

        // TexCoord attribute (location 1)
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_texture != 0)
        {
            _gl.DeleteTexture(_texture);
            _texture = 0;
        }
        if (_vao != 0)
        {
            _gl.DeleteVertexArray(_vao);
            _vao = 0;
        }
        if (_vbo != 0)
        {
            _gl.DeleteBuffer(_vbo);
            _vbo = 0;
        }
        if (_shader != 0)
        {
            _gl.DeleteProgram(_shader);
            _shader = 0;
        }
    }
}
