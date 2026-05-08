using Rayo.Rendering;
using RenderColor = Rayo.Rendering.Color;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rayo.Core;

public class UITree
{
    /// <summary>
    /// The most recently active UITree instance.
    /// Set by platform hosting (Desktop via UIApplication, Android via RayoGLSurfaceView).
    /// Allows VisualElement.MarkNeedsLayout/Paint to notify the tree on all platforms.
    /// </summary>
    public static UITree? Current { get; set; }

    public VisualElement? Root { get; private set; }
    private bool _needsLayout = true;
    private bool _needsRender = true;
    private bool _renderRequestQueued;
    private float _lastWidth;
    private float _lastHeight;
    private readonly DirtyRegionTracker _dirtyRegions = new();

    // Modern scheduling system with batching
    private readonly FrameScheduler _scheduler = new();

    // Callback to notify UIApplication of changes
    public Action? OnNeedsRenderChanged { get; set; }

    /// <summary>
    /// Event fired when the root element is changed (e.g., hot reload).
    /// Used by DevTools to refresh the UI tree.
    /// </summary>
    public event Action? RootChanged;

    /// <summary>
    /// Event fired when overlays are added or removed.
    /// Used by DevTools to refresh the UI tree.
    /// </summary>
    public event Action? OverlaysChanged;

    // Reference to DragDropManager to render the ghost
    private DragDropManager? _dragDropManager;

    // SOLID: Dependency Injection of the effects renderer
    private readonly VisualEffectsRenderer _effectsRenderer = new();

    // ? NEW: EventManager for input handling (mouse/touch/keyboard)
    public EventManager? EventManager { get; private set; }

    // Overlay support for Android/iOS (components like Drawer, Dialog, etc.)
    private readonly List<VisualElement> _overlays = new();
    private LayerCache? _layerCache;
    private IRenderer? _layerCacheRenderer;
    private DateTime _lastLayerCacheCleanupUtc = DateTime.UtcNow;

    /// <summary>
    /// Gets the list of overlay elements (read-only).
    /// Overlays are rendered on top of the main UI tree.
    /// </summary>
    public IReadOnlyList<VisualElement> Overlays => _overlays;
    public DirtyRegionTracker DirtyRegions => _dirtyRegions;
    public PartialRenderPolicy PartialRenderMode { get; set; } = PartialRenderPolicy.Disabled;

    public bool NeedsRender => _needsRender || _scheduler.HasScheduledWork;

    public UITree()
    {
        // Connect the scheduler to notify when there is scheduled work
        _scheduler.OnFrameScheduled = () =>
        {
            MarkNeedsRender();
        };
    }

    public void SetRoot(VisualElement root)
    {
        MarkAllCachedLayersDirty();
        Root = root;
        Root.NotifyMounted();
        MarkNeedsLayout();
        MarkNeedsRender();
        RootChanged?.Invoke();
    }

    /// <summary>
    /// Sets the DragDropManager to render drag & drop ghosts.
    /// </summary>
    public void SetDragDropManager(DragDropManager dragDropManager)
    {
        _dragDropManager = dragDropManager;
    }

    /// <summary>
    /// Initializes the EventManager for input handling.
    /// Should be called once during setup (from UIApplication or platform-specific code).
    /// </summary>
    /// <param name="app">Optional UIApplication reference (null for Android/iOS)</param>
    public void InitializeEventManager(UIApplication? app)
    {
        if (EventManager == null)
        {
            EventManager = new EventManager(this, app);
        }
    }

    /// <summary>
    /// Adds an overlay element that renders on top of the main UI.
    /// Used by Drawer, Dialog, Menu, etc. on platforms without UIApplication.
    /// </summary>
    public void AddOverlay(VisualElement overlay)
    {
        if (!_overlays.Contains(overlay))
        {
            _overlays.Add(overlay);
            MarkOverlayLayerDirty(overlay);
            MarkElementNeedsLayout(overlay);
            OverlaysChanged?.Invoke();
        }
    }

    /// <summary>
    /// Removes an overlay element.
    /// </summary>
    public void RemoveOverlay(VisualElement overlay)
    {
        if (_overlays.Remove(overlay))
        {
            _layerCache?.RemoveLayer(GetOverlayLayerId(overlay));
            _dirtyRegions.MarkElementDirty(overlay, DirtyReason.LayoutChanged);
            MarkNeedsRender();
            OverlaysChanged?.Invoke();
        }
    }

    public void MarkNeedsLayout()
    {
        _needsLayout = true;
        _dirtyRegions.MarkFullScreenDirty();
        MarkAllCachedLayersDirty();
        MarkNeedsRender();
    }

