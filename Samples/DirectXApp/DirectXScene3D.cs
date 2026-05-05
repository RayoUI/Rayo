namespace DirectXApp;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Self-contained Direct3D 11 scene that renders a rotating Lambert-shaded cube to an
/// off-screen RGBA8 texture and exposes the pixels via <see cref="RenderFrame"/>.
/// <para>
/// Owns its own <see cref="ID3D11Device"/> — entirely independent of Rayo's renderer.
/// The caller (DirectXView) composites the returned pixel buffer into the UI via
/// <see cref="Rayo.Rendering.IRenderer.CreateTextureFromPixels"/>.
/// </para>
/// </summary>
internal sealed unsafe class DirectXScene3D : IDisposable
{
    // Vertex: vec3 pos (12 bytes) + vec4 color (16 bytes) = 28 bytes
    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    // ── Configurable state ────────────────────────────────────────────────

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

    // ── Animation state ───────────────────────────────────────────────────

    private float   _rotX, _rotY, _rotZ;
    private Vector3 _cubeColor  = new(0.3f, 0.6f, 1.0f);
    private bool    _colorDirty = true;
    private bool    _disposed;

    // ── Cube geometry (24 unique vertices, 6 faces × 4) ───────────────────

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

    // ── Direct P/Invoke — bypasses Silk.NET's library loader for system DLLs ─
    // D3D11.GetApi(null) cannot locate system DLLs without a windowing context;
    // plain DllImport resolves d3d11.dll and d3dcompiler_47.dll from System32.

    [DllImport("d3d11")]
    private static extern int D3D11CreateDevice(
        IDXGIAdapter* pAdapter, int DriverType, nint Software, uint Flags,
        void* pFeatureLevels, uint FeatureLevels, uint SDKVersion,
        ID3D11Device** ppDevice, void* pFeatureLevel,
        ID3D11DeviceContext** ppImmediateContext);

    [DllImport("d3dcompiler_47")]
    private static extern int D3DCompile(
        void* pSrcData, nuint SrcDataSize, byte* pSourceName,
        void* pDefines, void* pInclude, byte* pEntrypoint, byte* pTarget,
        uint Flags1, uint Flags2, ID3D10Blob** ppCode, ID3D10Blob** ppErrorMsgs);

    // ── D3D11 device objects ──────────────────────────────────────────────

    private ID3D11Device*           _device;
    private ID3D11DeviceContext*    _context;
    private ID3D11VertexShader*     _vertexShader;
    private ID3D11PixelShader*      _pixelShader;
    private ID3D11InputLayout*      _inputLayout;
    private ID3D11Buffer*           _vertexBuffer;
    private ID3D11Buffer*           _indexBuffer;
    private uint                    _indexCount;
    private ID3D11Buffer*           _constantBuffer;
    private ID3D11DepthStencilState* _depthStencilState;
    private ID3D11RasterizerState*  _rasterizerState;

    // Off-screen render targets (resized lazily)
    private ID3D11Texture2D*        _renderTarget;
    private ID3D11RenderTargetView* _renderTargetView;
    private ID3D11Texture2D*        _depthBuffer;
    private ID3D11DepthStencilView* _depthStencilView;
    private ID3D11Texture2D*        _stagingTexture;
    private int                     _rtWidth, _rtHeight;

    // ── HLSL shaders ─────────────────────────────────────────────────────

    // row_major tells HLSL to interpret the cbuffer matrix as row-major,
    // matching .NET's System.Numerics.Matrix4x4 memory layout directly.
    private const string VertHlsl = @"
cbuffer MVP : register(b0) { row_major float4x4 mvp; };
struct VSIn { float3 pos : POSITION; float4 col : COLOR; };
struct PSIn { float4 pos : SV_POSITION; float4 col : COLOR; };
PSIn main(VSIn v) {
    PSIn o;
    o.pos = mul(float4(v.pos, 1.0), mvp);
    o.col = v.col;
    return o;
}";

    private const string FragHlsl = @"
struct PSIn { float4 pos : SV_POSITION; float4 col : COLOR; };
float4 main(PSIn p) : SV_TARGET { return p.col; }";

    // ── Construction ──────────────────────────────────────────────────────

