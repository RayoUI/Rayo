using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Owns the Vulkan swapchain lifecycle: surface, swapchain images/views,
/// render pass, and framebuffers.  Call <see cref="Recreate"/> after a
/// window resize or when the swapchain becomes out-of-date.
/// </summary>
internal sealed unsafe class VulkanSwapchain : IDisposable
{
    private readonly VulkanDevice _dev;
    private readonly IWindow _window;
    private bool _disposed;

    // Surface
    public SurfaceKHR Surface { get; private set; }

    // Swapchain
    public SwapchainKHR Swapchain { get; private set; }
    public Format       ImageFormat { get; private set; }
    public Extent2D     Extent      { get; private set; }

    // Per-image resources
    public Image[]     Images     { get; private set; } = [];
    public ImageView[] ImageViews { get; private set; } = [];
    public Framebuffer[] Framebuffers { get; private set; } = [];

    // Render pass shared by all framebuffers
    public RenderPass RenderPass { get; private set; }

    public VulkanSwapchain(VulkanDevice dev, IWindow window)
    {
        _dev    = dev;
        _window = window;

        Surface = CreateSurface();
        CreateSwapchain();
        RenderPass = CreateRenderPass();
        CreateFramebuffers();
    }

    // ── Surface ───────────────────────────────────────────────────────────

    private SurfaceKHR CreateSurface()
    {
        return _window.VkSurface!.Create<AllocationCallbacks>(
            _dev.Instance.ToHandle(), null).ToSurface();
    }

    // ── Swapchain ─────────────────────────────────────────────────────────

    private void CreateSwapchain()
    {
        _dev.KhrSurface.GetPhysicalDeviceSurfaceCapabilities(
            _dev.PhysicalDevice, Surface, out var caps);

        var format  = ChooseSurfaceFormat();
        var present = ChoosePresentMode();
        var extent  = ChooseExtent(caps);

        uint imageCount = caps.MinImageCount + 1;
        if (caps.MaxImageCount > 0 && imageCount > caps.MaxImageCount)
            imageCount = caps.MaxImageCount;

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType            = StructureType.SwapchainCreateInfoKhr,
            Surface          = Surface,
            MinImageCount    = imageCount,
            ImageFormat      = format.Format,
            ImageColorSpace  = format.ColorSpace,
            ImageExtent      = extent,
            ImageArrayLayers = 1,
            ImageUsage       = ImageUsageFlags.ColorAttachmentBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform     = caps.CurrentTransform,
            CompositeAlpha   = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode      = present,
            Clipped          = true,
        };

        VulkanUtil.Check(
            _dev.KhrSwapchain.CreateSwapchain(_dev.Device, &createInfo, null, out SwapchainKHR sc),
            "CreateSwapchain");

        Swapchain   = sc;
        ImageFormat = format.Format;
        Extent      = extent;

        // Retrieve swapchain images.
        uint cnt = 0;
        _dev.KhrSwapchain.GetSwapchainImages(_dev.Device, Swapchain, &cnt, null);
        Images = new Image[cnt];
        fixed (Image* ptr = Images)
            _dev.KhrSwapchain.GetSwapchainImages(_dev.Device, Swapchain, &cnt, ptr);