    public void MarkElementNeedsLayout(VisualElement element)
    {
        _needsLayout = true;
        _scheduler.ScheduleLayout(element);
        _dirtyRegions.MarkElementDirty(element, DirtyReason.LayoutChanged);
        var owningOverlay = FindOwningOverlay(element);
        if (owningOverlay != null)
        {
            MarkOverlayLayerDirty(owningOverlay);
        }
        else
        {
            MarkRootLayerDirty();
        }

        MarkNeedsRender();
    }

    public void MarkElementNeedsPaint(VisualElement element)
    {
        _scheduler.SchedulePaint(element);
        _dirtyRegions.MarkElementDirty(element, DirtyReason.ContentChanged);
        var owningOverlay = FindOwningOverlay(element);
        if (owningOverlay != null)
        {
            MarkOverlayLayerDirty(owningOverlay);
        }
        else
        {
            MarkRootLayerDirty();
        }

        MarkNeedsRender();
    }

    public void MarkNeedsRender()
    {
        _needsRender = true;

        if (_renderRequestQueued)
        {
            // A render request is already queued/executing
            return;
        }

        _renderRequestQueued = true;
        // Notify UIApplication immediately to exit idle mode
        OnNeedsRenderChanged?.Invoke();
    }

    /// <summary>
    /// Signals that a render pass is starting so new invalidations can queue another frame.
    /// </summary>
    public void NotifyRenderStarted()
    {
        _renderRequestQueued = false;
    }

    public void Update(float width, float height)
    {
        if (Root == null) return;

        // NOTE: When SkiaSharpRenderer applies canvas scaling,
        // the dimensions passed here should already be in logical pixels
        // (divided by scale factor before calling Update)

        // Only do layout if there are changes
        bool sizeChanged = _lastWidth != width || _lastHeight != height;
        if (sizeChanged)
        {
            _lastWidth = width;
            _lastHeight = height;
            _dirtyRegions.MarkFullScreenDirty();
            MarkAllCachedLayersDirty();
            MarkNeedsLayout();
        }

        bool rootNeedsLayout = _needsLayout || Root.NeedsLayout || HasScheduledRootLayoutWork();
        var overlaysNeedingLayout = CaptureOverlaysNeedingLayout();

        // Use the new granular dirty flags system
        if (rootNeedsLayout || overlaysNeedingLayout.Count > 0)
        {
            if (rootNeedsLayout)
            {
                _dirtyRegions.MarkFullScreenDirty();
                Rayo.DevTools.PerformanceTracker.RecordMeasured();
                Root.Measure(width, height);
                Rayo.DevTools.PerformanceTracker.RecordArranged();
                Root.Arrange(0, 0, width, height);
                ClearDirtyFlags(Root);

                // If the root moved, refresh every overlay too because they share the viewport.
                overlaysNeedingLayout = _overlays;
            }

            foreach (var overlay in overlaysNeedingLayout)
            {
                LayoutOverlay(overlay, width, height);
            }

            _needsLayout = false;

            // Complete scheduler frame
            _scheduler.FrameComplete();

            MarkNeedsRender();
        }
		else if (Root.NeedsPaint || _scheduler.NeedsPaint)
        {
            var dirtyElements = CaptureTrackedDirtyElements(includeLayout: false, includePaint: true);
            if (dirtyElements.Count > 0)
            {
                ClearDirtyFlagsForTrackedElements(dirtyElements);
            }
            else
            {
                ClearDirtyFlags(Root);
            }
			// Ensure paint-only frames release scheduled work so idle mode can resume
			_scheduler.FrameComplete();
            MarkNeedsRender();
        }
    }

    public void ClearRenderFlag()
    {
        _needsRender = false;
        _renderRequestQueued = false;
        _dirtyRegions.Clear();
    }

    private void ClearDirtyFlags(VisualElement element)
    {
        element.NeedsLayout = false;
        element.NeedsPaint = false;
        // Use GetChildren() instead of Children property to handle LayoutBase correctly
        foreach (var child in element.GetChildren().ToArray())
        {
            ClearDirtyFlags(child);
        }
    }

    private void LayoutOverlay(VisualElement overlay, float width, float height)
    {
        overlay.Measure(width, height);

        float x = overlay.X;
        float y = overlay.Y;
        float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? width : overlay.DesiredWidth;
        float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? height : overlay.DesiredHeight;

        overlay.Arrange(x, y, w, h);
        ClearDirtyFlags(overlay);
    }

