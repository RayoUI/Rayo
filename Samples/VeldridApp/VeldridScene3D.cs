namespace VeldridApp;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Veldrid;
using Veldrid.SPIRV;

/// <summary>
/// Encapsulates all Veldrid 3D state for the rotating cube scene.
/// Runs entirely on a dedicated background thread to avoid interfering with
/// the UI renderer's graphics context.
/// </summary>
internal sealed class VeldridScene3D : IDisposable
{
    // ── Veldrid resources ─────────────────────────────────────────────────────

    private GraphicsDevice  _gd        = null!;
    private ResourceFactory _rf        = null!;
    private CommandList     _cl        = null!;
    private DeviceBuffer    _vb        = null!;
    private DeviceBuffer    _ib        = null!;
    private DeviceBuffer    _ub        = null!;
    private Shader[]        _shaders   = Array.Empty<Shader>();
    private Pipeline        _pipeline  = null!;
    private ResourceLayout  _resLayout = null!;
    private ResourceSet     _resSet    = null!;
    private Texture         _colorTex  = null!;
    private Texture         _depthTex  = null!;
    private Texture?        _stagingTex;
    private Framebuffer     _fb        = null!;
    private int             _renderW, _renderH;

    // ── Threading ─────────────────────────────────────────────────────────────

    private bool                          _disposed;
    private readonly Thread               _gpuThread;
    private readonly ManualResetEventSlim _initDone        = new(false);
    private Exception?                    _initError;
    private volatile bool                 _stopRequested;
    private int                           _reqW, _reqH;
    private byte[]?                       _framePixels;
    private readonly SemaphoreSlim        _renderRequested = new(0, 1);
    private readonly SemaphoreSlim        _renderComplete  = new(0, 1);

    // ── Animation state ───────────────────────────────────────────────────────

    private float _rotationX, _rotationY, _rotationZ;

    // ── Configurable properties ───────────────────────────────────────────────

    public bool    Animate        { get; set; } = true;
    public float   AnimationSpeed { get; set; } = 1.0f;
    public Vector3 CubeColor      { get; set; } = new(0.2f, 0.6f, 1.0f);

    public Vector3 Rotation
    {
        get => new(_rotationX, _rotationY, _rotationZ);
        set { _rotationX = value.X; _rotationY = value.Y; _rotationZ = value.Z; }
    }

    // ── Uniform buffer layout (matches GLSL struct, 240 bytes) ────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct SceneUniforms
    {
        public Matrix4x4 Model;       //  64 bytes
        public Matrix4x4 View;        //  64 bytes
        public Matrix4x4 Projection;  //  64 bytes
        public Vector4   Color;       //  16 bytes  (xyz = color, w unused)
        public Vector4   LightPos;    //  16 bytes  (xyz = position, w unused)
        public Vector4   ViewPos;     //  16 bytes  (xyz = position, w unused)
    }                                 // = 240 bytes total

    // ── GLSL shaders (Vulkan 4.50, cross-compiled by Veldrid.SPIRV) ──────────

    private const string VertSrc = @"
#version 450
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(set = 0, binding = 0) uniform SceneUniforms {
    mat4 Model;
    mat4 View;
    mat4 Projection;
    vec4 Color;
    vec4 LightPos;
    vec4 ViewPos;
};
layout(location = 0) out vec3 vNormal;
layout(location = 1) out vec3 vFragPos;
layout(location = 2) out vec3 vColor;
layout(location = 3) out vec3 vLightPos;
layout(location = 4) out vec3 vViewPos;
void main() {
    vFragPos    = vec3(Model * vec4(Position, 1.0));
    vNormal     = mat3(transpose(inverse(Model))) * Normal;
    gl_Position = Projection * View * vec4(vFragPos, 1.0);
    vColor    = Color.xyz;
    vLightPos = LightPos.xyz;
    vViewPos  = ViewPos.xyz;
}";

