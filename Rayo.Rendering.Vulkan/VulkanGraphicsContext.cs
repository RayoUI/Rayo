using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Vulkan implementation of IGraphicsContext.
/// Requires a native (non-GL) window; bootstraps the Vulkan stack once
/// <see cref="OnWindowCreated"/> is called by UIApplication.
/// </summary>
public class VulkanGraphicsContext : IGraphicsContext
{
    private VulkanDevice?    _device;
    private VulkanSwapchain? _swapchain;
    private bool _disposed;

    /// <inheritdoc/>
    public bool RequiresNativeWindow => true;

    /// <inheritdoc/>
    public void OnWindowCreated(object nativeWindow)
    {
        var window = (IWindow)nativeWindow;
        _device   = new VulkanDevice(window);
        _swapchain = new VulkanSwapchain(_device, window);
    }

    /// <inheritdoc/>
    public IRenderer CreateRenderer()
    {
        ArgumentNullException.ThrowIfNull(_device,   nameof(_device));
        ArgumentNullException.ThrowIfNull(_swapchain, nameof(_swapchain));
        return new VulkanRenderer(_device, _swapchain);
    }

    // ── IGraphicsContext low-level methods ────────────────────────────────
    // These are rarely called; the renderer handles all drawing internally.

    public ITexture CreateTexture(int width, int height, byte[] data, TextureFormat format)
        => throw new NotSupportedException(
            "Use IRenderer methods for texture management in the Vulkan backend.");

    public IShaderProgram CreateShaderProgram(string vertexShader, string fragmentShader)
        => throw new NotSupportedException("Shader programs are managed internally by VulkanRenderer.");

    public IBuffer CreateVertexBuffer(int sizeInBytes)
        => throw new NotSupportedException("Buffers are managed internally by VulkanRenderer.");

    public IBuffer CreateIndexBuffer(int sizeInBytes)
        => throw new NotSupportedException("Buffers are managed internally by VulkanRenderer.");

    public void SetViewport(int x, int y, int width, int height) { }
    public void Clear(float r, float g, float b, float a) { }
    public void SetBlendingEnabled(bool enabled) { }
    public void SetBlendFunction(BlendFactor srcFactor, BlendFactor dstFactor) { }
    public void SetScissorEnabled(bool enabled) { }
    public void SetScissorRect(int x, int y, int width, int height) { }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _swapchain?.Dispose();
        _device?.Dispose();
    }
}
