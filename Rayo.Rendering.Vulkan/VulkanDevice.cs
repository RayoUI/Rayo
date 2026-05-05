using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Runtime.InteropServices;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Owns the core Vulkan stack: Instance, PhysicalDevice, logical Device,
/// graphics Queue, and CommandPool.  Also loads the KhrSurface and
/// KhrSwapchain extension APIs for use by VulkanSwapchain.
/// </summary>
internal sealed unsafe class VulkanDevice : IDisposable
{
    public readonly Vk Vk;
    public readonly Instance Instance;
    public readonly PhysicalDevice PhysicalDevice;
    public readonly Device Device;
    public readonly Queue GraphicsQueue;
    public readonly uint GraphicsQueueFamily;
    public readonly CommandPool CommandPool;

    public KhrSurface KhrSurface { get; private set; }
    public KhrSwapchain KhrSwapchain { get; private set; }

    private bool _disposed;

    public VulkanDevice(IWindow window)
    {
        Vk = Vk.GetApi();
        Instance = CreateInstance(window);
        (PhysicalDevice, GraphicsQueueFamily) = PickPhysicalDevice();
        Device = CreateLogicalDevice();
        Vk.GetDeviceQueue(Device, GraphicsQueueFamily, 0, out GraphicsQueue);
        CommandPool = CreateCommandPool();

        if (!Vk.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
            throw new InvalidOperationException("KHR_surface extension not available");
        KhrSurface = khrSurface;

        if (!Vk.TryGetDeviceExtension(Instance, Device, out KhrSwapchain khrSwapchain))
            throw new InvalidOperationException("KHR_swapchain extension not available");
        KhrSwapchain = khrSwapchain;
    }

    // ── Instance ─────────────────────────────────────────────────────────

    private Instance CreateInstance(IWindow window)
    {
        var appInfo = new ApplicationInfo
        {
            SType            = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr("Rayo"),
            ApplicationVersion = Vk.MakeVersion(1, 0, 0),
            PEngineName      = (byte*)SilkMarshal.StringToPtr("Rayo"),
            EngineVersion    = Vk.MakeVersion(1, 0, 0),
            ApiVersion       = Vk.Version13,
        };

        // Collect required instance extensions from the windowing layer.
        var windowExts = SilkMarshal.PtrToStringArray(
            (nint)window.VkSurface!.GetRequiredExtensions(out uint count), (int)count);

        var extensions = new List<string>(windowExts);

#if DEBUG
        extensions.Add(ExtDebugUtils.ExtensionName);
#endif

        var extPtrs = SilkMarshal.StringArrayToPtr(extensions);

#if DEBUG
        var layers = new[] { "VK_LAYER_KHRONOS_validation" };
        var layerPtrs = SilkMarshal.StringArrayToPtr(layers);
#endif

        var createInfo = new InstanceCreateInfo
        {
            SType                   = StructureType.InstanceCreateInfo,
            PApplicationInfo        = &appInfo,
            EnabledExtensionCount   = (uint)extensions.Count,
            PpEnabledExtensionNames = (byte**)extPtrs,
#if DEBUG
            EnabledLayerCount       = (uint)layers.Length,
            PpEnabledLayerNames     = (byte**)layerPtrs,
#else
            EnabledLayerCount       = 0,
#endif
        };

        VulkanUtil.Check(Vk.CreateInstance(&createInfo, null, out Instance inst), "CreateInstance");

        SilkMarshal.Free(extPtrs);
#if DEBUG
        SilkMarshal.Free(layerPtrs);
#endif

        return inst;
    }

    // ── Physical device ───────────────────────────────────────────────────

    private (PhysicalDevice gpu, uint queueFamily) PickPhysicalDevice()
    {
        uint count = 0;
        Vk.EnumeratePhysicalDevices(Instance, &count, null);
        if (count == 0)
            throw new InvalidOperationException("No Vulkan-capable GPU found");

        var gpus = new PhysicalDevice[count];
        fixed (PhysicalDevice* ptr = gpus)
            Vk.EnumeratePhysicalDevices(Instance, &count, ptr);

        // Prefer discrete GPU, fall back to integrated or any available.
        PhysicalDevice? discrete   = null;
        PhysicalDevice? integrated = null;

        foreach (var gpu in gpus)
        {
            Vk.GetPhysicalDeviceProperties(gpu, out var props);
            if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)
                discrete ??= gpu;
            else if (props.DeviceType == PhysicalDeviceType.IntegratedGpu)
                integrated ??= gpu;
        }

        var chosen = discrete ?? integrated ?? gpus[0];
        uint family = FindGraphicsQueueFamily(chosen);
        return (chosen, family);
    }

