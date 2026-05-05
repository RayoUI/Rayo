using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Creates and owns the three graphics pipelines used by VulkanRenderer:
/// Shape (filled geometry), Text (R8 font atlas), and Image (RGBA texture).
/// </summary>
internal sealed unsafe class VulkanPipelines : IDisposable
{
    private readonly VulkanDevice _dev;
    private bool _disposed;

    // Pipeline handles
    public Pipeline ShapePipeline  { get; private set; }
    public Pipeline TextPipeline   { get; private set; }
    public Pipeline ImagePipeline  { get; private set; }

    // Pipeline layouts
    public PipelineLayout ShapeLayout { get; private set; }
    public PipelineLayout TextLayout  { get; private set; }
    public PipelineLayout ImageLayout { get; private set; }

    // Descriptor set layouts (for texture/font pipelines)
    public DescriptorSetLayout TextDescriptorSetLayout  { get; private set; }
    public DescriptorSetLayout ImageDescriptorSetLayout { get; private set; }

    public VulkanPipelines(VulkanDevice dev, RenderPass renderPass)
    {
        _dev = dev;
        Build(renderPass);
    }

    // ── Build ─────────────────────────────────────────────────────────────

    private void Build(RenderPass renderPass)
    {
        TextDescriptorSetLayout  = CreateSamplerDescriptorSetLayout();
        ImageDescriptorSetLayout = CreateSamplerDescriptorSetLayout();

        // Push constant: mat4 (64 bytes) shared by all pipelines.
        var pcRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset     = 0,
            Size       = 64,
        };

        ShapeLayout = CreatePipelineLayout(null, &pcRange, 1);

        // Properties can't have their address taken directly; copy to locals first.
        DescriptorSetLayout textLayout  = TextDescriptorSetLayout;
        DescriptorSetLayout imageLayout = ImageDescriptorSetLayout;
        TextLayout  = CreatePipelineLayout(&textLayout,  &pcRange, 1);
        ImageLayout = CreatePipelineLayout(&imageLayout, &pcRange, 1);

        ShapePipeline = BuildPipeline(renderPass,
            VulkanShaders.ShapeVertex, VulkanShaders.ShapeFragment,
            ShapeLayout, isShape: true);

        TextPipeline = BuildPipeline(renderPass,
            VulkanShaders.TextVertex, VulkanShaders.TextFragment,
            TextLayout, isShape: false);