        // Create image views.
        ImageViews = new ImageView[cnt];
        for (int i = 0; i < cnt; i++)
            ImageViews[i] = CreateImageView(Images[i], ImageFormat, ImageAspectFlags.ColorBit);
    }

    private SurfaceFormatKHR ChooseSurfaceFormat()
    {
        uint cnt = 0;
        _dev.KhrSurface.GetPhysicalDeviceSurfaceFormats(_dev.PhysicalDevice, Surface, &cnt, null);
        var formats = new SurfaceFormatKHR[cnt];
        fixed (SurfaceFormatKHR* ptr = formats)
            _dev.KhrSurface.GetPhysicalDeviceSurfaceFormats(_dev.PhysicalDevice, Surface, &cnt, ptr);

        foreach (var fmt in formats)
        {
            if (fmt.Format     == Format.B8G8R8A8Srgb &&
                fmt.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                return fmt;
        }

        return formats[0];
    }

    private PresentModeKHR ChoosePresentMode()
    {
        uint cnt = 0;
        _dev.KhrSurface.GetPhysicalDeviceSurfacePresentModes(_dev.PhysicalDevice, Surface, &cnt, null);
        var modes = new PresentModeKHR[cnt];
        fixed (PresentModeKHR* ptr = modes)
            _dev.KhrSurface.GetPhysicalDeviceSurfacePresentModes(_dev.PhysicalDevice, Surface, &cnt, ptr);

        foreach (var mode in modes)
        {
            if (mode == PresentModeKHR.MailboxKhr)
                return mode;
        }

        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseExtent(SurfaceCapabilitiesKHR caps)
    {
        if (caps.CurrentExtent.Width != uint.MaxValue)
            return caps.CurrentExtent;

        return new Extent2D(
            Math.Clamp((uint)_window.Size.X, caps.MinImageExtent.Width, caps.MaxImageExtent.Width),
            Math.Clamp((uint)_window.Size.Y, caps.MinImageExtent.Height, caps.MaxImageExtent.Height));
    }

    // ── Render pass ───────────────────────────────────────────────────────

    private RenderPass CreateRenderPass()
    {
        var colorAttach = new AttachmentDescription
        {
            Format         = ImageFormat,
            Samples        = SampleCountFlags.Count1Bit,
            LoadOp         = AttachmentLoadOp.Clear,
            StoreOp        = AttachmentStoreOp.Store,
            StencilLoadOp  = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout  = ImageLayout.Undefined,
            FinalLayout    = ImageLayout.PresentSrcKhr,
        };

        var colorRef = new AttachmentReference
        {
            Attachment = 0,
            Layout     = ImageLayout.ColorAttachmentOptimal,
        };

        var subpass = new SubpassDescription
        {
            PipelineBindPoint       = PipelineBindPoint.Graphics,
            ColorAttachmentCount    = 1,
            PColorAttachments       = &colorRef,
        };

        var dependency = new SubpassDependency
        {
            SrcSubpass    = Vk.SubpassExternal,
            DstSubpass    = 0,
            SrcStageMask  = PipelineStageFlags.ColorAttachmentOutputBit,
            DstStageMask  = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit,
        };

        var rpInfo = new RenderPassCreateInfo
        {
            SType           = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments    = &colorAttach,
            SubpassCount    = 1,
            PSubpasses      = &subpass,
            DependencyCount = 1,
            PDependencies   = &dependency,
        };

        VulkanUtil.Check(_dev.Vk.CreateRenderPass(_dev.Device, &rpInfo, null, out RenderPass rp),
            "CreateRenderPass");
        return rp;
    }

    // ── Framebuffers ──────────────────────────────────────────────────────

    private void CreateFramebuffers()
    {
        Framebuffers = new Framebuffer[ImageViews.Length];

        for (int i = 0; i < ImageViews.Length; i++)
        {
            var view = ImageViews[i];
            var fbInfo = new FramebufferCreateInfo
            {
                SType           = StructureType.FramebufferCreateInfo,
                RenderPass      = RenderPass,
                AttachmentCount = 1,
                PAttachments    = &view,
                Width           = Extent.Width,
                Height          = Extent.Height,
                Layers          = 1,
            };

            VulkanUtil.Check(_dev.Vk.CreateFramebuffer(_dev.Device, &fbInfo, null, out Framebuffers[i]),
                "CreateFramebuffer");
        }
    }

    // ── Image view helper ─────────────────────────────────────────────────

    public ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspect)
    {
        var viewInfo = new ImageViewCreateInfo
        {
            SType    = StructureType.ImageViewCreateInfo,
            Image    = image,
            ViewType = ImageViewType.Type2D,
            Format   = format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask     = aspect,
                LevelCount     = 1,
                LayerCount     = 1,
            },
        };

        VulkanUtil.Check(_dev.Vk.CreateImageView(_dev.Device, &viewInfo, null, out ImageView view),
            "CreateImageView");
        return view;
    }

    // ── Recreate after resize ─────────────────────────────────────────────

    /// <summary>
    /// Destroys and recreates the swapchain, image views, and framebuffers
    /// for the new window size.  The render pass is reused unchanged.
    /// </summary>
    public void Recreate()
    {
        _dev.Vk.DeviceWaitIdle(_dev.Device);

        CleanupSwapchain();
        CreateSwapchain();
        CreateFramebuffers();
    }

    private void CleanupSwapchain()
    {
        foreach (var fb in Framebuffers)
            _dev.Vk.DestroyFramebuffer(_dev.Device, fb, null);

        foreach (var iv in ImageViews)
            _dev.Vk.DestroyImageView(_dev.Device, iv, null);

        _dev.KhrSwapchain.DestroySwapchain(_dev.Device, Swapchain, null);
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dev.Vk.DeviceWaitIdle(_dev.Device);

        CleanupSwapchain();
        _dev.Vk.DestroyRenderPass(_dev.Device, RenderPass, null);
        _dev.KhrSurface.DestroySurface(_dev.Instance, Surface, null);
    }
}