    private const string FragSrc = @"
#version 450
layout(location = 0) in vec3 vNormal;
layout(location = 1) in vec3 vFragPos;
layout(location = 2) in vec3 vColor;
layout(location = 3) in vec3 vLightPos;
layout(location = 4) in vec3 vViewPos;
layout(location = 0) out vec4 FragColor;
void main() {
    vec3 ambient  = 0.3 * vec3(1.0);
    vec3 norm     = normalize(vNormal);
    vec3 lightDir = normalize(vLightPos - vFragPos);
    vec3 diffuse  = max(dot(norm, lightDir), 0.0) * vec3(1.0);
    vec3 viewDir  = normalize(vViewPos - vFragPos);
    vec3 reflDir  = reflect(-lightDir, norm);
    vec3 specular = 0.5 * pow(max(dot(viewDir, reflDir), 0.0), 32.0) * vec3(1.0);
    FragColor = vec4((ambient + diffuse + specular) * vColor, 1.0);
}";

    // ── Cube geometry (pos + normal per vertex, same layout as OpenGLScene) ───

    private static readonly float[] CubeVertices =
    {
        // pos (x,y,z)          normal (nx,ny,nz)
        // Front (Z+)
        -0.5f, -0.5f,  0.5f,   0f,  0f,  1f,
         0.5f, -0.5f,  0.5f,   0f,  0f,  1f,
         0.5f,  0.5f,  0.5f,   0f,  0f,  1f,
        -0.5f,  0.5f,  0.5f,   0f,  0f,  1f,
        // Back (Z-)
         0.5f, -0.5f, -0.5f,   0f,  0f, -1f,
        -0.5f, -0.5f, -0.5f,   0f,  0f, -1f,
        -0.5f,  0.5f, -0.5f,   0f,  0f, -1f,
         0.5f,  0.5f, -0.5f,   0f,  0f, -1f,
        // Top (Y+)
        -0.5f,  0.5f,  0.5f,   0f,  1f,  0f,
         0.5f,  0.5f,  0.5f,   0f,  1f,  0f,
         0.5f,  0.5f, -0.5f,   0f,  1f,  0f,
        -0.5f,  0.5f, -0.5f,   0f,  1f,  0f,
        // Bottom (Y-)
        -0.5f, -0.5f, -0.5f,   0f, -1f,  0f,
         0.5f, -0.5f, -0.5f,   0f, -1f,  0f,
         0.5f, -0.5f,  0.5f,   0f, -1f,  0f,
        -0.5f, -0.5f,  0.5f,   0f, -1f,  0f,
        // Right (X+)
         0.5f, -0.5f,  0.5f,   1f,  0f,  0f,
         0.5f, -0.5f, -0.5f,   1f,  0f,  0f,
         0.5f,  0.5f, -0.5f,   1f,  0f,  0f,
         0.5f,  0.5f,  0.5f,   1f,  0f,  0f,
        // Left (X-)
        -0.5f, -0.5f, -0.5f,  -1f,  0f,  0f,
        -0.5f, -0.5f,  0.5f,  -1f,  0f,  0f,
        -0.5f,  0.5f,  0.5f,  -1f,  0f,  0f,
        -0.5f,  0.5f, -0.5f,  -1f,  0f,  0f,
    };

    private static readonly ushort[] CubeIndices =
    {
         0,  1,  2,  0,  2,  3,  // Front
         4,  5,  6,  4,  6,  7,  // Back
         8,  9, 10,  8, 10, 11,  // Top
        12, 13, 14, 12, 14, 15,  // Bottom
        16, 17, 18, 16, 18, 19,  // Right
        20, 21, 22, 20, 22, 23,  // Left
    };

    // ── Constructor ───────────────────────────────────────────────────────────