    public DirectXScene3D()
    {
        CreateDevice();
        CompileShaders();
        CreatePipelineState();
        CreateConstantBuffer();
        RebuildVertexBuffer();
    }

    // ── Animation ─────────────────────────────────────────────────────────

    public void Tick(float deltaTime)
    {
        if (!Animate) return;
        float s = deltaTime * AnimationSpeed;
        _rotY += s;
        _rotX += s * 0.5f;
        _rotZ += s * 0.25f;
    }

    // ── Frame rendering ───────────────────────────────────────────────────

    /// <summary>
    /// Renders the 3D scene at <paramref name="width"/> × <paramref name="height"/>
    /// and returns the result as raw RGBA8 bytes. Synchronous — waits for GPU.
    /// </summary>
    public byte[] RenderFrame(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return Array.Empty<byte>();

        if (_colorDirty)
            RebuildVertexBuffer();

        EnsureRenderTargets(width, height);
        RenderScene(width, height);
        return ReadbackPixels(width, height);
    }

    // ── Device creation ───────────────────────────────────────────────────

    private void CreateDevice()
    {
        ID3D11Device*        device = null;
        ID3D11DeviceContext* ctx    = null;
        SilkMarshal.ThrowHResult(D3D11CreateDevice(
            null, (int)D3DDriverType.Hardware, 0, 0u,
            null, 0u, 7u,   // SDK version 7
            &device, null, &ctx));
        _device  = device;
        _context = ctx;
    }

    // ── Shader compilation ────────────────────────────────────────────────

    private void CompileShaders()
    {
        // ── Vertex shader ────────────────────────────────────────────────
        ID3D10Blob* vsBlob  = null;
        ID3D10Blob* vsError = null;

        var vsBytes     = Encoding.UTF8.GetBytes(VertHlsl);
        var vsEntry     = Encoding.ASCII.GetBytes("main\0");
        var vsTarget    = Encoding.ASCII.GetBytes("vs_4_0\0");
        fixed (byte* pVs = vsBytes)
        fixed (byte* pVsEntry = vsEntry)
        fixed (byte* pVsTarget = vsTarget)
        {
            int hr = D3DCompile(
                pVs, (nuint)vsBytes.Length,
                (byte*)null, (D3DShaderMacro*)null, (ID3DInclude*)null,
                pVsEntry, pVsTarget,
                0u, 0u,
                &vsBlob, &vsError);

            if (hr < 0)
            {
                string msg = vsError != null
                    ? Marshal.PtrToStringAnsi((nint)vsError->GetBufferPointer()) ?? "unknown"
                    : $"HRESULT 0x{hr:X8}";
                if (vsError != null) vsError->Release();
                throw new InvalidOperationException($"VS compile error: {msg}");
            }
        }

        if (vsError != null) { vsError->Release(); vsError = null; }

        SilkMarshal.ThrowHResult(
            _device->CreateVertexShader(
                vsBlob->GetBufferPointer(),
                vsBlob->GetBufferSize(),
                null,
                ref _vertexShader
            )
        );

        // ── Input layout — must match the Vertex struct and HLSL semantics ──
        var semanticPos = SilkMarshal.StringToPtr("POSITION", NativeStringEncoding.LPStr);
        var semanticCol = SilkMarshal.StringToPtr("COLOR",    NativeStringEncoding.LPStr);

        var elements = stackalloc InputElementDesc[2];
        elements[0] = new InputElementDesc
        {
            SemanticName         = (byte*)semanticPos,
            SemanticIndex        = 0u,
            Format               = Format.FormatR32G32B32Float,
            InputSlot            = 0u,
            AlignedByteOffset    = 0u,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0u,
        };
        elements[1] = new InputElementDesc
        {
            SemanticName         = (byte*)semanticCol,
            SemanticIndex        = 0u,
            Format               = Format.FormatR32G32B32A32Float,
            InputSlot            = 0u,
            AlignedByteOffset    = 12u,
            InputSlotClass       = InputClassification.PerVertexData,
            InstanceDataStepRate = 0u,
        };

        SilkMarshal.ThrowHResult(
            _device->CreateInputLayout(
                elements, 2u,
                vsBlob->GetBufferPointer(),
                vsBlob->GetBufferSize(),
                ref _inputLayout
            )
        );

        SilkMarshal.Free(semanticPos);
        SilkMarshal.Free(semanticCol);
        vsBlob->Release();

        // ── Pixel shader ─────────────────────────────────────────────────
        ID3D10Blob* psBlob  = null;
        ID3D10Blob* psError = null;

        var psBytes  = Encoding.UTF8.GetBytes(FragHlsl);
        var psEntry  = Encoding.ASCII.GetBytes("main\0");
        var psTarget = Encoding.ASCII.GetBytes("ps_4_0\0");
        fixed (byte* pPs = psBytes)
        fixed (byte* pPsEntry = psEntry)
        fixed (byte* pPsTarget = psTarget)
        {
            int hr = D3DCompile(
                pPs, (nuint)psBytes.Length,
                (byte*)null, (D3DShaderMacro*)null, (ID3DInclude*)null,
                pPsEntry, pPsTarget,
                0u, 0u,
                &psBlob, &psError);

            if (hr < 0)
            {
                string msg = psError != null
                    ? Marshal.PtrToStringAnsi((nint)psError->GetBufferPointer()) ?? "unknown"
                    : $"HRESULT 0x{hr:X8}";
                if (psError != null) psError->Release();
                throw new InvalidOperationException($"PS compile error: {msg}");
            }
        }

        if (psError != null) { psError->Release(); psError = null; }

        SilkMarshal.ThrowHResult(
            _device->CreatePixelShader(
                psBlob->GetBufferPointer(),
                psBlob->GetBufferSize(),
                null,
                ref _pixelShader
            )
        );

        psBlob->Release();
    }

