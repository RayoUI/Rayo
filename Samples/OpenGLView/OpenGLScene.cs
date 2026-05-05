namespace OpenGLApp;

using Silk.NET.OpenGL;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Encapsulates all OpenGL 3D state for the rotating cube scene.
/// Managed by <see cref="OpenGLView"/> and isolated from Rayo layout concerns.
/// </summary>
internal sealed class OpenGLScene : IDisposable
{
    // GL resources
    private GL?  _gl;
    private uint _vao, _vbo, _ebo, _shaderProgram;
    private bool _initialized;

    // Framebuffer object
    private uint _fbo, _fboTexture, _rbo;
    private int  _fboWidth, _fboHeight;

    // Pixel staging buffers (SkiaSharp bridge)
    private byte[]? _pixelBuffer;
    private byte[]? _flippedPixelBuffer;

    // Cube geometry (built once in the constructor)
    private readonly float[] _cubeVertices;
    private readonly uint[]  _cubeIndices;

    private static readonly int[][] FaceVertexIndices =
    {
        new[] {  0,  1,  2,  3 }, // Front  (Z+)
        new[] {  4,  5,  6,  7 }, // Back   (Z-)
        new[] {  8,  9, 10, 11 }, // Top    (Y+)
        new[] { 12, 13, 14, 15 }, // Bottom (Y-)
        new[] { 16, 17, 18, 19 }, // Right  (X+)
        new[] { 20, 21, 22, 23 }, // Left   (X-)
    };

    // Animation state
    private float _rotationX, _rotationY, _rotationZ;

    // ── Configurable state (set by OpenGLView) ────────────────────────────────

    public bool    Animate         { get; set; } = true;
    public float   AnimationSpeed  { get; set; } = 1.0f;
    public Vector3 CubeColor       { get; set; } = new(0.2f, 0.6f, 1.0f);
    public Vector3 CameraPosition  { get; set; } = new(0, 0, 5);
    public float   Fov             { get; set; } = 45.0f;

    public Vector3 Rotation
    {
        get => new(_rotationX, _rotationY, _rotationZ);
        set { _rotationX = value.X; _rotationY = value.Y; _rotationZ = value.Z; }
    }

    // Exposed so OpenGLView can wrap the texture without owning its lifetime
    public uint FboTexture => _fboTexture;
    public int  FboWidth   => _fboWidth;
    public int  FboHeight  => _fboHeight;

    // ── Constructor ───────────────────────────────────────────────────────────

