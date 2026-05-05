using Rayo.Rendering;
using RenderColor = Rayo.Rendering.Color;

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

    /// <summary>
    /// Gets the list of overlay elements (read-only).
    /// Overlays are rendered on top of the main UI tree.
    /// </summary>
    public IReadOnlyList<VisualElement> Overlays => _overlays;

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
            MarkNeedsLayout();
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
            MarkNeedsRender();
            OverlaysChanged?.Invoke();
        }
    }

    public void MarkNeedsLayout()
    {
        _needsLayout = true;
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
            MarkNeedsLayout();
        }

        // Use the new granular dirty flags system
        if (_needsLayout || Root.NeedsLayout)
        {
            Rayo.DevTools.PerformanceTracker.RecordMeasured();
            Root.Measure(width, height);
            Rayo.DevTools.PerformanceTracker.RecordArranged();
            Root.Arrange(0, 0, width, height);
            ClearDirtyFlags(Root);

            // Layout overlays (they get full window size)
            foreach (var overlay in _overlays)
            {
                overlay.Measure(width, height);

                float x = overlay.X;
                float y = overlay.Y;
                float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? width : overlay.DesiredWidth;
                float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? height : overlay.DesiredHeight;

                overlay.Arrange(x, y, w, h);
                ClearDirtyFlags(overlay);
            }

            _needsLayout = false;

            // Complete scheduler frame
            _scheduler.FrameComplete();

            MarkNeedsRender();
        }
		else if (Root.NeedsPaint)
        {
            ClearDirtyFlags(Root);
			// Ensure paint-only frames release scheduled work so idle mode can resume
			_scheduler.FrameComplete();
            MarkNeedsRender();
        }
    }

    public void ClearRenderFlag()
    {
        _needsRender = false;
        _renderRequestQueued = false;
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

    public void Render(IRenderer renderer)
    {
        if (Root == null) return;
        RenderElement(Root, renderer);

        // Render overlays on top of the main UI (for Drawer, Dialog, Menu, etc.)
        foreach (var overlay in _overlays)
        {
            RenderElement(overlay, renderer);
        }

        // Render drag & drop ghost at the end (on top of everything)
        _dragDropManager?.RenderDragGhost(renderer);

        // Render performance debug overlays (heatmap and overdraw on top)
        Rayo.DevTools.DirtyHeatmap.Render(renderer, Root);
        Rayo.DevTools.OverdrawVisualizer.Render(renderer, Root);
    }

    private void RenderElement(VisualElement element, IRenderer renderer)
    {
        if (!element.IsVisible) return;

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
                    RenderElement(child, renderer);
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

    private static bool HasRoundedClip(Rayo.CornerRadius radius)
    {
        return radius.TopLeft > 0 || radius.TopRight > 0 || radius.BottomRight > 0 || radius.BottomLeft > 0;
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
}