    public VeldridScene3D()
    {
        _gpuThread = new Thread(GpuThreadMain) { IsBackground = true, Name = "Veldrid Worker" };
        _gpuThread.Start();
        if (!_initDone.Wait(15_000))
            throw new TimeoutException("Veldrid initialisation timed out after 15 s.");
        if (_initError != null)
            throw new InvalidOperationException("Veldrid initialisation failed.", _initError);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Advances the rotation by <paramref name="deltaTime"/> seconds.</summary>
    public void Tick(float deltaTime)
    {
        if (!Animate) return;
        float speed = deltaTime * AnimationSpeed;
        _rotationY += speed;
        _rotationX += speed * 0.5f;
        _rotationZ += speed * 0.25f;
    }

    /// <summary>
    /// Requests a frame at the given pixel dimensions. Blocks until the GPU thread
    /// completes the render and returns the RGBA8 pixel array (top-left origin).
    /// </summary>
    public byte[] RenderFrame(int width, int height)
    {
        if (width <= 0 || height <= 0 || _stopRequested) return Array.Empty<byte>();
        _reqW = width;
        _reqH = height;
        _renderRequested.Release();
        _renderComplete.Wait();
        return _framePixels ?? Array.Empty<byte>();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stopRequested = true;
        _renderRequested.Release();   // unblock the GPU thread if it is waiting
        _gpuThread.Join(5_000);
        _renderRequested.Dispose();
        _renderComplete.Dispose();
        _initDone.Dispose();
    }

    // ── GPU thread ────────────────────────────────────────────────────────────

    private void GpuThreadMain()
    {
        try   { GpuInit(); _initDone.Set(); }
        catch (Exception ex) { _initError = ex; _initDone.Set(); return; }

        while (!_stopRequested)
        {
            _renderRequested.Wait();
            if (_stopRequested) break;

            int w = _reqW, h = _reqH;
            try
            {
                EnsureFramebufferSized(w, h);
                DrawFrame(w, h);
                _framePixels = ReadPixels(w, h);
            }
            catch { _framePixels = null; }
            finally { _renderComplete.Release(); }
        }

        GpuDispose();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private void GpuInit()
    {
        var opts = new GraphicsDeviceOptions
        {
            Debug                             = false,
            HasMainSwapchain                  = false,
            PreferDepthRangeZeroToOne         = true,
            PreferStandardClipSpaceYDirection = true,
            SyncToVerticalBlank               = false,
        };

        // Prefer D3D11 on Windows (lowest overhead, no extra drivers needed).
        // Fall back to Vulkan on Linux/macOS or if D3D11 is unavailable.
        if (OperatingSystem.IsWindows() && GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11))
            _gd = GraphicsDevice.CreateD3D11(opts);
        else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
            _gd = GraphicsDevice.CreateVulkan(opts);
        else
            throw new PlatformNotSupportedException(
                "VeldridApp requires Direct3D11 (Windows) or Vulkan. " +
                "Neither backend is available on this system.");

        _rf = _gd.ResourceFactory;

        // Static geometry buffers
        _vb = _rf.CreateBuffer(new BufferDescription(
            (uint)(CubeVertices.Length * sizeof(float)), BufferUsage.VertexBuffer));
        _gd.UpdateBuffer(_vb, 0, CubeVertices);

        _ib = _rf.CreateBuffer(new BufferDescription(
            (uint)(CubeIndices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
        _gd.UpdateBuffer(_ib, 0, CubeIndices);

        // Uniform buffer — updated every frame
        _ub = _rf.CreateBuffer(new BufferDescription(
            (uint)Unsafe.SizeOf<SceneUniforms>(),
            BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        // Shaders: GLSL 4.50 → SPIR-V → target backend (D3D11 HLSL / Vulkan SPIR-V)
        _shaders = _rf.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex,   Encoding.UTF8.GetBytes(VertSrc), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragSrc), "main"));

        // Resource layout: one uniform buffer visible from vertex + fragment stages
        _resLayout = _rf.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription(
                "SceneUniforms",
                ResourceKind.UniformBuffer,
                ShaderStages.Vertex | ShaderStages.Fragment)));

        _resSet = _rf.CreateResourceSet(new ResourceSetDescription(_resLayout, _ub));

        _cl = _rf.CreateCommandList();

        // Bootstrap a 1×1 framebuffer so we have an OutputDescription for pipeline creation.
        // The actual size is set on each render request via EnsureFramebufferSized().
        EnsureFramebufferSized(1, 1);

        // Vertex layout: float3 Position + float3 Normal (stride = 24 bytes)
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Normal",   VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

        _pipeline = _rf.CreateGraphicsPipeline(new GraphicsPipelineDescription
        {
            BlendState        = BlendStateDescription.SingleOverrideBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled:  true,
                depthWriteEnabled: true,
                comparisonKind:    ComparisonKind.Less),
            RasterizerState   = new RasterizerStateDescription(
                cullMode:           FaceCullMode.None,
                fillMode:           PolygonFillMode.Solid,
                frontFace:          FrontFace.Clockwise,
                depthClipEnabled:   true,
                scissorTestEnabled: false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts   = new[] { _resLayout },
            ShaderSet         = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders:       _shaders),
            Outputs           = _fb.OutputDescription,
        });
    }

    // ── Per-frame resize / render / readback ──────────────────────────────────