    private void ClearDirtyFlagsForTrackedElements(IReadOnlyList<VisualElement> dirtyElements)
    {
        var cleared = new HashSet<VisualElement>();

        foreach (var element in dirtyElements)
        {
            ClearDirtySubtree(element, cleared);

            var current = element.Parent;
            while (current != null)
            {
                if (cleared.Add(current))
                {
                    current.NeedsLayout = false;
                    current.NeedsPaint = false;
                }

                current = current.Parent;
            }
        }
    }

    private void ClearDirtySubtree(VisualElement element, HashSet<VisualElement> cleared)
    {
        if (!cleared.Add(element))
            return;

        element.NeedsLayout = false;
        element.NeedsPaint = false;

        foreach (var child in element.GetChildren())
        {
            if (child.NeedsLayout || child.NeedsPaint)
                ClearDirtySubtree(child, cleared);
        }
    }

    private List<VisualElement> CaptureTrackedDirtyElements(bool includeLayout, bool includePaint)
    {
        var tracked = new HashSet<VisualElement>();

        if (includeLayout)
        {
            foreach (var element in _scheduler.DirtyLayoutElements)
                tracked.Add(element);
        }

        if (includePaint)
        {
            foreach (var element in _scheduler.DirtyPaintElements)
                tracked.Add(element);
        }

        if (tracked.Count == 0)
        {
            if (Root != null && (Root.NeedsLayout || Root.NeedsPaint))
                tracked.Add(Root);

            foreach (var overlay in _overlays)
            {
                if (overlay.NeedsLayout || overlay.NeedsPaint)
                    tracked.Add(overlay);
            }
        }

        return tracked.ToList();
    }

    private bool HasScheduledRootLayoutWork()
    {
        foreach (var element in _scheduler.DirtyLayoutElements)
        {
            if (FindOwningOverlay(element) == null)
                return true;
        }

        return false;
    }

    private IReadOnlyList<VisualElement> CaptureOverlaysNeedingLayout()
    {
        if (_overlays.Count == 0)
            return Array.Empty<VisualElement>();

        var overlays = new HashSet<VisualElement>();

        foreach (var element in _scheduler.DirtyLayoutElements)
        {
            var owningOverlay = FindOwningOverlay(element);
            if (owningOverlay != null)
                overlays.Add(owningOverlay);
        }

        foreach (var overlay in _overlays)
        {
            if (overlay.NeedsLayout)
                overlays.Add(overlay);
        }

        return overlays.Count == 0 ? Array.Empty<VisualElement>() : overlays.ToList();
    }

    public void Render(IRenderer renderer, bool fullSurfaceCleared = true)
    {
        if (Root == null) return;
        EnsureLayerCache(renderer);
        CleanupLayerCacheIfNeeded();
        var renderState = CaptureRenderState(fullSurfaceCleared);
        RenderRoot(renderer, renderState);

        // Render overlays on top of the main UI (for Drawer, Dialog, Menu, etc.)
        foreach (var overlay in _overlays)
        {
            RenderOverlay(overlay, renderer, renderState);
        }

        // Render drag & drop ghost at the end (on top of everything)
        _dragDropManager?.RenderDragGhost(renderer);

        // Render performance debug overlays (heatmap and overdraw on top)
        Rayo.DevTools.DirtyHeatmap.Render(renderer, Root);
        Rayo.DevTools.OverdrawVisualizer.Render(renderer, Root);
    }

    private void RenderElement(VisualElement element, IRenderer renderer, RenderState renderState)
    {
        if (!element.IsVisible) return;

        bool subtreeIntersectsDirty = renderState.RequiresFullRender || SubtreeIntersectsDirty(element, renderState);
        if (renderState.AllowPartialTraversal && !subtreeIntersectsDirty)
            return;

        Rayo.DevTools.PerformanceTracker.RecordRendered();

        bool hasTransform = element.HasRenderTransform;
        if (hasTransform)
        {
            renderer.PushTransform(element.GetRenderTransform());
        }

        try
        {
            // SOLID: Delegation to specialized renderer
            var effects = element.GetVisualEffects();

            if (effects.Count > 0)
            {
                // Pre-render effects (opacity, blur context)
                _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.PreRender);

                // Background effects (shadows, gradients)
                _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.Background);
            }

            // Render the element with lifecycle hooks
            element.InvokeOnBeforeRender(renderer);
            element.Render(renderer);
            element.InvokeOnAfterRender(renderer);

