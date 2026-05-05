using Silk.NET.Vulkan;
using StbImageSharp;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Loads, caches, and manages Vulkan textures (RGBA8 VkImage + descriptor set).
/// </summary>
internal sealed unsafe class VulkanTextureManager : IDisposable
{
    private readonly VulkanDevice _dev;
    private readonly DescriptorSetLayout _descLayout;
    private readonly Dictionary<string, VulkanTexture> _cache = new();
    private bool _disposed;

    public VulkanTextureManager(VulkanDevice dev, DescriptorSetLayout descLayout)
    {
        _dev        = dev;
        _descLayout = descLayout;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public VulkanTexture? LoadTexture(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;
        if (_cache.TryGetValue(filePath, out var cached)) return cached;

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[VulkanTextureManager] File not found: {filePath}");
            return null;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return LoadFromStream(stream, filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VulkanTextureManager] Error loading {filePath}: {ex.Message}");
            return null;
        }
    }

    public VulkanTexture? LoadTextureFromStream(Stream stream, string cacheKey)
    {
        if (stream == null || string.IsNullOrEmpty(cacheKey)) return null;
        if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

        try { return LoadFromStream(stream, cacheKey); }
        catch (Exception ex)
        {
            Console.WriteLine($"[VulkanTextureManager] Error loading stream {cacheKey}: {ex.Message}");
            return null;
        }
    }

    public VulkanTexture CreateTextureFromPixels(byte[] rgba, int width, int height)
    {
        return Upload(rgba, width, height, key: null);
    }

    // ── Internal helpers ──────────────────────────────────────────────────

    private VulkanTexture LoadFromStream(Stream stream, string cacheKey)
    {
        StbImage.stbi_set_flip_vertically_on_load(0);
        var img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha)
                  ?? throw new Exception("StbImageSharp failed to decode image");

        var tex = Upload(img.Data, img.Width, img.Height, cacheKey);
        _cache[cacheKey] = tex;
        return tex;
    }

    private VulkanTexture Upload(byte[] rgba, int width, int height, string? key)
    {
        var (image, memory) = VulkanMemory.CreateImage(
            _dev, (uint)width, (uint)height,
            Format.R8G8B8A8Srgb,
            ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit);

        VulkanMemory.UploadImage(_dev, image, (uint)width, (uint)height,
            Format.R8G8B8A8Srgb, rgba);

        var view    = CreateImageView(image);
        var sampler = CreateSampler();
        var ds      = AllocateDescriptorSet(view, sampler);

        return new VulkanTexture(image, memory, view, sampler, ds, width, height, _dev);
    }

    private ImageView CreateImageView(Image image)
    {
        var info = new ImageViewCreateInfo
        {
            SType    = StructureType.ImageViewCreateInfo,
            Image    = image,
            ViewType = ImageViewType.Type2D,
            Format   = Format.R8G8B8A8Srgb,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        VulkanUtil.Check(_dev.Vk.CreateImageView(_dev.Device, &info, null, out ImageView iv),
            "Texture CreateImageView");
        return iv;
    }

    private Sampler CreateSampler()
    {
        var info = new SamplerCreateInfo
        {
            SType        = StructureType.SamplerCreateInfo,
            MagFilter    = Filter.Linear,
            MinFilter    = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
        };

        VulkanUtil.Check(_dev.Vk.CreateSampler(_dev.Device, &info, null, out Sampler s),
            "Texture CreateSampler");
        return s;
    }

    private DescriptorSet AllocateDescriptorSet(ImageView view, Sampler sampler)
    {
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.CombinedImageSampler, DescriptorCount = 1,
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1, PPoolSizes = &poolSize, MaxSets = 1,
        };
        VulkanUtil.Check(_dev.Vk.CreateDescriptorPool(_dev.Device, &poolInfo, null, out DescriptorPool pool),
            "Texture CreateDescriptorPool");

        var layout   = _descLayout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool, DescriptorSetCount = 1, PSetLayouts = &layout,
        };
        VulkanUtil.Check(_dev.Vk.AllocateDescriptorSets(_dev.Device, &allocInfo, out DescriptorSet ds),
            "Texture AllocateDescriptorSets");

        var imgInfo = new DescriptorImageInfo
        {
            Sampler = sampler, ImageView = view, ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = ds, DstBinding = 0, DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler, PImageInfo = &imgInfo,
        };
        _dev.Vk.UpdateDescriptorSets(_dev.Device, 1, &write, 0, null);

        // Store pool inside VulkanTexture so it can be freed.
        // We stash it via a side-channel: return ds and let VulkanTexture own pool via ctor.
        // For simplicity, store pool in a thread-safe dict keyed by ds handle.
        _poolMap[ds.Handle] = pool;

        return ds;
    }

    // Pool map so VulkanTexture can free its pool on disposal.
    internal readonly Dictionary<ulong, DescriptorPool> _poolMap = new();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var tex in _cache.Values)
            tex.Dispose();
        _cache.Clear();
    }
}

/// <summary>
/// Holds all Vulkan resources for a single loaded texture.
/// </summary>
internal sealed unsafe class VulkanTexture : ITexture
{
    private readonly VulkanDevice _dev;
    private readonly Image         _image;
    private readonly DeviceMemory  _memory;
    private readonly ImageView     _view;
    private readonly Sampler       _sampler;
    private bool _disposed;

    public DescriptorSet DescriptorSet { get; }
    public int Width  { get; }
    public int Height { get; }

    internal VulkanTexture(
        Image image, DeviceMemory memory, ImageView view,
        Sampler sampler, DescriptorSet ds,
        int width, int height, VulkanDevice dev)
    {
        _image        = image;
        _memory       = memory;
        _view         = view;
        _sampler      = sampler;
        DescriptorSet = ds;
        Width         = width;
        Height        = height;
        _dev          = dev;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dev.Vk.DestroySampler(_dev.Device, _sampler, null);
        _dev.Vk.DestroyImageView(_dev.Device, _view, null);
        _dev.Vk.DestroyImage(_dev.Device, _image, null);
        _dev.Vk.FreeMemory(_dev.Device, _memory, null);
    }
}
