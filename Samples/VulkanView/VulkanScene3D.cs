namespace VulkanApp;

using Silk.NET.Core.Native;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Self-contained Vulkan 3D scene that renders a rotating Lambert-shaded cube to an
/// off-screen RGBA8 image and exposes the pixels via <see cref="RenderFrame"/>.
/// <para>
/// Owns its own <see cref="Vk"/> instance and logical device — entirely independent of
/// Rayo's VulkanRenderer stack. The caller (VulkanView) composites the returned
/// pixel buffer into the UI via
/// <see cref="Rayo.Rendering.IRenderer.CreateTextureFromPixels"/>.
/// </para>
/// </summary>
internal sealed unsafe class VulkanScene3D : IDisposable
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
    private Vector3 _cubeColor  = new(0.2f, 0.6f, 1.0f);
    private bool    _colorDirty = true;

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

    // ── Vulkan objects (own stack, no windowing / surface / swapchain) ─────

    private readonly Vk     _vk;
    private readonly Instance      _instance;
    private readonly PhysicalDevice _physDev;
    private readonly Device        _device;
    private readonly Queue         _queue;
    private readonly uint          _queueFamily;
    private readonly CommandPool   _cmdPool;

    private readonly RenderPass     _renderPass;
    private readonly Pipeline       _pipeline;
    private readonly PipelineLayout _pipelineLayout;

    // GPU vertex / index buffers (host-visible; rebuilt when CubeColor changes)
    private Buffer       _vertexBuffer;
    private DeviceMemory _vertexMem;
    private Buffer       _indexBuffer;
    private DeviceMemory _indexMem;
    private uint         _indexCount;

    // Off-screen render target (resized lazily)
    private Image        _colorImage;
    private DeviceMemory _colorMem;
    private ImageView    _colorView;
    private Image        _depthImage;
    private DeviceMemory _depthMem;
    private ImageView    _depthView;
    private Framebuffer  _framebuffer;
    private int          _fbWidth, _fbHeight;

    // Staging buffer for CPU readback (resized lazily alongside framebuffer)
    private Buffer       _readbackBuffer;
    private DeviceMemory _readbackMem;

    // Per-frame command buffer + fence
    private CommandBuffer _cmdBuffer;
    private Fence         _fence;

    private bool _disposed;

    // ── GLSL shaders ─────────────────────────────────────────────────────

    private const string VertGlsl = @"
#version 450
layout(location = 0) in vec3 inPos;
layout(location = 1) in vec4 inColor;
layout(push_constant) uniform PC { mat4 mvp; } pc;
layout(location = 0) out vec4 fragColor;
void main() {
    gl_Position = pc.mvp * vec4(inPos, 1.0);
    fragColor   = inColor;
}";

    private const string FragGlsl = @"