    // ── Pipeline state ────────────────────────────────────────────────────

    private void CreatePipelineState()
    {
        // Depth-stencil: depth test + write enabled, stencil disabled
        var dsDesc = new DepthStencilDesc
        {
            DepthEnable    = 1,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc      = ComparisonFunc.Less,
            StencilEnable  = 0,
        };
        SilkMarshal.ThrowHResult(_device->CreateDepthStencilState(ref dsDesc, ref _depthStencilState));

        // Rasterizer: solid fill, back-face culling, CCW front faces
        var rsDesc = new RasterizerDesc
        {
            FillMode              = FillMode.Solid,
            CullMode              = CullMode.Back,
            FrontCounterClockwise = 1,
            DepthClipEnable       = 1,
            ScissorEnable         = 0,
            MultisampleEnable     = 0,
            AntialiasedLineEnable = 0,
        };
        SilkMarshal.ThrowHResult(_device->CreateRasterizerState(ref rsDesc, ref _rasterizerState));
    }

    private void CreateConstantBuffer()
    {
        // 64 bytes = one 4×4 float matrix for the row-major MVP
        var cbDesc = new BufferDesc
        {
            ByteWidth           = 64u,
            Usage               = Usage.Dynamic,
            BindFlags           = (uint)BindFlag.ConstantBuffer,
            CPUAccessFlags      = (uint)CpuAccessFlag.Write,
            MiscFlags           = 0u,
            StructureByteStride = 0u,
        };
        SilkMarshal.ThrowHResult(_device->CreateBuffer(ref cbDesc, null, ref _constantBuffer));
    }

    // ── Vertex baking (Lambert per face) ──────────────────────────────────

    private void RebuildVertexBuffer()
    {
        _colorDirty = false;

        var lightDir = Vector3.Normalize(new Vector3(2f, 2f, 2f));
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

        DestroyVertexBuffers();

        // Vertex buffer
        var vbDesc = new BufferDesc
        {
            ByteWidth           = (uint)(verts.Length * Unsafe.SizeOf<Vertex>()),
            Usage               = Usage.Default,
            BindFlags           = (uint)BindFlag.VertexBuffer,
            CPUAccessFlags      = 0u,
            MiscFlags           = 0u,
            StructureByteStride = (uint)Unsafe.SizeOf<Vertex>(),
        };
        fixed (Vertex* pVerts = verts)
        {
            var initData = new SubresourceData { PSysMem = pVerts };
            SilkMarshal.ThrowHResult(_device->CreateBuffer(ref vbDesc, ref initData, ref _vertexBuffer));
        }

        // Index buffer
        var ibDesc = new BufferDesc
        {
            ByteWidth           = (uint)(indices.Length * sizeof(ushort)),
            Usage               = Usage.Default,
            BindFlags           = (uint)BindFlag.IndexBuffer,
            CPUAccessFlags      = 0u,
            MiscFlags           = 0u,
            StructureByteStride = sizeof(ushort),
        };
        fixed (ushort* pIdx = indices)
        {
            var initData = new SubresourceData { PSysMem = pIdx };
            SilkMarshal.ThrowHResult(_device->CreateBuffer(ref ibDesc, ref initData, ref _indexBuffer));
        }
    }