    public OpenGLScene()
    {
        _cubeVertices = BuildCubeVertices();
        _cubeIndices  = BuildCubeIndices();
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    /// <summary>Advances the rotation by <paramref name="deltaTime"/> seconds.</summary>
    public void Tick(float deltaTime)
    {
        if (!Animate) return;
        float speed  = deltaTime * AnimationSpeed;
        _rotationY  += speed;
        _rotationX  += speed * 0.5f;
        _rotationZ  += speed * 0.25f;
    }

    // ── GL rendering ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises GL resources on first call, resizes the FBO when dimensions change,
    /// then renders the cube scene into the FBO.
    /// </summary>
    public void RenderToFBO(GL gl, int width, int height)
    {
        EnsureInitialized(gl);
        EnsureFramebuffer(gl, width, height);
        DrawToFBO(gl);
    }

    /// <summary>
    /// Reads the current FBO pixels into a vertically-flipped byte array for SkiaSharp.
    /// Must be called after <see cref="RenderToFBO"/>.
    /// </summary>
    public byte[]? ReadPixels(GL gl)
    {
        EnsurePixelBuffers();
        if (_pixelBuffer == null || _flippedPixelBuffer == null) return null;

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        unsafe
        {
            fixed (byte* ptr = _pixelBuffer)
            {
                gl.ReadPixels(0, 0, (uint)_fboWidth, (uint)_fboHeight,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        FlipPixelBuffer();
        return _flippedPixelBuffer;
    }

    // ── Software fallback ─────────────────────────────────────────────────────

    /// <summary>
    /// CPU-side software rasterisation of the cube. Used when no OpenGL context
    /// is available (e.g. SkiaSharp renderer without GL access).
    /// </summary>
    public void RenderSoftwareFallback(
        IRenderer renderer,
        float contentX, float contentY,
        float contentWidth, float contentHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0) return;

        renderer.PushScissor(contentX, contentY, contentWidth, contentHeight);

        float aspect     = contentWidth / contentHeight;
        var   projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 180f * Fov, aspect, 0.1f, 100f);
        var   view       = Matrix4x4.CreateLookAt(CameraPosition, Vector3.Zero, Vector3.UnitY);
        var   model      = Matrix4x4.Identity
                         * Matrix4x4.CreateRotationX(_rotationX)
                         * Matrix4x4.CreateRotationY(_rotationY)
                         * Matrix4x4.CreateRotationZ(_rotationZ);
        var   lightDir   = Vector3.Normalize(new Vector3(2f, 2f, 2f));

        var faces = new List<(List<(float x, float y)> pts, float depth, Color color)>(
            FaceVertexIndices.Length);

        foreach (var faceIndices in FaceVertexIndices)
        {
            var     projected = new List<(float x, float y)>(faceIndices.Length);
            float   depthSum  = 0f;
            Vector3 normalSum = Vector3.Zero;
            bool    skip      = false;

            foreach (int idx in faceIndices)
            {
                if (!TryProject(GetPosition(idx), model, view, projection, out var ndc))
                {
                    skip = true;
                    break;
                }
                float sx = contentX + ((ndc.X + 1f) * 0.5f) * contentWidth;
                float sy = contentY + (1f - (ndc.Y + 1f) * 0.5f) * contentHeight;
                projected.Add((sx, sy));
                depthSum  += ndc.Z;
                normalSum += Vector3.Normalize(Vector3.TransformNormal(GetNormal(idx), model));
            }

            if (skip) continue;

            Vector3 avgNormal = Vector3.Normalize(normalSum / faceIndices.Length);
            float   intensity = 0.25f + MathF.Max(Vector3.Dot(avgNormal, lightDir), 0f) * 0.75f;
            var     color     = new Color(
                Math.Clamp(CubeColor.X * intensity, 0f, 1f),
                Math.Clamp(CubeColor.Y * intensity, 0f, 1f),
                Math.Clamp(CubeColor.Z * intensity, 0f, 1f));

            faces.Add((projected, depthSum / faceIndices.Length, color));
        }

        faces.Sort((a, b) => b.depth.CompareTo(a.depth));

        foreach (var (pts, _, color) in faces)
        {
            renderer.DrawPolygon(pts, color);
            for (int i = 0; i < pts.Count; i++)
            {
                var s = pts[i];
                var e = pts[(i + 1) % pts.Count];
                renderer.DrawLine(s.x, s.y, e.x, e.y, 1.5f, new Color(0f, 0f, 0f, 0.35f));
            }
        }

        renderer.PopScissor();
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_gl == null) return;
        if (_fbo         != 0) _gl.DeleteFramebuffer(_fbo);
        if (_fboTexture  != 0) _gl.DeleteTexture(_fboTexture);
        if (_rbo         != 0) _gl.DeleteRenderbuffer(_rbo);
        if (_vao         != 0) _gl.DeleteVertexArray(_vao);
        if (_vbo         != 0) _gl.DeleteBuffer(_vbo);
        if (_ebo         != 0) _gl.DeleteBuffer(_ebo);
        if (_shaderProgram != 0) _gl.DeleteProgram(_shaderProgram);
    }

    // ── Private GL helpers ────────────────────────────────────────────────────

    private void EnsureInitialized(GL gl)
    {
        if (_initialized) return;
        _gl = gl;
        CompileShaders(gl);
        UploadBuffers(gl);
        _initialized = true;
    }

    private void EnsureFramebuffer(GL gl, int width, int height)
    {
        if (_fbo != 0 && _fboWidth == width && _fboHeight == height) return;

        if (_fbo != 0)
        {
            gl.DeleteFramebuffer(_fbo);
            gl.DeleteTexture(_fboTexture);
            gl.DeleteRenderbuffer(_rbo);
        }

        _fboWidth  = width;
        _fboHeight = height;

        _fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        _fboTexture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _fboTexture);
        unsafe
        {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.BindTexture(TextureTarget.Texture2D, 0);

        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _fboTexture, 0);

        _rbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
            InternalFormat.Depth24Stencil8, (uint)width, (uint)height);
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);

        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            throw new Exception("Framebuffer is not complete.");

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void DrawToFBO(GL gl)
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        gl.Viewport(0, 0, (uint)_fboWidth, (uint)_fboHeight);
        gl.Enable(EnableCap.DepthTest);
        gl.ClearColor(CubeColor.X * 0.2f, CubeColor.Y * 0.2f, CubeColor.Z * 0.2f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        gl.UseProgram(_shaderProgram);

        float aspect     = (float)_fboWidth / _fboHeight;
        var   projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 180.0f * Fov, aspect, 0.1f, 100.0f);
        var   view       = Matrix4x4.CreateLookAt(CameraPosition, Vector3.Zero, Vector3.UnitY);
        var   model      = Matrix4x4.Identity
                         * Matrix4x4.CreateRotationX(_rotationX)
                         * Matrix4x4.CreateRotationY(_rotationY)
                         * Matrix4x4.CreateRotationZ(_rotationZ);

        SetUniform(gl, "uModel",      model);
        SetUniform(gl, "uView",       view);
        SetUniform(gl, "uProjection", projection);
        SetUniform(gl, "uColor",      CubeColor);
        SetUniform(gl, "uLightPos",   new Vector3(2, 2, 2));
        SetUniform(gl, "uViewPos",    CameraPosition);

        gl.BindVertexArray(_vao);
        unsafe { gl.DrawElements(PrimitiveType.Triangles, (uint)_cubeIndices.Length, DrawElementsType.UnsignedInt, null); }
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private void CompileShaders(GL gl)
    {
        const string VertSrc = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
uniform mat4 uModel; uniform mat4 uView; uniform mat4 uProjection;
out vec3 vNormal; out vec3 vFragPos;
void main() {
    vFragPos    = vec3(uModel * vec4(aPosition, 1.0));
    vNormal     = mat3(transpose(inverse(uModel))) * aNormal;
    gl_Position = uProjection * uView * vec4(vFragPos, 1.0);
}";
        const string FragSrc = @"
#version 330 core
in vec3 vNormal; in vec3 vFragPos;
uniform vec3 uColor; uniform vec3 uLightPos; uniform vec3 uViewPos;
out vec4 FragColor;
void main() {
    vec3 ambient  = 0.3 * vec3(1.0);
    vec3 norm     = normalize(vNormal);
    vec3 lightDir = normalize(uLightPos - vFragPos);
    vec3 diffuse  = max(dot(norm, lightDir), 0.0) * vec3(1.0);
    vec3 viewDir  = normalize(uViewPos - vFragPos);
    vec3 reflDir  = reflect(-lightDir, norm);
    vec3 specular = 0.5 * pow(max(dot(viewDir, reflDir), 0.0), 32.0) * vec3(1.0);
    FragColor = vec4((ambient + diffuse + specular) * uColor, 1.0);
}";

        uint vert = CompileShader(gl, ShaderType.VertexShader,   VertSrc, "vertex");
        uint frag = CompileShader(gl, ShaderType.FragmentShader, FragSrc, "fragment");

        _shaderProgram = gl.CreateProgram();
        gl.AttachShader(_shaderProgram, vert);
        gl.AttachShader(_shaderProgram, frag);
        gl.LinkProgram(_shaderProgram);

        gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out int linked);
        if (linked == 0)
            throw new Exception($"3D shader linking failed: {gl.GetProgramInfoLog(_shaderProgram)}");

        gl.DeleteShader(vert);
        gl.DeleteShader(frag);
    }

    private static uint CompileShader(GL gl, ShaderType type, string source, string label)
    {
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int ok);
        if (ok == 0)
            throw new Exception($"3D {label} shader compilation failed: {gl.GetShaderInfoLog(shader)}");
        return shader;
    }

    private unsafe void UploadBuffers(GL gl)
    {
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* v = _cubeVertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(_cubeVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* i = _cubeIndices)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(_cubeIndices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);

        // position (location 0)
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);
        // normal (location 1)
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
    }

    private void SetUniform(GL gl, string name, Matrix4x4 m)
    {
        int loc = gl.GetUniformLocation(_shaderProgram, name);
        unsafe { gl.UniformMatrix4(loc, 1, false, (float*)&m); }
    }

    private void SetUniform(GL gl, string name, Vector3 v)
    {
        int loc = gl.GetUniformLocation(_shaderProgram, name);
        gl.Uniform3(loc, v.X, v.Y, v.Z);
    }

    // ── Pixel buffer helpers ──────────────────────────────────────────────────

    private void EnsurePixelBuffers()
    {
        int size = _fboWidth * _fboHeight * 4;
        if (size <= 0) return;
        if (_pixelBuffer        == null || _pixelBuffer.Length        != size) _pixelBuffer        = new byte[size];
        if (_flippedPixelBuffer == null || _flippedPixelBuffer.Length != size) _flippedPixelBuffer = new byte[size];
    }

    private void FlipPixelBuffer()
    {
        if (_pixelBuffer == null || _flippedPixelBuffer == null) return;
        int stride = _fboWidth * 4;
        for (int row = 0; row < _fboHeight; row++)
            System.Buffer.BlockCopy(_pixelBuffer, row * stride,
                _flippedPixelBuffer, (_fboHeight - 1 - row) * stride, stride);
    }

    // ── Vertex helpers (software fallback) ───────────────────────────────────

    private Vector3 GetPosition(int idx)
    {
        int i = idx * 6;
        return new(_cubeVertices[i], _cubeVertices[i + 1], _cubeVertices[i + 2]);
    }

    private Vector3 GetNormal(int idx)
    {
        int i = idx * 6;
        return new(_cubeVertices[i + 3], _cubeVertices[i + 4], _cubeVertices[i + 5]);
    }

    private static bool TryProject(
        Vector3 pos,
        in Matrix4x4 model, in Matrix4x4 view, in Matrix4x4 projection,
        out Vector3 ndc)
    {
        var worldPos = Vector3.Transform(pos, model);
        var viewPos  = Vector3.Transform(worldPos, view);
        var clip     = Vector4.Transform(new Vector4(viewPos, 1f), projection);

        if (MathF.Abs(clip.W) < 1e-5f) { ndc = Vector3.Zero; return false; }

        float invW = 1f / clip.W;
        ndc = new(clip.X * invW, clip.Y * invW, clip.Z * invW);
        return true;
    }

    // ── Cube geometry ─────────────────────────────────────────────────────────

    private static float[] BuildCubeVertices() => new float[]
    {
        // pos (x,y,z)          normal (nx,ny,nz)
        // Front (Z+)
        -0.5f, -0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,   0.0f,  0.0f,  1.0f,
        // Back (Z-)
         0.5f, -0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
        -0.5f, -0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
        -0.5f,  0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
         0.5f,  0.5f, -0.5f,   0.0f,  0.0f, -1.0f,
        // Top (Y+)
        -0.5f,  0.5f,  0.5f,   0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,   0.0f,  1.0f,  0.0f,
         0.5f,  0.5f, -0.5f,   0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,   0.0f,  1.0f,  0.0f,
        // Bottom (Y-)
        -0.5f, -0.5f, -0.5f,   0.0f, -1.0f,  0.0f,
         0.5f, -0.5f, -0.5f,   0.0f, -1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,   0.0f, -1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,   0.0f, -1.0f,  0.0f,
        // Right (X+)
         0.5f, -0.5f,  0.5f,   1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,   1.0f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,   1.0f,  0.0f,  0.0f,
         0.5f,  0.5f,  0.5f,   1.0f,  0.0f,  0.0f,
        // Left (X-)
        -0.5f, -0.5f, -0.5f,  -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,  -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f,  0.5f,  -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  -1.0f,  0.0f,  0.0f,
    };

    private static uint[] BuildCubeIndices() => new uint[]
    {
         0,  1,  2,  0,  2,  3,  // Front
         4,  5,  6,  4,  6,  7,  // Back
         8,  9, 10,  8, 10, 11,  // Top
        12, 13, 14, 12, 14, 15,  // Bottom
        16, 17, 18, 16, 18, 19,  // Right
        20, 21, 22, 20, 22, 23,  // Left
    };
}