    private void EnsureFramebufferSized(int w, int h)
    {
        if (_renderW == w && _renderH == h) return;

        _fb?.Dispose();
        _colorTex?.Dispose();
        _depthTex?.Dispose();

        _renderW = w;
        _renderH = h;

        _colorTex = _rf.CreateTexture(TextureDescription.Texture2D(
            (uint)w, (uint)h, 1, 1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.RenderTarget | TextureUsage.Sampled));

        _depthTex = _rf.CreateTexture(TextureDescription.Texture2D(
            (uint)w, (uint)h, 1, 1,
            PixelFormat.D24_UNorm_S8_UInt,
            TextureUsage.DepthStencil));

        _fb = _rf.CreateFramebuffer(new FramebufferDescription(_depthTex, _colorTex));
    }

    private void DrawFrame(int w, int h)
    {
        var cameraPos = new Vector3(0f, 0f, 5f);
        float aspect  = (float)w / h;

        // .NET Matrix4x4 is row-major; GLSL mat4 is column-major.
        // Copying raw bytes gives GLSL Transpose(M_net) = M_col — correct for
        // column-vector MVP:  gl_Position = Projection * View * Model * pos
        var model = Matrix4x4.CreateRotationX(_rotationX)
                  * Matrix4x4.CreateRotationY(_rotationY)
                  * Matrix4x4.CreateRotationZ(_rotationZ);
        var view  = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
        var proj  = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 180f * 45f, aspect, 0.1f, 100f);

        var uniforms = new SceneUniforms
        {
            Model      = model,
            View       = view,
            Projection = proj,
            Color      = new Vector4(CubeColor, 1f),
            LightPos   = new Vector4(2f, 2f, 2f, 0f),
            ViewPos    = new Vector4(cameraPos, 0f),
        };
        _gd.UpdateBuffer(_ub, 0, uniforms);

        _cl.Begin();
        _cl.SetFramebuffer(_fb);
        _cl.SetViewport(0, new Viewport(0f, 0f, (float)w, (float)h, 0f, 1f));
        _cl.ClearColorTarget(0, new RgbaFloat(
            CubeColor.X * 0.15f, CubeColor.Y * 0.15f, CubeColor.Z * 0.15f, 1f));
        _cl.ClearDepthStencil(1f);
        _cl.SetPipeline(_pipeline);
        _cl.SetGraphicsResourceSet(0, _resSet);
        _cl.SetVertexBuffer(0, _vb);
        _cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
        _cl.DrawIndexed((uint)CubeIndices.Length);
        _cl.End();

        _gd.SubmitCommands(_cl);
        _gd.WaitForIdle();
    }

    private unsafe byte[] ReadPixels(int w, int h)
    {
        // Lazily create / resize the staging texture
        if (_stagingTex == null
            || _stagingTex.Width  != (uint)w
            || _stagingTex.Height != (uint)h)
        {
            _stagingTex?.Dispose();
            _stagingTex = _rf.CreateTexture(TextureDescription.Texture2D(
                (uint)w, (uint)h, 1, 1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Staging));
        }

        // Copy render target into the CPU-accessible staging texture
        _cl.Begin();
        _cl.CopyTexture(_colorTex, _stagingTex);
        _cl.End();
        _gd.SubmitCommands(_cl);
        _gd.WaitForIdle();

        // Map and read rows (accounting for possible row-pitch padding)
        var  mapped   = _gd.Map(_stagingTex, MapMode.Read, 0);
        uint rowBytes = (uint)w * 4;
        var  result   = new byte[w * h * 4];

        fixed (byte* dstPtr = result)
        {
            byte* srcPtr = (byte*)mapped.Data;
            for (int row = 0; row < h; row++)
            {
                Buffer.MemoryCopy(
                    srcPtr + (long)row * mapped.RowPitch,
                    dstPtr + (long)row * rowBytes,
                    rowBytes, rowBytes);
            }
        }

        _gd.Unmap(_stagingTex, 0);
        return result;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void GpuDispose()
    {
        _stagingTex?.Dispose();
        _fb?.Dispose();
        _colorTex?.Dispose();
        _depthTex?.Dispose();
        _resSet?.Dispose();
        _resLayout?.Dispose();
        _pipeline?.Dispose();
        foreach (var s in _shaders) s?.Dispose();
        _ub?.Dispose();
        _ib?.Dispose();
        _vb?.Dispose();
        _cl?.Dispose();
        _gd?.Dispose();
    }
}
