using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Rayo.Rendering.OpenGL;

/// <summary>
/// Complete OpenGL implementation of the renderer.
/// Migrated from UIRenderer.cs to be backend-agnostic.
/// </summary>
public unsafe class OpenGLRenderer : IRenderer
{
    private readonly GL _gl;
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private uint _shaderProgram;
    private uint _textShaderProgram;
    private uint _textVao;
    private uint _textVbo;
    private uint _textEbo;
    private uint _imageShaderProgram;
    private uint _imageVao;
    private uint _imageVbo;
    private uint _imageEbo;

    private List<Vertex> _vertices = new();
    private List<ushort> _indices = new();
    private List<TextVertex> _textVertices = new();
    private List<ushort> _textIndices = new();
    private const int MaxVertices = 10000;
    private const int MaxIndices = 30000;

    private Matrix4x4 _projection;
    private OpenGLFontAtlas? _defaultFont;
    private OpenGLTextureManager? _textureManager;

    // Stack for scissor testing (clipping)
    private Stack<(int x, int y, int width, int height)> _scissorStack = new();
    private Stack<int> _roundedClipStack = new();
    private Stack<Matrix3x2> _transformStack = new();
    private Matrix3x2 _currentTransform = Matrix3x2.Identity;

    private uint _viewportWidth = 0;
    private uint _viewportHeight = 0;

    public OpenGLRenderer(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
    }

    public void Initialize(int width, int height)
    {
        InitializeOpenGL();
        InitializeTextRendering();
        InitializeImageRendering();
        LoadDefaultFont();
        _textureManager = new OpenGLTextureManager(_gl);

        Resize(width, height);
    }

    private void InitializeOpenGL()
    {
        var vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec4 aColor;

uniform mat4 uProjection;

out vec4 vColor;

void main()
{
    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
    vColor = aColor;
}
";

        var fragmentShaderSource = @"
#version 330 core
in vec4 vColor;
out vec4 FragColor;

void main()
{
    FragColor = vColor;
}
";

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);

