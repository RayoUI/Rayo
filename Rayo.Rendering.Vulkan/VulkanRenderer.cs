using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rayo.Rendering.Vulkan;

/// <summary>
/// Full Vulkan implementation of IRenderer.
/// Uses three pipelines (shape / text / image) and a CPU-side vertex accumulator
/// that is flushed to the GPU via staging buffers before each submit.
/// </summary>
public sealed unsafe class VulkanRenderer : IRenderer
{
    // ── Vertex layouts ────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct ShapeVertex
    {
        public Vector2 Position; // 8
        public Vector4 Color;    // 16  → 24 bytes total
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UVVertex
    {
        public Vector2 Position; // 8
        public Vector2 UV;       // 8
        public Vector4 Color;    // 16 → 32 bytes total
    }

    // ── Core objects ──────────────────────────────────────────────────────

    private readonly VulkanDevice    _dev;
    private readonly VulkanSwapchain _swapchain;
    private VulkanPipelines          _pipelines = null!;
    private VulkanCommandFrame       _frame     = null!;
    private VulkanFontAtlas?         _defaultFont;
    private VulkanTextureManager     _textures  = null!;

    // ── CPU vertex/index accumulators ─────────────────────────────────────

    private readonly List<ShapeVertex> _shapeVerts   = new();
    private readonly List<ushort>      _shapeIndices = new();
    private readonly List<UVVertex>    _textVerts    = new();
    private readonly List<ushort>      _textIndices  = new();
    private readonly List<UVVertex>    _imageVerts   = new();
    private readonly List<ushort>      _imageIndices = new();

    // ── State ─────────────────────────────────────────────────────────────

    private Matrix4x4 _projection;
    private uint      _currentImageIndex;
    private int       _vpWidth, _vpHeight;
    private Color     _clearColor = new Color(30, 30, 30, 255);

    // Active descriptor sets for text and image batches.
    private DescriptorSet _activeTextDesc;
    private DescriptorSet _activeImageDesc;
    private bool _hasActiveTextDesc;
    private bool _hasActiveImageDesc;

    // Scissor stack
    private readonly Stack<(int x, int y, int w, int h)> _scissorStack = new();
    private readonly Stack<Matrix3x2> _transformStack = new();
    private Matrix3x2 _currentTransform = Matrix3x2.Identity;

    // Render-to-texture state
    public bool IsRenderingToTexture => false; // Off-screen pass not yet active

    private bool _disposed;

    // ── Construction ──────────────────────────────────────────────────────

    internal VulkanRenderer(VulkanDevice dev, VulkanSwapchain swapchain)
    {
        _dev      = dev;
        _swapchain = swapchain;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void Initialize(int width, int height)
    {
        _vpWidth  = width;
        _vpHeight = height;

        _pipelines = new VulkanPipelines(_dev, _swapchain.RenderPass);
        _frame     = new VulkanCommandFrame(_dev.Vk, _dev.Device, _dev.CommandPool);
        _textures  = new VulkanTextureManager(_dev, _pipelines.ImageDescriptorSetLayout);

        LoadDefaultFont();
        UpdateProjection(width, height);
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;

        _vpWidth  = width;
        _vpHeight = height;

        _dev.Vk.DeviceWaitIdle(_dev.Device);
        _swapchain.Recreate();
        _pipelines.Recreate(_swapchain.RenderPass);
        UpdateProjection(width, height);
    }

    private void UpdateProjection(int w, int h)
    {
        // Orthographic: top-left origin, maps [0,w]×[0,h] → NDC.
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, w, h, 0, -1, 1);
    }

    // ── Frame ─────────────────────────────────────────────────────────────

    public void BeginFrame()
    {
        _transformStack.Clear();
        _currentTransform = Matrix3x2.Identity;

        // Wait for the previous frame to finish.
        _dev.Vk.WaitForFences(_dev.Device, 1, in _frame.InFlightFence, true, ulong.MaxValue);
        _dev.Vk.ResetFences(_dev.Device, 1, in _frame.InFlightFence);

        // Acquire the next swapchain image.
        Result acquireResult = _dev.KhrSwapchain.AcquireNextImage(
            _dev.Device, _swapchain.Swapchain, ulong.MaxValue,
            _frame.ImageAvailableSemaphore, default, ref _currentImageIndex);

        if (acquireResult is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
        {
            _swapchain.Recreate();
            _pipelines.Recreate(_swapchain.RenderPass);
            return;
        }

        // Begin command buffer recording.
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        VulkanUtil.Check(_dev.Vk.BeginCommandBuffer(_frame.CommandBuffer, &beginInfo),
            "BeginCommandBuffer");

        // Begin render pass.
        var clearValue = new ClearValue
        {
            Color = new ClearColorValue(_clearColor.R / 255f, _clearColor.G / 255f,
                                        _clearColor.B / 255f, _clearColor.A / 255f),
        };

        var rpBegin = new RenderPassBeginInfo
        {
            SType           = StructureType.RenderPassBeginInfo,
            RenderPass      = _swapchain.RenderPass,
            Framebuffer     = _swapchain.Framebuffers[_currentImageIndex],
            RenderArea      = new Rect2D(default, _swapchain.Extent),
            ClearValueCount = 1,
            PClearValues    = &clearValue,
        };

        _dev.Vk.CmdBeginRenderPass(_frame.CommandBuffer, &rpBegin, SubpassContents.Inline);

        // Set full-viewport dynamic state.
        SetDynamicViewportScissor(_frame.CommandBuffer,
            0, 0, (int)_swapchain.Extent.Width, (int)_swapchain.Extent.Height);
    }

    public void EndFrame()
    {
        // Flush any remaining geometry.
        FlushShapeBatch();
        FlushTextBatch();
        FlushImageBatch();

        _dev.Vk.CmdEndRenderPass(_frame.CommandBuffer);
        VulkanUtil.Check(_dev.Vk.EndCommandBuffer(_frame.CommandBuffer), "EndCommandBuffer");

        // Submit.
        var waitSemaphore   = _frame.ImageAvailableSemaphore;
        var signalSemaphore = _frame.RenderFinishedSemaphore;
        var waitStage       = PipelineStageFlags.ColorAttachmentOutputBit;

        var submitInfo = new SubmitInfo
        {
            SType                = StructureType.SubmitInfo,
            WaitSemaphoreCount   = 1,
            PWaitSemaphores      = &waitSemaphore,
            PWaitDstStageMask    = &waitStage,
            CommandBufferCount   = 1,
            PCommandBuffers      = (CommandBuffer*)Unsafe.AsPointer(ref _frame.CommandBuffer),
            SignalSemaphoreCount = 1,
            PSignalSemaphores    = &signalSemaphore,
        };

        VulkanUtil.Check(_dev.Vk.QueueSubmit(_dev.GraphicsQueue, 1, &submitInfo, _frame.InFlightFence),
            "QueueSubmit");

        // Present.
        var sc = _swapchain.Swapchain;
        var idx = _currentImageIndex;

        var presentInfo = new PresentInfoKHR
        {
            SType              = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores    = &signalSemaphore,
            SwapchainCount     = 1,
            PSwapchains        = &sc,
            PImageIndices      = &idx,
        };

        var presentResult = _dev.KhrSwapchain.QueuePresent(_dev.GraphicsQueue, &presentInfo);
        if (presentResult is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
        {
            _swapchain.Recreate();
            _pipelines.Recreate(_swapchain.RenderPass);
        }
    }

    // ── Clear ─────────────────────────────────────────────────────────────

    public void Clear(Color color)
    {
        // Store clear color; it is applied as the render pass clear value in BeginFrame.
        _clearColor = color;
    }

    // ── Shape primitives ──────────────────────────────────────────────────

    public void DrawRect(float x, float y, float width, float height, Color color)
    {
        var c = ToVec4(color);
        ushort b = (ushort)_shapeVerts.Count;

        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x,         y),          Color = c });
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x + width, y),          Color = c });
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x + width, y + height), Color = c });
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x,         y + height), Color = c });

        _shapeIndices.Add(b); _shapeIndices.Add((ushort)(b+1)); _shapeIndices.Add((ushort)(b+2));
        _shapeIndices.Add(b); _shapeIndices.Add((ushort)(b+2)); _shapeIndices.Add((ushort)(b+3));
    }

    public void DrawRect(float x, float y, float width, float height, Brush brush)
    {
        if (brush == null) return;
        var c = brush.PrimaryColor;
        DrawRect(x, y, width, height, new Color(c.R, c.G, c.B, (byte)(c.A * brush.Opacity)));
    }

    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Color color)
    {
        var path = VectorPath.RoundedRectangle(x, y, width, height, radius);
        DrawPath(path, color);
    }

    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Brush brush)
    {
        if (brush == null) return;
        var c = brush.PrimaryColor;
        DrawRoundedRect(x, y, width, height, radius,
            new Color(c.R, c.G, c.B, (byte)(c.A * brush.Opacity)));
    }

    public void DrawRectOutline(float x, float y, float width, float height, float thickness, Color color)
    {
        // Four thin rects forming the border.
        DrawRect(x,                    y,                     width,     thickness, color);
        DrawRect(x,                    y + height - thickness, width,     thickness, color);
        DrawRect(x,                    y,                     thickness, height,    color);
        DrawRect(x + width - thickness, y,                     thickness, height,    color);
    }

    public void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Color color)
    {
        var path = VectorPath.RoundedRectangle(x, y, width, height, radius);
        DrawPathStroke(path, color, thickness);
    }

    public void DrawRoundedRectOutline(float x, float y, float width, float height, float radius, float thickness, Brush brush)
    {
        if (brush == null) return;
        var c = brush.PrimaryColor;
        DrawRoundedRectOutline(x, y, width, height, radius, thickness,
            new Color(c.R, c.G, c.B, (byte)(c.A * brush.Opacity)));
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        var dir = Vector2.Normalize(new Vector2(x2 - x1, y2 - y1));
        var perp = new Vector2(-dir.Y, dir.X) * (thickness * 0.5f);
        var c = ToVec4(color);
        ushort b = (ushort)_shapeVerts.Count;

        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x1, y1) + perp, Color = c });
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x1, y1) - perp, Color = c });
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x2, y2) - perp, Color = c });
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(x2, y2) + perp, Color = c });

        _shapeIndices.Add(b); _shapeIndices.Add((ushort)(b+1)); _shapeIndices.Add((ushort)(b+2));
        _shapeIndices.Add(b); _shapeIndices.Add((ushort)(b+2)); _shapeIndices.Add((ushort)(b+3));
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float thickness, Brush brush)
    {
        if (brush == null) return;
        var c = brush.PrimaryColor;
        DrawLine(x1, y1, x2, y2, thickness, new Color(c.R, c.G, c.B, (byte)(c.A * brush.Opacity)));
    }

    public void DrawCircle(float cx, float cy, float radius, Color color)
    {
        const int segments = 32;
        var c = ToVec4(color);
        ushort centre = (ushort)_shapeVerts.Count;
        _shapeVerts.Add(new ShapeVertex { Position = new Vector2(cx, cy), Color = c });

        for (int i = 0; i <= segments; i++)
        {
            float angle = MathF.PI * 2f * i / segments;
            _shapeVerts.Add(new ShapeVertex
            {
                Position = new Vector2(cx + MathF.Cos(angle) * radius,
                                       cy + MathF.Sin(angle) * radius),
                Color = c,
            });
        }

        for (int i = 0; i < segments; i++)
        {
            _shapeIndices.Add(centre);
            _shapeIndices.Add((ushort)(centre + 1 + i));
            _shapeIndices.Add((ushort)(centre + 2 + i));
        }
    }

    public void DrawCircle(float cx, float cy, float radius, Brush brush)
    {
        if (brush == null) return;
        var c = brush.PrimaryColor;
        DrawCircle(cx, cy, radius, new Color(c.R, c.G, c.B, (byte)(c.A * brush.Opacity)));
    }

    public void DrawCircleOutline(float cx, float cy, float radius, float thickness, Color color)
    {
        const int segments = 32;
        float inner = radius - thickness, outer = radius;
        var c = ToVec4(color);
        ushort b = (ushort)_shapeVerts.Count;

        for (int i = 0; i <= segments; i++)
        {
            float angle = MathF.PI * 2f * i / segments;
            float cos = MathF.Cos(angle), sin = MathF.Sin(angle);
            _shapeVerts.Add(new ShapeVertex { Position = new Vector2(cx + cos * outer, cy + sin * outer), Color = c });
            _shapeVerts.Add(new ShapeVertex { Position = new Vector2(cx + cos * inner, cy + sin * inner), Color = c });
        }

        for (int i = 0; i < segments; i++)
        {
            ushort a0 = (ushort)(b + i * 2), a1 = (ushort)(a0 + 1),
                   a2 = (ushort)(a0 + 2), a3 = (ushort)(a0 + 3);
            _shapeIndices.Add(a0); _shapeIndices.Add(a1); _shapeIndices.Add(a2);
            _shapeIndices.Add(a1); _shapeIndices.Add(a3); _shapeIndices.Add(a2);
        }
    }

    public void DrawCircleOutline(float cx, float cy, float radius, float thickness, Brush brush)
    {
        if (brush == null) return;
        var c = brush.PrimaryColor;
        DrawCircleOutline(cx, cy, radius, thickness,
            new Color(c.R, c.G, c.B, (byte)(c.A * brush.Opacity)));
    }

    public void DrawPolygon(List<(float x, float y)> points, Color color)
    {
        if (points.Count < 3) return;
        var c = ToVec4(color);
        ushort b = (ushort)_shapeVerts.Count;

        foreach (var (px, py) in points)
            _shapeVerts.Add(new ShapeVertex { Position = new Vector2(px, py), Color = c });

        for (int i = 1; i < points.Count - 1; i++)
        {
            _shapeIndices.Add(b);
            _shapeIndices.Add((ushort)(b + i));
            _shapeIndices.Add((ushort)(b + i + 1));
        }
    }

    // ── Vector paths ──────────────────────────────────────────────────────

    public void DrawPath(VectorPath path, Color fillColor)
    {
        var tris = PathTessellator.TessellateFillAdvanced(path);
        if (tris.Count == 0) return;

        var c = ToVec4(fillColor);
        ushort b = (ushort)_shapeVerts.Count;

        foreach (var pt in tris)
            _shapeVerts.Add(new ShapeVertex { Position = pt, Color = c });

        for (ushort i = 0; i < tris.Count; i++)
            _shapeIndices.Add((ushort)(b + i));
    }

    public void DrawPath(VectorPath path, Brush fillBrush)
    {
        if (fillBrush == null) return;
        var c = fillBrush.PrimaryColor;
        DrawPath(path, new Color(c.R, c.G, c.B, (byte)(c.A * fillBrush.Opacity)));
    }

    public void DrawPathStroke(VectorPath path, Color strokeColor, float strokeWidth)
    {
        var tris = PathTessellator.TessellateStrokeAdvanced(path, strokeWidth);
        if (tris.Count == 0) return;

        var c = ToVec4(strokeColor);
        ushort b = (ushort)_shapeVerts.Count;

        foreach (var pt in tris)
            _shapeVerts.Add(new ShapeVertex { Position = pt, Color = c });

        for (ushort i = 0; i < tris.Count; i++)
            _shapeIndices.Add((ushort)(b + i));
    }

    public void DrawPathStroke(VectorPath path, Brush strokeBrush, float strokeWidth)
    {
        if (strokeBrush == null) return;
        var c = strokeBrush.PrimaryColor;
        DrawPathStroke(path, new Color(c.R, c.G, c.B, (byte)(c.A * strokeBrush.Opacity)), strokeWidth);
    }

    public void DrawPathFillAndStroke(VectorPath path, Color fillColor, Color strokeColor, float strokeWidth)
    {
        DrawPath(path, fillColor);
        DrawPathStroke(path, strokeColor, strokeWidth);
    }

    public void DrawPathFillAndStroke(VectorPath path, Brush fillBrush, Brush strokeBrush, float strokeWidth)
    {
        DrawPath(path, fillBrush);
        DrawPathStroke(path, strokeBrush, strokeWidth);
    }

    public void DrawQuadraticBezier(float startX, float startY, float controlX, float controlY,
        float endX, float endY, Color color, float thickness = 2f)
    {
        var path = new VectorPath();
        path.MoveTo(startX, startY);
        path.QuadraticBezierTo(controlX, controlY, endX, endY);
        DrawPathStroke(path, color, thickness);
    }

    public void DrawCubicBezier(float startX, float startY, float cp1X, float cp1Y,
        float cp2X, float cp2Y, float endX, float endY, Color color, float thickness = 2f)
    {
        var path = new VectorPath();
        path.MoveTo(startX, startY);
        path.CubicBezierTo(cp1X, cp1Y, cp2X, cp2Y, endX, endY);
        DrawPathStroke(path, color, thickness);
    }

    // ── Text ──────────────────────────────────────────────────────────────

    public IFont LoadFont(byte[] fontData, float fontSize)
    {
        return new VulkanFontAtlas(_dev, _pipelines.TextDescriptorSetLayout,
            fontData, fontSize);
    }

    public void DrawText(string text, float x, float y, Color color, float fontSize = 24)
    {
        if (_defaultFont == null || string.IsNullOrEmpty(text)) return;
        DrawTextInternal(text, x, y, color, _defaultFont);
    }

    public void DrawText(string text, float x, float y, Brush color, float fontSize = 24)
    {
        if (_defaultFont == null || string.IsNullOrEmpty(text)) return;
        var c = color.PrimaryColor;
        DrawText(text, x, y, new Color(c.R, c.G, c.B, (byte)(c.A * color.Opacity)), fontSize);
    }

    public void DrawTextWithFont(string text, float x, float y, Color color, IFont font, float fontSize = 24)
    {
        if (font is not VulkanFontAtlas vFont) { DrawText(text, x, y, color, fontSize); return; }
        if (string.IsNullOrEmpty(text)) return;
        DrawTextInternal(text, x, y, color, vFont);
    }

    public void DrawTextWithFont(string text, float x, float y, Brush color, IFont font, float fontSize = 24)
    {
        var c = color.PrimaryColor;
        DrawTextWithFont(text, x, y, new Color(c.R, c.G, c.B, (byte)(c.A * color.Opacity)), font, fontSize);
    }

    private void DrawTextInternal(string text, float x, float y, Color color, VulkanFontAtlas atlas)
    {
        // Flush shape batch before switching to text pipeline.
        FlushShapeBatch();

        // If descriptor changes, flush current text batch first.
        if (_hasActiveTextDesc && _activeTextDesc.Handle != atlas.DescriptorSet.Handle)
            FlushTextBatch();

        _activeTextDesc    = atlas.DescriptorSet;
        _hasActiveTextDesc = true;

        var col = ToVec4(color);
        float penX = x;
        float baseline = y + atlas.Ascent;

        for (int i = 0; i < text.Length;)
        {
            int cp = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            if (!atlas.TryGetGlyph(cp, out var g))
            {
                penX += atlas.Size * 0.25f;
                continue;
            }

            if (g.Width <= 0 || g.Height <= 0)
            {
                penX += g.AdvanceX;
                continue;
            }

            float gx = penX + g.OffsetX;
            float gy = baseline - g.OffsetY - g.Height;
            float gw = g.Width, gh = g.Height;

            ushort b = (ushort)_textVerts.Count;

            _textVerts.Add(new UVVertex { Position = new Vector2(gx,      gy),      UV = new Vector2(g.X0, g.Y0), Color = col });
            _textVerts.Add(new UVVertex { Position = new Vector2(gx + gw, gy),      UV = new Vector2(g.X1, g.Y0), Color = col });
            _textVerts.Add(new UVVertex { Position = new Vector2(gx + gw, gy + gh), UV = new Vector2(g.X1, g.Y1), Color = col });
            _textVerts.Add(new UVVertex { Position = new Vector2(gx,      gy + gh), UV = new Vector2(g.X0, g.Y1), Color = col });

            _textIndices.Add(b); _textIndices.Add((ushort)(b+1)); _textIndices.Add((ushort)(b+2));
            _textIndices.Add(b); _textIndices.Add((ushort)(b+2)); _textIndices.Add((ushort)(b+3));

            penX += g.AdvanceX;
        }
    }

    public Vector2 MeasureText(string text, float fontSize = 24)
    {
        if (_defaultFont == null || string.IsNullOrEmpty(text)) return Vector2.Zero;
        return _defaultFont.MeasureText(text);
    }

    public Vector2 MeasureTextWithFont(string text, IFont font, float fontSize = 24)
    {
        if (font is VulkanFontAtlas vf) return vf.MeasureText(text);
        return MeasureText(text, fontSize);
    }

    // ── Textures ──────────────────────────────────────────────────────────

    public ITexture? LoadTexture(string filePath)
        => _textures.LoadTexture(filePath);

    public ITexture? LoadTextureFromStream(Stream stream, string cacheKey)
        => _textures.LoadTextureFromStream(stream, cacheKey);

    public ITexture CreateTextureFromPixels(byte[] rgbaPixels, int width, int height)
        => _textures.CreateTextureFromPixels(rgbaPixels, width, height);

    public void DrawTexture(ITexture texture, float x, float y, float width, float height, Color? tint = null)
    {
        if (texture is not VulkanTexture vTex) return;

        // Flush shape + text before switching to image pipeline.
        FlushShapeBatch();
        FlushTextBatch();

        if (_hasActiveImageDesc && _activeImageDesc.Handle != vTex.DescriptorSet.Handle)
            FlushImageBatch();

        _activeImageDesc    = vTex.DescriptorSet;
        _hasActiveImageDesc = true;

        var col = tint.HasValue ? ToVec4(tint.Value) : Vector4.One;
        ushort b = (ushort)_imageVerts.Count;

        _imageVerts.Add(new UVVertex { Position = new Vector2(x,         y),          UV = new Vector2(0, 0), Color = col });
        _imageVerts.Add(new UVVertex { Position = new Vector2(x + width, y),          UV = new Vector2(1, 0), Color = col });
        _imageVerts.Add(new UVVertex { Position = new Vector2(x + width, y + height), UV = new Vector2(1, 1), Color = col });
        _imageVerts.Add(new UVVertex { Position = new Vector2(x,         y + height), UV = new Vector2(0, 1), Color = col });

        _imageIndices.Add(b); _imageIndices.Add((ushort)(b+1)); _imageIndices.Add((ushort)(b+2));
        _imageIndices.Add(b); _imageIndices.Add((ushort)(b+2)); _imageIndices.Add((ushort)(b+3));
    }

    // ── Render-to-texture (basic stub) ────────────────────────────────────

    public ITexture CreateRenderTarget(int width, int height)
        => _textures.CreateTextureFromPixels(new byte[width * height * 4], width, height);

    public void BeginRenderToTexture(ITexture target)
    {
        // Not yet fully implemented; flush current frame geometry.
        FlushShapeBatch();
        FlushTextBatch();
        FlushImageBatch();
    }

    public void EndRenderToTexture() { }

    public void PushTransform(Matrix3x2 transform)
    {
        FlushShapeBatch();
        FlushTextBatch();
        FlushImageBatch();

        _transformStack.Push(_currentTransform);
        _currentTransform *= transform;
    }

    public void PopTransform()
    {
        FlushShapeBatch();
        FlushTextBatch();
        FlushImageBatch();

        if (_transformStack.Count == 0)
            throw new InvalidOperationException("PopTransform called without matching PushTransform");

        _currentTransform = _transformStack.Pop();
    }

    // ── Clipping ──────────────────────────────────────────────────────────

    public void PushScissor(float x, float y, float width, float height)
    {
        var transformed = TransformRectToAabb(x, y, width, height);

        int ix = (int)MathF.Floor(transformed.x);
        int iy = (int)MathF.Floor(transformed.y);
        int iw = (int)MathF.Ceiling(transformed.width);
        int ih = (int)MathF.Ceiling(transformed.height);

        if (_scissorStack.Count > 0)
        {
            var (cx, cy, cw, ch) = _scissorStack.Peek();

            int nx = Math.Max(ix, cx);
            int ny = Math.Max(iy, cy);
            int nr = Math.Min(ix + iw, cx + cw);
            int nb = Math.Min(iy + ih, cy + ch);

            ix = nx;
            iy = ny;
            iw = Math.Max(0, nr - nx);
            ih = Math.Max(0, nb - ny);
        }

        _scissorStack.Push((ix, iy, iw, ih));
        ApplyScissor(ix, iy, iw, ih);
    }

    public void PopScissor()
    {
        if (_scissorStack.Count > 0) _scissorStack.Pop();

        if (_scissorStack.Count > 0)
        {
            var (x, y, w, h) = _scissorStack.Peek();
            ApplyScissor(x, y, w, h);
        }
        else
        {
            ApplyScissor(0, 0, _vpWidth, _vpHeight);
        }
    }

    private void ApplyScissor(int x, int y, int w, int h)
    {
        // Flush pending geometry so the new scissor takes effect cleanly.
        FlushShapeBatch();
        FlushTextBatch();
        FlushImageBatch();

        int cw = Math.Max(0, Math.Min(w, _vpWidth  - x));
        int ch = Math.Max(0, Math.Min(h, _vpHeight - y));

        SetDynamicScissor(_frame.CommandBuffer, x, y, cw, ch);
    }

    // ── Flush helpers ─────────────────────────────────────────────────────

    private void FlushShapeBatch()
    {
        if (_shapeIndices.Count == 0) return;

        var cb  = _frame.CommandBuffer;
        var proj = _projection;

        _dev.Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, _pipelines.ShapePipeline);
        _dev.Vk.CmdPushConstants(cb, _pipelines.ShapeLayout,
            ShaderStageFlags.VertexBit, 0, 64, &proj);

        var shapeVerts = GetShapeVertsForCurrentTransform();

        DrawIndexedFromCpu<ShapeVertex>(cb,
            shapeVerts, _shapeIndices,
            BufferUsageFlags.VertexBufferBit,
            BufferUsageFlags.IndexBufferBit);

        _shapeVerts.Clear();
        _shapeIndices.Clear();
    }

    private void FlushTextBatch()
    {
        if (_textIndices.Count == 0) return;

        var cb  = _frame.CommandBuffer;
        var proj = _projection;
        var ds   = _activeTextDesc;

        _dev.Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, _pipelines.TextPipeline);
        _dev.Vk.CmdPushConstants(cb, _pipelines.TextLayout,
            ShaderStageFlags.VertexBit, 0, 64, &proj);
        _dev.Vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, _pipelines.TextLayout,
            0, 1, &ds, 0, null);

        var textVerts = GetUvVertsForCurrentTransform(_textVerts);

        DrawIndexedFromCpu<UVVertex>(cb,
            textVerts, _textIndices,
            BufferUsageFlags.VertexBufferBit,
            BufferUsageFlags.IndexBufferBit);

        _textVerts.Clear();
        _textIndices.Clear();
        _hasActiveTextDesc = false;
    }

    private void FlushImageBatch()
    {
        if (_imageIndices.Count == 0) return;

        var cb  = _frame.CommandBuffer;
        var proj = _projection;
        var ds   = _activeImageDesc;

        _dev.Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, _pipelines.ImagePipeline);
        _dev.Vk.CmdPushConstants(cb, _pipelines.ImageLayout,
            ShaderStageFlags.VertexBit, 0, 64, &proj);
        _dev.Vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, _pipelines.ImageLayout,
            0, 1, &ds, 0, null);

        var imageVerts = GetUvVertsForCurrentTransform(_imageVerts);

        DrawIndexedFromCpu<UVVertex>(cb,
            imageVerts, _imageIndices,
            BufferUsageFlags.VertexBufferBit,
            BufferUsageFlags.IndexBufferBit);

        _imageVerts.Clear();
        _imageIndices.Clear();
        _hasActiveImageDesc = false;
    }

    /// <summary>
    /// Uploads CPU vertex and index data via staging buffers and issues a draw call.
    /// </summary>
    private void DrawIndexedFromCpu<T>(
        CommandBuffer cb,
        List<T> verts, List<ushort> indices,
        BufferUsageFlags vbUsage, BufferUsageFlags ibUsage)
        where T : unmanaged
    {
        if (verts.Count == 0 || indices.Count == 0) return;

        ulong vbBytes = (ulong)(verts.Count  * Unsafe.SizeOf<T>());
        ulong ibBytes = (ulong)(indices.Count * sizeof(ushort));

        // Create device-local buffers.
        var (vb, vbMem) = VulkanMemory.CreateDeviceBuffer(_dev, vbBytes, BufferUsageFlags.VertexBufferBit);
        var (ib, ibMem) = VulkanMemory.CreateDeviceBuffer(_dev, ibBytes, BufferUsageFlags.IndexBufferBit);

        // Upload data synchronously via staging buffers.
        VulkanMemory.UploadBuffer(_dev, vb, CollectionsMarshal.AsSpan(verts));
        VulkanMemory.UploadBuffer(_dev, ib, CollectionsMarshal.AsSpan(indices));

        // Bind and draw.
        ulong offset = 0;
        _dev.Vk.CmdBindVertexBuffers(cb, 0, 1, &vb, &offset);
        _dev.Vk.CmdBindIndexBuffer(cb, ib, 0, IndexType.Uint16);
        _dev.Vk.CmdDrawIndexed(cb, (uint)indices.Count, 1, 0, 0, 0);

        // Destroy staging resources after the GPU command is recorded.
        // They are safe to destroy because OneTimeSubmit is fully synchronous,
        // but these device buffers are used in the *current* command buffer,
        // so we defer destruction to after the queue submit in EndFrame().
        // For simplicity we keep them alive until the next frame via a deferred list.
        // TODO: use a per-frame arena or ring buffer for production use.
        _pendingFree.Add((vb, vbMem));
        _pendingFree.Add((ib, ibMem));
    }

    // Simple deferred-free list cleared after the fence is signalled.
    private readonly List<(Silk.NET.Vulkan.Buffer buf, DeviceMemory mem)> _pendingFree = new();

    private void FreePending()
    {
        foreach (var (buf, mem) in _pendingFree)
        {
            _dev.Vk.DestroyBuffer(_dev.Device, buf, null);
            _dev.Vk.FreeMemory(_dev.Device, mem, null);
        }
        _pendingFree.Clear();
    }

    // ── Dynamic state helpers ─────────────────────────────────────────────

    private void SetDynamicViewportScissor(CommandBuffer cb, int x, int y, int w, int h)
    {
        var vp = new Viewport
        {
            X        = x, Y = y,
            Width    = w, Height = h,
            MinDepth = 0, MaxDepth = 1,
        };
        _dev.Vk.CmdSetViewport(cb, 0, 1, &vp);

        SetDynamicScissor(cb, x, y, w, h);
    }

    private void SetDynamicScissor(CommandBuffer cb, int x, int y, int w, int h)
    {
        var scissor = new Rect2D
        {
            Offset = new Offset2D(x, y),
            Extent = new Extent2D((uint)Math.Max(0, w), (uint)Math.Max(0, h)),
        };
        _dev.Vk.CmdSetScissor(cb, 0, 1, &scissor);
    }

    private Vector2 TransformPoint(Vector2 point)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return point;

        return Vector2.Transform(point, _currentTransform);
    }

    private (float x, float y, float width, float height) TransformRectToAabb(float x, float y, float width, float height)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return (x, y, width, height);

        var p1 = TransformPoint(new Vector2(x, y));
        var p2 = TransformPoint(new Vector2(x + width, y));
        var p3 = TransformPoint(new Vector2(x + width, y + height));
        var p4 = TransformPoint(new Vector2(x, y + height));

        float minX = MathF.Min(MathF.Min(p1.X, p2.X), MathF.Min(p3.X, p4.X));
        float minY = MathF.Min(MathF.Min(p1.Y, p2.Y), MathF.Min(p3.Y, p4.Y));
        float maxX = MathF.Max(MathF.Max(p1.X, p2.X), MathF.Max(p3.X, p4.X));
        float maxY = MathF.Max(MathF.Max(p1.Y, p2.Y), MathF.Max(p3.Y, p4.Y));

        return (minX, minY, maxX - minX, maxY - minY);
    }

    private List<ShapeVertex> GetShapeVertsForCurrentTransform()
    {
        if (_currentTransform == Matrix3x2.Identity)
            return _shapeVerts;

        var transformed = new List<ShapeVertex>(_shapeVerts.Count);
        foreach (var vertex in _shapeVerts)
        {
            transformed.Add(new ShapeVertex
            {
                Position = Vector2.Transform(vertex.Position, _currentTransform),
                Color = vertex.Color
            });
        }

        return transformed;
    }

    private List<UVVertex> GetUvVertsForCurrentTransform(List<UVVertex> source)
    {
        if (_currentTransform == Matrix3x2.Identity)
            return source;

        var transformed = new List<UVVertex>(source.Count);
        foreach (var vertex in source)
        {
            transformed.Add(new UVVertex
            {
                Position = Vector2.Transform(vertex.Position, _currentTransform),
                UV = vertex.UV,
                Color = vertex.Color
            });
        }

        return transformed;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private static Vector4 ToVec4(Color c)
        => new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    private void LoadDefaultFont()
    {
        string[] candidates =
        [
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\calibri.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/System/Library/Fonts/SFNS.ttf",
        ];

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var data = File.ReadAllBytes(path);
                _defaultFont = new VulkanFontAtlas(_dev,
                    _pipelines.TextDescriptorSetLayout, data, 24);
                return;
            }
            catch { /* try next */ }
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dev.Vk.DeviceWaitIdle(_dev.Device);

        FreePending();
        _defaultFont?.Dispose();
        _textures?.Dispose();
        _pipelines?.Dispose();
        _frame?.Dispose();
    }
}