    // ── Off-screen resource management ────────────────────────────────────

    private void EnsureRenderTargets(int w, int h)
    {
        if (_rtWidth == w && _rtHeight == h && _renderTargetView != null)
            return;

        DestroyRenderTargets();
        _rtWidth  = w;
        _rtHeight = h;

        // Color render target (RGBA8, default usage, bound as render target)
        var rtDesc = new Texture2DDesc
        {
            Width          = (uint)w,
            Height         = (uint)h,
            MipLevels      = 1u,
            ArraySize      = 1u,
            Format         = Format.FormatR8G8B8A8Unorm,
            SampleDesc     = new SampleDesc(1u, 0u),
            Usage          = Usage.Default,
            BindFlags      = (uint)BindFlag.RenderTarget,
            CPUAccessFlags = 0u,
            MiscFlags      = 0u,
        };
        SilkMarshal.ThrowHResult(_device->CreateTexture2D(ref rtDesc, null, ref _renderTarget));
        {
            ID3D11RenderTargetView* rtv = null;
            SilkMarshal.ThrowHResult(_device->CreateRenderTargetView((ID3D11Resource*)_renderTarget, null, &rtv));
            _renderTargetView = rtv;
        }

        // Depth buffer (D24S8)
        var depthDesc = new Texture2DDesc
        {
            Width          = (uint)w,
            Height         = (uint)h,
            MipLevels      = 1u,
            ArraySize      = 1u,
            Format         = Format.FormatD24UnormS8Uint,
            SampleDesc     = new SampleDesc(1u, 0u),
            Usage          = Usage.Default,
            BindFlags      = (uint)BindFlag.DepthStencil,
            CPUAccessFlags = 0u,
            MiscFlags      = 0u,
        };
        SilkMarshal.ThrowHResult(_device->CreateTexture2D(ref depthDesc, null, ref _depthBuffer));
        {
            ID3D11DepthStencilView* dsv = null;
            SilkMarshal.ThrowHResult(_device->CreateDepthStencilView((ID3D11Resource*)_depthBuffer, null, &dsv));
            _depthStencilView = dsv;
        }

        // Staging texture for CPU readback (same format, Staging usage, CPU-readable)
        var stagingDesc = new Texture2DDesc
        {
            Width          = (uint)w,
            Height         = (uint)h,
            MipLevels      = 1u,
            ArraySize      = 1u,
            Format         = Format.FormatR8G8B8A8Unorm,
            SampleDesc     = new SampleDesc(1u, 0u),
            Usage          = Usage.Staging,
            BindFlags      = 0u,
            CPUAccessFlags = (uint)CpuAccessFlag.Read,
            MiscFlags      = 0u,
        };
        SilkMarshal.ThrowHResult(_device->CreateTexture2D(ref stagingDesc, null, ref _stagingTexture));
    }

    private void DestroyRenderTargets()
    {
        SafeRelease(ref _stagingTexture);
        SafeRelease(ref _depthStencilView);
        SafeRelease(ref _depthBuffer);
        SafeRelease(ref _renderTargetView);
        SafeRelease(ref _renderTarget);
        _rtWidth = _rtHeight = 0;
    }

    // ── Draw ──────────────────────────────────────────────────────────────