        int success;
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Vertex shader compilation failed: {infoLog}");
        }

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Fragment shader compilation failed: {infoLog}");
        }

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);

        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(_shaderProgram);
            throw new Exception($"Shader program linking failed: {infoLog}");
        }

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MaxVertices * sizeof(Vertex)), null, BufferUsageARB.DynamicDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(MaxIndices * sizeof(ushort)), null, BufferUsageARB.DynamicDraw);

        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)8);
        _gl.EnableVertexAttribArray(1);

        _gl.BindVertexArray(0);

        // Enable blending for transparency
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Enable multisampling for anti-aliasing
        _gl.Enable(EnableCap.Multisample);
    }

    private void InitializeTextRendering()
    {
        var vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec4 aColor;

uniform mat4 uProjection;

out vec2 vTexCoord;
out vec4 vColor;

void main()
{
    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
    vTexCoord = aTexCoord;
    vColor = aColor;
}
";

        var fragmentShaderSource = @"
#version 330 core
in vec2 vTexCoord;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    float alpha = texture(uTexture, vTexCoord).r;
    FragColor = vec4(vColor.rgb, vColor.a * alpha);
}
";

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);

        int success;
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Text vertex shader compilation failed: {infoLog}");
        }

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Text fragment shader compilation failed: {infoLog}");
        }

        _textShaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_textShaderProgram, vertexShader);
        _gl.AttachShader(_textShaderProgram, fragmentShader);
        _gl.LinkProgram(_textShaderProgram);

        _gl.GetProgram(_textShaderProgram, ProgramPropertyARB.LinkStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(_textShaderProgram);
            throw new Exception($"Text shader program linking failed: {infoLog}");
        }

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        _textVao = _gl.GenVertexArray();
        _textVbo = _gl.GenBuffer();
        _textEbo = _gl.GenBuffer();

        _gl.BindVertexArray(_textVao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _textVbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MaxVertices * sizeof(TextVertex)), null, BufferUsageARB.DynamicDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _textEbo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(MaxIndices * sizeof(ushort)), null, BufferUsageARB.DynamicDraw);

        // Position
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TextVertex), (void*)0);
        _gl.EnableVertexAttribArray(0);

        // TexCoord
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TextVertex), (void*)8);
        _gl.EnableVertexAttribArray(1);

        // Color
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)sizeof(TextVertex), (void*)16);
        _gl.EnableVertexAttribArray(2);

        _gl.BindVertexArray(0);
    }

    private void InitializeImageRendering()
    {
        var vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec4 aColor;

uniform mat4 uProjection;

out vec2 vTexCoord;
out vec4 vColor;

void main()
{
    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
    vTexCoord = aTexCoord;
    vColor = aColor;
}
";

        var fragmentShaderSource = @"
#version 330 core
in vec2 vTexCoord;
in vec4 vColor;
out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    vec4 texColor = texture(uTexture, vTexCoord);
    FragColor = texColor * vColor;
}
";

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);

        int success;
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Image vertex shader compilation failed: {infoLog}");
        }

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);

        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Image fragment shader compilation failed: {infoLog}");
        }

        _imageShaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_imageShaderProgram, vertexShader);
        _gl.AttachShader(_imageShaderProgram, fragmentShader);
        _gl.LinkProgram(_imageShaderProgram);

        _gl.GetProgram(_imageShaderProgram, ProgramPropertyARB.LinkStatus, out success);
        if (success == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(_imageShaderProgram);
            throw new Exception($"Image shader program linking failed: {infoLog}");
        }

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        _imageVao = _gl.GenVertexArray();
        _imageVbo = _gl.GenBuffer();
        _imageEbo = _gl.GenBuffer();

        _gl.BindVertexArray(_imageVao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _imageVbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MaxVertices * sizeof(TextVertex)), null, BufferUsageARB.DynamicDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _imageEbo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(MaxIndices * sizeof(ushort)), null, BufferUsageARB.DynamicDraw);

        // Position
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TextVertex), (void*)0);
        _gl.EnableVertexAttribArray(0);

        // TexCoord
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TextVertex), (void*)8);
        _gl.EnableVertexAttribArray(1);

        // Color
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)sizeof(TextVertex), (void*)16);
        _gl.EnableVertexAttribArray(2);

        _gl.BindVertexArray(0);
    }

    private void LoadDefaultFont()
    {
        try
        {
            // Primary text font candidates, ordered by preference.
            // Segoe UI is preferred on Windows for its clean rendering and broad Unicode coverage.
            string[] textFontCandidates =
            [
                @"C:\Windows\Fonts\segoeui.ttf",
                @"C:\Windows\Fonts\arial.ttf",
                @"C:\Windows\Fonts\calibri.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                "/System/Library/Fonts/SFNS.ttf",
                "/System/Library/Fonts/Helvetica.ttc",
            ];

            foreach (var fontPath in textFontCandidates)
            {
                if (!File.Exists(fontPath))
                    continue;

                byte[] fontData = File.ReadAllBytes(fontPath);
                _defaultFont = new OpenGLFontAtlas(_gl, fontData, 24);

                // Load emoji fallback font alongside the primary text font.
                // Segoe UI Emoji (Windows) or Noto Color Emoji (Linux) provide emoji glyphs.
                // OpenGL uses a grayscale-only atlas, so only monochrome/text-style emoji fonts
                // are supported (color emoji requires a separate RGBA pipeline).
                LoadEmojiFallbackFont();

                return;
            }
        }
        catch
        {
            // Silently fail — rendering will skip text if no font is found.
        }
    }

    /// <summary>
    /// Secondary font atlas used for emoji and other glyphs missing from the primary font.
    /// </summary>
    private OpenGLFontAtlas? _emojiFallbackFont;

    private void LoadEmojiFallbackFont()
    {
        // Prefer Segoe UI Emoji (monochrome outline variant on older Windows builds,
        // or the text-style glyphs embedded in the color font).  On Linux, use Noto Emoji.
        // Note: color emoji (CBDT/CBLC/COLR tables) cannot be rendered by StbTrueType;
        // only the outline/grayscale glyphs from these files will be available.
        string[] emojiFontCandidates =
        [
            @"C:\Windows\Fonts\seguiemj.ttf",   // Segoe UI Emoji
            @"C:\Windows\Fonts\seguisym.ttf",   // Segoe UI Symbol (fallback)
            "/usr/share/fonts/truetype/noto/NotoEmoji-Regular.ttf",
            "/usr/share/fonts/noto/NotoEmoji-Regular.ttf",
            "/System/Library/Fonts/Apple Color Emoji.ttc",
        ];

        foreach (var fontPath in emojiFontCandidates)
        {
            if (!File.Exists(fontPath))
                continue;

            try
            {
                byte[] fontData = File.ReadAllBytes(fontPath);
                _emojiFallbackFont = new OpenGLFontAtlas(_gl, fontData, 24, isEmojiAtlas: true);
                return;
            }
            catch
            {
                // Try next candidate.
            }
        }
    }

    public void Resize(int width, int height)
    {
        _viewportWidth = (uint)width;
        _viewportHeight = (uint)height;
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
    }

    public void BeginFrame()
    {
        _vertices.Clear();
        _indices.Clear();
        _textVertices.Clear();
        _textIndices.Clear();
        _scissorStack.Clear();
        _roundedClipStack.Clear();
        _transformStack.Clear();
        _currentTransform = Matrix3x2.Identity;
        _gl.Disable(EnableCap.StencilTest);
    }

    public void EndFrame()
    {
        // Flush all pending draw calls
        Flush();

        // Ensure that scissor test is disabled at the end of the frame
        if (_scissorStack.Count > 0)
        {
            _gl.Disable(EnableCap.ScissorTest);
            _scissorStack.Clear();
        }

        if (_roundedClipStack.Count > 0)
        {
            _gl.Disable(EnableCap.StencilTest);
            _roundedClipStack.Clear();
        }
    }

    public void Clear(Color color)
    {
        _gl.ClearColor(color.R, color.G, color.B, color.A);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void DrawRect(float x, float y, float width, float height, Color color)
    {
        ushort baseIndex = (ushort)_vertices.Count;

        _vertices.Add(new Vertex { Position = new Vector2(x, y), Color = new Vector4(color.R, color.G, color.B, color.A) });
        _vertices.Add(new Vertex { Position = new Vector2(x + width, y), Color = new Vector4(color.R, color.G, color.B, color.A) });
        _vertices.Add(new Vertex { Position = new Vector2(x + width, y + height), Color = new Vector4(color.R, color.G, color.B, color.A) });
        _vertices.Add(new Vertex { Position = new Vector2(x, y + height), Color = new Vector4(color.R, color.G, color.B, color.A) });

        _indices.Add(baseIndex);
        _indices.Add((ushort)(baseIndex + 1));
        _indices.Add((ushort)(baseIndex + 2));
        _indices.Add(baseIndex);
        _indices.Add((ushort)(baseIndex + 2));
        _indices.Add((ushort)(baseIndex + 3));
    }

    public void DrawRect(float x, float y, float width, float height, Brushes.Brush brush)
    {
        if (brush == null) return;

        if (brush.IsGradient)
        {
            Brushes.BrushRendererExtensions.DrawRect(this, x, y, width, height, brush);
            return;
        }

        var color = brush.PrimaryColor;
        var finalColor = new Color(color.R, color.G, color.B, color.A * brush.Opacity);
        DrawRect(x, y, width, height, finalColor);
    }

    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        // If there are no rounded corners, use the normal method
        if (radius == 0)
        {
            DrawRect(x, y, width, height, color);
            return;
        }

        // Limit radius to avoid overlap
        float maxRadius = Math.Min(width / 2, height / 2);
        float r = Math.Min(radius, maxRadius);

        var colorVec = new Vector4(color.R, color.G, color.B, color.A);
        ushort baseIndex = (ushort)_vertices.Count;
        int segments = 16;

        // Center of the rectangle
        _vertices.Add(new Vertex { Position = new Vector2(x + width / 2, y + height / 2), Color = colorVec });
        int centerIndex = _vertices.Count - 1;

        // Top-left corner
        if (r > 0)
        {
            float cx = x + r;
            float cy = y + r;
            for (int i = 0; i <= segments; i++)
            {
                float angle = MathF.PI + i * (MathF.PI / 2) / segments;
                float px = cx + MathF.Cos(angle) * r;
                float py = cy + MathF.Sin(angle) * r;
                _vertices.Add(new Vertex { Position = new Vector2(px, py), Color = colorVec });
            }
        }

        else
        {
            _vertices.Add(new Vertex { Position = new Vector2(x, y), Color = colorVec });
        }

        // Top side
        _vertices.Add(new Vertex { Position = new Vector2(x + width - r, y), Color = colorVec });

        // Top-right corner
        if (r > 0)
        {
            float cx = x + width - r;
            float cy = y + r;
            for (int i = 0; i <= segments; i++)
            {
                float angle = 1.5f * MathF.PI + i * (MathF.PI / 2) / segments;
                float px = cx + MathF.Cos(angle) * r;
                float py = cy + MathF.Sin(angle) * r;
                _vertices.Add(new Vertex { Position = new Vector2(px, py), Color = colorVec });
            }
        }
        else
        {
            _vertices.Add(new Vertex { Position = new Vector2(x + width, y), Color = colorVec });
        }

        // Right side
        _vertices.Add(new Vertex { Position = new Vector2(x + width, y + height - r), Color = colorVec });

        // Bottom-right corner
        if (r > 0)
        {
            float cx = x + width - r;
            float cy = y + height - r;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * (MathF.PI / 2) / segments;
                float px = cx + MathF.Cos(angle) * r;
                float py = cy + MathF.Sin(angle) * r;
                _vertices.Add(new Vertex { Position = new Vector2(px, py), Color = colorVec });
            }
        }
        else
        {
            _vertices.Add(new Vertex { Position = new Vector2(x + width, y + height), Color = colorVec });
        }

        // Bottom side
        _vertices.Add(new Vertex { Position = new Vector2(x + r, y + height), Color = colorVec });

        // Bottom-left corner
        if (r > 0)
        {
            float cx = x + r;
            float cy = y + height - r;
            for (int i = 0; i <= segments; i++)
            {
                float angle = MathF.PI / 2 + i * (MathF.PI / 2) / segments;
                float px = cx + MathF.Cos(angle) * r;
                float py = cy + MathF.Sin(angle) * r;
                _vertices.Add(new Vertex { Position = new Vector2(px, py), Color = colorVec });
            }
        }
        else
        {
            _vertices.Add(new Vertex { Position = new Vector2(x, y + height), Color = colorVec });
        }

        // Left side
        _vertices.Add(new Vertex { Position = new Vector2(x, y + r), Color = colorVec });

        // Create triangles from the center to all the edge vertices
        int vertexCount = _vertices.Count - centerIndex - 1;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            _indices.Add((ushort)centerIndex);
            _indices.Add((ushort)(centerIndex + 1 + i));
            _indices.Add((ushort)(centerIndex + 1 + i + 1));
        }
        // Last triangle connects to the first vertex
        _indices.Add((ushort)centerIndex);
        _indices.Add((ushort)(_vertices.Count - 1));
        _indices.Add((ushort)(centerIndex + 1));
    }

    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Brushes.Brush brush)
    {
        if (brush == null) return;

        if (brush.IsGradient)
        {
            Brushes.BrushRendererExtensions.DrawRoundedRect(this, x, y, width, height, radius, brush);
            return;
        }

        var color = brush.PrimaryColor;
        var finalColor = new Color(color.R, color.G, color.B, color.A * brush.Opacity);
        DrawRoundedRect(x, y, width, height, radius, finalColor);
    }

    public void DrawRectOutline(float x, float y, float width, float height, float thickness, Color color)
    {
        // FIX: Add small overlap to prevent seams with background
        float overlap = 0.5f;
        float effectiveThickness = thickness + overlap;

        // Draw 4 rectangles for the outline, inset to be inside the bounds
        // Top
        DrawRect(x, y, width, effectiveThickness, color);
        // Bottom
        DrawRect(x, y + height - effectiveThickness, width, effectiveThickness, color);
        // Left (between top and bottom)
        DrawRect(x, y + effectiveThickness, effectiveThickness, height - 2 * effectiveThickness, color);
        // Right (between top and bottom)
        DrawRect(x + width - effectiveThickness, y + effectiveThickness, effectiveThickness, height - 2 * effectiveThickness, color);
    }

    public void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Color color)
    {
        // Clamp radius to valid range
        float r = Math.Min(radius, Math.Min(width / 2, height / 2));

        // Use higher segment count for smoother curves - adaptive based on radius
        int segments = Math.Max(16, (int)(r / 2));

        // FIX: Add small overlap to prevent seams with background
        float overlap = 0.5f;
        float effectiveThickness = thickness + overlap;
        float effectiveHalfThickness = effectiveThickness / 2;

        // Draw straight edges as filled rectangles (more robust than lines)
        // Top edge
        DrawRect(x + r, y, width - 2 * r, effectiveThickness, color);

        // Right edge
        DrawRect(x + width - effectiveThickness, y + r, effectiveThickness, height - 2 * r, color);

        // Bottom edge
        DrawRect(x + r, y + height - effectiveThickness, width - 2 * r, effectiveThickness, color);

        // Left edge
        DrawRect(x, y + r, effectiveThickness, height - 2 * r, color);

        // Draw rounded corners as filled arc segments
        // FIX: Adjust radius to align with straight edges (which are drawn inside)
        // The straight edges are drawn from 0 to effectiveThickness.
        // The corner arc should be drawn such that its outer edge is at radius (matching the corner of the rect)
        // and its inner edge is at radius - effectiveThickness.
        // DrawRoundedCornerOutline draws a stroke centered on the given radius.
        // So we pass radius - effectiveThickness/2.
        float cornerRadius = r - effectiveHalfThickness;

        DrawRoundedCornerOutline(x + r, y + r, cornerRadius, effectiveThickness, MathF.PI, MathF.PI * 1.5f, segments, color); // Top-left
        DrawRoundedCornerOutline(x + width - r, y + r, cornerRadius, effectiveThickness, MathF.PI * 1.5f, MathF.PI * 2, segments, color); // Top-right
        DrawRoundedCornerOutline(x + width - r, y + height - r, cornerRadius, effectiveThickness, 0, MathF.PI * 0.5f, segments, color); // Bottom-right
        DrawRoundedCornerOutline(x + r, y + height - r, cornerRadius, effectiveThickness, MathF.PI * 0.5f, MathF.PI, segments, color); // Bottom-left
    }

    private void DrawRoundedCornerOutline(float cx, float cy, float radius, float thickness, float startAngle, float endAngle, int segments, Color color)
    {
        // Draw arc outline as filled quads (creating a thick arc)
        float halfThickness = thickness / 2;
        float outerRadius = radius + halfThickness;
        float innerRadius = Math.Max(0.1f, radius - halfThickness);

        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + (endAngle - startAngle) * i / segments;
            float angle2 = startAngle + (endAngle - startAngle) * (i + 1) / segments;

            // Outer arc points
            float x1Outer = cx + MathF.Cos(angle1) * outerRadius;
            float y1Outer = cy + MathF.Sin(angle1) * outerRadius;
            float x2Outer = cx + MathF.Cos(angle2) * outerRadius;
            float y2Outer = cy + MathF.Sin(angle2) * outerRadius;

            // Inner arc points
            float x1Inner = cx + MathF.Cos(angle1) * innerRadius;
            float y1Inner = cy + MathF.Sin(angle1) * innerRadius;
            float x2Inner = cx + MathF.Cos(angle2) * innerRadius;
            float y2Inner = cy + MathF.Sin(angle2) * innerRadius;

            // Draw quad segment (two triangles forming the thick arc segment)
            DrawQuad(x1Outer, y1Outer, x2Outer, y2Outer, x2Inner, y2Inner, x1Inner, y1Inner, color);
        }
    }

    private void DrawQuad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, Color color)
    {
        // Draw a quadrilateral as two triangles
        ushort baseIndex = (ushort)_vertices.Count;

        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        // Add 4 vertices for the quad
        _vertices.Add(new Vertex { Position = new Vector2(x1, y1), Color = colorVec });
        _vertices.Add(new Vertex { Position = new Vector2(x2, y2), Color = colorVec });
        _vertices.Add(new Vertex { Position = new Vector2(x3, y3), Color = colorVec });
        _vertices.Add(new Vertex { Position = new Vector2(x4, y4), Color = colorVec });

        // Triangle 1: v0, v1, v2
        _indices.Add(baseIndex);
        _indices.Add((ushort)(baseIndex + 1));
        _indices.Add((ushort)(baseIndex + 2));

        // Triangle 2: v0, v2, v3
        _indices.Add(baseIndex);
        _indices.Add((ushort)(baseIndex + 2));
        _indices.Add((ushort)(baseIndex + 3));
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float len = MathF.Sqrt(dx * dx + dy * dy);

        if (len < 0.001f) return;

        float nx = -dy / len * (thickness / 2);
        float ny = dx / len * (thickness / 2);

        ushort baseIndex = (ushort)_vertices.Count;
        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        _vertices.Add(new Vertex { Position = new Vector2(x1 + nx, y1 + ny), Color = colorVec });
        _vertices.Add(new Vertex { Position = new Vector2(x1 - nx, y1 - ny), Color = colorVec });
        _vertices.Add(new Vertex { Position = new Vector2(x2 - nx, y2 - ny), Color = colorVec });
        _vertices.Add(new Vertex { Position = new Vector2(x2 + nx, y2 + ny), Color = colorVec });

        _indices.Add(baseIndex);
        _indices.Add((ushort)(baseIndex + 1));
        _indices.Add((ushort)(baseIndex + 2));
        _indices.Add(baseIndex);
        _indices.Add((ushort)(baseIndex + 2));
        _indices.Add((ushort)(baseIndex + 3));
    }

    public void DrawCircle(float cx, float cy, float radius, Color color)
    {
        int segments = Math.Max(16, (int)(radius * 2));
        segments = Math.Min(segments, 64);

        ushort baseIndex = (ushort)_vertices.Count;
        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        _vertices.Add(new Vertex { Position = new Vector2(cx, cy), Color = colorVec });

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * MathF.PI * 2;
            float x = cx + MathF.Cos(angle) * radius;
            float y = cy + MathF.Sin(angle) * radius;
            _vertices.Add(new Vertex { Position = new Vector2(x, y), Color = colorVec });
        }

        for (int i = 0; i < segments; i++)
        {
            _indices.Add(baseIndex);
            _indices.Add((ushort)(baseIndex + i + 1));
            _indices.Add((ushort)(baseIndex + i + 2));
        }
    }

    public void DrawCircleOutline(float cx, float cy, float radius, float thickness, Color color)
    {
        int segments = Math.Max(16, (int)(radius * 2));
        segments = Math.Min(segments, 64);

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * MathF.PI * 2;
            float angle2 = (float)(i + 1) / segments * MathF.PI * 2;

            float x1 = cx + MathF.Cos(angle1) * radius;
            float y1 = cy + MathF.Sin(angle1) * radius;
            float x2 = cx + MathF.Cos(angle2) * radius;
            float y2 = cy + MathF.Sin(angle2) * radius;

            DrawLine(x1, y1, x2, y2, thickness, color);
        }
    }

    public void DrawPolygon(List<(float x, float y)> points, Color color)
    {
        if (points.Count < 3) return;

        ushort baseIndex = (ushort)_vertices.Count;
        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        float centerX = 0, centerY = 0;
        foreach (var p in points)
        {
            centerX += p.x;
            centerY += p.y;
        }
        centerX /= points.Count;
        centerY /= points.Count;

        _vertices.Add(new Vertex { Position = new Vector2(centerX, centerY), Color = colorVec });

        foreach (var p in points)
        {
            _vertices.Add(new Vertex { Position = new Vector2(p.x, p.y), Color = colorVec });
        }

        for (int i = 0; i < points.Count; i++)
        {
            _indices.Add(baseIndex);
            _indices.Add((ushort)(baseIndex + i + 1));
            _indices.Add((ushort)(baseIndex + ((i + 1) % points.Count) + 1));
        }
    }

    public IFont LoadFont(byte[] fontData, float fontSize)
    {
        return new OpenGLFontAtlas(_gl, fontData, fontSize);
    }

    public void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Brushes.Brush brush)
        => DrawRoundedRectOutline(x, y, width, height, radius, thickness, brush.PrimaryColor);

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Brushes.Brush brush)
        => DrawLine(x1, y1, x2, y2, thickness, brush.PrimaryColor);

    public void DrawCircle(float cx, float cy, float radius, Brushes.Brush brush)
    {
        if (brush.IsGradient)
            Brushes.BrushRendererExtensions.DrawCircle(this, cx, cy, radius, brush);
        else
            DrawCircle(cx, cy, radius, brush.PrimaryColor);
    }

    public void DrawCircleOutline(float cx, float cy, float radius, float thickness, Brushes.Brush brush)
        => DrawCircleOutline(cx, cy, radius, thickness, brush.PrimaryColor);

    // === Gráficos Vectoriales ===

    public void DrawPath(Graphics.VectorGraphics.VectorPath path, Color color)
    {
        var triangles = Graphics.VectorGraphics.PathTessellator.TessellateFill(path);

        if (triangles.Count < 3)
            return;

        ushort baseIndex = (ushort)_vertices.Count;
        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        foreach (var point in triangles)
        {
            _vertices.Add(new Vertex { Position = point, Color = colorVec });
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            _indices.Add((ushort)(baseIndex + i));
        }
    }

    public void DrawPathStroke(Graphics.VectorGraphics.VectorPath path, Color color, float thickness)
    {
        // ✅ MEJORA: Usar teselador avanzado similar a Skia para suavidad máxima
        var triangles = Graphics.VectorGraphics.PathTessellator.TessellateStrokeAdvanced(path, thickness);

        if (triangles.Count < 3)
            return;

        ushort baseIndex = (ushort)_vertices.Count;
        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        foreach (var point in triangles)
        {
            _vertices.Add(new Vertex { Position = point, Color = colorVec });
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            _indices.Add((ushort)(baseIndex + i));
        }
    }

    public void DrawPathFillAndStroke(Graphics.VectorGraphics.VectorPath path, Color fillColor, Color strokeColor, float strokeWidth)
    {
        DrawPath(path, fillColor);
        DrawPathStroke(path, strokeColor, strokeWidth);
    }

    public void DrawPath(Graphics.VectorGraphics.VectorPath path, Brushes.Brush fillColor)
        => DrawPath(path, fillColor.PrimaryColor);

    public void DrawPathStroke(Graphics.VectorGraphics.VectorPath path, Brushes.Brush strokeColor, float strokeWidth)
        => DrawPathStroke(path, strokeColor.PrimaryColor, strokeWidth);

    public void DrawPathFillAndStroke(Graphics.VectorGraphics.VectorPath path, Brushes.Brush fillColor, Brushes.Brush strokeColor, float strokeWidth)
    {
        DrawPath(path, fillColor.PrimaryColor);
        DrawPathStroke(path, strokeColor.PrimaryColor, strokeWidth);
    }

    public void DrawQuadraticBezier(float startX, float startY, float controlX, float controlY, float endX, float endY, Color color, float thickness = 2f)
    {
        var path = new Graphics.VectorGraphics.VectorPath()
     .MoveTo(startX, startY)
      .QuadraticBezierTo(controlX, controlY, endX, endY);

        DrawPathStroke(path, color, thickness);
    }

    public void DrawCubicBezier(float startX, float startY, float cp1X, float cp1Y, float cp2X, float cp2Y, float endX, float endY, Color color, float thickness = 2f)
    {
        var path = new Graphics.VectorGraphics.VectorPath()
 .MoveTo(startX, startY)
        .CubicBezierTo(cp1X, cp1Y, cp2X, cp2Y, endX, endY);

        DrawPathStroke(path, color, thickness);
    }

    // ── Font-fallback helpers ─────────────────────────────────────────────

    /// <summary>
    /// Resolves the best atlas + glyph info for a given Unicode codepoint.
    /// Tries the primary atlas first, then the emoji fallback atlas.
    /// Returns false only when neither atlas has the glyph.
    /// </summary>
    private bool TryResolveGlyph(int codePoint,
        out OpenGLFontAtlas atlas, out OpenGLFontAtlas.CharInfo info)
    {
        if (_defaultFont != null && _defaultFont.TryGetGlyph(codePoint, out info))
        {
            atlas = _defaultFont;
            return true;
        }

        if (_emojiFallbackFont != null && _emojiFallbackFont.TryGetGlyph(codePoint, out info))
        {
            atlas = _emojiFallbackFont;
            return true;
        }

        atlas = _defaultFont!;
        info  = default;
        return false;
    }

    /// <summary>
    /// Calculates the left-most pixel offset of the text run so the caller can
    /// adjust the starting pen position for proper left-edge alignment.
    /// Handles surrogate pairs and emoji fallback.
    /// </summary>
    private bool TryGetTextBounds(string text, float scale, out float minX)
    {
        minX = float.MaxValue;
        float maxX = float.MinValue;
        float penX = 0;

        for (int i = 0; i < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            if (TryResolveGlyph(codePoint, out var atlas, out var charInfo))
            {
                float x0 = penX + charInfo.OffsetX * scale;
                float x1 = x0 + charInfo.Width * scale;
                minX = Math.Min(minX, x0);
                maxX = Math.Max(maxX, x1);
                penX += charInfo.AdvanceX * scale;
            }
            else if (codePoint == 0x0020) // space
            {
                if (minX == float.MaxValue) minX = penX;
                penX += (_defaultFont?.Size ?? 24) * 0.25f * scale;
                maxX = Math.Max(maxX, penX);
            }
        }

        return minX != float.MaxValue && maxX != float.MinValue;
    }

    /// <summary>
    /// Computes the screen-space top-left Y for a glyph, taking into account that
    /// the glyph may come from a different atlas (e.g. emoji fallback) with its own
    /// ascent metrics. The result places the glyph on the shared baseline <paramref name="baselineY"/>.
    /// </summary>
    private static float GlyphTopY(float baselineY, OpenGLFontAtlas.CharInfo info,
        float scale, OpenGLFontAtlas atlas, float atlasScale)
    {
        // info.OffsetY == -y0 from stbtt_GetCodepointBitmapBox, which is the distance
        // from the baseline UP to the glyph top. Multiply by this atlas's own scale.
        return baselineY - info.OffsetY * atlasScale;
    }

    /// <summary>
    /// Emits two textured triangles for a single glyph quad into the supplied vertex/index lists.
    /// </summary>
    private static void EmitGlyphQuad(
        List<TextVertex> verts, List<ushort> indices,
        float x0, float y0, float x1, float y1,
        float u0, float v0, float u1, float v1,
        Vector4 color)
    {
        ushort bi = (ushort)verts.Count;
        verts.Add(new TextVertex { Position = new Vector2(x0, y0), TexCoord = new Vector2(u0, v0), Color = color });
        verts.Add(new TextVertex { Position = new Vector2(x1, y0), TexCoord = new Vector2(u1, v0), Color = color });
        verts.Add(new TextVertex { Position = new Vector2(x1, y1), TexCoord = new Vector2(u1, v1), Color = color });
        verts.Add(new TextVertex { Position = new Vector2(x0, y1), TexCoord = new Vector2(u0, v1), Color = color });
        indices.Add(bi); indices.Add((ushort)(bi + 1)); indices.Add((ushort)(bi + 2));
        indices.Add(bi); indices.Add((ushort)(bi + 2)); indices.Add((ushort)(bi + 3));
    }

    // ── Public draw methods ───────────────────────────────────────────────

    // Returns the baseline Y for a given top-of-line Y, using the real font ascent.
    // Falls back to 75% of fontSize if no default font is loaded yet.
    private float BaselineY(float topY, float fontSize)
    {
        if (_defaultFont != null && _defaultFont.Size > 0)
        {
            float scale = fontSize / _defaultFont.Size;
            return topY + _defaultFont.Ascent * scale;
        }
        return topY + fontSize * 0.75f;
    }

    public void DrawText(string text, float x, float y, Color color, float fontSize = 24)
    {
        if (_defaultFont == null || string.IsNullOrEmpty(text))
            return;

        float scale = fontSize / _defaultFont.Size;

        float penX = x;
        if (TryGetTextBounds(text, scale, out var minX))
            penX = x - minX;
        float penY = BaselineY(y, fontSize);

        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        // Batch glyphs per-atlas to avoid mid-string texture switches.
        var primaryVerts  = new List<TextVertex>();
        var primaryIdx    = new List<ushort>();
        var fallbackVerts = new List<TextVertex>();
        var fallbackIdx   = new List<ushort>();

        for (int i = 0; i < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            if (!TryResolveGlyph(codePoint, out var atlas, out var charInfo))
            {
                if (codePoint == 0x0020) penX += (_defaultFont.Size * 0.25f * scale);
                continue;
            }

            if (charInfo.Width > 0 && charInfo.Height > 0)
            {
                // Use this glyph's own atlas scale so cross-font baseline alignment is correct.
                float as_ = fontSize / atlas.Size;
                float gx0 = penX + charInfo.OffsetX * as_;
                float gy0 = GlyphTopY(penY, charInfo, scale, atlas, as_);
                float gx1 = gx0 + charInfo.Width  * as_;
                float gy1 = gy0 + charInfo.Height * as_;

                bool isEmoji = atlas == _emojiFallbackFont;
                EmitGlyphQuad(
                    isEmoji ? fallbackVerts : primaryVerts,
                    isEmoji ? fallbackIdx   : primaryIdx,
                    gx0, gy0, gx1, gy1,
                    charInfo.X0, charInfo.Y0, charInfo.X1, charInfo.Y1,
                    colorVec);
            }

            penX += charInfo.AdvanceX * scale;
        }

        // Render primary atlas glyphs into the shared batch.
        foreach (var v in primaryVerts)  _textVertices.Add(v);
        foreach (var idx in primaryIdx)  _textIndices.Add(idx);

        // Render emoji fallback glyphs as a separate immediate batch (different texture).
        if (fallbackVerts.Count > 0 && _emojiFallbackFont != null)
        {
            Flush();
            RenderTextBatch(fallbackVerts, fallbackIdx, _emojiFallbackFont.TextureId);
        }
    }

    public void DrawTextWithFont(string text, float x, float y, Color color, IFont font, float fontSize = 24)
    {
        if (font is not OpenGLFontAtlas fontAtlas || string.IsNullOrEmpty(text))
            return;

        Flush();

        float scale = fontSize / fontAtlas.Size;

        float penX = x;
        if (TryGetTextBounds(text, scale, out var minX))
            penX = x - minX;
        float penY = BaselineY(y, fontSize);

        var primaryVerts  = new List<TextVertex>();
        var primaryIdx    = new List<ushort>();
        var fallbackVerts = new List<TextVertex>();
        var fallbackIdx   = new List<ushort>();

        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        for (int i = 0; i < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            // Try the explicit font first, then the emoji fallback.
            bool found = fontAtlas.TryGetGlyph(codePoint, out var charInfo);
            bool isEmoji = false;

            if (!found && _emojiFallbackFont != null &&
                _emojiFallbackFont.TryGetGlyph(codePoint, out charInfo))
            {
                found   = true;
                isEmoji = true;
            }

            if (!found)
            {
                if (codePoint == 0x0020) penX += fontAtlas.Size * 0.25f * scale;
                continue;
            }

            if (charInfo.Width > 0 && charInfo.Height > 0)
            {
                var   glyphAtlas = isEmoji ? _emojiFallbackFont! : fontAtlas;
                float as_        = fontSize / glyphAtlas.Size;
                float gx0 = penX + charInfo.OffsetX * as_;
                float gy0 = GlyphTopY(penY, charInfo, scale, glyphAtlas, as_);
                float gx1 = gx0 + charInfo.Width  * as_;
                float gy1 = gy0 + charInfo.Height * as_;

                EmitGlyphQuad(
                    isEmoji ? fallbackVerts : primaryVerts,
                    isEmoji ? fallbackIdx   : primaryIdx,
                    gx0, gy0, gx1, gy1,
                    charInfo.X0, charInfo.Y0, charInfo.X1, charInfo.Y1,
                    colorVec);
            }

            penX += charInfo.AdvanceX * scale;
        }

        if (primaryVerts.Count > 0)
            RenderTextBatch(primaryVerts, primaryIdx, fontAtlas.TextureId);

        if (fallbackVerts.Count > 0 && _emojiFallbackFont != null)
            RenderTextBatch(fallbackVerts, fallbackIdx, _emojiFallbackFont.TextureId);
    }

    public void DrawText(string text, float x, float y, Brushes.Brush color, float fontSize = 24)
        => DrawText(text, x, y, color.PrimaryColor, fontSize);

    public void DrawTextWithFont(string text, float x, float y, Brushes.Brush color, IFont font, float fontSize = 24)
        => DrawTextWithFont(text, x, y, color.PrimaryColor, font, fontSize);

    public void DrawTextStyled(string text, float x, float y, Brushes.Brush color, float fontSize, bool isBold, bool isItalic)
    {
        if (_defaultFont == null || string.IsNullOrEmpty(text)) return;

        float scale = fontSize / _defaultFont.Size;
        float penX  = x;
        if (TryGetTextBounds(text, scale, out var minX))
            penX = x - minX;
        float penY = BaselineY(y, fontSize);

        const float italicSkew = 0.25f;
        var colorVec = new Vector4(color.PrimaryColor.R, color.PrimaryColor.G, color.PrimaryColor.B, color.PrimaryColor.A);

        var primaryVerts  = new List<TextVertex>();
        var primaryIdx    = new List<ushort>();
        var fallbackVerts = new List<TextVertex>();
        var fallbackIdx   = new List<ushort>();

        void EmitPass(float startX)
        {
            float px = startX;

            for (int i = 0; i < text.Length; )
            {
                int codePoint = char.ConvertToUtf32(text, i);
                i += char.IsSurrogatePair(text, i) ? 2 : 1;

                if (!TryResolveGlyph(codePoint, out var atlas, out var charInfo))
                {
                    if (codePoint == 0x0020) px += _defaultFont.Size * 0.25f * scale;
                    continue;
                }

                if (charInfo.Width > 0 && charInfo.Height > 0)
                {
                    bool  isEmoji = atlas == _emojiFallbackFont;
                    float as_     = fontSize / atlas.Size;
                    float gx0 = px  + charInfo.OffsetX * as_;
                    float gy0 = GlyphTopY(penY, charInfo, scale, atlas, as_);
                    float gx1 = gx0 + charInfo.Width  * as_;
                    float gy1 = gy0 + charInfo.Height * as_;

                    if (isItalic)
                    {
                        float skewTop    = (penY - gy0) * italicSkew;
                        float skewBottom = (penY - gy1) * italicSkew;
                        gx0 += skewBottom; gx1 += skewBottom;
                        float topSkewDelta = skewTop - skewBottom;

                        var    vl = isEmoji ? fallbackVerts : primaryVerts;
                        var    il = isEmoji ? fallbackIdx   : primaryIdx;
                        ushort bi = (ushort)vl.Count;
                        vl.Add(new TextVertex { Position = new Vector2(gx0 + topSkewDelta, gy0), TexCoord = new Vector2(charInfo.X0, charInfo.Y0), Color = colorVec });
                        vl.Add(new TextVertex { Position = new Vector2(gx1 + topSkewDelta, gy0), TexCoord = new Vector2(charInfo.X1, charInfo.Y0), Color = colorVec });
                        vl.Add(new TextVertex { Position = new Vector2(gx1,                gy1), TexCoord = new Vector2(charInfo.X1, charInfo.Y1), Color = colorVec });
                        vl.Add(new TextVertex { Position = new Vector2(gx0,                gy1), TexCoord = new Vector2(charInfo.X0, charInfo.Y1), Color = colorVec });
                        il.Add(bi); il.Add((ushort)(bi+1)); il.Add((ushort)(bi+2));
                        il.Add(bi); il.Add((ushort)(bi+2)); il.Add((ushort)(bi+3));
                    }
                    else
                    {
                        EmitGlyphQuad(
                            isEmoji ? fallbackVerts : primaryVerts,
                            isEmoji ? fallbackIdx   : primaryIdx,
                            gx0, gy0, gx1, gy1,
                            charInfo.X0, charInfo.Y0, charInfo.X1, charInfo.Y1,
                            colorVec);
                    }
                }

                px += charInfo.AdvanceX * scale;
            }
        }

        EmitPass(penX);
        if (isBold) EmitPass(penX + 1f); // synthetic bold: second pass shifted 1 px right

        // Add primary-font glyphs to the shared batch.
        foreach (var v in primaryVerts)  _textVertices.Add(v);
        foreach (var idx in primaryIdx)  _textIndices.Add(idx);

        // Emoji fallback glyphs need their own immediate draw call (different texture).
        if (fallbackVerts.Count > 0 && _emojiFallbackFont != null)
        {
            Flush();
            RenderTextBatch(fallbackVerts, fallbackIdx, _emojiFallbackFont.TextureId);
        }
    }

    private void RenderTextBatch(List<TextVertex> vertices, List<ushort> indices, uint textureId)
    {
        _gl.UseProgram(_textShaderProgram);

        var projLocation = _gl.GetUniformLocation(_textShaderProgram, "uProjection");
        var proj = _projection;
        _gl.UniformMatrix4(projLocation, 1, false, (float*)&proj);

        var textureLocation = _gl.GetUniformLocation(_textShaderProgram, "uTexture");
        _gl.Uniform1(textureLocation, 0);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, textureId);

        _gl.BindVertexArray(_textVao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _textVbo);
        var transformedVertices = vertices.ToArray();
        ApplyCurrentTransform(transformedVertices);
        fixed (TextVertex* vertexPtr = transformedVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Count * sizeof(TextVertex)), vertexPtr, BufferUsageARB.DynamicDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _textEbo);
        fixed (ushort* indexPtr = indices.ToArray())
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Count * sizeof(ushort)), indexPtr, BufferUsageARB.DynamicDraw);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Count, DrawElementsType.UnsignedShort, null);

        _gl.BindVertexArray(0);
    }

    public Vector2 MeasureText(string text, float fontSize = 24)
    {
        if (_defaultFont == null || string.IsNullOrEmpty(text))
            return Vector2.Zero;

        return MeasureWithFallback(text, _defaultFont, fontSize);
    }

    public Vector2 MeasureTextWithFont(string text, IFont font, float fontSize = 24)
    {
        if (font == null || string.IsNullOrEmpty(text))
            return Vector2.Zero;

        if (font is OpenGLFontAtlas glFont)
            return MeasureWithFallback(text, glFont, fontSize);

        return MeasureText(text, fontSize);
    }

    /// <summary>
    /// Measures text width/height using <paramref name="primaryAtlas"/> with emoji
    /// fallback, honouring full Unicode codepoints (including surrogate pairs).
    /// </summary>
    private Vector2 MeasureWithFallback(string text, OpenGLFontAtlas primaryAtlas, float fontSize)
    {
        float scale      = fontSize / primaryAtlas.Size;
        float penX       = 0;
        float maxHeight  = primaryAtlas.LineHeight * scale;
        float minX       = float.MaxValue;
        float maxX       = float.MinValue;

        OpenGLFontAtlas.CharInfo? lastInfo = null;
        float                     lastAdvance = 0;

        for (int i = 0; i < text.Length; )
        {
            int codePoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            OpenGLFontAtlas.CharInfo info;
            bool found = primaryAtlas.TryGetGlyph(codePoint, out info) ||
                         (_emojiFallbackFont != null && _emojiFallbackFont.TryGetGlyph(codePoint, out info));

            if (found)
            {
                if (lastInfo.HasValue)
                {
                    penX  += lastAdvance;
                }

                lastInfo    = info;
                lastAdvance = info.AdvanceX * scale;

                float x0 = penX + info.OffsetX * scale;
                float x1 = x0  + info.Width   * scale;
                minX = Math.Min(minX, x0);
                maxX = Math.Max(maxX, x1);

                float glyphH = info.Height * scale;
                maxHeight = Math.Max(maxHeight, glyphH);
            }
            else if (codePoint == 0x0020)
            {
                penX += primaryAtlas.Size * 0.25f * scale;
            }
        }

        // Use visual width for the last glyph (not advance).
        if (lastInfo.HasValue)
        {
            float visualW = (lastInfo.Value.OffsetX + lastInfo.Value.Width) * scale;
            maxX = Math.Max(maxX, penX + visualW);
        }

        float width = minX == float.MaxValue ? 0f : Math.Max(0f, maxX - minX);
        return new Vector2(width, maxHeight);
    }

    public ITexture? LoadTexture(string filePath)
    {
        return _textureManager?.LoadTexture(filePath);
    }

    public ITexture? LoadTextureFromStream(Stream stream, string cacheKey)
    {
        return _textureManager?.LoadTextureFromStream(stream, cacheKey);
    }

    public void DrawTexture(ITexture texture, float x, float y, float width, float height, Color? tint = null)
    {
        if (texture is not OpenGLTexture glTexture)
            return;

        Flush();

        var color = tint ?? new Color(1, 1, 1, 1);
        var colorVec = new Vector4(color.R, color.G, color.B, color.A);

        var imageVertices = new List<TextVertex>();
        var imageIndices = new List<ushort>();

        imageVertices.Add(new TextVertex
        {
            Position = new Vector2(x, y),
            TexCoord = new Vector2(0, 0),
            Color = colorVec
        });
        imageVertices.Add(new TextVertex
        {
            Position = new Vector2(x + width, y),
            TexCoord = new Vector2(1, 0),
            Color = colorVec
        });
        imageVertices.Add(new TextVertex
        {
            Position = new Vector2(x + width, y + height),
            TexCoord = new Vector2(1, 1),
            Color = colorVec
        });
        imageVertices.Add(new TextVertex
        {
            Position = new Vector2(x, y + height),
            TexCoord = new Vector2(0, 1),
            Color = colorVec
        });

        imageIndices.Add(0);
        imageIndices.Add(1);
        imageIndices.Add(2);
        imageIndices.Add(0);
        imageIndices.Add(2);
        imageIndices.Add(3);

        ApplyCurrentTransform(imageVertices);

        _gl.UseProgram(_imageShaderProgram);

        int projectionLocation = _gl.GetUniformLocation(_imageShaderProgram, "uProjection");
        var proj = _projection;
        _gl.UniformMatrix4(projectionLocation, 1, false, (float*)&proj);

        int textureLocation = _gl.GetUniformLocation(_imageShaderProgram, "uTexture");
        _gl.Uniform1(textureLocation, 0);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, glTexture.Id);

        _gl.BindVertexArray(_imageVao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _imageVbo);
        fixed (TextVertex* vertPtr = imageVertices.ToArray())
        {
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(imageVertices.Count * sizeof(TextVertex)), vertPtr);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _imageEbo);
        fixed (ushort* idxPtr = imageIndices.ToArray())
        {
            _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, 0, (nuint)(imageIndices.Count * sizeof(ushort)), idxPtr);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)imageIndices.Count, DrawElementsType.UnsignedShort, null);

        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        _gl.UseProgram(0);
    }

    public void PushTransform(Matrix3x2 transform)
    {
        Flush();
        _transformStack.Push(_currentTransform);
        _currentTransform *= transform;
    }

    public void PopTransform()
    {
        Flush();

        if (_transformStack.Count == 0)
            throw new InvalidOperationException("PopTransform called without matching PushTransform");

        _currentTransform = _transformStack.Pop();
    }

    public void PushScissor(float x, float y, float width, float height)
    {
        Flush();

        var scissorBounds = TransformRectToAabb(x, y, width, height);
        x = scissorBounds.x;
        y = scissorBounds.y;
        width = scissorBounds.width;
        height = scissorBounds.height;

        // Clamp width and height to positive values
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        // Improved precision using rounding/floor/ceiling
        int glX = (int)Math.Floor(x);
        // OpenGL Y is from bottom. Top-Left system Y means 'y + height' is the bottom edge.
        int glY = (int)(_viewportHeight - (y + height));
        int glWidth = (int)Math.Ceiling(width);
        int glHeight = (int)Math.Ceiling(height);

        // Implement Intersection with current scissor rect
        if (_scissorStack.Count > 0)
        {
            var parent = _scissorStack.Peek();
            
            // Calculate intersection
            int newX = Math.Max(glX, parent.x);
            int newY = Math.Max(glY, parent.y);
            
            int right = Math.Min(glX + glWidth, parent.x + parent.width);
            int top = Math.Min(glY + glHeight, parent.y + parent.height);
            
            int newWidth = Math.Max(0, right - newX);
            int newHeight = Math.Max(0, top - newY);
            
            glX = newX;
            glY = newY;
            glWidth = newWidth;
            glHeight = newHeight;
        }

        _scissorStack.Push((glX, glY, glWidth, glHeight));

        // Always enable ScissorTest when a scissor rect is active to ensure state consistency
        if (_scissorStack.Count >= 1)
        {
            _gl.Enable(EnableCap.ScissorTest);
        }

        _gl.Scissor(glX, glY, (uint)glWidth, (uint)glHeight);
    }

    public void PopScissor()
    {
        Flush();

        if (_scissorStack.Count == 0)
        {
            throw new InvalidOperationException("PopScissor called without matching PushScissor");
        }

        _scissorStack.Pop();

        if (_scissorStack.Count == 0)
        {
            _gl.Disable(EnableCap.ScissorTest);
        }
        else
        {
            var (x, y, w, h) = _scissorStack.Peek();
            _gl.Scissor(x, y, (uint)w, (uint)h);
        }
    }

    public void PushRoundedClip(float x, float y, float width, float height, float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        Flush();

        width = Math.Max(0, width);
        height = Math.Max(0, height);

        int nextLevel = _roundedClipStack.Count + 1;

        if (_roundedClipStack.Count == 0)
        {
            _gl.Enable(EnableCap.StencilTest);
            _gl.ClearStencil(0);
            _gl.Clear(ClearBufferMask.StencilBufferBit);
        }

        _gl.StencilMask(0xFF);
        _gl.ColorMask(false, false, false, false);

        if (nextLevel == 1)
        {
            _gl.StencilFunc(StencilFunction.Always, nextLevel, 0xFF);
        }
        else
        {
            _gl.StencilFunc(StencilFunction.Equal, nextLevel - 1, 0xFF);
        }

        _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

        var clipPath = Rayo.Rendering.Graphics.VectorGraphics.VectorPath.RoundedRectangle(
            x,
            y,
            width,
            height,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft);

        DrawPath(clipPath, new Color(1, 1, 1, 1));
        Flush();

        _gl.ColorMask(true, true, true, true);
        _gl.StencilFunc(StencilFunction.Equal, nextLevel, 0xFF);
        _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
        _gl.StencilMask(0x00);

        _roundedClipStack.Push(nextLevel);
    }

    public void PopRoundedClip()
    {
        Flush();

        if (_roundedClipStack.Count == 0)
        {
            throw new InvalidOperationException("PopRoundedClip called without matching PushRoundedClip");
        }

        _roundedClipStack.Pop();

        if (_roundedClipStack.Count == 0)
        {
            _gl.Disable(EnableCap.StencilTest);
            _gl.StencilMask(0xFF);
            _gl.StencilFunc(StencilFunction.Always, 0, 0xFF);
        }
        else
        {
            int level = _roundedClipStack.Peek();
            _gl.StencilFunc(StencilFunction.Equal, level, 0xFF);
            _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
            _gl.StencilMask(0x00);
        }
    }

    private Vector2 TransformPoint(float x, float y)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return new Vector2(x, y);

        return Vector2.Transform(new Vector2(x, y), _currentTransform);
    }

    private (float x, float y, float width, float height) TransformRectToAabb(float x, float y, float width, float height)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return (x, y, width, height);

        var p1 = TransformPoint(x, y);
        var p2 = TransformPoint(x + width, y);
        var p3 = TransformPoint(x + width, y + height);
        var p4 = TransformPoint(x, y + height);

        float minX = MathF.Min(MathF.Min(p1.X, p2.X), MathF.Min(p3.X, p4.X));
        float minY = MathF.Min(MathF.Min(p1.Y, p2.Y), MathF.Min(p3.Y, p4.Y));
        float maxX = MathF.Max(MathF.Max(p1.X, p2.X), MathF.Max(p3.X, p4.X));
        float maxY = MathF.Max(MathF.Max(p1.Y, p2.Y), MathF.Max(p3.Y, p4.Y));

        return (minX, minY, maxX - minX, maxY - minY);
    }

    private void ApplyCurrentTransform(Span<Vertex> vertices)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = Vector2.Transform(vertices[i].Position, _currentTransform);
        }
    }

    private void ApplyCurrentTransform(Span<TextVertex> vertices)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Position = Vector2.Transform(vertices[i].Position, _currentTransform);
        }
    }

    private void ApplyCurrentTransform(List<TextVertex> vertices)
    {
        if (_currentTransform == Matrix3x2.Identity || vertices.Count == 0)
            return;

        for (int i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            vertex.Position = Vector2.Transform(vertex.Position, _currentTransform);
            vertices[i] = vertex;
        }
    }

    private void Flush()
    {
        // Enforce Scissor Test state if stack is active
        // This ensures that even if state was lost (e.g. by another operation), it is restored before drawing
        if (_scissorStack.Count > 0)
        {
            _gl.Enable(EnableCap.ScissorTest);
            
            // ✅ FIX: Restore the scissor box as well, in case it was modified or reset
            var (x, y, w, h) = _scissorStack.Peek();
            _gl.Scissor(x, y, (uint)w, (uint)h);
        }

  // Renderizar rectángulos
  if (_vertices.Count > 0)
        {
          _gl.UseProgram(_shaderProgram);

  int projectionLocation = _gl.GetUniformLocation(_shaderProgram, "uProjection");
         var proj = _projection;
            _gl.UniformMatrix4(projectionLocation, 1, false, (float*)&proj);

     _gl.BindVertexArray(_vao);

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            var transformedVertices = _vertices.ToArray();
            ApplyCurrentTransform(transformedVertices);
   fixed (Vertex* vertPtr = transformedVertices)
    {
 _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_vertices.Count * sizeof(Vertex)), vertPtr);
            }

  _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (ushort* idxPtr = _indices.ToArray())
         {
 _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, 0, (nuint)(_indices.Count * sizeof(ushort)), idxPtr);
  }

  _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count, DrawElementsType.UnsignedShort, null);

            _gl.BindVertexArray(0);
      _gl.UseProgram(0);

       _vertices.Clear();
      _indices.Clear();
        }

        // Renderizar texto
        if (_textVertices.Count > 0 && _defaultFont != null)
        {
            _gl.UseProgram(_textShaderProgram);

            int projectionLocation = _gl.GetUniformLocation(_textShaderProgram, "uProjection");
            var proj = _projection;
            _gl.UniformMatrix4(projectionLocation, 1, false, (float*)&proj);

            int textureLocation = _gl.GetUniformLocation(_textShaderProgram, "uTexture");
          _gl.Uniform1(textureLocation, 0);

            _gl.ActiveTexture(TextureUnit.Texture0);
     _gl.BindTexture(TextureTarget.Texture2D, _defaultFont.TextureId);

     _gl.BindVertexArray(_textVao);

     _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _textVbo);
            var transformedTextVertices = _textVertices.ToArray();
            ApplyCurrentTransform(transformedTextVertices);
            fixed (TextVertex* vertPtr = transformedTextVertices)
        {
   _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_textVertices.Count * sizeof(TextVertex)), vertPtr);
     }

_gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _textEbo);
     fixed (ushort* idxPtr = _textIndices.ToArray())
            {
        _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, 0, (nuint)(_textIndices.Count * sizeof(ushort)), idxPtr);
            }

            _gl.DrawElements(PrimitiveType.Triangles, (uint)_textIndices.Count, DrawElementsType.UnsignedShort, null);

            _gl.BindVertexArray(0);
            _gl.BindTexture(TextureTarget.Texture2D, 0);
            _gl.UseProgram(0);

            _textVertices.Clear();
    _textIndices.Clear();
        }
    }

    // === Render-to-Texture Implementation ===

    private uint _currentFBO = 0;
    private Stack<uint> _fboStack = new();

    public ITexture CreateRenderTarget(int width, int height)
    {
        // Crear framebuffer object (FBO)
        uint fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

     // Crear textura para color attachment
        uint textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureId);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, 
        (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        
        // Attach textura al framebuffer
     _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
TextureTarget.Texture2D, textureId, 0);

        // Verificar que el framebuffer está completo
        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
   throw new Exception($"Framebuffer incompleto: {status}");
        }

     // Volver al framebuffer por defecto
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return new OpenGLRenderTargetTexture(textureId, fbo, width, height, _gl);
    }

    public void BeginRenderToTexture(ITexture target)
    {
        if (target is not OpenGLRenderTargetTexture renderTarget)
        {
 throw new ArgumentException("Texture must be a render target created by CreateRenderTarget");
        }

        // Flush cualquier renderizado pendiente alFramebuffer actual
        Flush();

      // Guardar el FBO actual
        _fboStack.Push(_currentFBO);

     // Cambiar al nuevo FBO
        _currentFBO = renderTarget.FBO;
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _currentFBO);

   // Actualizar viewport
        _gl.Viewport(0, 0, (uint)renderTarget.Width, (uint)renderTarget.Height);

   // Actualizar matriz de proyección para el nuevo tamaño
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, renderTarget.Width, renderTarget.Height, 0, -1, 1);
}

    public void EndRenderToTexture()
    {
        // Flush todo lo renderizado a la textura
        Flush();

        // Restaurar el FBO anterior
        if (_fboStack.Count == 0)
        {
            throw new InvalidOperationException("EndRenderToTexture called without matching BeginRenderToTexture");
        }

        _currentFBO = _fboStack.Pop();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _currentFBO);

        // ✅ CRÍTICO: Restaurar viewport con WIDTH y HEIGHT correctos
        _gl.Viewport(0, 0, _viewportWidth, _viewportHeight);
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, _viewportWidth, _viewportHeight, 0, -1, 1);
    }

    public bool IsRenderingToTexture => _currentFBO != 0;

    public ITexture CreateTextureFromPixels(byte[] rgbaPixels, int width, int height)
    {
        // Delegates to the existing OpenGLTexture constructor that uploads pixel data.
        return new OpenGLTexture(_gl, width, height, rgbaPixels, TextureFormat.RGBA8);
    }

    public void Dispose()
    {
        _defaultFont?.Dispose();
        _textureManager?.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteProgram(_shaderProgram);
        _gl.DeleteVertexArray(_textVao);
        _gl.DeleteBuffer(_textVbo);
        _gl.DeleteBuffer(_textEbo);
        _gl.DeleteProgram(_textShaderProgram);
        _gl.DeleteVertexArray(_imageVao);
        _gl.DeleteBuffer(_imageVbo);
        _gl.DeleteBuffer(_imageEbo);
        _gl.DeleteProgram(_imageShaderProgram);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct Vertex
{
  public Vector2 Position;
    public Vector4 Color;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TextVertex
{
    public Vector2 Position;
    public Vector2 TexCoord;
    public Vector4 Color;
}