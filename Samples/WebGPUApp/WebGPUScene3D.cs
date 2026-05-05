namespace WebGPUApp;

using Silk.NET.Core.Native;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Self-contained WebGPU scene that renders a rotating Lambert-shaded cube to an
/// off-screen RGBA8 texture and exposes the pixels via <see cref="RenderFrame"/>.
/// <para>
/// Owns its own wgpu <see cref="Device"/> — entirely independent of Rayo's renderer.
/// The caller (WebGPUView) composites the returned pixel buffer into the UI via
/// <see cref="Rayo.Rendering.IRenderer.CreateTextureFromPixels"/>.
/// </para>
/// </summary>
internal sealed unsafe class WebGPUScene3D : IDisposable
{
    // Vertex: vec3 pos (12 bytes) + vec4 color (16 bytes) = 28 bytes
    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    // ── Callback result structs ───────────────────────────────────────────────
    // Stack-allocated and passed as void* userdata to synchronous wgpu callbacks.

    [StructLayout(LayoutKind.Sequential)]
    private struct AdapterResult
    {
        public Adapter*             Adapter;
        public RequestAdapterStatus Status;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceResult
    {
        public Device*             Device;
        public RequestDeviceStatus Status;
    }

    // ── Static callbacks (UnmanagedCallersOnly — no GC allocation, no marshaling) ─

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnAdapterRequested(
        RequestAdapterStatus status, Adapter* adapter, byte* msg, void* userdata)
    {
        var r = (AdapterResult*)userdata;
        r->Status  = status;
        r->Adapter = adapter;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnDeviceRequested(
        RequestDeviceStatus status, Device* device, byte* msg, void* userdata)
    {
        var r = (DeviceResult*)userdata;
        r->Status = status;
        r->Device = device;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnBufferMapped(BufferMapAsyncStatus status, void* userdata)
    {
        *(BufferMapAsyncStatus*)userdata = status;
    }

    // ── Configurable state ────────────────────────────────────────────────────

    public bool    Animate        { get; set; } = true;
    public float   AnimationSpeed { get; set; } = 1.0f;

    public Vector3 CubeColor
    {
        get => _cubeColor;
        set { _cubeColor = value; _colorDirty = true; }
    }

    public Vector3 Rotation
    {
        get => new(_rotX, _rotY, _rotZ);
        set { _rotX = value.X; _rotY = value.Y; _rotZ = value.Z; }
    }

    // ── Animation state ───────────────────────────────────────────────────────

    private float   _rotX, _rotY, _rotZ;
    private Vector3 _cubeColor  = new(0.2f, 0.6f, 1.0f);
    private bool    _colorDirty = true;
    private bool    _disposed;

    // ── Cube geometry (24 unique vertices, 6 faces × 4) ──────────────────────

    private static readonly int[][] FaceVertexIndices =
    {
        new[] {  0,  1,  2,  3 }, // Front  (Z+)
        new[] {  4,  5,  6,  7 }, // Back   (Z-)
        new[] {  8,  9, 10, 11 }, // Top    (Y+)
        new[] { 12, 13, 14, 15 }, // Bottom (Y-)
        new[] { 16, 17, 18, 19 }, // Right  (X+)
        new[] { 20, 21, 22, 23 }, // Left   (X-)
    };

    private static readonly Vector3[] FaceNormals =
    {
        new( 0,  0,  1), new( 0,  0, -1),
        new( 0,  1,  0), new( 0, -1,  0),
        new( 1,  0,  0), new(-1,  0,  0),
    };

    private static readonly Vector3[] Positions =
    {
        // Front (Z+)
        new(-0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f,  0.5f),
        new( 0.5f,  0.5f,  0.5f), new(-0.5f,  0.5f,  0.5f),
        // Back (Z-)
        new( 0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f),
        new(-0.5f,  0.5f, -0.5f), new( 0.5f,  0.5f, -0.5f),
        // Top (Y+)
        new(-0.5f,  0.5f,  0.5f), new( 0.5f,  0.5f,  0.5f),
        new( 0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f, -0.5f),
        // Bottom (Y-)
        new(-0.5f, -0.5f, -0.5f), new( 0.5f, -0.5f, -0.5f),
        new( 0.5f, -0.5f,  0.5f), new(-0.5f, -0.5f,  0.5f),
        // Right (X+)
        new( 0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f, -0.5f),
        new( 0.5f,  0.5f, -0.5f), new( 0.5f,  0.5f,  0.5f),
        // Left (X-)
        new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f,  0.5f),
        new(-0.5f,  0.5f,  0.5f), new(-0.5f,  0.5f, -0.5f),
    };

    // ── WebGPU handles ────────────────────────────────────────────────────────

    private WebGPU  _wgpu    = null!;  // assigned in GpuInit() before first use
    private Wgpu?   _wgpuExt;

    private Instance* _instance;
    private Adapter*  _adapter;
    private Device*   _device;
    private Queue*    _queue;

    // Static pipeline objects (created once)
    private ShaderModule*    _shaderModule;
    private BindGroupLayout* _bgl;
    private PipelineLayout*  _pipelineLayout;
    private RenderPipeline*  _pipeline;

    // Geometry buffers (recreated when CubeColor changes)
    private Buffer* _vertexBuffer;
    private Buffer* _indexBuffer;
    private uint    _indexCount;

    // Uniform buffer and bind group (reused every frame, updated via QueueWriteBuffer)
    private Buffer*    _uniformBuffer;
    private BindGroup* _uniformBindGroup;

    // Per-frame render targets (lazily resized on viewport change)
    private Texture*     _renderTexture;
    private TextureView* _renderView;
    private Texture*     _depthTexture;
    private TextureView* _depthView;
    private Buffer*      _outputBuffer;
    private int          _rtWidth, _rtHeight;
    private uint         _bytesPerRow;   // aligned to 256 for CopyTextureToBuffer

    // ── Threading ─────────────────────────────────────────────────────────────
    // All wgpu handles live and are used exclusively on _gpuThread.
    // This prevents wgpu-native from interfering with the UI thread's GL context.

    private readonly Thread               _gpuThread;
    private readonly ManualResetEventSlim _initDone  = new(false);
    private Exception?                    _initError;
    private volatile bool                 _stopRequested;

    // Render request channel: UI thread writes _reqW/_reqH then releases
    // _renderRequested; wgpu thread wakes, renders, stores _framePixels,
    // then releases _renderComplete.
    private int     _reqW, _reqH;
    private byte[]? _framePixels;
    private readonly SemaphoreSlim _renderRequested = new(0, 1);
    private readonly SemaphoreSlim _renderComplete  = new(0, 1);

    // ── WGSL shader ───────────────────────────────────────────────────────────
    // MVP is uploaded pre-transposed so WGSL's column-major mat4x4<f32>
    // matches .NET's row-major Matrix4x4 (see UpdateUniforms).

    private const string Wgsl = @"
struct Uniforms { mvp : mat4x4<f32> }
@group(0) @binding(0) var<uniform> uniforms : Uniforms;

struct VertIn  { @location(0) pos : vec3<f32>, @location(1) col : vec4<f32> }
struct VertOut { @builtin(position) pos : vec4<f32>, @location(0) col : vec4<f32> }

@vertex fn vs_main(v : VertIn) -> VertOut {
    var o : VertOut;
    o.pos = uniforms.mvp * vec4<f32>(v.pos, 1.0);
    o.col = v.col;
    return o;
}

@fragment fn fs_main(v : VertOut) -> @location(0) vec4<f32> { return v.col; }
";

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the dedicated wgpu worker thread and blocks until GPU initialisation
    /// completes. Running wgpu on its own thread prevents it from interfering with
    /// any OpenGL context that the UI renderer holds on the calling thread.
    /// </summary>
    public WebGPUScene3D()
    {
        _gpuThread = new Thread(GpuThreadMain)
        {
            IsBackground = true,
            Name         = "WebGPU Worker",
        };
        _gpuThread.Start();

        if (!_initDone.Wait(15_000))
            throw new TimeoutException("WebGPU initialisation timed out after 15 s.");
        if (_initError != null)
            throw new InvalidOperationException("WebGPU initialisation failed.", _initError);
    }

    // ── GPU worker thread ─────────────────────────────────────────────────────

    private void GpuThreadMain()
    {
        try
        {
            GpuInit();
            _initDone.Set();
        }
        catch (Exception ex)
        {
            _initError = ex;
            _initDone.Set();
            GpuDispose(); // release any handles that were allocated before the failure
            return;
        }

        // Render loop: wait for requests from the UI thread.
        while (!_stopRequested)
        {
            _renderRequested.Wait();
            if (_stopRequested) break;

            int w = _reqW, h = _reqH;
            try
            {
                if (_colorDirty) RebuildVertexBuffer();
                EnsureRenderTargets(w, h);
                UpdateUniforms(w, h);
                SubmitRenderPass(w, h);
                _framePixels = ReadbackPixels(w, h);
            }
            catch
            {
                _framePixels = null;
            }
            _renderComplete.Release();
        }

        GpuDispose();
    }

    private void GpuInit()
    {
        _wgpu = WebGPU.GetApi();
        CreateInstance();
        RequestAdapter();
        RequestDevice();

        if (!_wgpu.TryGetDeviceExtension(_device, out Wgpu? ext) || ext is null)
            throw new InvalidOperationException(
                "WGPU device extension unavailable. " +
                "Ensure Silk.NET.WebGPU.Extensions.WGPU is referenced.");
        _wgpuExt = ext;

        CreatePipeline();
        CreateUniformBuffer();
        RebuildVertexBuffer();
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    public void Tick(float deltaTime)
    {
        if (!Animate) return;
        float s = deltaTime * AnimationSpeed;
        _rotY += s;
        _rotX += s * 0.5f;
        _rotZ += s * 0.25f;
    }

    // ── Frame rendering ───────────────────────────────────────────────────────

    /// <summary>
    /// Renders the 3D scene at <paramref name="width"/> × <paramref name="height"/>
    /// and returns the result as raw RGBA8 bytes. Synchronous — blocks until the GPU
    /// has finished and the pixel buffer has been read back to the CPU.
    /// </summary>
    /// <summary>
    /// Sends a render request to the wgpu worker thread and returns the RGBA8 pixel
    /// buffer once the GPU has finished. Blocks the calling thread briefly while the
    /// GPU works, but does NOT hold any OpenGL context.
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

    // ── Device setup ──────────────────────────────────────────────────────────

    private void CreateInstance()
    {
        var desc = new InstanceDescriptor();
        _instance = _wgpu.CreateInstance(&desc);
        if (_instance == null)
            throw new InvalidOperationException("Failed to create WebGPU instance.");
    }

    private void RequestAdapter()
    {
        var opts = new RequestAdapterOptions
        {
            CompatibleSurface    = null,
            PowerPreference      = PowerPreference.HighPerformance,
            ForceFallbackAdapter = false,
        };

        AdapterResult result = default;
        _wgpu.InstanceRequestAdapter(_instance, &opts,
            new PfnRequestAdapterCallback(&OnAdapterRequested), &result);

        if (result.Status != RequestAdapterStatus.Success || result.Adapter == null)
            throw new InvalidOperationException(
                $"WebGPU adapter request failed ({result.Status}). " +
                "Check that a compatible GPU driver is installed.");
        _adapter = result.Adapter;
    }

    private void RequestDevice()
    {
        var desc   = new DeviceDescriptor();
        DeviceResult result = default;
        _wgpu.AdapterRequestDevice(_adapter, &desc,
            new PfnRequestDeviceCallback(&OnDeviceRequested), &result);

        if (result.Status != RequestDeviceStatus.Success || result.Device == null)
            throw new InvalidOperationException(
                $"WebGPU device request failed ({result.Status}).");
        _device = result.Device;
        _queue  = _wgpu.DeviceGetQueue(_device);
    }

    // ── Pipeline ──────────────────────────────────────────────────────────────

    private void CreatePipeline()
    {
        // ── Shader module (WGSL) ─────────────────────────────────────────────
        var shaderBytes = Encoding.UTF8.GetBytes(Wgsl + "\0");
        fixed (byte* pShader = shaderBytes)
        {
            var wgslDesc = new ShaderModuleWGSLDescriptor
            {
                // SType 6 = WGPUSType_ShaderModuleWGSLDescriptor (WebGPU spec constant)
                Chain = new ChainedStruct { SType = (SType)6 },
                Code  = pShader,
            };
            var smDesc = new ShaderModuleDescriptor
            {
                NextInChain = (ChainedStruct*)&wgslDesc,
            };
            _shaderModule = _wgpu.DeviceCreateShaderModule(_device, &smDesc);
        }

        // ── Bind group layout — one uniform buffer at binding 0 (vertex stage) ─
        var bglEntry = new BindGroupLayoutEntry
        {
            Binding    = 0u,
            Visibility = ShaderStage.Vertex,
            Buffer     = new BufferBindingLayout
            {
                Type           = BufferBindingType.Uniform,
                MinBindingSize = 64ul,
            },
        };
        var bglDesc = new BindGroupLayoutDescriptor
        {
            EntryCount = 1u,
            Entries    = &bglEntry,
        };
        _bgl = _wgpu.DeviceCreateBindGroupLayout(_device, &bglDesc);

        // ── Pipeline layout ───────────────────────────────────────────────────
        var bgl    = _bgl;
        var plDesc = new PipelineLayoutDescriptor
        {
            BindGroupLayoutCount = 1u,
            BindGroupLayouts     = &bgl,
        };
        _pipelineLayout = _wgpu.DeviceCreatePipelineLayout(_device, &plDesc);

        // ── Render pipeline ───────────────────────────────────────────────────
        var vsEntry = SilkMarshal.StringToPtr("vs_main", NativeStringEncoding.UTF8);
        var fsEntry = SilkMarshal.StringToPtr("fs_main", NativeStringEncoding.UTF8);
        try
        {
            // Vertex layout: slot 0 = (vec3 pos @ location 0, vec4 col @ location 1)
            var attribs = stackalloc VertexAttribute[2];
            attribs[0] = new VertexAttribute
            {
                Format        = VertexFormat.Float32x3,
                Offset        = 0ul,
                ShaderLocation = 0u,
            };
            attribs[1] = new VertexAttribute
            {
                Format        = VertexFormat.Float32x4,
                Offset        = 12ul,
                ShaderLocation = 1u,
            };

            var vbLayout = new VertexBufferLayout
            {
                ArrayStride    = 28ul,   // sizeof(Vertex) = 12 + 16
                StepMode       = VertexStepMode.Vertex,
                AttributeCount = 2u,
                Attributes     = attribs,
            };

            var vertexState = new VertexState
            {
                Module      = _shaderModule,
                EntryPoint  = (byte*)vsEntry,
                BufferCount = 1u,
                Buffers     = &vbLayout,
            };

            // Opaque blending (src=1, dst=0)
            var blendNone = new BlendState
            {
                Color = new BlendComponent
                {
                    Operation = BlendOperation.Add,
                    SrcFactor = BlendFactor.One,
                    DstFactor = BlendFactor.Zero,
                },
                Alpha = new BlendComponent
                {
                    Operation = BlendOperation.Add,
                    SrcFactor = BlendFactor.One,
                    DstFactor = BlendFactor.Zero,
                },
            };

            var colorTarget = new ColorTargetState
            {
                Format    = TextureFormat.Rgba8Unorm,
                Blend     = &blendNone,
                WriteMask = ColorWriteMask.All,
            };

            var fragState = new FragmentState
            {
                Module      = _shaderModule,
                EntryPoint  = (byte*)fsEntry,
                TargetCount = 1u,
                Targets     = &colorTarget,
            };

            // Depth-stencil: depth test + write, stencil pass-through
            var dsState = new DepthStencilState
            {
                Format            = TextureFormat.Depth24PlusStencil8,
                DepthWriteEnabled = true,
                DepthCompare      = CompareFunction.Less,
                StencilFront = new StencilFaceState
                {
                    Compare      = CompareFunction.Always,
                    FailOp       = StencilOperation.Keep,
                    DepthFailOp  = StencilOperation.Keep,
                    PassOp       = StencilOperation.Keep,
                },
                StencilBack = new StencilFaceState
                {
                    Compare      = CompareFunction.Always,
                    FailOp       = StencilOperation.Keep,
                    DepthFailOp  = StencilOperation.Keep,
                    PassOp       = StencilOperation.Keep,
                },
                StencilReadMask  = 0xFFFFFFFF,
                StencilWriteMask = 0u,
            };

            var rpDesc = new RenderPipelineDescriptor
            {
                Layout    = _pipelineLayout,
                Vertex    = vertexState,
                Primitive = new PrimitiveState
                {
                    Topology         = PrimitiveTopology.TriangleList,
                    StripIndexFormat = IndexFormat.Undefined,
                    FrontFace        = FrontFace.Ccw,
                    CullMode         = CullMode.None,
                },
                DepthStencil = &dsState,
                Multisample  = new MultisampleState
                {
                    Count                  = 1u,
                    Mask                   = uint.MaxValue,
                    AlphaToCoverageEnabled = false,
                },
                Fragment = &fragState,
            };

            _pipeline = _wgpu.DeviceCreateRenderPipeline(_device, &rpDesc);
        }
        finally
        {
            SilkMarshal.Free(vsEntry);
            SilkMarshal.Free(fsEntry);
        }
    }

    private void CreateUniformBuffer()
    {
        // 64 bytes = one 4×4 float matrix (the row-major MVP, transposed before upload)
        var bufDesc = new BufferDescriptor
        {
            Usage            = BufferUsage.Uniform | BufferUsage.CopyDst,
            Size             = 64ul,
            MappedAtCreation = false,
        };
        _uniformBuffer = _wgpu.DeviceCreateBuffer(_device, &bufDesc);

        // Bind group pointing at the uniform buffer — reused every frame
        var bgEntry = new BindGroupEntry
        {
            Binding = 0u,
            Buffer  = _uniformBuffer,
            Offset  = 0ul,
            Size    = 64ul,
        };
        var bgDesc = new BindGroupDescriptor
        {
            Layout     = _bgl,
            EntryCount = 1u,
            Entries    = &bgEntry,
        };
        _uniformBindGroup = _wgpu.DeviceCreateBindGroup(_device, &bgDesc);
    }

    // ── Vertex baking (Lambert per face) ─────────────────────────────────────

    private void RebuildVertexBuffer()
    {
        _colorDirty = false;

        // Asymmetric light so each of the three visible faces gets a distinct brightness.
        // (2,2,2) is perfectly diagonal → all three forward-facing axes get identical
        // Lambert value, making the cube look flat/isometric.
        var lightDir = Vector3.Normalize(new Vector3(1f, 2f, 3f));
        var verts    = new Vertex[24];

        for (int face = 0; face < 6; face++)
        {
            float intensity = 0.25f + MathF.Max(Vector3.Dot(FaceNormals[face], lightDir), 0f) * 0.75f;
            var   color     = new Vector4(
                Math.Clamp(_cubeColor.X * intensity, 0f, 1f),
                Math.Clamp(_cubeColor.Y * intensity, 0f, 1f),
                Math.Clamp(_cubeColor.Z * intensity, 0f, 1f),
                1.0f);

            foreach (int vi in FaceVertexIndices[face])
                verts[vi] = new Vertex { Position = Positions[vi], Color = color };
        }

        // 6 faces × 2 triangles × 3 indices = 36
        var indices = new ushort[36];
        int n = 0;
        foreach (var fi in FaceVertexIndices)
        {
            indices[n++] = (ushort)fi[0]; indices[n++] = (ushort)fi[1]; indices[n++] = (ushort)fi[2];
            indices[n++] = (ushort)fi[0]; indices[n++] = (ushort)fi[2]; indices[n++] = (ushort)fi[3];
        }
        _indexCount = (uint)indices.Length;

        // Release old buffers
        if (_vertexBuffer != null) { _wgpu.BufferRelease(_vertexBuffer); _vertexBuffer = null; }
        if (_indexBuffer  != null) { _wgpu.BufferRelease(_indexBuffer);  _indexBuffer  = null; }

        uint vbSize = (uint)(24 * sizeof(Vertex));
        var vbDesc = new BufferDescriptor
        {
            Usage            = BufferUsage.Vertex | BufferUsage.CopyDst,
            Size             = vbSize,
            MappedAtCreation = false,
        };
        _vertexBuffer = _wgpu.DeviceCreateBuffer(_device, &vbDesc);

        uint ibSize = (uint)(36 * sizeof(ushort));
        var ibDesc = new BufferDescriptor
        {
            Usage            = BufferUsage.Index | BufferUsage.CopyDst,
            Size             = ibSize,
            MappedAtCreation = false,
        };
        _indexBuffer = _wgpu.DeviceCreateBuffer(_device, &ibDesc);

        fixed (Vertex* pV = verts)
            _wgpu.QueueWriteBuffer(_queue, _vertexBuffer, 0ul, pV, vbSize);
        fixed (ushort* pI = indices)
            _wgpu.QueueWriteBuffer(_queue, _indexBuffer, 0ul, pI, ibSize);
    }

    // ── Off-screen resource management ───────────────────────────────────────

    private void EnsureRenderTargets(int w, int h)
    {
        if (_rtWidth == w && _rtHeight == h && _renderTexture != null) return;

        DestroyRenderTargets();
        _rtWidth  = w;
        _rtHeight = h;

        // bytesPerRow must be a multiple of 256 for CopyTextureToBuffer
        _bytesPerRow = ((uint)w * 4u + 255u) & ~255u;

        // Color render texture — RGBA8, render target + copy source for readback
        var colDesc = new TextureDescriptor
        {
            Usage         = TextureUsage.RenderAttachment | TextureUsage.CopySrc,
            Dimension     = TextureDimension.Dimension2D,
            Size          = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1u },
            Format        = TextureFormat.Rgba8Unorm,
            MipLevelCount = 1u,
            SampleCount   = 1u,
        };
        _renderTexture = _wgpu.DeviceCreateTexture(_device, &colDesc);
        _renderView    = _wgpu.TextureCreateView(_renderTexture, null);

        // Depth+stencil texture
        var depDesc = new TextureDescriptor
        {
            Usage         = TextureUsage.RenderAttachment,
            Dimension     = TextureDimension.Dimension2D,
            Size          = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1u },
            Format        = TextureFormat.Depth24PlusStencil8,
            MipLevelCount = 1u,
            SampleCount   = 1u,
        };
        _depthTexture = _wgpu.DeviceCreateTexture(_device, &depDesc);
        _depthView    = _wgpu.TextureCreateView(_depthTexture, null);

        // CPU-readable output buffer (COPY_DST + MAP_READ)
        var outDesc = new BufferDescriptor
        {
            Usage            = BufferUsage.CopyDst | BufferUsage.MapRead,
            Size             = _bytesPerRow * (uint)h,
            MappedAtCreation = false,
        };
        _outputBuffer = _wgpu.DeviceCreateBuffer(_device, &outDesc);
    }