    private void RenderScene(int w, int h)
    {
        // Clear
        var clearColor = stackalloc float[4] { 0.07f, 0.08f, 0.15f, 1.0f };
        _context->ClearRenderTargetView(_renderTargetView, clearColor);
        _context->ClearDepthStencilView(_depthStencilView, (uint)ClearFlag.Depth, 1.0f, 0);

        // Output merger — local copy required; cannot take address of moveable class field (CS0212)
        var rtv = _renderTargetView;
        _context->OMSetRenderTargets(1u, &rtv, _depthStencilView);
        _context->OMSetDepthStencilState(_depthStencilState, 0u);

        // Rasterizer
        _context->RSSetState(_rasterizerState);
        var vp = new Viewport { TopLeftX = 0f, TopLeftY = 0f, Width = w, Height = h, MinDepth = 0f, MaxDepth = 1f };
        _context->RSSetViewports(1u, &vp);

        // Input assembler
        _context->IASetInputLayout(_inputLayout);
        _context->IASetPrimitiveTopology(D3DPrimitiveTopology.D3D11PrimitiveTopologyTrianglelist);
        uint stride = (uint)Unsafe.SizeOf<Vertex>();
        uint offset = 0u;
        var vb = _vertexBuffer;
        _context->IASetVertexBuffers(0u, 1u, &vb, &stride, &offset);
        _context->IASetIndexBuffer(_indexBuffer, Format.FormatR16Uint, 0u);

        // Shaders
        _context->VSSetShader(_vertexShader, null, 0u);
        _context->PSSetShader(_pixelShader, null, 0u);

        // Update constant buffer — pass the row-major MVP directly (row_major cbuffer in HLSL)
        float aspect = w / (float)h;
        var proj  = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 0.1f, 100f);
        var view  = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
        var model = Matrix4x4.CreateRotationX(_rotX)
                  * Matrix4x4.CreateRotationY(_rotY)
                  * Matrix4x4.CreateRotationZ(_rotZ);
        var mvp = model * view * proj;

        MappedSubresource mapped = default;
        SilkMarshal.ThrowHResult(
            _context->Map((ID3D11Resource*)_constantBuffer, 0u, Map.WriteDiscard, 0u, &mapped)
        );
        *(Matrix4x4*)mapped.PData = mvp;
        _context->Unmap((ID3D11Resource*)_constantBuffer, 0u);

        var cb = _constantBuffer;
        _context->VSSetConstantBuffers(0u, 1u, &cb);

        // Draw
        _context->DrawIndexed(_indexCount, 0u, 0);

        // Flush so the GPU has finished before we CopyResource
        _context->Flush();
    }

    private byte[] ReadbackPixels(int w, int h)
    {
        // Copy render target to CPU-accessible staging texture
        _context->CopyResource((ID3D11Resource*)_stagingTexture, (ID3D11Resource*)_renderTarget);

        MappedSubresource mapped = default;
        SilkMarshal.ThrowHResult(
            _context->Map((ID3D11Resource*)_stagingTexture, 0u, Map.Read, 0u, &mapped)
        );

        var pixels = new byte[w * h * 4];
        var src    = (byte*)mapped.PData;

        // Copy row by row — RowPitch may include hardware alignment padding
        for (int row = 0; row < h; row++)
            Marshal.Copy((nint)(src + row * mapped.RowPitch), pixels, row * w * 4, w * 4);

        _context->Unmap((ID3D11Resource*)_stagingTexture, 0u);
        return pixels;
    }

    // ── Cleanup helpers ───────────────────────────────────────────────────

    private void DestroyVertexBuffers()
    {
        SafeRelease(ref _vertexBuffer);
        SafeRelease(ref _indexBuffer);
    }

    private static void SafeRelease<T>(ref T* ptr) where T : unmanaged
    {
        if (ptr != null)
        {
            ((IUnknown*)ptr)->Release();
            ptr = null;
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_context != null) _context->ClearState();

        DestroyRenderTargets();
        DestroyVertexBuffers();

        SafeRelease(ref _constantBuffer);
        SafeRelease(ref _depthStencilState);
        SafeRelease(ref _rasterizerState);
        SafeRelease(ref _inputLayout);
        SafeRelease(ref _pixelShader);
        SafeRelease(ref _vertexShader);
        SafeRelease(ref _context);
        SafeRelease(ref _device);
    }
}