            // Render children (use ToArray to avoid collection modification during iteration)
            if (!element.RendersChildrenManually)
            {
                var clipBounds = GetClipBounds(element);
                bool shouldClipChildren = element.ClipToBounds && clipBounds.width > 0 && clipBounds.height > 0;
                bool useRoundedClip = shouldClipChildren && HasRoundedClip(clipBounds.radius);
                if (useRoundedClip)
                {
                    renderer.PushRoundedClip(
                        clipBounds.x,
                        clipBounds.y,
                        clipBounds.width,
                        clipBounds.height,
                        clipBounds.radius.TopLeft,
                        clipBounds.radius.TopRight,
                        clipBounds.radius.BottomRight,
                        clipBounds.radius.BottomLeft);
                }
                else if (shouldClipChildren)
                {
                    renderer.PushScissor(clipBounds.x, clipBounds.y, clipBounds.width, clipBounds.height);
                }

                // Use GetChildrenByZIndex() so ZIndex controls rendering order (like MAUI)
                foreach (var child in element.GetChildrenByZIndex().ToArray())
                {
                    RenderElement(child, renderer, renderState);
                }

                if (useRoundedClip)
                {
                    renderer.PopRoundedClip();
                }
                else if (shouldClipChildren)
                {
                    renderer.PopScissor();
                }
            }