    private void DestroyRenderTargets()
    {
        if (_outputBuffer  != null) { _wgpu.BufferRelease(_outputBuffer);    _outputBuffer  = null; }
        if (_depthView     != null) { _wgpu.TextureViewRelease(_depthView);  _depthView     = null; }
        if (_depthTexture  != null) { _wgpu.TextureRelease(_depthTexture);   _depthTexture  = null; }
        if (_renderView    != null) { _wgpu.TextureViewRelease(_renderView); _renderView    = null; }
        if (_renderTexture != null) { _wgpu.TextureRelease(_renderTexture);  _renderTexture = null; }
        _rtWidth = _rtHeight = 0;
    }

    // ── Per-frame GPU work ────────────────────────────────────────────────────

    private void UpdateUniforms(int w, int h)
    {
        float aspect = w / (float)h;
        var proj  = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, aspect, 0.1f, 100f);
        var view  = Matrix4x4.CreateLookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);
        var model = Matrix4x4.CreateRotationX(_rotX)
                  * Matrix4x4.CreateRotationY(_rotY)
                  * Matrix4x4.CreateRotationZ(_rotZ);
        var mvp = model * view * proj;

        // .NET Matrix4x4 is row-major (rows in memory). WGSL reads memory as columns,
        // so the bytes land transposed — exactly what the column-vector shader needs.
        // Do NOT call Transpose() here; uploading raw is already correct.
        _wgpu.QueueWriteBuffer(_queue, _uniformBuffer, 0ul, &mvp, 64);
    }

    private void SubmitRenderPass(int w, int h)
    {
        var encDesc = new CommandEncoderDescriptor();
        var encoder = _wgpu.DeviceCreateCommandEncoder(_device, &encDesc);

        // Color attachment — clear to dark navy
        var colorAttach = new RenderPassColorAttachment
        {
            View          = _renderView,
            ResolveTarget = null,
            LoadOp        = LoadOp.Clear,
            StoreOp       = StoreOp.Store,
            ClearValue    = new Color { R = 0.07, G = 0.08, B = 0.15, A = 1.0 },
        };

        // Depth-stencil attachment
        var depthAttach = new RenderPassDepthStencilAttachment
        {
            View              = _depthView,
            DepthLoadOp       = LoadOp.Clear,
            DepthStoreOp      = StoreOp.Discard,
            DepthClearValue   = 1.0f,
            StencilLoadOp     = LoadOp.Clear,
            StencilStoreOp    = StoreOp.Discard,
            StencilClearValue = 0u,
        };

        var rpDesc = new RenderPassDescriptor
        {
            ColorAttachmentCount   = 1u,
            ColorAttachments       = &colorAttach,
            DepthStencilAttachment = &depthAttach,
        };

        var rp = _wgpu.CommandEncoderBeginRenderPass(encoder, &rpDesc);
        _wgpu.RenderPassEncoderSetPipeline(rp, _pipeline);
        _wgpu.RenderPassEncoderSetBindGroup(rp, 0u, _uniformBindGroup, 0u, null);
        _wgpu.RenderPassEncoderSetVertexBuffer(rp, 0u, _vertexBuffer, 0ul, (ulong)(24 * sizeof(Vertex)));
        _wgpu.RenderPassEncoderSetIndexBuffer(rp, _indexBuffer, IndexFormat.Uint16, 0ul, (ulong)(36 * sizeof(ushort)));
        _wgpu.RenderPassEncoderDrawIndexed(rp, _indexCount, 1u, 0u, 0, 0u);
        _wgpu.RenderPassEncoderEnd(rp);
        _wgpu.RenderPassEncoderRelease(rp);

        // Copy render texture → output buffer for CPU readback
        var srcTex = new ImageCopyTexture
        {
            Texture  = _renderTexture,
            MipLevel = 0u,
            Origin   = new Origin3D { X = 0u, Y = 0u, Z = 0u },
            Aspect   = TextureAspect.All,
        };
        var dstBuf = new ImageCopyBuffer
        {
            Layout = new TextureDataLayout
            {
                Offset       = 0ul,
                BytesPerRow  = _bytesPerRow,
                RowsPerImage = (uint)h,
            },
            Buffer = _outputBuffer,
        };
        var extent = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1u };
        _wgpu.CommandEncoderCopyTextureToBuffer(encoder, &srcTex, &dstBuf, &extent);

        var cmdDesc = new CommandBufferDescriptor();
        var cmdBuf  = _wgpu.CommandEncoderFinish(encoder, &cmdDesc);
        _wgpu.CommandEncoderRelease(encoder);

        _wgpu.QueueSubmit(_queue, 1u, &cmdBuf);
        _wgpu.CommandBufferRelease(cmdBuf);
    }

    private byte[] ReadbackPixels(int w, int h)
    {
        nuint bufSize = (nuint)(_bytesPerRow * (uint)h);

        // Start async map — wgpu-native calls the callback synchronously during DevicePoll
        BufferMapAsyncStatus mapStatus = default;
        _wgpu.BufferMapAsync(_outputBuffer, MapMode.Read, 0u, bufSize,
            new PfnBufferMapCallback(&OnBufferMapped), &mapStatus);

        // Block until all submitted GPU work is complete and the callback has fired
        _wgpuExt!.DevicePoll(_device, true, null);

        if (mapStatus != BufferMapAsyncStatus.Success)
            throw new InvalidOperationException($"WebGPU buffer map failed: {mapStatus}");

        var pData  = (byte*)_wgpu.BufferGetMappedRange(_outputBuffer, 0u, bufSize);
        var pixels = new byte[w * h * 4];

        // Copy row by row — bytesPerRow may include padding to satisfy 256-byte alignment
        for (int row = 0; row < h; row++)
            Marshal.Copy((nint)(pData + row * _bytesPerRow), pixels, row * w * 4, w * 4);

        _wgpu.BufferUnmap(_outputBuffer);
        return pixels;
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Signal the worker thread to stop, then wait for it to release all wgpu handles.
        _stopRequested = true;
        _renderRequested.Release(); // unblock if waiting for a render request
        _gpuThread.Join(5_000);

        _renderRequested.Dispose();
        _renderComplete.Dispose();
        _initDone.Dispose();
    }

    // Called exclusively from the wgpu worker thread — safe to release all handles here.
    private void GpuDispose()
    {
        DestroyRenderTargets();

        if (_vertexBuffer     != null) _wgpu!.BufferRelease(_vertexBuffer);
        if (_indexBuffer      != null) _wgpu!.BufferRelease(_indexBuffer);
        if (_uniformBindGroup != null) _wgpu!.BindGroupRelease(_uniformBindGroup);
        if (_uniformBuffer    != null) _wgpu!.BufferRelease(_uniformBuffer);
        if (_pipeline         != null) _wgpu!.RenderPipelineRelease(_pipeline);
        if (_pipelineLayout   != null) _wgpu!.PipelineLayoutRelease(_pipelineLayout);
        if (_bgl              != null) _wgpu!.BindGroupLayoutRelease(_bgl);
        if (_shaderModule     != null) _wgpu!.ShaderModuleRelease(_shaderModule);
        if (_queue            != null) _wgpu!.QueueRelease(_queue);
        if (_device           != null) _wgpu!.DeviceRelease(_device);
        if (_adapter          != null) _wgpu!.AdapterRelease(_adapter);
        if (_instance         != null) _wgpu!.InstanceRelease(_instance);

        _wgpuExt?.Dispose();
        if (_wgpu is not null) _wgpu.Dispose();
    }
}
