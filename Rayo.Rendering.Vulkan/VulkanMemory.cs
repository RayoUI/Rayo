using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Static helpers for Vulkan buffer and image allocation and data upload.
/// </summary>
internal static unsafe class VulkanMemory
{
    // ── Buffers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a host-visible staging buffer suitable for CPU→GPU data copies.
    /// </summary>
    public static (Silk.NET.Vulkan.Buffer Buffer, DeviceMemory Memory) CreateStagingBuffer(
        VulkanDevice dev, ulong size)
        => CreateBuffer(dev, size,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

    /// <summary>
    /// Creates a device-local buffer (fast GPU memory) with the specified usage.
    /// </summary>
    public static (Silk.NET.Vulkan.Buffer Buffer, DeviceMemory Memory) CreateDeviceBuffer(
        VulkanDevice dev, ulong size, BufferUsageFlags usage)
        => CreateBuffer(dev, size,
            usage | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);

    private static (Silk.NET.Vulkan.Buffer, DeviceMemory) CreateBuffer(
        VulkanDevice dev, ulong size, BufferUsageFlags usage, MemoryPropertyFlags memProps)
    {
        var bufInfo = new BufferCreateInfo
        {
            SType       = StructureType.BufferCreateInfo,
            Size        = size,
            Usage       = usage,
            SharingMode = SharingMode.Exclusive,
        };

        VulkanUtil.Check(dev.Vk.CreateBuffer(dev.Device, &bufInfo, null, out var buffer),
            "CreateBuffer");

        dev.Vk.GetBufferMemoryRequirements(dev.Device, buffer, out var req);

        var allocInfo = new MemoryAllocateInfo
        {
            SType           = StructureType.MemoryAllocateInfo,
            AllocationSize  = req.Size,
            MemoryTypeIndex = dev.FindMemoryType(req.MemoryTypeBits, memProps),
        };

        VulkanUtil.Check(dev.Vk.AllocateMemory(dev.Device, &allocInfo, null, out var mem),
            "AllocateMemory (buffer)");

        dev.Vk.BindBufferMemory(dev.Device, buffer, mem, 0);
        return (buffer, mem);
    }

    /// <summary>
    /// Uploads data to a device-local buffer using an intermediate staging buffer.
    /// </summary>
    public static void UploadBuffer<T>(
        VulkanDevice dev, Silk.NET.Vulkan.Buffer dst, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        ulong bytes = (ulong)(data.Length * Unsafe.SizeOf<T>());
        if (bytes == 0) return;

        var (staging, stagingMem) = CreateStagingBuffer(dev, bytes);

        // Map, copy, unmap.
        void* mapped;
        dev.Vk.MapMemory(dev.Device, stagingMem, 0, bytes, 0, &mapped);
        fixed (T* src = data)
            System.Buffer.MemoryCopy(src, mapped, (long)bytes, (long)bytes);
        dev.Vk.UnmapMemory(dev.Device, stagingMem);

        // Copy staging → device buffer via one-time command.
        dev.OneTimeSubmit(cb =>
        {
            var region = new BufferCopy { Size = bytes };
            dev.Vk.CmdCopyBuffer(cb, staging, dst, 1, &region);
        });

        dev.Vk.DestroyBuffer(dev.Device, staging, null);
        dev.Vk.FreeMemory(dev.Device, stagingMem, null);
    }

    // ── Images ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a VkImage + device memory with the given format and usage.
    /// </summary>
    public static (Image Image, DeviceMemory Memory) CreateImage(
        VulkanDevice dev, uint w, uint h, Format fmt, ImageUsageFlags usage)
    {
        var imgInfo = new ImageCreateInfo
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

        VulkanUtil.Check(dev.Vk.CreateImage(dev.Device, &imgInfo, null, out Image image),
            "CreateImage");

        dev.Vk.GetImageMemoryRequirements(dev.Device, image, out var req);

        var allocInfo = new MemoryAllocateInfo
        {
            SType           = StructureType.MemoryAllocateInfo,
            AllocationSize  = req.Size,
            MemoryTypeIndex = dev.FindMemoryType(req.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
        };

        VulkanUtil.Check(dev.Vk.AllocateMemory(dev.Device, &allocInfo, null, out DeviceMemory mem),
            "AllocateMemory (image)");

        dev.Vk.BindImageMemory(dev.Device, image, mem, 0);
        return (image, mem);
    }

    /// <summary>
    /// Transitions a VkImage from one layout to another via a pipeline barrier.
    /// </summary>
    public static void TransitionImageLayout(
        VulkanDevice dev, Image image, Format format,
        ImageLayout oldLayout, ImageLayout newLayout)
    {
        dev.OneTimeSubmit(cb =>
        {
            var barrier = new ImageMemoryBarrier
            {
                SType               = StructureType.ImageMemoryBarrier,
                OldLayout           = oldLayout,
                NewLayout           = newLayout,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image               = image,
                SubresourceRange    = new ImageSubresourceRange
                {
                    AspectMask     = ImageAspectFlags.ColorBit,
                    LevelCount     = 1,
                    LayerCount     = 1,
                },
            };

            PipelineStageFlags srcStage, dstStage;

            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStage = PipelineStageFlags.TopOfPipeBit;
                dstStage = PipelineStageFlags.TransferBit;
            }
            else if (oldLayout == ImageLayout.TransferDstOptimal &&
                     newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStage = PipelineStageFlags.TransferBit;
                dstStage = PipelineStageFlags.FragmentShaderBit;
            }
            else
            {
                barrier.SrcAccessMask = AccessFlags.MemoryReadBit;
                barrier.DstAccessMask = AccessFlags.MemoryWriteBit;
                srcStage = PipelineStageFlags.AllCommandsBit;
                dstStage = PipelineStageFlags.AllCommandsBit;
            }

            dev.Vk.CmdPipelineBarrier(cb,
                srcStage, dstStage,
                0, 0, null, 0, null, 1, &barrier);
        });
    }

    /// <summary>
    /// Uploads raw pixel bytes to a VkImage via a staging buffer.
    /// Assumes the image starts in Undefined layout and transitions to
    /// ShaderReadOnlyOptimal after the copy.
    /// </summary>
    public static void UploadImage(
        VulkanDevice dev, Image dst, uint w, uint h, Format fmt, ReadOnlySpan<byte> pixels)
    {
        ulong bytes = (ulong)pixels.Length;

        var (staging, stagingMem) = CreateStagingBuffer(dev, bytes);

        void* mapped;
        dev.Vk.MapMemory(dev.Device, stagingMem, 0, bytes, 0, &mapped);
        fixed (byte* src = pixels)
            System.Buffer.MemoryCopy(src, mapped, (long)bytes, (long)bytes);
        dev.Vk.UnmapMemory(dev.Device, stagingMem);

        TransitionImageLayout(dev, dst, fmt, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

        dev.OneTimeSubmit(cb =>
        {
            var region = new BufferImageCopy
            {
                BufferOffset      = 0,
                BufferRowLength   = 0,
                BufferImageHeight = 0,
                ImageSubresource  = new ImageSubresourceLayers
                {
                    AspectMask     = ImageAspectFlags.ColorBit,
                    LayerCount     = 1,
                },
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D(w, h, 1),
            };

            dev.Vk.CmdCopyBufferToImage(cb, staging, dst,
                ImageLayout.TransferDstOptimal, 1, &region);
        });

        TransitionImageLayout(dev, dst, fmt,
            ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        dev.Vk.DestroyBuffer(dev.Device, staging, null);
        dev.Vk.FreeMemory(dev.Device, stagingMem, null);
    }
}