            // Post-render effects (glow, inner shadows)
            if (effects.Count > 0)
            {
                _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.PostRender);
            }
        }
        finally
        {
            if (hasTransform)
            {
                renderer.PopTransform();
            }
        }
    }

    private void RenderRoot(IRenderer renderer, RenderState renderState)
    {
        if (Root == null)
            return;

        if (_layerCache == null)
        {
            RenderElement(Root, renderer, renderState);
            return;
        }

        float width = Math.Max(0, Root.ComputedWidth);
        float height = Math.Max(0, Root.ComputedHeight);
        if (width <= 0 || height <= 0)
            return;

        var layer = _layerCache.GetOrCreateLayer(GetRootLayerId(), width, height);
        layer.MarkUsed();

        if (Root.NeedsLayout || Root.NeedsPaint || renderState.RequiresFullRender || layer.IsDirty)
        {
            renderer.BeginRenderToTexture(layer.Texture!);
            renderer.Clear(RenderColor.Transparent);

            try
            {
                RenderElement(
                    Root,
                    renderer,
                    renderState with { RequiresFullRender = true, AllowPartialTraversal = false });
            }
            finally
            {
                renderer.EndRenderToTexture();
            }

            layer.IsDirty = false;
        }

        renderer.DrawTexture(layer.Texture!, Root.ComputedX, Root.ComputedY, width, height);
    }

    private void RenderOverlay(VisualElement overlay, IRenderer renderer, RenderState renderState)
    {
        if (!overlay.IsVisible)
            return;

        if (_layerCache == null)
        {
            RenderElement(overlay, renderer, renderState);
            return;
        }

        float width = Math.Max(0, overlay.ComputedWidth);
        float height = Math.Max(0, overlay.ComputedHeight);
        if (width <= 0 || height <= 0)
            return;

        string layerId = GetOverlayLayerId(overlay);
        var layer = _layerCache.GetOrCreateLayer(layerId, width, height);
        layer.MarkUsed();

        if (overlay.NeedsLayout || overlay.NeedsPaint || layer.IsDirty)
        {
            renderer.BeginRenderToTexture(layer.Texture!);
            renderer.Clear(RenderColor.Transparent);
            renderer.PushTransform(Matrix3x2.CreateTranslation(-overlay.ComputedX, -overlay.ComputedY));

            try
            {
                RenderElement(
                    overlay,
                    renderer,
                    renderState with { RequiresFullRender = true, AllowPartialTraversal = false });
            }
            finally
            {
                renderer.PopTransform();
                renderer.EndRenderToTexture();
            }

            layer.IsDirty = false;
        }

        renderer.DrawTexture(layer.Texture!, overlay.ComputedX, overlay.ComputedY, width, height);
    }

    private static bool HasRoundedClip(Rayo.CornerRadius radius)
    {
        return radius.TopLeft > 0 || radius.TopRight > 0 || radius.BottomRight > 0 || radius.BottomLeft > 0;
    }

    private void EnsureLayerCache(IRenderer renderer)
    {
        if (ReferenceEquals(_layerCacheRenderer, renderer) && _layerCache != null)
            return;

        _layerCache?.Dispose();
        _layerCache = new LayerCache(renderer);
        _layerCacheRenderer = renderer;
        _lastLayerCacheCleanupUtc = DateTime.UtcNow;
    }

    private static string GetOverlayLayerId(VisualElement overlay)
    {
        return $"overlay:{RuntimeHelpers.GetHashCode(overlay)}";
    }

    private static string GetRootLayerId()
    {
        return "root";
    }

    private void MarkAllCachedLayersDirty()
    {
        _layerCache?.MarkAllDirty();
    }

    private void MarkRootLayerDirty()
    {
        _layerCache?.MarkLayerDirty(GetRootLayerId());
    }

    private void CleanupLayerCacheIfNeeded()
    {
        if (_layerCache == null)
            return;

        var now = DateTime.UtcNow;
        if (now - _lastLayerCacheCleanupUtc < TimeSpan.FromMinutes(1))
            return;

        _layerCache.Cleanup(TimeSpan.FromMinutes(2));
        _lastLayerCacheCleanupUtc = now;
    }

    private void MarkOverlayLayerDirty(VisualElement overlay)
    {
        if (_layerCache == null)
            return;

        _layerCache.MarkLayerDirty(GetOverlayLayerId(overlay));
    }

    private VisualElement? FindOwningOverlay(VisualElement element)
    {
        var current = element;
        while (current != null)
        {
            if (_overlays.Contains(current))
                return current;

            current = current.Parent;
        }

        return null;
    }

    private RenderState CaptureRenderState(bool fullSurfaceCleared)
    {
        bool requiresFullRender = _dirtyRegions.IsFullScreenDirty();
        bool allowPartialTraversal =
            PartialRenderMode == PartialRenderPolicy.ExperimentalTraversal &&
            !fullSurfaceCleared &&
            !requiresFullRender;

        return new RenderState(requiresFullRender, _dirtyRegions.GetDirtyRegions(), allowPartialTraversal);
    }

    private static bool SubtreeIntersectsDirty(VisualElement element, RenderState renderState)
    {
        if (renderState.RequiresFullRender || IntersectsAnyRegion(element, renderState.DirtyRegions))
            return true;

        foreach (var child in element.GetChildren())
        {
            if (SubtreeIntersectsDirty(child, renderState))
                return true;
        }

        return false;
    }

    private static bool IntersectsAnyRegion(VisualElement element, IReadOnlyList<DirtyRegion> dirtyRegions)
    {
        if (dirtyRegions.Count == 0 || element.ComputedWidth <= 0 || element.ComputedHeight <= 0)
            return false;

        float elemX = element.ComputedX;
        float elemY = element.ComputedY;
        float elemW = element.ComputedWidth;
        float elemH = element.ComputedHeight;

        foreach (var region in dirtyRegions)
        {
            if (RectanglesIntersect(elemX, elemY, elemW, elemH, region.X, region.Y, region.Width, region.Height))
                return true;
        }

        return false;
    }

    private static bool RectanglesIntersect(float x1, float y1, float w1, float h1,
        float x2, float y2, float w2, float h2)
    {
        return !(x1 + w1 < x2 || x1 > x2 + w2 || y1 + h1 < y2 || y1 > y2 + h2);
    }

    private static (float x, float y, float width, float height, Rayo.CornerRadius radius) GetClipBounds(VisualElement element)
    {
        float x = element.ComputedX;
        float y = element.ComputedY;
        float width = element.ComputedWidth;
        float height = element.ComputedHeight;
        var radius = element.BorderRadius;

        if (element is Rayo.Controls.Frame frame)
        {
            float inset = Math.Max(0, frame.BorderWidth);
            x += inset;
            y += inset;
            width -= inset * 2f;
            height -= inset * 2f;

            radius = new Rayo.CornerRadius(
                Math.Max(0, radius.TopLeft - inset),
                Math.Max(0, radius.TopRight - inset),
                Math.Max(0, radius.BottomRight - inset),
                Math.Max(0, radius.BottomLeft - inset));
        }
        else if (element is Rayo.Controls.Border border)
        {
            var thickness = border.BorderThickness;
            x += thickness.Left;
            y += thickness.Top;
            width -= thickness.Left + thickness.Right;
            height -= thickness.Top + thickness.Bottom;

            var borderRadius = border.CornerRadius;
            radius = new Rayo.CornerRadius(
                Math.Max(0, borderRadius.TopLeft - thickness.Left),
                Math.Max(0, borderRadius.TopRight - thickness.Right),
                Math.Max(0, borderRadius.BottomRight - thickness.Right),
                Math.Max(0, borderRadius.BottomLeft - thickness.Left));
        }

        if (width < 0) width = 0;
        if (height < 0) height = 0;

        return (x, y, width, height, radius);
    }

    private sealed record RenderState(bool RequiresFullRender, IReadOnlyList<DirtyRegion> DirtyRegions, bool AllowPartialTraversal);
}

public enum PartialRenderPolicy
{
    Disabled,
    ExperimentalTraversal
}