    private uint FindGraphicsQueueFamily(PhysicalDevice gpu)
    {
        uint count = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &count, null);

        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* ptr = families)
            Vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &count, ptr);

        for (uint i = 0; i < families.Length; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                return i;
        }

        throw new InvalidOperationException("No graphics queue family found");
    }

    // ── Logical device ────────────────────────────────────────────────────

    private Device CreateLogicalDevice()
    {
        float priority = 1.0f;
        var queueInfo = new DeviceQueueCreateInfo
        {
            SType            = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = GraphicsQueueFamily,
            QueueCount       = 1,
            PQueuePriorities = &priority,
        };

        var swapchainExtName = (byte*)SilkMarshal.StringToPtr(KhrSwapchain.ExtensionName);
        var features = new PhysicalDeviceFeatures();

        var deviceInfo = new DeviceCreateInfo
        {
            SType                   = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount    = 1,
            PQueueCreateInfos       = &queueInfo,
            EnabledExtensionCount   = 1,
            PpEnabledExtensionNames = &swapchainExtName,
            PEnabledFeatures        = &features,
        };

        VulkanUtil.Check(Vk.CreateDevice(PhysicalDevice, &deviceInfo, null, out Device dev),
            "CreateDevice");

        SilkMarshal.Free((nint)swapchainExtName);
        return dev;
    }

    // ── Command pool ──────────────────────────────────────────────────────

    private CommandPool CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType            = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = GraphicsQueueFamily,
            Flags            = CommandPoolCreateFlags.ResetCommandBufferBit,
        };

        VulkanUtil.Check(Vk.CreateCommandPool(Device, &poolInfo, null, out CommandPool pool),
            "CreateCommandPool");
        return pool;
    }

    // ── Memory helper ─────────────────────────────────────────────────────

    /// <summary>
    /// Finds a memory type index that satisfies the given type filter and property flags.
    /// </summary>
    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out var memProps);

        for (uint i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 &&
                (memProps.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }

        throw new InvalidOperationException(
            $"No suitable memory type for filter=0x{typeFilter:X} props={properties}");
    }

    // ── One-time command helper ───────────────────────────────────────────

    /// <summary>
    /// Allocates a temporary command buffer, records the provided action,
    /// submits it, and waits for it to complete.
    /// </summary>
    public void OneTimeSubmit(Action<CommandBuffer> record)
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType              = StructureType.CommandBufferAllocateInfo,
            CommandPool        = CommandPool,
            Level              = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        Vk.AllocateCommandBuffers(Device, &allocInfo, out CommandBuffer cb);

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        Vk.BeginCommandBuffer(cb, &beginInfo);

        record(cb);

        Vk.EndCommandBuffer(cb);

        var submitInfo = new SubmitInfo
        {
            SType                = StructureType.SubmitInfo,
            CommandBufferCount   = 1,
            PCommandBuffers      = &cb,
        };
        Vk.QueueSubmit(GraphicsQueue, 1, &submitInfo, default);
        Vk.QueueWaitIdle(GraphicsQueue);

        Vk.FreeCommandBuffers(Device, CommandPool, 1, &cb);
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Vk.DestroyCommandPool(Device, CommandPool, null);
        Vk.DestroyDevice(Device, null);
        Vk.DestroyInstance(Instance, null);
        Vk.Dispose();
    }
}
