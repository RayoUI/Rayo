using Silk.NET.Vulkan;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Groups per-frame GPU synchronization primitives:
/// a command buffer, two semaphores, and an in-flight fence.
/// </summary>
internal sealed class VulkanCommandFrame : IDisposable
{
    private readonly Vk _vk;
    private readonly Device _device;
    private bool _disposed;

    public CommandBuffer              CommandBuffer;
    public Silk.NET.Vulkan.Semaphore  ImageAvailableSemaphore;
    public Silk.NET.Vulkan.Semaphore  RenderFinishedSemaphore;
    public Fence                      InFlightFence;

    public unsafe VulkanCommandFrame(Vk vk, Device device, CommandPool commandPool)
    {
        _vk     = vk;
        _device = device;

        // Allocate primary command buffer from the pool.
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType              = StructureType.CommandBufferAllocateInfo,
            CommandPool        = commandPool,
            Level              = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        VulkanUtil.Check(vk.AllocateCommandBuffers(device, &allocInfo, out CommandBuffer),
            "AllocateCommandBuffers");

        // Semaphores.
        var semInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        Silk.NET.Vulkan.Semaphore ias, rfs;
        VulkanUtil.Check(vk.CreateSemaphore(device, &semInfo, null, out ias), "CreateSemaphore (imageAvailable)");
        VulkanUtil.Check(vk.CreateSemaphore(device, &semInfo, null, out rfs), "CreateSemaphore (renderFinished)");
        ImageAvailableSemaphore = ias;
        RenderFinishedSemaphore = rfs;

        // Fence (starts signalled so the first frame does not wait forever).
        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };
        VulkanUtil.Check(vk.CreateFence(device, &fenceInfo, null, out InFlightFence),
            "CreateFence");
    }

    public unsafe void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _vk.DestroyFence(_device, InFlightFence, null);
        _vk.DestroySemaphore(_device, RenderFinishedSemaphore, null);
        _vk.DestroySemaphore(_device, ImageAvailableSemaphore, null);
        // Command buffer is freed when the pool is destroyed.
    }
}

/// <summary>
/// Small helper for Vulkan result checking.
/// </summary>
internal static class VulkanUtil
{
    public static void Check(Result result, string operation)
    {
        if (result != Result.Success)
            throw new InvalidOperationException($"Vulkan operation '{operation}' failed: {result}");
    }
}
