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
    private bool _needsMeasure = true;
    private bool _needsArrange = true;
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
    public void AddOverlay(VisualElement overlay, VisualElement? owner = null)
    {
        if (!_overlays.Contains(overlay))
        {
            overlay.CaptureDetachedTheme(owner);
            _overlays.Add(overlay);
            overlay.NotifyMounted();
            MarkOverlayLayerDirty(overlay);
            MarkElementNeedsMeasure(overlay);
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
            overlay.NotifyUnmounted();
            _layerCache?.RemoveLayer(GetOverlayLayerId(overlay));
            _dirtyRegions.MarkElementDirty(overlay, DirtyReason.LayoutChanged);
            MarkNeedsRender();
            OverlaysChanged?.Invoke();
        }
    }

    public void MarkNeedsLayout()
    {
        MarkNeedsMeasure();
    }

    public void MarkNeedsMeasure()
    {
        _needsMeasure = true;
        _needsArrange = true;
        _dirtyRegions.MarkFullScreenDirty();
        MarkAllCachedLayersDirty();
        MarkNeedsRender();
    }

    public void MarkElementNeedsMeasure(VisualElement element)
    {
        _scheduler.ScheduleMeasure(element);
        _dirtyRegions.MarkElementDirty(element, DirtyReason.LayoutChanged);
        MarkElementRetainedLayerDirty(element);

        MarkNeedsRender();
    }

    public void MarkElementNeedsArrange(VisualElement element)
    {
        _needsArrange = true;
        _scheduler.ScheduleArrange(element);
        _dirtyRegions.MarkElementDirty(element, DirtyReason.LayoutChanged);
        MarkElementRetainedLayerDirty(element);

        MarkNeedsRender();
    }

    public void MarkElementNeedsPaint(VisualElement element)
    {
        _scheduler.SchedulePaint(element);
        _dirtyRegions.MarkElementDirty(element, DirtyReason.ContentChanged);
        MarkElementRetainedLayerDirty(element);

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
            MarkViewportSubtreeNeedsLayout();
            MarkNeedsMeasure();
        }

        bool fullRootMeasure = _needsMeasure || Root.NeedsMeasure;
        bool fullRootArrange = _needsArrange || Root.NeedsArrange;

        if (fullRootMeasure)
        {
            _dirtyRegions.MarkFullScreenDirty();
            Rayo.DevTools.PerformanceTracker.RecordRelayoutRoot();
            Rayo.DevTools.PerformanceTracker.RecordMeasured();
            Root.MeasureUpdate(width, height);
            Rayo.DevTools.PerformanceTracker.RecordArranged();
            Root.ArrangeUpdate(0, 0, width, height);
            ClearDirtyFlags(Root);
            LayoutOverlays(_overlays, width, height);
            _needsMeasure = false;
            _needsArrange = false;
            _scheduler.FrameComplete();
            MarkNeedsRender();
            return;
        }

        bool didMeasure = ProcessIncrementalMeasureRoots(width, height);
        bool didArrange = ProcessIncrementalArrangeRoots(width, height);
        bool didOverlayLayout = ProcessIncrementalOverlayLayout(width, height);

        if (didMeasure || didArrange || didOverlayLayout)
        {
            _scheduler.FrameComplete();
            MarkNeedsRender();
        }
        else if (Root.NeedsPaint || _scheduler.NeedsPaint || AnyOverlayNeedsPaint())
        {
            var dirtyElements = CaptureTrackedDirtyElements(includeMeasure: false, includeArrange: false, includePaint: true);
            if (dirtyElements.Count > 0)
                ClearDirtyFlagsForTrackedElements(dirtyElements);
            else
                ClearDirtyFlags(Root);

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

    public void ResetRenderCache()
    {
        _layerCache?.Dispose();
        _layerCache = null;
        _layerCacheRenderer = null;
        _dirtyRegions.MarkFullScreenDirty();
        MarkNeedsRender();
    }

    private void ClearDirtyFlags(VisualElement element)
    {
        element.NeedsMeasure = false;
        element.NeedsArrange = false;
        element.NeedsPaint = false;
        // Use GetChildren() instead of Children property to handle LayoutBase correctly
        foreach (var child in element.GetChildren().ToArray())
        {
            ClearDirtyFlags(child);
        }
    }

    private void MarkViewportSubtreeNeedsLayout()
    {
        if (Root != null)
            MarkSubtreeNeedsLayout(Root);

        foreach (var overlay in _overlays.ToArray())
            MarkSubtreeNeedsLayout(overlay);
    }

    private static void MarkSubtreeNeedsLayout(VisualElement element)
    {
        element.NeedsMeasure = true;
        element.NeedsArrange = true;
        element.NeedsPaint = true;

        foreach (var child in element.GetChildren().ToArray())
            MarkSubtreeNeedsLayout(child);
    }

    private void LayoutOverlay(VisualElement overlay, float width, float height, bool includeMeasure = true)
    {
        if (includeMeasure)
        {
            Rayo.DevTools.PerformanceTracker.RecordMeasured();
            overlay.MeasureUpdate(width, height);
        }

        float x = overlay.X;
        float y = overlay.Y;
        float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? width : overlay.DesiredWidth;
        float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? height : overlay.DesiredHeight;

        Rayo.DevTools.PerformanceTracker.RecordArranged();
        overlay.ArrangeUpdate(x, y, w, h);
        ClearDirtyFlags(overlay);
    }

    private void LayoutOverlays(IEnumerable<VisualElement> overlays, float width, float height)
    {
        foreach (var overlay in overlays)
            LayoutOverlay(overlay, width, height);
    }

    private bool ProcessIncrementalMeasureRoots(float width, float height)
    {
        var roots = CollectMeasureRoots();
        if (roots.Count == 0)
            return false;

        foreach (var batch in GroupMeasureRootsByArrangeHost(roots))
        {
            Rayo.DevTools.PerformanceTracker.RecordRelayoutRoot();

            foreach (var root in batch.Roots)
            {
                if (FindOwningOverlay(root) != null)
                    continue;

                bool hasPreviousMeasureConstraints =
                    !float.IsNaN(root.LastMeasuredAvailableWidth) &&
                    !float.IsNaN(root.LastMeasuredAvailableHeight);
                if (!hasPreviousMeasureConstraints)
                {
                    _needsMeasure = true;
                    return ProcessIncrementalMeasureRootsFallback(width, height);
                }

                Rayo.DevTools.PerformanceTracker.RecordMeasured();
                root.ForceMeasure(root.LastMeasuredAvailableWidth, root.LastMeasuredAvailableHeight);
            }

            var arrangeHost = batch.ArrangeHost;
            Rayo.DevTools.PerformanceTracker.RecordArranged();
            arrangeHost.ForceArrange(arrangeHost.ComputedX, arrangeHost.ComputedY, arrangeHost.ComputedWidth, arrangeHost.ComputedHeight);
            ClearDirtyFlags(arrangeHost);
        }

        _needsArrange = false;
        return true;
    }

    private bool ProcessIncrementalMeasureRootsFallback(float width, float height)
    {
        if (Root == null)
            return false;

        Rayo.DevTools.PerformanceTracker.RecordRelayoutRoot();
        Rayo.DevTools.PerformanceTracker.RecordMeasured();
        Root.ForceMeasure(width, height);
        Rayo.DevTools.PerformanceTracker.RecordArranged();
        Root.ForceArrange(0, 0, width, height);
        ClearDirtyFlags(Root);
        LayoutOverlays(_overlays, width, height);
        _needsMeasure = false;
        _needsArrange = false;
        return true;
    }

    private bool ProcessIncrementalArrangeRoots(float width, float height)
    {
        var arrangeHosts = CollectArrangeHosts();
        if (arrangeHosts.Count == 0)
            return false;

        foreach (var arrangeHost in arrangeHosts)
        {
            if (FindOwningOverlay(arrangeHost) != null)
                continue;

            float arrangeWidth = ReferenceEquals(arrangeHost, Root) ? width : arrangeHost.ComputedWidth;
            float arrangeHeight = ReferenceEquals(arrangeHost, Root) ? height : arrangeHost.ComputedHeight;
            float arrangeX = ReferenceEquals(arrangeHost, Root) ? 0 : arrangeHost.ComputedX;
            float arrangeY = ReferenceEquals(arrangeHost, Root) ? 0 : arrangeHost.ComputedY;

            Rayo.DevTools.PerformanceTracker.RecordRelayoutRoot();
            Rayo.DevTools.PerformanceTracker.RecordArranged();
            arrangeHost.ForceArrange(arrangeX, arrangeY, arrangeWidth, arrangeHeight);
            ClearDirtyFlags(arrangeHost);
        }

        _needsArrange = false;
        return true;
    }

    private bool ProcessIncrementalOverlayLayout(float width, float height)
    {
        var workItems = CaptureOverlayLayoutWorkItems();
        if (workItems.Count == 0)
            return false;

        for (int i = 0; i < workItems.Count; i++)
            Rayo.DevTools.PerformanceTracker.RecordRelayoutRoot();

        foreach (var item in workItems)
        {
            if (!item.RequiresMeasure && !item.Overlay.HasValidMeasure)
            {
                LayoutOverlay(item.Overlay, width, height, includeMeasure: true);
                continue;
            }

            LayoutOverlay(item.Overlay, width, height, includeMeasure: item.RequiresMeasure);
        }

        return true;
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
                    current.NeedsMeasure = false;
                    current.NeedsArrange = false;
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

        element.NeedsMeasure = false;
        element.NeedsArrange = false;
        element.NeedsPaint = false;

        foreach (var child in element.GetChildren())
        {
            if (child.NeedsMeasure || child.NeedsArrange || child.NeedsPaint)
                ClearDirtySubtree(child, cleared);
        }
    }

    private List<VisualElement> CaptureTrackedDirtyElements(bool includeMeasure, bool includeArrange, bool includePaint)
    {
        var tracked = new HashSet<VisualElement>();
        var dirtyMeasureElements = _scheduler.SnapshotDirtyMeasureElements();
        var dirtyArrangeElements = _scheduler.SnapshotDirtyArrangeElements();
        var dirtyPaintElements = _scheduler.SnapshotDirtyPaintElements();

        if (includeMeasure)
        {
            foreach (var element in dirtyMeasureElements)
                tracked.Add(element);
        }

        if (includeArrange)
        {
            foreach (var element in dirtyArrangeElements)
                tracked.Add(element);
        }

        if (includePaint)
        {
            foreach (var element in dirtyPaintElements)
                tracked.Add(element);
        }

        if (tracked.Count == 0)
        {
            if (Root != null && (Root.NeedsMeasure || Root.NeedsArrange || Root.NeedsPaint))
                tracked.Add(Root);

            foreach (var overlay in _overlays)
            {
                if (overlay.NeedsMeasure || overlay.NeedsArrange || overlay.NeedsPaint)
                    tracked.Add(overlay);
            }
        }

        return tracked.ToList();
    }

    private bool AnyOverlayNeedsPaint()
    {
        return _overlays.Any(static overlay => overlay.NeedsPaint);
    }

    private IReadOnlyList<OverlayLayoutWorkItem> CaptureOverlayLayoutWorkItems()
    {
        if (_overlays.Count == 0)
            return Array.Empty<OverlayLayoutWorkItem>();

        var overlays = new Dictionary<VisualElement, bool>();
        var dirtyMeasureElements = _scheduler.SnapshotDirtyMeasureElements();
        var dirtyArrangeElements = _scheduler.SnapshotDirtyArrangeElements();

        foreach (var element in dirtyMeasureElements)
        {
            var owningOverlay = FindOwningOverlay(element);
            if (owningOverlay != null)
                overlays[owningOverlay] = true;
        }

        foreach (var element in dirtyArrangeElements)
        {
            var owningOverlay = FindOwningOverlay(element);
            if (owningOverlay == null)
                continue;

            if (!overlays.ContainsKey(owningOverlay))
                overlays[owningOverlay] = false;
        }

        foreach (var overlay in _overlays)
        {
            if (overlay.NeedsMeasure)
            {
                overlays[overlay] = true;
            }
            else if (overlay.NeedsArrange && !overlays.ContainsKey(overlay))
            {
                overlays[overlay] = false;
            }
        }

        if (overlays.Count == 0)
            return Array.Empty<OverlayLayoutWorkItem>();

        return overlays
            .Select(static pair => new OverlayLayoutWorkItem(pair.Key, pair.Value))
            .ToList();
    }

    private List<VisualElement> CollectMeasureRoots()
    {
        var roots = new HashSet<VisualElement>();
        var dirtyMeasureElements = _scheduler.SnapshotDirtyMeasureElements();

        foreach (var element in dirtyMeasureElements)
        {
            if (FindOwningOverlay(element) != null)
                continue;

            var root = FindMeasureRelayoutRoot(element);
            roots.Add(root);
        }

        return DeduplicateRoots(roots);
    }

    private List<MeasureArrangeBatch> GroupMeasureRootsByArrangeHost(IEnumerable<VisualElement> roots)
    {
        var batches = new Dictionary<VisualElement, List<VisualElement>>();

        foreach (var root in roots)
        {
            var arrangeHost = root.Parent ?? root;
            if (!batches.TryGetValue(arrangeHost, out var batchRoots))
            {
                batchRoots = new List<VisualElement>();
                batches[arrangeHost] = batchRoots;
            }

            batchRoots.Add(root);
        }

        return batches
            .Select(static pair => new MeasureArrangeBatch(pair.Key, pair.Value))
            .ToList();
    }

    private List<VisualElement> CollectArrangeHosts()
    {
        var hosts = new HashSet<VisualElement>();
        var dirtyArrangeElements = _scheduler.SnapshotDirtyArrangeElements();

        foreach (var element in dirtyArrangeElements)
        {
            if (FindOwningOverlay(element) != null)
                continue;

            var root = FindArrangeRelayoutRoot(element);
            var host = root.Parent ?? root;
            hosts.Add(host);
        }

        return DeduplicateRoots(hosts);
    }

    private static List<VisualElement> DeduplicateRoots(IEnumerable<VisualElement> candidates)
    {
        var roots = new List<VisualElement>();

        foreach (var candidate in candidates)
        {
            bool covered = roots.Any(existing => IsAncestorOrSelf(existing, candidate));
            if (covered)
                continue;

            roots.RemoveAll(existing => IsAncestorOrSelf(candidate, existing));
            roots.Add(candidate);
        }

        return roots;
    }

    private VisualElement FindMeasureRelayoutRoot(VisualElement element)
    {
        var current = element;

        while (current.Parent != null &&
               current.Parent.NeedsMeasure &&
               !current.CreatesMeasureBoundaryForParent() &&
               !current.Parent.AbsorbsDescendantMeasureChange())
        {
            current = current.Parent;
        }

        return current;
    }

    private VisualElement FindArrangeRelayoutRoot(VisualElement element)
    {
        var current = element;

        while (current.Parent != null &&
               _scheduler.ContainsDirtyArrangeElement(current.Parent) &&
               !current.CreatesMeasureBoundaryForParent() &&
               !current.Parent.AbsorbsDescendantArrangeChange())
        {
            current = current.Parent;
        }

        return current;
    }

    private static bool IsAncestorOrSelf(VisualElement ancestor, VisualElement element)
    {
        var current = element;
        while (current != null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;

            current = current.Parent;
        }

        return false;
    }

    private sealed record MeasureArrangeBatch(VisualElement ArrangeHost, IReadOnlyList<VisualElement> Roots);
    private sealed record OverlayLayoutWorkItem(VisualElement Overlay, bool RequiresMeasure);

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

        bool hasOpacity = element.Opacity < 1f;
        if (hasOpacity)
        {
            renderer.PushOpacity(element.Opacity);
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
                foreach (var child in element.GetChildrenByZIndex())
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

            element.InvokeOnAfterRender(renderer);

            // Post-render effects (glow, inner shadows)
            if (effects.Count > 0)
            {
                _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.PostRender);
            }
        }
        finally
        {
            if (hasOpacity)
            {
                renderer.PopOpacity();
            }

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

        if (CanUseRetainedChildLayers(Root))
        {
            RenderRetainedContainer(Root, renderer, renderState, GetRootBaseLayerId());
            return;
        }

        float width = Math.Max(0, Root.ComputedWidth);
        float height = Math.Max(0, Root.ComputedHeight);
        if (width <= 0 || height <= 0)
            return;

        var layer = _layerCache.GetOrCreateLayer(GetRootLayerId(), width, height);
        layer.MarkUsed();

        if (Root.NeedsMeasure || Root.NeedsArrange || Root.NeedsPaint || renderState.RequiresFullRender || layer.IsDirty)
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

        if (CanUseRetainedChildLayers(overlay))
        {
            RenderRetainedContainer(overlay, renderer, renderState, GetOverlayBaseLayerId(overlay));
            return;
        }

        float width = Math.Max(0, overlay.ComputedWidth);
        float height = Math.Max(0, overlay.ComputedHeight);
        if (width <= 0 || height <= 0)
            return;

        string layerId = GetOverlayLayerId(overlay);
        var layer = _layerCache.GetOrCreateLayer(layerId, width, height);
        layer.MarkUsed();

        if (overlay.NeedsMeasure || overlay.NeedsArrange || overlay.NeedsPaint || layer.IsDirty)
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

    private void RenderRetainedContainer(VisualElement host, IRenderer renderer, RenderState renderState, string baseLayerId)
    {
        if (_layerCache == null)
        {
            RenderElement(host, renderer, renderState);
            return;
        }

        float width = Math.Max(0, host.ComputedWidth);
        float height = Math.Max(0, host.ComputedHeight);
        if (width <= 0 || height <= 0)
            return;

        var baseLayer = _layerCache.GetOrCreateLayer(baseLayerId, width, height);
        baseLayer.MarkUsed();

        if (host.NeedsMeasure || host.NeedsArrange || host.NeedsPaint || renderState.RequiresFullRender || baseLayer.IsDirty)
        {
            renderer.BeginRenderToTexture(baseLayer.Texture!);
            renderer.Clear(RenderColor.Transparent);
            renderer.PushTransform(Matrix3x2.CreateTranslation(-host.ComputedX, -host.ComputedY));

            try
            {
                RenderElementVisualsOnly(host, renderer);
            }
            finally
            {
                renderer.PopTransform();
                renderer.EndRenderToTexture();
            }

            baseLayer.IsDirty = false;
        }

        bool hasTransform = host.HasRenderTransform;
        if (hasTransform)
        {
            renderer.PushTransform(host.GetRenderTransform());
        }

        bool hasOpacity = host.Opacity < 1f;
        if (hasOpacity)
        {
            renderer.PushOpacity(host.Opacity);
        }

        try
        {
            renderer.DrawTexture(baseLayer.Texture!, host.ComputedX, host.ComputedY, width, height);

            var clipBounds = GetClipBounds(host);
            bool shouldClipChildren = host.ClipToBounds && clipBounds.width > 0 && clipBounds.height > 0;
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

            foreach (var child in host.GetChildrenByZIndex())
            {
                RenderRetainedBranch(host, child, renderer, renderState);
            }

            if (useRoundedClip)
            {
                renderer.PopRoundedClip();
            }
            else if (shouldClipChildren)
            {
                renderer.PopScissor();
            }

            host.InvokeOnAfterRender(renderer);
        }
        finally
        {
            if (hasOpacity)
            {
                renderer.PopOpacity();
            }

            if (hasTransform)
            {
                renderer.PopTransform();
            }
        }
    }

    private void RenderRetainedBranch(VisualElement host, VisualElement branch, IRenderer renderer, RenderState renderState)
    {
        if (!branch.IsVisible)
            return;

        if (_layerCache == null)
        {
            RenderElement(branch, renderer, renderState);
            return;
        }

        float width = Math.Max(0, branch.ComputedWidth);
        float height = Math.Max(0, branch.ComputedHeight);
        if (width <= 0 || height <= 0)
            return;

        string layerId = GetRetainedBranchLayerId(host, branch);
        var layer = _layerCache.GetOrCreateLayer(layerId, width, height);
        layer.MarkUsed();

        bool branchDirty = renderState.RequiresFullRender || branch.NeedsMeasure || branch.NeedsArrange || branch.NeedsPaint || layer.IsDirty || SubtreeIntersectsDirty(branch, renderState);
        if (branchDirty)
        {
            renderer.BeginRenderToTexture(layer.Texture!);
            renderer.Clear(RenderColor.Transparent);
            renderer.PushTransform(Matrix3x2.CreateTranslation(-branch.ComputedX, -branch.ComputedY));

            try
            {
                RenderElement(branch, renderer, renderState with { RequiresFullRender = true, AllowPartialTraversal = false });
            }
            finally
            {
                renderer.PopTransform();
                renderer.EndRenderToTexture();
            }

            layer.IsDirty = false;
        }

        renderer.DrawTexture(layer.Texture!, branch.ComputedX, branch.ComputedY, width, height);
    }

    private void RenderElementVisualsOnly(VisualElement element, IRenderer renderer)
    {
        var effects = element.GetVisualEffects();

        if (effects.Count > 0)
        {
            _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.PreRender);
            _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.Background);
        }

        element.InvokeOnBeforeRender(renderer);
        element.Render(renderer);

        if (effects.Count > 0)
        {
            _effectsRenderer.RenderEffects(element, renderer, EffectRenderPhase.PostRender);
        }
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

    private static string GetOverlayBaseLayerId(VisualElement overlay)
    {
        return $"{GetOverlayLayerId(overlay)}:base";
    }

    private static string GetRootLayerId()
    {
        return "root";
    }

    private static string GetRootBaseLayerId()
    {
        return "root:base";
    }

    private static string GetRetainedBranchLayerId(VisualElement host, VisualElement branch)
    {
        string hostId = ReferenceEquals(host, Current?.Root) ? "root" : $"overlay:{RuntimeHelpers.GetHashCode(host)}";
        return $"{hostId}:branch:{RuntimeHelpers.GetHashCode(branch)}";
    }

    private void MarkAllCachedLayersDirty()
    {
        _layerCache?.MarkAllDirty();
    }

    private void MarkRootLayerDirty()
    {
        _layerCache?.MarkLayerDirty(GetRootLayerId());
        _layerCache?.MarkLayerDirty(GetRootBaseLayerId());
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
        _layerCache.MarkLayerDirty(GetOverlayBaseLayerId(overlay));
    }

    private void MarkElementRetainedLayerDirty(VisualElement element)
    {
        if (_layerCache == null)
            return;

        var host = FindOwningOverlay(element) ?? Root;
        if (host == null)
            return;

        if (!CanUseRetainedChildLayers(host) || ReferenceEquals(host, element))
        {
            MarkContainerBaseLayerDirty(host);
            return;
        }

        var branch = FindDirectRetainedBranch(host, element);
        if (branch == null)
        {
            MarkContainerBaseLayerDirty(host);
            return;
        }

        _layerCache.MarkLayerDirty(GetRetainedBranchLayerId(host, branch));
    }

    private void MarkContainerBaseLayerDirty(VisualElement host)
    {
        if (_layerCache == null)
            return;

        if (ReferenceEquals(host, Root))
        {
            _layerCache.MarkLayerDirty(GetRootBaseLayerId());
            _layerCache.MarkLayerDirty(GetRootLayerId());
            return;
        }

        _layerCache.MarkLayerDirty(GetOverlayBaseLayerId(host));
        _layerCache.MarkLayerDirty(GetOverlayLayerId(host));
    }

    private static bool CanUseRetainedChildLayers(VisualElement host)
    {
        return !host.RendersChildrenManually && host.GetVisualEffects().Count == 0;
    }

    private static VisualElement? FindDirectRetainedBranch(VisualElement host, VisualElement element)
    {
        var current = element;
        while (current != null && current.Parent != null && !ReferenceEquals(current.Parent, host))
        {
            current = current.Parent;
        }

        return current != null && ReferenceEquals(current.Parent, host) ? current : null;
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
        var radius = element.VisualCornerRadius;

        if (element is Rayo.Controls.Frame frame)
        {
            float inset = Math.Max(0, frame.BorderThickness.Left);
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