        ImagePipeline = BuildPipeline(renderPass,
            VulkanShaders.ImageVertex, VulkanShaders.ImageFragment,
            ImageLayout, isShape: false);
    }

    public void Recreate(RenderPass renderPass)
    {
        DestroyPipelines();
        Build(renderPass);
    }

    // ── Descriptor set layout ─────────────────────────────────────────────

    private DescriptorSetLayout CreateSamplerDescriptorSetLayout()
    {
        var binding = new DescriptorSetLayoutBinding
        {
            Binding            = 0,
            DescriptorType     = DescriptorType.CombinedImageSampler,
            DescriptorCount    = 1,
            StageFlags         = ShaderStageFlags.FragmentBit,
        };

        var info = new DescriptorSetLayoutCreateInfo
        {
            SType        = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings    = &binding,
        };

        VulkanUtil.Check(
            _dev.Vk.CreateDescriptorSetLayout(_dev.Device, &info, null, out DescriptorSetLayout layout),
            "CreateDescriptorSetLayout");
        return layout;
    }

    // ── Pipeline layout ───────────────────────────────────────────────────

    private PipelineLayout CreatePipelineLayout(
        DescriptorSetLayout* setLayouts, PushConstantRange* pcRanges, uint pcCount)
    {
        bool hasSet = setLayouts != null;

        var info = new PipelineLayoutCreateInfo
        {
            SType                  = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount         = hasSet ? 1u : 0u,
            PSetLayouts            = hasSet ? setLayouts : null,
            PushConstantRangeCount = pcCount,
            PPushConstantRanges    = pcRanges,
        };

        VulkanUtil.Check(
            _dev.Vk.CreatePipelineLayout(_dev.Device, &info, null, out PipelineLayout layout),
            "CreatePipelineLayout");
        return layout;
    }

    // ── Pipeline construction ─────────────────────────────────────────────

    private Pipeline BuildPipeline(RenderPass renderPass,
        string vertGlsl, string fragGlsl, PipelineLayout layout, bool isShape)
    {
        var vertSpv = CompileGlsl(vertGlsl, ShaderKind.VertexShader);
        var fragSpv = CompileGlsl(fragGlsl, ShaderKind.FragmentShader);

        ShaderModule vertModule = CreateShaderModule(vertSpv);
        ShaderModule fragModule = CreateShaderModule(fragSpv);

        var entryPoint = (byte*)Marshal.StringToHGlobalAnsi("main");

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[2];
        shaderStages[0] = new PipelineShaderStageCreateInfo
        {
            SType  = StructureType.PipelineShaderStageCreateInfo,
            Stage  = ShaderStageFlags.VertexBit,
            Module = vertModule,
            PName  = entryPoint,
        };
        shaderStages[1] = new PipelineShaderStageCreateInfo
        {
            SType  = StructureType.PipelineShaderStageCreateInfo,
            Stage  = ShaderStageFlags.FragmentBit,
            Module = fragModule,
            PName  = entryPoint,
        };

        // Vertex input state differs between shape (pos+color) and text/image (pos+uv+color).
        VertexInputBindingDescription binding;
        VertexInputAttributeDescription* attribs;
        uint attribCount;

        if (isShape)
        {
            // ShapeVertex: vec2 pos (8), vec4 color (16) = 24 bytes
            binding = new VertexInputBindingDescription
            {
                Binding   = 0,
                Stride    = 24,
                InputRate = VertexInputRate.Vertex,
            };

            var a = stackalloc VertexInputAttributeDescription[2];
            a[0] = new VertexInputAttributeDescription { Location = 0, Binding = 0, Format = Format.R32G32Sfloat,         Offset = 0 };
            a[1] = new VertexInputAttributeDescription { Location = 1, Binding = 0, Format = Format.R32G32B32A32Sfloat,   Offset = 8 };
            attribs = a;
            attribCount = 2;
        }
        else
        {
            // TextVertex / ImageVertex: vec2 pos (8), vec2 uv (8), vec4 color (16) = 32 bytes
            binding = new VertexInputBindingDescription
            {
                Binding   = 0,
                Stride    = 32,
                InputRate = VertexInputRate.Vertex,
            };

            var a = stackalloc VertexInputAttributeDescription[3];
            a[0] = new VertexInputAttributeDescription { Location = 0, Binding = 0, Format = Format.R32G32Sfloat,       Offset = 0 };
            a[1] = new VertexInputAttributeDescription { Location = 1, Binding = 0, Format = Format.R32G32Sfloat,       Offset = 8 };
            a[2] = new VertexInputAttributeDescription { Location = 2, Binding = 0, Format = Format.R32G32B32A32Sfloat, Offset = 16 };
            attribs = a;
            attribCount = 3;
        }

        var vertexInput = new PipelineVertexInputStateCreateInfo
        {
            SType                           = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount   = 1,
            PVertexBindingDescriptions      = &binding,
            VertexAttributeDescriptionCount = attribCount,
            PVertexAttributeDescriptions    = attribs,
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType                  = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology               = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        // Viewport and scissor are dynamic.
        var dynamicStates = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };
        var dynamicState = new PipelineDynamicStateCreateInfo
        {
            SType             = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 2,
            PDynamicStates    = dynamicStates,
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType         = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount  = 1,
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType       = StructureType.PipelineRasterizationStateCreateInfo,
            PolygonMode = PolygonMode.Fill,
            CullMode    = CullModeFlags.None,
            FrontFace   = FrontFace.CounterClockwise,
            LineWidth   = 1.0f,
        };

        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType                = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable  = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            BlendEnable         = true,
            SrcColorBlendFactor = Silk.NET.Vulkan.BlendFactor.SrcAlpha,
            DstColorBlendFactor = Silk.NET.Vulkan.BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp        = BlendOp.Add,
            SrcAlphaBlendFactor = Silk.NET.Vulkan.BlendFactor.One,
            DstAlphaBlendFactor = Silk.NET.Vulkan.BlendFactor.Zero,
            AlphaBlendOp        = BlendOp.Add,
            ColorWriteMask      = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                                  ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType           = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable   = false,
            AttachmentCount = 1,
            PAttachments    = &colorBlendAttachment,
        };

        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType               = StructureType.GraphicsPipelineCreateInfo,
            StageCount          = 2,
            PStages             = shaderStages,
            PVertexInputState   = &vertexInput,
            PInputAssemblyState = &inputAssembly,
            PViewportState      = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState   = &multisampling,
            PColorBlendState    = &colorBlending,
            PDynamicState       = &dynamicState,
            Layout              = layout,
            RenderPass          = renderPass,
            Subpass             = 0,
        };

        VulkanUtil.Check(
            _dev.Vk.CreateGraphicsPipelines(_dev.Device, default, 1, &pipelineInfo, null, out Pipeline pipeline),
            "CreateGraphicsPipelines");

        _dev.Vk.DestroyShaderModule(_dev.Device, vertModule, null);
        _dev.Vk.DestroyShaderModule(_dev.Device, fragModule, null);
        Marshal.FreeHGlobal((nint)entryPoint);

        return pipeline;
    }

    // ── SPIR-V compilation via Shaderc ────────────────────────────────────

    private static byte[] CompileGlsl(string glsl, ShaderKind kind)
    {
        var shaderc = Shaderc.GetApi();
        var compiler = shaderc.CompilerInitialize();
        var options  = shaderc.CompileOptionsInitialize();

        shaderc.CompileOptionsSetOptimizationLevel(options, OptimizationLevel.Performance);

        var result = shaderc.CompileIntoSpv(
            compiler,
            glsl,
            (nuint)System.Text.Encoding.UTF8.GetByteCount(glsl),
            kind,
            "shader",
            "main",
            options);

        var status = shaderc.ResultGetCompilationStatus(result);
        if (status != CompilationStatus.Success)
        {
            var errMsg = shaderc.ResultGetErrorMessageS(result);
            shaderc.ResultRelease(result);
            shaderc.CompilerRelease(compiler);
            shaderc.Dispose();
            throw new InvalidOperationException($"GLSL compilation failed ({status}): {errMsg}");
        }

        nuint length  = shaderc.ResultGetLength(result);
        var   bytes   = shaderc.ResultGetBytes(result);
        var   spirv   = new byte[length];
        System.Runtime.InteropServices.Marshal.Copy((nint)bytes, spirv, 0, (int)length);

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

            VulkanUtil.Check(
                _dev.Vk.CreateShaderModule(_dev.Device, &info, null, out ShaderModule module),
                "CreateShaderModule");
            return module;
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────

    private void DestroyPipelines()
    {
        _dev.Vk.DestroyPipeline(_dev.Device, ShapePipeline,  null);
        _dev.Vk.DestroyPipeline(_dev.Device, TextPipeline,   null);
        _dev.Vk.DestroyPipeline(_dev.Device, ImagePipeline,  null);
        _dev.Vk.DestroyPipelineLayout(_dev.Device, ShapeLayout, null);
        _dev.Vk.DestroyPipelineLayout(_dev.Device, TextLayout,  null);
        _dev.Vk.DestroyPipelineLayout(_dev.Device, ImageLayout, null);
        _dev.Vk.DestroyDescriptorSetLayout(_dev.Device, TextDescriptorSetLayout,  null);
        _dev.Vk.DestroyDescriptorSetLayout(_dev.Device, ImageDescriptorSetLayout, null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyPipelines();
    }
}