#version 450
layout(location = 0) in  vec4 fragColor;
layout(location = 0) out vec4 outColor;
void main() { outColor = fragColor; }";

    // ── Construction ──────────────────────────────────────────────────────

    public VulkanScene3D()
    {
        _vk = Vk.GetApi();

        _instance    = CreateInstance();
        (_physDev, _queueFamily) = PickPhysicalDevice();
        _device      = CreateLogicalDevice();
        _vk.GetDeviceQueue(_device, _queueFamily, 0, out _queue);
        _cmdPool     = CreateCommandPool();
        _cmdBuffer   = AllocCommandBuffer();
        _fence       = CreateFence();

        _renderPass    = CreateRenderPass();
        _pipelineLayout = CreatePipelineLayout();
        _pipeline      = CreatePipeline();

        RebuildVertexBuffer();   // initial Lambert bake
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

        EnsureOffscreenResources(width, height);
        RecordAndSubmit(width, height);
        return ReadbackPixels(width, height);
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

        (_vertexBuffer, _vertexMem) = CreateHostBuffer(
            (ulong)(verts.Length * Unsafe.SizeOf<Vertex>()),
            BufferUsageFlags.VertexBufferBit);
        UploadHostBuffer(_vertexMem, (ulong)(verts.Length * Unsafe.SizeOf<Vertex>()), verts.AsSpan());

        (_indexBuffer, _indexMem) = CreateHostBuffer(
            (ulong)(indices.Length * sizeof(ushort)),
            BufferUsageFlags.IndexBufferBit);
        UploadHostBuffer(_indexMem, (ulong)(indices.Length * sizeof(ushort)), indices.AsSpan());
    }

    // ── Off-screen resource management ───────────────────────────────────

    private void EnsureOffscreenResources(int w, int h)
    {
        if (_fbWidth == w && _fbHeight == h && _framebuffer.Handle != 0)
            return;

        DestroyOffscreenResources();
        _fbWidth  = w;
        _fbHeight = h;

        // Color: RGBA8, written by render pass, read back via transfer
        (_colorImage, _colorMem) = CreateImage((uint)w, (uint)h,
            Format.R8G8B8A8Unorm,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit);
        _colorView = CreateImageView(_colorImage, Format.R8G8B8A8Unorm, ImageAspectFlags.ColorBit);

        // Depth: D16
        (_depthImage, _depthMem) = CreateImage((uint)w, (uint)h,
            Format.D16Unorm,
            ImageUsageFlags.DepthStencilAttachmentBit);
        _depthView = CreateImageView(_depthImage, Format.D16Unorm, ImageAspectFlags.DepthBit);

        // Both images start Undefined → transition them to their attachment layouts
        TransitionLayout(_colorImage,
            ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal,
            ImageAspectFlags.ColorBit,
            0, AccessFlags.ColorAttachmentWriteBit,
            PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.ColorAttachmentOutputBit);

        TransitionLayout(_depthImage,
            ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal,
            ImageAspectFlags.DepthBit,
            0, AccessFlags.DepthStencilAttachmentWriteBit,
            PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.EarlyFragmentTestsBit);

        // Framebuffer
        var attachViews = stackalloc ImageView[] { _colorView, _depthView };
        var fbInfo = new FramebufferCreateInfo
        {
            SType           = StructureType.FramebufferCreateInfo,
            RenderPass      = _renderPass,
            AttachmentCount = 2,
            PAttachments    = attachViews,
            Width           = (uint)w,
            Height          = (uint)h,
            Layers          = 1,
        };
        Check(_vk.CreateFramebuffer(_device, &fbInfo, null, out _framebuffer), "CreateFramebuffer");

        // Readback staging buffer (host-visible, transfer destination)
        (_readbackBuffer, _readbackMem) = CreateHostBuffer(
            (ulong)(w * h * 4),
            BufferUsageFlags.TransferDstBit);
    }

    private void DestroyOffscreenResources()
    {
        if (_framebuffer.Handle != 0) { _vk.DestroyFramebuffer(_device, _framebuffer, null); _framebuffer = default; }
        if (_colorView.Handle   != 0) { _vk.DestroyImageView(_device, _colorView,   null); _colorView   = default; }
        if (_colorImage.Handle  != 0) { _vk.DestroyImage(_device, _colorImage,  null);     _colorImage  = default; }
        if (_colorMem.Handle    != 0) { _vk.FreeMemory(_device, _colorMem,    null);       _colorMem    = default; }
        if (_depthView.Handle   != 0) { _vk.DestroyImageView(_device, _depthView,   null); _depthView   = default; }
        if (_depthImage.Handle  != 0) { _vk.DestroyImage(_device, _depthImage,  null);     _depthImage  = default; }
        if (_depthMem.Handle    != 0) { _vk.FreeMemory(_device, _depthMem,    null);       _depthMem    = default; }
        if (_readbackBuffer.Handle != 0) { _vk.DestroyBuffer(_device, _readbackBuffer, null); _vk.FreeMemory(_device, _readbackMem, null); _readbackBuffer = default; _readbackMem = default; }
        _fbWidth = _fbHeight = 0;
    }

    // ── Command recording ─────────────────────────────────────────────────

    private void RecordAndSubmit(int w, int h)
    {
        _vk.ResetCommandBuffer(_cmdBuffer, 0);

        var begin = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        _vk.BeginCommandBuffer(_cmdBuffer, &begin);

        // ── Render pass ──────────────────────────────────────────────────

        var clearValues = stackalloc ClearValue[2];
        clearValues[0].Color = new ClearColorValue(0.07f, 0.08f, 0.15f, 1.0f);
        clearValues[1].DepthStencil = new ClearDepthStencilValue(1.0f, 0);

        var rpBegin = new RenderPassBeginInfo
        {
            SType           = StructureType.RenderPassBeginInfo,
            RenderPass      = _renderPass,
            Framebuffer     = _framebuffer,
            RenderArea      = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)w, (uint)h)),
            ClearValueCount = 2,
            PClearValues    = clearValues,
        };
        _vk.CmdBeginRenderPass(_cmdBuffer, &rpBegin, SubpassContents.Inline);
        _vk.CmdBindPipeline(_cmdBuffer, PipelineBindPoint.Graphics, _pipeline);

        var viewport = new Viewport(0, 0, w, h, 0f, 1f);
        var scissor  = new Rect2D(new Offset2D(0, 0), new Extent2D((uint)w, (uint)h));
        _vk.CmdSetViewport(_cmdBuffer, 0, 1, &viewport);
        _vk.CmdSetScissor (_cmdBuffer, 0, 1, &scissor);

        // MVP — transposed so GLSL column-major left-multiply gives the same
        // result as .NET row-vector right-multiply conventions.
        float aspect = w / (float)h;
        var   proj   = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 0.1f, 100f);
        // Vulkan NDC has Y pointing down; flip the projection Y-axis to keep
        // the cube right-side-up after readback.
        proj.M22 = -proj.M22;
        var   view   = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
        var   model  = Matrix4x4.CreateRotationX(_rotX)
                     * Matrix4x4.CreateRotationY(_rotY)
                     * Matrix4x4.CreateRotationZ(_rotZ);
        // No Transpose: .NET row-major bytes read by GLSL as column-major means
        // mat*vec in GLSL is equivalent to v*mat in .NET — correct for both.
        var   mvp    = model * view * proj;

        _vk.CmdPushConstants(_cmdBuffer, _pipelineLayout, ShaderStageFlags.VertexBit, 0, 64, &mvp);

        ulong zero = 0;
        var   vb   = _vertexBuffer;
        _vk.CmdBindVertexBuffers(_cmdBuffer, 0, 1, &vb, &zero);
        _vk.CmdBindIndexBuffer  (_cmdBuffer, _indexBuffer, 0, IndexType.Uint16);
        _vk.CmdDrawIndexed      (_cmdBuffer, _indexCount, 1, 0, 0, 0);

        _vk.CmdEndRenderPass(_cmdBuffer);

        // ── Transition color image → TransferSrc for readback ─────────────

        RecordBarrier(_cmdBuffer, _colorImage,
            ImageLayout.ColorAttachmentOptimal, ImageLayout.TransferSrcOptimal,
            ImageAspectFlags.ColorBit,
            AccessFlags.ColorAttachmentWriteBit, AccessFlags.TransferReadBit,
            PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TransferBit);

        // ── Copy to readback buffer ───────────────────────────────────────

        var copyRegion = new BufferImageCopy
        {
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
            },
            ImageExtent = new Extent3D((uint)w, (uint)h, 1),
        };
        _vk.CmdCopyImageToBuffer(_cmdBuffer, _colorImage,
            ImageLayout.TransferSrcOptimal, _readbackBuffer, 1, &copyRegion);

        // ── Transition back for next frame ────────────────────────────────

        RecordBarrier(_cmdBuffer, _colorImage,
            ImageLayout.TransferSrcOptimal, ImageLayout.ColorAttachmentOptimal,
            ImageAspectFlags.ColorBit,
            AccessFlags.TransferReadBit, AccessFlags.ColorAttachmentWriteBit,
            PipelineStageFlags.TransferBit, PipelineStageFlags.ColorAttachmentOutputBit);

        _vk.EndCommandBuffer(_cmdBuffer);

        // ── Submit & wait ─────────────────────────────────────────────────

        _vk.ResetFences(_device, 1, in _fence);
        var cb         = _cmdBuffer;
        var submitInfo = new SubmitInfo
        {
            SType              = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers    = &cb,
        };
        Check(_vk.QueueSubmit(_queue, 1, &submitInfo, _fence), "QueueSubmit");
        _vk.WaitForFences(_device, 1, in _fence, true, ulong.MaxValue);
    }

    private byte[] ReadbackPixels(int w, int h)
    {
        ulong size   = (ulong)(w * h * 4);
        var   pixels = new byte[size];
        void* mapped;
        _vk.MapMemory(_device, _readbackMem, 0, size, 0, &mapped);
        Marshal.Copy((nint)mapped, pixels, 0, (int)size);
        _vk.UnmapMemory(_device, _readbackMem);
        return pixels;
    }

    // ── Pipeline creation ─────────────────────────────────────────────────

    private RenderPass CreateRenderPass()
    {
        var color = new AttachmentDescription
        {
            Format        = Format.R8G8B8A8Unorm,
            Samples       = SampleCountFlags.Count1Bit,
            LoadOp        = AttachmentLoadOp.Clear,
            StoreOp       = AttachmentStoreOp.Store,
            InitialLayout = ImageLayout.ColorAttachmentOptimal,
            FinalLayout   = ImageLayout.ColorAttachmentOptimal,
        };
        var depth = new AttachmentDescription
        {
            Format         = Format.D16Unorm,
            Samples        = SampleCountFlags.Count1Bit,
            LoadOp         = AttachmentLoadOp.Clear,
            StoreOp        = AttachmentStoreOp.DontCare,
            StencilLoadOp  = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout  = ImageLayout.DepthStencilAttachmentOptimal,
            FinalLayout    = ImageLayout.DepthStencilAttachmentOptimal,
        };

        var colorRef = new AttachmentReference { Attachment = 0, Layout = ImageLayout.ColorAttachmentOptimal };
        var depthRef = new AttachmentReference { Attachment = 1, Layout = ImageLayout.DepthStencilAttachmentOptimal };

        var subpass = new SubpassDescription
        {
            PipelineBindPoint       = PipelineBindPoint.Graphics,
            ColorAttachmentCount    = 1,
            PColorAttachments       = &colorRef,
            PDepthStencilAttachment = &depthRef,
        };

        var descs = stackalloc AttachmentDescription[] { color, depth };
        var rpInfo = new RenderPassCreateInfo
        {
            SType           = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments    = descs,
            SubpassCount    = 1,
            PSubpasses      = &subpass,
        };
        Check(_vk.CreateRenderPass(_device, &rpInfo, null, out RenderPass rp), "CreateRenderPass");
        return rp;
    }

    private PipelineLayout CreatePipelineLayout()
    {
        var pc = new PushConstantRange { StageFlags = ShaderStageFlags.VertexBit, Size = 64 };
        var info = new PipelineLayoutCreateInfo
        {
            SType                  = StructureType.PipelineLayoutCreateInfo,
            PushConstantRangeCount = 1,
            PPushConstantRanges    = &pc,
        };
        Check(_vk.CreatePipelineLayout(_device, &info, null, out PipelineLayout layout), "CreatePipelineLayout");
        return layout;
    }

    private Pipeline CreatePipeline()
    {
        var vertSpv = CompileGlsl(VertGlsl, ShaderKind.VertexShader);
        var fragSpv = CompileGlsl(FragGlsl, ShaderKind.FragmentShader);
        var vertMod = CreateShaderModule(vertSpv);
        var fragMod = CreateShaderModule(fragSpv);

        var entry  = (byte*)Marshal.StringToHGlobalAnsi("main");
        var stages = stackalloc PipelineShaderStageCreateInfo[2];
        stages[0] = new PipelineShaderStageCreateInfo { SType = StructureType.PipelineShaderStageCreateInfo, Stage = ShaderStageFlags.VertexBit,   Module = vertMod, PName = entry };
        stages[1] = new PipelineShaderStageCreateInfo { SType = StructureType.PipelineShaderStageCreateInfo, Stage = ShaderStageFlags.FragmentBit, Module = fragMod, PName = entry };

        // Vertex: vec3 pos (off 0) + vec4 color (off 12) = 28 bytes
        var binding = new VertexInputBindingDescription
        {
            Binding   = 0,
            Stride    = (uint)Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex,
        };
        var attribs = stackalloc VertexInputAttributeDescription[2];
        attribs[0] = new VertexInputAttributeDescription { Location = 0, Binding = 0, Format = Format.R32G32B32Sfloat,     Offset = 0  };
        attribs[1] = new VertexInputAttributeDescription { Location = 1, Binding = 0, Format = Format.R32G32B32A32Sfloat,  Offset = 12 };

        var vi = new PipelineVertexInputStateCreateInfo
        {
            SType                           = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount   = 1,
            PVertexBindingDescriptions      = &binding,
            VertexAttributeDescriptionCount = 2,
            PVertexAttributeDescriptions    = attribs,
        };
        var ia = new PipelineInputAssemblyStateCreateInfo
        {
            SType    = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
        };
        var dynamicArr = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };
        var dyn = new PipelineDynamicStateCreateInfo
        {
            SType             = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 2,
            PDynamicStates    = dynamicArr,
        };
        var vps = new PipelineViewportStateCreateInfo
        {
            SType         = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount  = 1,
        };
        var rast = new PipelineRasterizationStateCreateInfo
        {
            SType       = StructureType.PipelineRasterizationStateCreateInfo,
            PolygonMode = PolygonMode.Fill,
            CullMode    = CullModeFlags.BackBit,
            FrontFace   = FrontFace.CounterClockwise,
            LineWidth   = 1.0f,
        };
        var msaa = new PipelineMultisampleStateCreateInfo
        {
            SType                = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };
        var ds = new PipelineDepthStencilStateCreateInfo
        {
            SType            = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable  = true,
            DepthWriteEnable = true,
            DepthCompareOp   = CompareOp.Less,
        };
        var blendAtt = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                             ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        };
        var blend = new PipelineColorBlendStateCreateInfo
        {
            SType           = StructureType.PipelineColorBlendStateCreateInfo,
            AttachmentCount = 1,
            PAttachments    = &blendAtt,
        };

        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType               = StructureType.GraphicsPipelineCreateInfo,
            StageCount          = 2,
            PStages             = stages,
            PVertexInputState   = &vi,
            PInputAssemblyState = &ia,
            PViewportState      = &vps,
            PRasterizationState = &rast,
            PMultisampleState   = &msaa,
            PDepthStencilState  = &ds,
            PColorBlendState    = &blend,
            PDynamicState       = &dyn,
            Layout              = _pipelineLayout,
            RenderPass          = _renderPass,
        };
        Check(_vk.CreateGraphicsPipelines(_device, default, 1, &pipelineInfo, null, out Pipeline pipeline),
            "CreateGraphicsPipelines");

        _vk.DestroyShaderModule(_device, vertMod, null);
        _vk.DestroyShaderModule(_device, fragMod, null);
        Marshal.FreeHGlobal((nint)entry);
        return pipeline;
    }

    // ── Device bootstrap ──────────────────────────────────────────────────

    private Instance CreateInstance()
    {
        var appInfo = new ApplicationInfo
        {
            SType      = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version12,
        };
        var ci = new InstanceCreateInfo
        {
            SType            = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
        };
        Check(_vk.CreateInstance(&ci, null, out Instance inst), "CreateInstance (scene)");
        return inst;
    }

    private (PhysicalDevice gpu, uint queueFamily) PickPhysicalDevice()
    {
        uint count = 0;
        _vk.EnumeratePhysicalDevices(_instance, &count, null);
        if (count == 0) throw new InvalidOperationException("No Vulkan GPU found");

        var gpus = new PhysicalDevice[count];
        fixed (PhysicalDevice* ptr = gpus)
            _vk.EnumeratePhysicalDevices(_instance, &count, ptr);

        PhysicalDevice? discrete = null, integrated = null;
        foreach (var gpu in gpus)
        {
            _vk.GetPhysicalDeviceProperties(gpu, out var props);
            if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)       discrete   ??= gpu;
            else if (props.DeviceType == PhysicalDeviceType.IntegratedGpu) integrated ??= gpu;
        }
        var chosen = discrete ?? integrated ?? gpus[0];

        uint count2 = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(chosen, &count2, null);
        var fams = new QueueFamilyProperties[count2];
        fixed (QueueFamilyProperties* ptr = fams)
            _vk.GetPhysicalDeviceQueueFamilyProperties(chosen, &count2, ptr);

        for (uint i = 0; i < fams.Length; i++)
            if ((fams[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                return (chosen, i);

        throw new InvalidOperationException("No graphics queue family");
    }

    private Device CreateLogicalDevice()
    {
        float pri = 1f;
        var qi = new DeviceQueueCreateInfo
        {
            SType            = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _queueFamily,
            QueueCount       = 1,
            PQueuePriorities = &pri,
        };
        var features = new PhysicalDeviceFeatures();
        var di = new DeviceCreateInfo
        {
            SType                = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos    = &qi,
            PEnabledFeatures     = &features,
        };
        Check(_vk.CreateDevice(_physDev, &di, null, out Device dev), "CreateDevice (scene)");
        return dev;
    }

    private CommandPool CreateCommandPool()
    {
        var info = new CommandPoolCreateInfo
        {
            SType            = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _queueFamily,
            Flags            = CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        Check(_vk.CreateCommandPool(_device, &info, null, out CommandPool pool), "CreateCommandPool");
        return pool;
    }

    private CommandBuffer AllocCommandBuffer()
    {
        var info = new CommandBufferAllocateInfo
        {
            SType              = StructureType.CommandBufferAllocateInfo,
            CommandPool        = _cmdPool,
            Level              = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        _vk.AllocateCommandBuffers(_device, &info, out CommandBuffer cb);
        return cb;
    }

    private Fence CreateFence()
    {
        var info = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
        Check(_vk.CreateFence(_device, &info, null, out Fence fence), "CreateFence");
        return fence;
    }

    // ── Buffer / image helpers ────────────────────────────────────────────

    private (Buffer buf, DeviceMemory mem) CreateHostBuffer(ulong size, BufferUsageFlags usage)
    {
        var bi = new BufferCreateInfo
        {
            SType       = StructureType.BufferCreateInfo,
            Size        = size,
            Usage       = usage,
            SharingMode = SharingMode.Exclusive,
        };
        Check(_vk.CreateBuffer(_device, &bi, null, out Buffer buf), "CreateBuffer");
        _vk.GetBufferMemoryRequirements(_device, buf, out var req);

        var ai = new MemoryAllocateInfo
        {
            SType           = StructureType.MemoryAllocateInfo,
            AllocationSize  = req.Size,
            MemoryTypeIndex = FindMemoryType(req.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit),
        };
        Check(_vk.AllocateMemory(_device, &ai, null, out DeviceMemory mem), "AllocateMemory (buffer)");
        _vk.BindBufferMemory(_device, buf, mem, 0);
        return (buf, mem);
    }

    private void UploadHostBuffer<T>(DeviceMemory mem, ulong size, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        void* mapped;
        _vk.MapMemory(_device, mem, 0, size, 0, &mapped);
        fixed (T* src = data)
            Unsafe.CopyBlock(mapped, src, (uint)size);
        _vk.UnmapMemory(_device, mem);
    }

    private (Image img, DeviceMemory mem) CreateImage(uint w, uint h, Format fmt, ImageUsageFlags usage)
    {
        var ii = new ImageCreateInfo
        {
            SType         = StructureType.ImageCreateInfo,
            ImageType     = ImageType.Type2D,
            Format        = fmt,
            Extent        = new Extent3D(w, h, 1),
            MipLevels     = 1,
            ArrayLayers   = 1,
            Samples       = SampleCountFlags.Count1Bit,
            Tiling        = ImageTiling.Optimal,
            Usage         = usage,
            SharingMode   = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
        };
        Check(_vk.CreateImage(_device, &ii, null, out Image img), "CreateImage");
        _vk.GetImageMemoryRequirements(_device, img, out var req);

        var ai = new MemoryAllocateInfo
        {
            SType           = StructureType.MemoryAllocateInfo,
            AllocationSize  = req.Size,
            MemoryTypeIndex = FindMemoryType(req.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
        };
        Check(_vk.AllocateMemory(_device, &ai, null, out DeviceMemory mem), "AllocateMemory (image)");
        _vk.BindImageMemory(_device, img, mem, 0);
        return (img, mem);
    }

    private ImageView CreateImageView(Image image, Format fmt, ImageAspectFlags aspect)
    {
        var info = new ImageViewCreateInfo
        {
            SType            = StructureType.ImageViewCreateInfo,
            Image            = image,
            ViewType         = ImageViewType.Type2D,
            Format           = fmt,
            SubresourceRange = new ImageSubresourceRange { AspectMask = aspect, LevelCount = 1, LayerCount = 1 },
        };
        Check(_vk.CreateImageView(_device, &info, null, out ImageView view), "CreateImageView");
        return view;
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags props)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physDev, out var mp);
        for (uint i = 0; i < mp.MemoryTypeCount; i++)
            if ((typeFilter & (1u << (int)i)) != 0 &&
                (mp.MemoryTypes[(int)i].PropertyFlags & props) == props)
                return i;
        throw new InvalidOperationException("No suitable memory type");
    }

    private void TransitionLayout(Image image,
        ImageLayout oldLayout, ImageLayout newLayout,
        ImageAspectFlags aspect,
        AccessFlags srcAccess, AccessFlags dstAccess,
        PipelineStageFlags srcStage, PipelineStageFlags dstStage)
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType              = StructureType.CommandBufferAllocateInfo,
            CommandPool        = _cmdPool,
            Level              = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        _vk.AllocateCommandBuffers(_device, &allocInfo, out CommandBuffer cb);

        var bi = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        _vk.BeginCommandBuffer(cb, &bi);
        RecordBarrier(cb, image, oldLayout, newLayout, aspect, srcAccess, dstAccess, srcStage, dstStage);
        _vk.EndCommandBuffer(cb);

        var si = new SubmitInfo { SType = StructureType.SubmitInfo, CommandBufferCount = 1, PCommandBuffers = &cb };
        _vk.QueueSubmit(_queue, 1, &si, default);
        _vk.QueueWaitIdle(_queue);
        _vk.FreeCommandBuffers(_device, _cmdPool, 1, &cb);
    }

    private void RecordBarrier(CommandBuffer cb, Image image,
        ImageLayout oldLayout, ImageLayout newLayout,
        ImageAspectFlags aspect,
        AccessFlags srcAccess, AccessFlags dstAccess,
        PipelineStageFlags srcStage, PipelineStageFlags dstStage)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType               = StructureType.ImageMemoryBarrier,
            OldLayout           = oldLayout,
            NewLayout           = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image               = image,
            SubresourceRange    = new ImageSubresourceRange { AspectMask = aspect, LevelCount = 1, LayerCount = 1 },
            SrcAccessMask       = srcAccess,
            DstAccessMask       = dstAccess,
        };
        _vk.CmdPipelineBarrier(cb, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);
    }

    // ── Shader compilation ────────────────────────────────────────────────

    private static byte[] CompileGlsl(string glsl, ShaderKind kind)
    {
        var shaderc  = Shaderc.GetApi();
        var compiler = shaderc.CompilerInitialize();
        var options  = shaderc.CompileOptionsInitialize();
        shaderc.CompileOptionsSetOptimizationLevel(options, OptimizationLevel.Performance);

        var result = shaderc.CompileIntoSpv(
            compiler, glsl,
            (nuint)System.Text.Encoding.UTF8.GetByteCount(glsl),
            kind, "shader.glsl", "main", options);

        if (shaderc.ResultGetCompilationStatus(result) != CompilationStatus.Success)
        {
            var msg = shaderc.ResultGetErrorMessageS(result);
            shaderc.ResultRelease(result);
            shaderc.CompilerRelease(compiler);
            shaderc.Dispose();
            throw new InvalidOperationException($"Shader compile error: {msg}");
        }

        nuint len   = shaderc.ResultGetLength(result);
        var   bytes = shaderc.ResultGetBytes(result);
        var   spirv = new byte[len];
        Marshal.Copy((nint)bytes, spirv, 0, (int)len);

        shaderc.ResultRelease(result);
        shaderc.CompileOptionsRelease(options);
        shaderc.CompilerRelease(compiler);
        shaderc.Dispose();
        return spirv;
    }

    private ShaderModule CreateShaderModule(byte[] spirv)
    {
        fixed (byte* ptr = spirv)
        {
            var info = new ShaderModuleCreateInfo
            {
                SType    = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode    = (uint*)ptr,
            };
            Check(_vk.CreateShaderModule(_device, &info, null, out ShaderModule mod), "CreateShaderModule");
            return mod;
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────

    private void DestroyVertexBuffers()
    {
        if (_vertexBuffer.Handle != 0) { _vk.DestroyBuffer(_device, _vertexBuffer, null); _vk.FreeMemory(_device, _vertexMem, null); _vertexBuffer = default; _vertexMem = default; }
        if (_indexBuffer.Handle  != 0) { _vk.DestroyBuffer(_device, _indexBuffer,  null); _vk.FreeMemory(_device, _indexMem,  null); _indexBuffer  = default; _indexMem  = default; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _vk.DeviceWaitIdle(_device);

        DestroyOffscreenResources();
        DestroyVertexBuffers();

        if (_pipeline.Handle       != 0) _vk.DestroyPipeline      (_device, _pipeline,       null);
        if (_pipelineLayout.Handle != 0) _vk.DestroyPipelineLayout(_device, _pipelineLayout,  null);
        if (_renderPass.Handle     != 0) _vk.DestroyRenderPass     (_device, _renderPass,      null);
        if (_fence.Handle          != 0) _vk.DestroyFence          (_device, _fence,           null);

        _vk.DestroyCommandPool(_device, _cmdPool, null);
        _vk.DestroyDevice    (_device,  null);
        _vk.DestroyInstance  (_instance, null);
        _vk.Dispose();
    }

    private static void Check(Result r, string op)
    {
        if (r != Result.Success)
            throw new InvalidOperationException($"Vulkan error in {op}: {r}");
    }
}
