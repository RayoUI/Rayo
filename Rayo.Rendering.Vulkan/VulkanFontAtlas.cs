using Silk.NET.Vulkan;
using StbTrueTypeSharp;
using System.Numerics;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Vulkan port of OpenGLFontAtlas.
/// Rasterises a TrueType font via StbTrueType into an R8 atlas, uploads it
/// as a VkImage, and exposes a descriptor set for binding to the text pipeline.
/// </summary>
internal sealed unsafe class VulkanFontAtlas : IFont
{
    private readonly VulkanDevice  _dev;
    private bool _disposed;

    // GPU resources
    private Image       _atlasImage;
    private DeviceMemory _atlasMemory;
    private ImageView   _atlasView;
    private Sampler     _sampler;
    private DescriptorPool _descPool;

    // Glyph data
    private readonly Dictionary<int, CharInfo> _glyphMap = new();
    private float _scale;

    // Font metrics
    private float _ascent;
    private float _descent;
    private float _lineGap;

    // IFont
    public string Name  { get; }
    public float  Size  { get; }

    // Descriptor
    public DescriptorSet DescriptorSet { get; private set; }

    public float Ascent     => _ascent;
    public float Descent    => _descent;
    public float LineHeight => _ascent - _descent + _lineGap;

    public struct CharInfo
    {
        public float X0, Y0, X1, Y1;   // Atlas UV coords (0–1)
        public float OffsetX, OffsetY; // Render offset relative to baseline
        public float AdvanceX;
        public float Width, Height;    // Glyph pixel size
    }

    public VulkanFontAtlas(VulkanDevice dev, DescriptorSetLayout descSetLayout,
        byte[] fontData, float fontSize, string name = "Default")
    {
        _dev  = dev;
        Size  = fontSize;
        Name  = name;

        GenerateAtlas(fontData, fontSize);
        CreateSampler();
        AllocateDescriptorSet(descSetLayout);
        UpdateDescriptorSet();
    }

    // ── Atlas generation ──────────────────────────────────────────────────

    private void GenerateAtlas(byte[] fontData, float fontSize)
    {
        const int W = 4096, H = 4096;
        var fontInfo = new StbTrueType.stbtt_fontinfo();
        fixed (byte* ptr = fontData)
        {
            if (StbTrueType.stbtt_InitFont(fontInfo, ptr, 0) == 0)
                throw new Exception("Failed to initialise font");

            _scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, fontSize);

            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);
            _ascent  = ascent  * _scale;
            _descent = descent * _scale;
            _lineGap = lineGap * _scale;

            byte[] atlasData = new byte[W * H];
            int penX = 0, penY = 0, rowHeight = 0;

            foreach (int cp in GetCodepoints())
            {
                int glyphIdx = StbTrueType.stbtt_FindGlyphIndex(fontInfo, cp);
                if (cp >= 0x00A0 && glyphIdx == 0) continue;

                int advance, lsb;
                StbTrueType.stbtt_GetCodepointHMetrics(fontInfo, cp, &advance, &lsb);

                int x0, y0, x1, y1;
                StbTrueType.stbtt_GetCodepointBitmapBox(fontInfo, cp, _scale, _scale,
                    &x0, &y0, &x1, &y1);

                int gw = x1 - x0, gh = y1 - y0;

                if (gw <= 0 || gh <= 0)
                {
                    _glyphMap[cp] = new CharInfo { AdvanceX = advance * _scale };
                    continue;
                }

                if (penX + gw + 1 >= W) { penX = 0; penY += rowHeight + 1; rowHeight = 0; }
                if (penY + gh >= H) break;

                fixed (byte* atlasPtr = atlasData)
                {
                    byte* dst = atlasPtr + penY * W + penX;
                    StbTrueType.stbtt_MakeCodepointBitmap(fontInfo, dst, gw, gh, W,
                        _scale, _scale, cp);
                }

                _glyphMap[cp] = new CharInfo
                {
                    X0      = (float)penX        / W,
                    Y0      = (float)penY        / H,
                    X1      = (float)(penX + gw) / W,
                    Y1      = (float)(penY + gh) / H,
                    OffsetX = x0,
                    OffsetY = -y0,
                    AdvanceX = advance * _scale,
                    Width   = gw,
                    Height  = gh,
                };

                penX += gw + 1;
                rowHeight = Math.Max(rowHeight, gh);
            }

            // Upload atlas to GPU.
            (_atlasImage, _atlasMemory) = VulkanMemory.CreateImage(
                _dev, (uint)W, (uint)H,
                Format.R8Unorm,
                ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit);

            VulkanMemory.UploadImage(_dev, _atlasImage, (uint)W, (uint)H,
                Format.R8Unorm, atlasData);

            _atlasView = CreateImageView();
        }
    }

    private static IEnumerable<int> GetCodepoints()
    {
        for (int i = 0x0020; i <= 0x007E; i++) yield return i;
        for (int i = 0x00A0; i <= 0x00FF; i++) yield return i;
        for (int i = 0xE000; i <= 0xF8FF; i++) yield return i;
    }

    // ── Vulkan resource creation ───────────────────────────────────────────

    private ImageView CreateImageView()
    {
        var viewInfo = new ImageViewCreateInfo
        {
            SType    = StructureType.ImageViewCreateInfo,
            Image    = _atlasImage,
            ViewType = ImageViewType.Type2D,
            Format   = Format.R8Unorm,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        VulkanUtil.Check(_dev.Vk.CreateImageView(_dev.Device, &viewInfo, null, out ImageView iv),
            "FontAtlas CreateImageView");
        return iv;
    }

    private void CreateSampler()
    {
        var samplerInfo = new SamplerCreateInfo
        {
            SType        = StructureType.SamplerCreateInfo,
            MagFilter    = Filter.Linear,
            MinFilter    = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
        };

        VulkanUtil.Check(_dev.Vk.CreateSampler(_dev.Device, &samplerInfo, null, out _sampler),
            "FontAtlas CreateSampler");
    }

    private void AllocateDescriptorSet(DescriptorSetLayout layout)
    {
        var poolSize = new DescriptorPoolSize
        {
            Type            = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
        };

        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType         = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes    = &poolSize,
            MaxSets       = 1,
        };

        VulkanUtil.Check(_dev.Vk.CreateDescriptorPool(_dev.Device, &poolInfo, null, out _descPool),
            "FontAtlas CreateDescriptorPool");

        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType              = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool     = _descPool,
            DescriptorSetCount = 1,
            PSetLayouts        = &layout,
        };

        VulkanUtil.Check(_dev.Vk.AllocateDescriptorSets(_dev.Device, &allocInfo, out DescriptorSet ds),
            "FontAtlas AllocateDescriptorSets");
        DescriptorSet = ds;
    }

    private void UpdateDescriptorSet()
    {
        var imageInfo = new DescriptorImageInfo
        {
            Sampler     = _sampler,
            ImageView   = _atlasView,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
        };

        var write = new WriteDescriptorSet
        {
            SType           = StructureType.WriteDescriptorSet,
            DstSet          = DescriptorSet,
            DstBinding      = 0,
            DescriptorCount = 1,
            DescriptorType  = DescriptorType.CombinedImageSampler,
            PImageInfo      = &imageInfo,
        };

        _dev.Vk.UpdateDescriptorSets(_dev.Device, 1, &write, 0, null);
    }

    // ── Glyph lookup ──────────────────────────────────────────────────────

    public bool TryGetGlyph(int codePoint, out CharInfo info)
        => _glyphMap.TryGetValue(codePoint, out info);

    public bool TryGetChar(char c, out CharInfo info)
        => _glyphMap.TryGetValue((int)c, out info);

    /// <summary>
    /// Measures the rendered dimensions of <paramref name="text"/>.
    /// </summary>
    public Vector2 MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;

        float width = 0;
        CharInfo? last = null;
        float lastAdv = 0;

        for (int i = 0; i < text.Length;)
        {
            int cp = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            if (_glyphMap.TryGetValue(cp, out var info))
            {
                if (last.HasValue) width += lastAdv;
                last = info; lastAdv = info.AdvanceX;
            }
            else if (cp == 0x0020)
            {
                width += Size * 0.25f;
            }
        }

        if (last.HasValue) width += last.Value.OffsetX + last.Value.Width;
        return new Vector2(width, LineHeight);
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dev.Vk.DestroyDescriptorPool(_dev.Device, _descPool, null);
        _dev.Vk.DestroySampler(_dev.Device, _sampler, null);
        _dev.Vk.DestroyImageView(_dev.Device, _atlasView, null);
        _dev.Vk.DestroyImage(_dev.Device, _atlasImage, null);
        _dev.Vk.FreeMemory(_dev.Device, _atlasMemory, null);
    }
}
