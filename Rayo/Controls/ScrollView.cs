namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interactions;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;
using IRenderer = Rayo.Rendering.IRenderer;

public enum ScrollBarVisibility
{
    Auto,
    Always,
    Disabled,
    AsNeeded = Auto
}

/// <summary>
/// Vertical and horizontal scroll container.
/// Implements IScrollable, IClippable, and IDragScrollable for generic integration with EventManager.
/// Migrated to new MAUI-like architecture: inherits from Rayo.Core.Layout<T>
/// </summary>
public class ScrollView : CompositeView<ScrollView>, IInputHandler, IScrollable, IClippable, IDragScrollable
{
    // Properties from old Layout base class
    // Background is now inherited from VisualElement as Brush
    // Use Background property for simple colors

    [LayoutProperty]
    public bool ShouldExpand
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = false;
    protected internal new bool HasExplicitWidth { get; private set; } = false;
    protected internal new bool HasExplicitHeight { get; private set; } = false;

    protected internal override bool RendersChildrenManually => true;

    #region ScrollbarBackground
    [PaintProperty]
    public Brush ScrollbarBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(40, 40, 40);
    #endregion

    #region ScrollbarThumb
    [PaintProperty]
    public Brush ScrollbarThumb
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region ScrollbarWidth
    [LayoutProperty]
    public float ScrollbarWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 8;
    #endregion

    #region VerticalScrollBarVisibility
    [LayoutProperty]
    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = ScrollBarVisibility.Auto;
    #endregion

    #region HorizontalScrollBarVisibility
    [LayoutProperty]
    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = ScrollBarVisibility.Disabled;
    #endregion



    // Convenience helpers — delegate to the tracked visibility properties.
    // [NotFluent] suppresses fluent generation (auto-invalidation happens via the real properties).
    [NotFluent]
    public bool ShowVerticalScrollbar
    {
        get => VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
        set => VerticalScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    [NotFluent]
    public bool ShowHorizontalScrollbar
    {
        get => HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
        set => HorizontalScrollBarVisibility = value ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    #region ShowScrollbars
    [LayoutProperty]
    public bool ShowScrollbars
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = !Rayo.Core.Platform.PlatformDetector.IsMobile;
    #endregion

    private float _verticalScrollOffset = 0;
    private float _horizontalScrollOffset = 0;
    private float _contentHeight = 0;
    private float _contentWidth = 0;

    // IScrollable - Public properties
    [NotFluent]
    public float ContentHeight => _contentHeight;
    
    [NotFluent]
    public float ContentWidth => _contentWidth;

    // State for drag scrolling
    private bool _isDragging = false;
    private bool _dragPending = false; // Waiting to see if the mouse moves
    private System.Numerics.Vector2 _dragStartPosition;
    private float _dragStartVerticalOffset;
    private float _dragStartHorizontalOffset;
    private const float DesktopDragThreshold = 5.0f;
    private const float MobileDragThreshold = 2.5f;

    // ✅ State for dragging the scrollbar thumb
    private bool _isDraggingVerticalThumb = false;
    private bool _isDraggingHorizontalThumb = false;
    private float _thumbDragStartOffset = 0;
    private float _thumbDragStartMouseY = 0;
    private float _thumbDragStartMouseX = 0;

    // ✅ Touch/Mobile scrolling with inertia
    private bool _isTouchDragging = false;
    private System.Numerics.Vector2 _lastTouchPosition;
    private DateTime _lastTouchTime;
    private float _velocityY = 0;
    private float _velocityX = 0;
    private bool _hasInertia = false;
    private const float InertiaDecelerationPerFrame = 0.94f;
    private const float MinInertiaVelocity = 0.5f; // Stop inertia when velocity is below this
    private const float InertiaBaseFrameTime = 1f / 60f;
    private const float MinInertiaDelta = 1f / 240f;
    private const float MaxInertiaDelta = 1f / 15f;
    private DateTime _lastInertiaUpdate = DateTime.UtcNow;

    private float GetDragThreshold()
    {
        return Rayo.Core.Platform.PlatformDetector.IsMobile ? MobileDragThreshold : DesktopDragThreshold;
    }

    private bool AreScrollbarsVisible()
    {
        return !Rayo.Core.Platform.PlatformDetector.IsMobile || ShowScrollbars;
    }

    // IInputHandler
    public bool CanHandleInput => true;

    // IDragScrollable
    public bool IsDragPending => _dragPending;

    public void StartDragPending()
    {
        _dragPending = true;
    }

    // Scroll offsets are managed directly (not via SetProperty); [NotFluent] suppresses
    // fluent generation and [PaintProperty] documents the repaint intent.
    [NotFluent, PaintProperty]
    public float VerticalScrollOffset
    {
        get => _verticalScrollOffset;
        set
        {
            // Force measurement if the content has changed before calculating the limit
            if (_contentHeight == 0 || _contentHeight < ComputedHeight)
            {
                // Measure the content with the current viewport size
                Measure(ComputedWidth, ComputedHeight);
            }
            float maxScroll = MaxVerticalScroll;
            float newValue = Math.Max(0, Math.Min(value, maxScroll));
            if (_verticalScrollOffset != newValue)
            {
                _verticalScrollOffset = newValue;
                RefreshScrollLayout();
                MarkNeedsPaint(); // ✅ CRITICAL: Force re-render for scrollbar

                ScrollInteractionNotifier.NotifyScrollActivity(this);
            }
        }
    }

    [NotFluent, PaintProperty]
    public float HorizontalScrollOffset
    {
        get => _horizontalScrollOffset;
        set
        {
            float maxScroll = MaxHorizontalScroll;
            float newValue = Math.Max(0, Math.Min(value, maxScroll));

            if (_horizontalScrollOffset != newValue)
            {
                _horizontalScrollOffset = newValue;
                RefreshScrollLayout();
                MarkNeedsPaint(); // ✅ CRITICAL: Force re-render for scrollbar

                ScrollInteractionNotifier.NotifyScrollActivity(this);
            }

        }
    }

    private void RefreshScrollLayout()
    {
        if (ComputedWidth > 0 && ComputedHeight > 0 && !NeedsLayout)
        {
            // OPTIMIZATION: Call Arrange directly with current bounds to update child positions based on new scroll offsets
            // without triggering a full re-measure/re-layout pass on the whole tree.
            Arrange(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
            
            // On Android, we need to ensure the renderer knows something changed visual-wise
            MarkNeedsPaint();
        }
        else
        {
            MarkNeedsLayout();
        }
    }



    public ScrollView()
    {
        // ScrollView should stretch to fill its parent container by default
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public ScrollView(VisualElement content) : this()
    {
        AddChild(content);
    }


    public ScrollView Content(VisualElement content)
    {
        AddChild(content);
        return this;
    }

    public void ScrollToTop()
    {
        VerticalScrollOffset = 0;
    }

    public void ScrollToBottom()
    {
        VerticalScrollOffset = MaxVerticalScroll;
    }

    public void Scroll(float deltaY)
    {
        // Scroll aligned to the thumb and content size
        VerticalScrollOffset = VerticalScrollOffset + deltaY;
    }

    /// <summary>
    /// Ensures that the specified rectangle is visible within the viewport.
    /// Adjusts scroll offsets if the rectangle is outside the visible area.
    /// Coordinates are relative to the content's top-left.
    /// </summary>
    public void EnsureRectVisible(float rectX, float rectY, float rectWidth, float rectHeight)
    {
        float viewportWidth = ComputedWidth - Padding.Horizontal;
        float viewportHeight = ComputedHeight - Padding.Vertical;

        if (ShowVerticalScrollbar && AreScrollbarsVisible() && _contentHeight > ComputedHeight)
            viewportWidth -= ScrollbarWidth;
        if (ShowHorizontalScrollbar && AreScrollbarsVisible() && _contentWidth > ComputedWidth)
            viewportHeight -= ScrollbarWidth;

        // Vertical
        if (rectY < _verticalScrollOffset)
        {
            VerticalScrollOffset = rectY;
        }
        else if (rectY + rectHeight > _verticalScrollOffset + viewportHeight)
        {
            VerticalScrollOffset = rectY + rectHeight - viewportHeight;
        }

        // Horizontal
        if (rectX < _horizontalScrollOffset)
        {
            HorizontalScrollOffset = rectX;
        }
        else if (rectX + rectWidth > _horizontalScrollOffset + viewportWidth)
        {
            HorizontalScrollOffset = rectX + rectWidth - viewportWidth;
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        const float InfiniteThreshold = float.PositiveInfinity;

        // Debug logging
        //Console.WriteLine($"[ScrollView.Measure] availableWidth={availableWidth}, availableHeight={availableHeight}, VerticalAlignment={VerticalAlignment}");

        bool canScrollVertically = Orientation == ScrollOrientation.Vertical || Orientation == ScrollOrientation.Both;
        bool canScrollHorizontally = Orientation == ScrollOrientation.Horizontal || Orientation == ScrollOrientation.Both;

        // STEP 1: Determine ScrollView dimensions
        float scrollViewWidth;
        float scrollViewHeight;

        // Width calculation
        if (HasExplicitWidth)
        {
            scrollViewWidth = Width;
        }
        else if (availableWidth >= InfiniteThreshold && canScrollHorizontally)
        {
            // No explicit width + infinite space + horizontal scroll → need to measure content first
            scrollViewWidth = 0; // Will calculate after measuring children
        }
        else
        {
            scrollViewWidth = availableWidth;
        }

        // Height calculation - FIX for infinite height bug
        if (HasExplicitHeight)
        {
            scrollViewHeight = Height;
        }
        else if (availableHeight >= InfiniteThreshold && canScrollVertically)
        {
            // ✅ FIX: No explicit height + infinite space + vertical scroll
            // → Calculate based on content with reasonable limits
            scrollViewHeight = 0; // Will calculate after measuring children
        }
        else
        {
            scrollViewHeight = availableHeight;
        }

        // STEP 2: Calculate viewport for measuring children
        float viewportWidth = (scrollViewWidth > 0 ? scrollViewWidth : availableWidth) - Padding.Horizontal;
        float viewportHeight = (scrollViewHeight > 0 ? scrollViewHeight : availableHeight) - Padding.Vertical;

        // Account for scrollbars if always visible
        if (canScrollVertically && VerticalScrollBarVisibility == ScrollBarVisibility.Always && AreScrollbarsVisible())
            viewportWidth -= ScrollbarWidth;

        if (canScrollHorizontally && HorizontalScrollBarVisibility == ScrollBarVisibility.Always && AreScrollbarsVisible())
            viewportHeight -= ScrollbarWidth;

        // STEP 3: Measure children with infinite constraints in scroll direction
        float measureWidth = canScrollHorizontally ? InfiniteThreshold : viewportWidth;
        float measureHeight = canScrollVertically ? InfiniteThreshold : viewportHeight;

        _contentWidth = 0;
        _contentHeight = 0;

        foreach (var child in Children.ToArray())
        {
            child.Measure(measureWidth, measureHeight);

            float childWidth = child.DesiredWidth > 0 ? child.DesiredWidth : child.Width;
            float childHeight = child.DesiredHeight > 0 ? child.DesiredHeight : child.Height;

            _contentWidth = Math.Max(_contentWidth, childWidth + child.Margin.Horizontal);
            _contentHeight = Math.Max(_contentHeight, childHeight + child.Margin.Vertical);
        }

        // =========================================================================
        // STEP 4: Finalize ScrollView size based on content if needed
        // =========================================================================
        if (scrollViewWidth == 0)
        {
            // Was infinite width + horizontal scroll → use content with limits
            const float MinScrollViewWidth = 100f;
            const float MaxScrollViewWidth = 600f;
            scrollViewWidth = Math.Clamp(_contentWidth + Padding.Horizontal, MinScrollViewWidth, MaxScrollViewWidth);
        }

        if (scrollViewHeight == 0)
        {
            // ✅ FIX: Was infinite height + vertical scroll → use content with limits
            const float MinScrollViewHeight = 100f;
            const float MaxScrollViewHeight = 400f;
            scrollViewHeight = Math.Clamp(_contentHeight + Padding.Vertical, MinScrollViewHeight, MaxScrollViewHeight);
        }

        // =========================================================================
        // CRITICAL FIX: Do NOT set Width/Height here - only set DesiredWidth/DesiredHeight
        // Setting Width/Height marks HasExplicitWidth/HasExplicitHeight = true,
        // which prevents parent layouts (like VStack) from treating this as a Stretch child
        // =========================================================================
        DesiredWidth = scrollViewWidth;
        DesiredHeight = scrollViewHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        // ✅ FIX: Detect size change and force re-measure
        bool sizeChanged = ComputedWidth != width || ComputedHeight != height;

        base.Arrange(x, y, width, height);

        // Always check if offsets are valid for current content size
        // This handles cases where content size decreased even if viewport size didn't change
        if (_verticalScrollOffset > MaxVerticalScroll)
        {
            _verticalScrollOffset = Math.Max(0, MaxVerticalScroll);
        }

        if (_horizontalScrollOffset > MaxHorizontalScroll)
        {
            _horizontalScrollOffset = Math.Max(0, MaxHorizontalScroll);
        }

        // ✅ If the size changed, re-measure the content to recalculate scrollbars
        if (sizeChanged)
        {
            // Re-measure with the new size to recalculate _contentWidth and _contentHeight
            Measure(width, height);

            // ✅ Adjust offsets if they are now greater than the maximum allowed (again, after re-measure)
            if (_verticalScrollOffset > MaxVerticalScroll)
            {
                _verticalScrollOffset = Math.Max(0, MaxVerticalScroll);
            }

            if (_horizontalScrollOffset > MaxHorizontalScroll)
            {
                _horizontalScrollOffset = Math.Max(0, MaxHorizontalScroll);
            }
        }

        float contentAreaWidth = width - Padding.Horizontal;
        float contentAreaHeight = height - Padding.Vertical;

        // Subtract the space occupied by visible scrollbars
        if (ShowVerticalScrollbar && AreScrollbarsVisible() && _contentHeight > height)
        {
            contentAreaWidth -= ScrollbarWidth;
        }

        if (ShowHorizontalScrollbar && AreScrollbarsVisible() && _contentWidth > width)
        {
            contentAreaHeight -= ScrollbarWidth;
        }

        foreach (var child in Children.ToArray())
        {
            // ✅ Calculate base position with scroll offset
            float baseX = x + Padding.Left - _horizontalScrollOffset;
            float baseY = y + Padding.Top - _verticalScrollOffset;

            float childX = baseX + child.Margin.Left;
            float childY = baseY + child.Margin.Top;

            // ✅ Use DesiredWidth/Height if Width/Height are not explicitly set (0)
            float childWidth = child.Width > 0 ? child.Width : child.DesiredWidth;
            float childHeight = child.Height > 0 ? child.Height : child.DesiredHeight;

            // ✅ FIX: Apply child's HorizontalAlignment
            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    // childX is already correct
                    break;

                case HorizontalAlignment.Center:
                    childX += (contentAreaWidth - child.Width - child.Margin.Horizontal) / 2;
                    break;

                case HorizontalAlignment.Right:
                    childX += contentAreaWidth - child.Width - child.Margin.Horizontal;
                    break;

                case HorizontalAlignment.Stretch:
                    childWidth = contentAreaWidth - child.Margin.Horizontal;
                    break;
            }

            // ✅ FIX: Apply child's VerticalAlignment
            switch (child.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    // childY is already correct
                    break;

                case VerticalAlignment.Center:
                    childY += (contentAreaHeight - child.Height - child.Margin.Vertical) / 2;
                    break;

                case VerticalAlignment.Bottom:
                    childY += contentAreaHeight - child.Height - child.Margin.Vertical;
                    break;

                case VerticalAlignment.Stretch:
                    // ✅ FIX: Allow stretching to content size if larger than viewport
                    // Otherwise content is clamped to viewport height, defeating scrolling purpose
                    childHeight = Math.Max(contentAreaHeight - child.Margin.Vertical, child.DesiredHeight);
                    break;
            }

            child.Arrange(childX, childY, childWidth, childHeight);
        }
    }

    #region Orientation
    [LayoutProperty]
    public ScrollOrientation Orientation
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            // Auto-enable scrollbars based on new orientation if they were disabled.
            // [LayoutProperty] triggers MarkNeedsLayout() automatically after this callback.
            if ((value == ScrollOrientation.Horizontal || value == ScrollOrientation.Both) &&
                HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }

            if ((value == ScrollOrientation.Vertical || value == ScrollOrientation.Both) &&
                VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        });
    } = ScrollOrientation.Vertical;
    #endregion

    // Helper to calculate scrollbar visibility with cyclic dependency resolution
    private void GetScrollbarVisibility(float viewportWidth, float viewportHeight, 
                                      out bool showVertical, out bool showHorizontal, 
                                      out float effectiveViewportWidth, out float effectiveViewportHeight)
    {
        bool canScrollVertically = Orientation == ScrollOrientation.Vertical || Orientation == ScrollOrientation.Both;
        bool canScrollHorizontally = Orientation == ScrollOrientation.Horizontal || Orientation == ScrollOrientation.Both;

        if (!AreScrollbarsVisible())
        {
            showVertical = false;
            showHorizontal = false;
            effectiveViewportWidth = viewportWidth;
            effectiveViewportHeight = viewportHeight;
            return;
        }

        // 1. Check Vertical assuming full width
        showVertical = canScrollVertically && (
            VerticalScrollBarVisibility == ScrollBarVisibility.Always ||
            (VerticalScrollBarVisibility == ScrollBarVisibility.Auto && _contentHeight > viewportHeight));

        // 2. Check Horizontal assuming full height (initial check)
        // OR if vertical is already known, subtract width
        float w = showVertical ? viewportWidth - ScrollbarWidth : viewportWidth;
        showHorizontal = canScrollHorizontally && (
            HorizontalScrollBarVisibility == ScrollBarVisibility.Always ||
            (HorizontalScrollBarVisibility == ScrollBarVisibility.Auto && _contentWidth > w));

        // 3. Re-Check Vertical if Horizontal triggered (it takes space now)
        if (showHorizontal && !showVertical && canScrollVertically)
        {
            float h = viewportHeight - ScrollbarWidth;
            if (VerticalScrollBarVisibility == ScrollBarVisibility.Always ||
               (VerticalScrollBarVisibility == ScrollBarVisibility.Auto && _contentHeight > h))
            {
                showVertical = true;
            }
        }

        effectiveViewportWidth = showVertical ? viewportWidth - ScrollbarWidth : viewportWidth;
        effectiveViewportHeight = showHorizontal ? viewportHeight - ScrollbarWidth : viewportHeight;
    }

    // The maximum scroll position should be where the last item is aligned with the edge of the viewport
    private float MaxVerticalScroll
    {
        get
        {
            if (Orientation == ScrollOrientation.Horizontal || Orientation == ScrollOrientation.Neither) return 0;

            float viewportHeight = ComputedHeight - Padding.Vertical;
            float viewportWidth = ComputedWidth - Padding.Horizontal;

            GetScrollbarVisibility(viewportWidth, viewportHeight, out _, out _, out _, out float effectiveHeight);
            
            return Math.Max(0, _contentHeight - effectiveHeight);
        }
    }

    private float MaxHorizontalScroll
    {
        get
        {
            if (Orientation == ScrollOrientation.Vertical || Orientation == ScrollOrientation.Neither) return 0;

            float viewportHeight = ComputedHeight - Padding.Vertical;
            float viewportWidth = ComputedWidth - Padding.Horizontal;

            GetScrollbarVisibility(viewportWidth, viewportHeight, out _, out _, out float effectiveWidth, out _);

            return Math.Max(0, _contentWidth - effectiveWidth);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Inertia is now processed before layout in the frame loop (not here)

        if (Background != null && Background.Opacity > 0 && Background.PrimaryColor.A > 0)
        {
            renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, Background);
        }

        float viewportWidth = ComputedWidth - Padding.Horizontal;
        float viewportHeight = ComputedHeight - Padding.Vertical;

        // ✅ Use centralized logic for visibility
        GetScrollbarVisibility(viewportWidth, viewportHeight, 
            out bool needsVerticalScrollbar, out bool needsHorizontalScrollbar, 
            out float availableWidth, out float availableHeight);

        // Round to integer coordinates
        float clipX = ComputedX + Padding.Left;
        float clipY = ComputedY + Padding.Top;
        float clipWidth = Math.Max(0, availableWidth);
        float clipHeight = Math.Max(0, availableHeight);

        renderer.PushScissor(clipX, clipY, clipWidth, clipHeight);

        // Render children
        foreach (var child in Children.ToArray())
        {
            if (child.IsVisible)
            {
                RenderChild(child, renderer, clipX, clipY, clipWidth, clipHeight);
            }
        }

        renderer.PopScissor();

        // ✅ FIX CORNER OVERLAP: Vertical scrollbar should stop above horizontal scrollbar
        // Vertical scrollbar
        if (needsVerticalScrollbar)
        {
            float scrollbarX = ComputedX + ComputedWidth - ScrollbarWidth - Padding.Right;
            float scrollbarY = ComputedY + Padding.Top;
            // Subtract scrollbar width from height if horizontal bar is present vs just padding
            float scrollbarHeight = availableHeight; 

            renderer.DrawRoundedRect(scrollbarX, scrollbarY, ScrollbarWidth, scrollbarHeight,
                ScrollbarWidth / 2, ScrollbarBackground);

            float contentH = Math.Max(_contentHeight, 1);
            float viewportRatio = Math.Min(1.0f, availableHeight / contentH);
            float thumbHeight = Math.Max(20, scrollbarHeight * viewportRatio);
            
            float maxScroll = MaxVerticalScroll;
            float scrollRatio = maxScroll > 0 ? _verticalScrollOffset / maxScroll : 0;
            scrollRatio = Math.Max(0, Math.Min(1, scrollRatio));
            
            float thumbY = scrollbarY + (scrollbarHeight - thumbHeight) * scrollRatio;

            renderer.DrawRoundedRect(scrollbarX, thumbY, ScrollbarWidth, thumbHeight,
                ScrollbarWidth / 2, ScrollbarThumb);
        }

        // Horizontal scrollbar
        if (needsHorizontalScrollbar)
        {
            float scrollbarX = ComputedX + Padding.Left;
            float scrollbarY = ComputedY + ComputedHeight - ScrollbarWidth - Padding.Bottom;
            // Subtract scrollbar width (vertical) from width if vertical bar is present
            float scrollbarWidth = availableWidth;

            renderer.DrawRoundedRect(scrollbarX, scrollbarY, scrollbarWidth, ScrollbarWidth,
                ScrollbarWidth / 2, ScrollbarBackground);

            float contentW = Math.Max(_contentWidth, 1);
            float viewportRatio = Math.Min(1.0f, availableWidth / contentW);
            float thumbWidth = Math.Max(20, scrollbarWidth * viewportRatio);
            
            float maxScroll = MaxHorizontalScroll;
            float scrollRatio = maxScroll > 0 ? _horizontalScrollOffset / maxScroll : 0;
            scrollRatio = Math.Max(0, Math.Min(1, scrollRatio));
            
            float thumbX = scrollbarX + (scrollbarWidth - thumbWidth) * scrollRatio;

            renderer.DrawRoundedRect(thumbX, scrollbarY, thumbWidth, ScrollbarWidth,
                ScrollbarWidth / 2, ScrollbarThumb);
        }
        
        // Optional: Draw corner square if both are visible
        if (needsVerticalScrollbar && needsHorizontalScrollbar)
        {
             // Draw corner background to avoid holes
             float cornerX = ComputedX + ComputedWidth - ScrollbarWidth - Padding.Right;
             float cornerY = ComputedY + ComputedHeight - ScrollbarWidth - Padding.Bottom;
             renderer.DrawRect(cornerX, cornerY, ScrollbarWidth, ScrollbarWidth, ScrollbarBackground);
        }
    }

    /// <summary>
    /// Renders a child and all its descendants recursively.
    /// </summary>
    private void RenderChild(VisualElement element, IRenderer renderer, float clipX, float clipY, float clipWidth, float clipHeight)
    {
        if (!OverlapsClip(element, clipX, clipY, clipWidth, clipHeight))
        {
            return;
        }

        float childClipX = Math.Max(clipX, element.ComputedX);
        float childClipY = Math.Max(clipY, element.ComputedY);
        float childClipRight = Math.Min(clipX + clipWidth, element.ComputedX + element.ComputedWidth);
        float childClipBottom = Math.Min(clipY + clipHeight, element.ComputedY + element.ComputedHeight);
        float childClipWidth = Math.Max(0, childClipRight - childClipX);
        float childClipHeight = Math.Max(0, childClipBottom - childClipY);

        if (childClipWidth <= 0 || childClipHeight <= 0)
        {
            return;
        }

        bool needsChildScissor = childClipX > clipX || childClipY > clipY ||
                                 childClipWidth < element.ComputedWidth || childClipHeight < element.ComputedHeight;

        if (needsChildScissor)
        {
            renderer.PushScissor(childClipX, childClipY, childClipWidth, childClipHeight);
        }

        element.Render(renderer);

        foreach (var child in element.GetChildren().ToArray())
        {
            if (child.IsVisible)
            {
                RenderChild(child, renderer, childClipX, childClipY, childClipWidth, childClipHeight);
            }
        }

        if (needsChildScissor)
        {
            renderer.PopScissor();
        }
    }

    private static bool OverlapsClip(VisualElement element, float clipX, float clipY, float clipWidth, float clipHeight)
    {
        float clipRight = clipX + clipWidth;
        float clipBottom = clipY + clipHeight;

        float elemLeft = element.ComputedX;
        float elemTop = element.ComputedY;
        float elemRight = elemLeft + element.ComputedWidth;
        float elemBottom = elemTop + element.ComputedHeight;

        const float tolerance = 1.0f;

        return elemLeft < clipRight + tolerance &&
               elemRight > clipX - tolerance &&
               elemTop < clipBottom + tolerance &&
               elemBottom > clipY - tolerance;
    }

    /// <summary>
    /// IInputHandler implementation for drag scrolling.
    /// </summary>
    public bool HandleInput(InputEventArgs args)
    {
        float viewportWidth = ComputedWidth - Padding.Horizontal;
        float viewportHeight = ComputedHeight - Padding.Vertical;
        switch (args.EventType)
        {
            case InputEventType.MouseDown:
                // ✅ Check if the click is on the scrollbar thumb
                if (IsPointOnVerticalThumb(args.Position))
                {
                    _isDraggingVerticalThumb = true;
                    _thumbDragStartOffset = _verticalScrollOffset;
                    _thumbDragStartMouseX = args.Position.X;
                    _thumbDragStartMouseY = args.Position.Y;
                    return true;
                }

                if (IsPointOnHorizontalThumb(args.Position))
                {
                    _isDraggingHorizontalThumb = true;
                    _thumbDragStartOffset = _horizontalScrollOffset;
                    _thumbDragStartMouseX = args.Position.X;
                    _thumbDragStartMouseY = args.Position.Y;
                    return true;
                }

                // Prepare for possible content drag only if there is content to scroll
                if ((_contentHeight > viewportHeight || _contentWidth > viewportWidth))
                {
                    _dragPending = true;
                    _dragStartPosition = args.Position;
                    _dragStartVerticalOffset = _verticalScrollOffset;
                    _dragStartHorizontalOffset = _horizontalScrollOffset;
                    // Do not capture the event yet - let the buttons process it
                    return false;
                }

                return false;

            case InputEventType.MouseDrag:
                // ✅ If we are dragging the thumb
                if (_isDraggingVerticalThumb)
                {
                    float deltaY = args.Position.Y - _thumbDragStartMouseY;
                    if (ShowVerticalScrollbar && AreScrollbarsVisible() && _contentHeight > viewportHeight)
                    {
                        float scrollbarHeight = viewportHeight;
                        float viewportRatio = viewportHeight / _contentHeight;
                        float thumbHeight = Math.Max(20, scrollbarHeight * viewportRatio);
                        float availableTravel = scrollbarHeight - thumbHeight;

                        if (availableTravel > 0)
                        {
                            float scrollDelta = (deltaY / availableTravel) * MaxVerticalScroll;
                            VerticalScrollOffset = _thumbDragStartOffset + scrollDelta;
                        }
                    }
                    return true;
                }

                if (_isDraggingHorizontalThumb)
                {
                    float deltaX = args.Position.X - _thumbDragStartMouseX;
                    if (ShowHorizontalScrollbar && AreScrollbarsVisible() && _contentWidth > viewportWidth)
                    {
                        float scrollbarWidth = viewportWidth;
                        float viewportRatio = viewportWidth / _contentWidth;
                        float thumbWidth = Math.Max(20, scrollbarWidth * viewportRatio);
                        float availableTravel = scrollbarWidth - thumbWidth;

                        if (availableTravel > 0)
                        {
                            float scrollDelta = (deltaX / availableTravel) * MaxHorizontalScroll;
                            HorizontalScrollOffset = _thumbDragStartOffset + scrollDelta;
                        }
                    }
                    return true;
                }

                // ✅ Touch dragging with velocity tracking for inertia
                if (_isTouchDragging)
                {
                    var now = DateTime.UtcNow;
                    var timeDelta = (float)(now - _lastTouchTime).TotalSeconds;
                    var posDelta = args.Position - _lastTouchPosition;

                    // Calculate velocity (for inertia when released)
                    if (timeDelta > 0.001f)
                    {
                        // Smooth velocity with some averaging
                        _velocityY = _velocityY * 0.3f + (posDelta.Y / timeDelta) * 0.7f;
                        _velocityX = _velocityX * 0.3f + (posDelta.X / timeDelta) * 0.7f;
                    }

                    // Touch scroll: content follows finger (natural mobile behavior)
                    // Positive delta.Y (finger moves down) = scroll offset decreases (content moves down)
                    if (ShowVerticalScrollbar && _contentHeight > viewportHeight)
                    {
                        VerticalScrollOffset -= posDelta.Y;
                    }

                    if (ShowHorizontalScrollbar && _contentWidth > viewportWidth)
                    {
                        HorizontalScrollOffset -= posDelta.X;
                    }

                    _lastTouchPosition = args.Position;
                    _lastTouchTime = now;

                    return true;
                }

                // If we are already dragging the content (mouse), continue
                if (_isDragging)
                {
                    var delta = args.Position - _dragStartPosition;

                    // Invert delta for natural scrolling (desktop mouse drag)
                    if (ShowVerticalScrollbar && _contentHeight > viewportHeight)
                    {
                        VerticalScrollOffset = _dragStartVerticalOffset - delta.Y;
                    }

                    if (ShowHorizontalScrollbar && _contentWidth > viewportWidth)
                    {
                        HorizontalScrollOffset = _dragStartHorizontalOffset - delta.X;
                    }

                    return true;
                }

                // If we have a pending drag, check if we exceeded the threshold
                if (_dragPending)
                {
                    var delta = args.Position - _dragStartPosition;
                    float distance = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                    float activationThreshold = GetDragThreshold();

                    if (distance >= activationThreshold)
                    {
                        // Exceeded the threshold - activate touch drag mode
                        _isTouchDragging = true;
                        _dragPending = false;
                        _lastTouchPosition = args.Position;
                        _lastTouchTime = DateTime.UtcNow;
                        _velocityY = 0;
                        _velocityX = 0;
                        _hasInertia = false;
                        _lastInertiaUpdate = DateTime.UtcNow;

                        // Apply the initial scroll
                        if (ShowVerticalScrollbar && _contentHeight > viewportHeight)
                        {
                            VerticalScrollOffset -= delta.Y;
                        }

                        if (ShowHorizontalScrollbar && _contentWidth > viewportWidth)
                        {
                            HorizontalScrollOffset -= delta.X;
                        }

                        return true; // Capture the drag
                    }
                }
                return false;

            case InputEventType.MouseUp:
                bool wasDragging = _isDragging || _isDraggingVerticalThumb || _isDraggingHorizontalThumb || _isTouchDragging;

                // Start inertia if we were touch dragging with significant velocity
                if (_isTouchDragging)
                {
                    float velocityMagnitude = MathF.Sqrt(_velocityY * _velocityY + _velocityX * _velocityX);
                    float inertiaThreshold = MinInertiaVelocity * (Rayo.Core.Platform.PlatformDetector.IsMobile ? 2f : 5f);
                    if (velocityMagnitude > inertiaThreshold)
                    {
                        _hasInertia = true;
                        _lastInertiaUpdate = DateTime.UtcNow;
                        // Schedule inertia updates
                        MarkNeedsPaint();
                    }
                }

                _isDragging = false;
                _dragPending = false;
                _isDraggingVerticalThumb = false;
                _isDraggingHorizontalThumb = false;
                _isTouchDragging = false;
                return wasDragging;

            default:
                return false;
        }
    }

    /// <summary>
    /// Required implementation for IInputHandler.
    /// </summary>
    public void OnFocusGained()
    {
        // ScrollView does not need to handle focus.
    }

    /// <summary>
    /// Required implementation for IInputHandler.
    /// </summary>
    public void OnFocusLost()
    {
        // ScrollView does not need to handle focus.
    }

    /// <summary>
    /// Cancels any pending or active drag.
    /// Called by EventManager to defensively clear state.
    /// </summary>
    public void CancelDragPending()
    {
        _dragPending = false;
        _isDragging = false;
        _isDraggingVerticalThumb = false;
        _isDraggingHorizontalThumb = false;
        _isTouchDragging = false;
        _hasInertia = false;
    }

    /// <summary>
    /// Processes inertia scrolling after touch release.
    /// Applies momentum-based scrolling for natural mobile feel.
    /// Called before layout phase to ensure scroll offsets are updated before Measure/Arrange.
    /// </summary>
    public void ProcessInertia()
    {
        if (!_hasInertia) return;

        float viewportHeight = ComputedHeight - Padding.Vertical;
        float viewportWidth = ComputedWidth - Padding.Horizontal;

        var now = DateTime.UtcNow;
        float deltaSeconds = (float)(now - _lastInertiaUpdate).TotalSeconds;
        if (deltaSeconds <= 0f)
        {
            deltaSeconds = InertiaBaseFrameTime;
        }
        else
        {
            deltaSeconds = MathF.Min(MathF.Max(deltaSeconds, MinInertiaDelta), MaxInertiaDelta);
        }
        _lastInertiaUpdate = now;

        // Apply velocity to scroll offset (inverted because velocity is in screen coords)
        bool hasVerticalInertia = ShowVerticalScrollbar && _contentHeight > viewportHeight && MathF.Abs(_velocityY) > MinInertiaVelocity;
        bool hasHorizontalInertia = ShowHorizontalScrollbar && _contentWidth > viewportWidth && MathF.Abs(_velocityX) > MinInertiaVelocity;

        if (hasVerticalInertia)
        {
            VerticalScrollOffset -= _velocityY * deltaSeconds;
            float decayMultiplier = MathF.Pow(InertiaDecelerationPerFrame, deltaSeconds / InertiaBaseFrameTime);
            _velocityY *= decayMultiplier;
        }

        if (hasHorizontalInertia)
        {
            HorizontalScrollOffset -= _velocityX * deltaSeconds;
            float decayMultiplier = MathF.Pow(InertiaDecelerationPerFrame, deltaSeconds / InertiaBaseFrameTime);
            _velocityX *= decayMultiplier;
        }

        // Check if we should stop inertia
        if (!hasVerticalInertia && !hasHorizontalInertia)
        {
            _hasInertia = false;
            _velocityY = 0;
            _velocityX = 0;
        }
        else
        {
            // Continue animation
            MarkNeedsPaint();
        }
    }

    /// <summary>
    /// Checks if the mouse is over the vertical scrollbar thumb.
    /// </summary>
    private bool IsPointOnVerticalThumb(System.Numerics.Vector2 position)
    {
        float viewportHeight = ComputedHeight - Padding.Vertical;

        if (!AreScrollbarsVisible() || !ShowVerticalScrollbar || _contentHeight <= viewportHeight)
            return false;

        float scrollbarX = ComputedX + ComputedWidth - ScrollbarWidth - Padding.Right;
        float scrollbarY = ComputedY + Padding.Top;
        float scrollbarHeight = viewportHeight;

        // Calculate thumb position
        float viewportRatio = viewportHeight / _contentHeight;
        float thumbHeight = Math.Max(20, scrollbarHeight * viewportRatio);
        float scrollRatio = MaxVerticalScroll > 0 ? _verticalScrollOffset / MaxVerticalScroll : 0;
        float thumbY = scrollbarY + (scrollbarHeight - thumbHeight) * scrollRatio;

        bool isOnThumb = position.X >= scrollbarX && position.X <= scrollbarX + ScrollbarWidth &&
                         position.Y >= thumbY && position.Y <= thumbY + thumbHeight;

        return isOnThumb;
    }

    /// <summary>
    /// Checks if the mouse is over the horizontal scrollbar thumb.
    /// </summary>
    private bool IsPointOnHorizontalThumb(System.Numerics.Vector2 position)
    {
        float viewportWidth = ComputedWidth - Padding.Horizontal;

        if (!AreScrollbarsVisible() || !ShowHorizontalScrollbar || _contentWidth <= viewportWidth)
            return false;

        float scrollbarX = ComputedX + Padding.Left;
        float scrollbarY = ComputedY + ComputedHeight - ScrollbarWidth - Padding.Bottom;
        float scrollbarWidth = viewportWidth;

        // Calculate thumb position
        float viewportRatio = viewportWidth / _contentWidth;
        float thumbWidth = Math.Max(20, scrollbarWidth * viewportRatio);
        float scrollRatio = MaxHorizontalScroll > 0 ? _horizontalScrollOffset / MaxHorizontalScroll : 0;
        float thumbX = scrollbarX + (scrollbarWidth - thumbWidth) * scrollRatio;

        // Check if the point is inside the thumb
        return position.X >= thumbX && position.X <= thumbX + thumbWidth &&
               position.Y >= scrollbarY && position.Y <= scrollbarY + ScrollbarWidth;
    }

    // IClippable implementation
    public bool IsPointInClipRegion(System.Numerics.Vector2 position)
    {
        float clipX = ComputedX + Padding.Left;
        float clipY = ComputedY + Padding.Top;
        float clipWidth = ComputedWidth - Padding.Horizontal;
        float clipHeight = ComputedHeight - Padding.Vertical;

        if (ShowVerticalScrollbar && AreScrollbarsVisible() && ContentHeight > ComputedHeight)
        {
            clipWidth -= ScrollbarWidth;
        }

        if (ShowHorizontalScrollbar && AreScrollbarsVisible() && ContentWidth > ComputedWidth)
        {
            clipHeight -= ScrollbarWidth;
        }

        // Check if the point is within the clip rect
        return position.X >= clipX && position.X <= clipX + clipWidth &&
               position.Y >= clipY && position.Y <= clipY + clipHeight;
    }

    public (float x, float y, float width, float height) GetClipRect()
    {
        float clipX = ComputedX + Padding.Left;
        float clipY = ComputedY + Padding.Top;
        float clipWidth = ComputedWidth - Padding.Horizontal;
        float clipHeight = ComputedHeight - Padding.Vertical;

        if (ShowVerticalScrollbar && AreScrollbarsVisible() && ContentHeight > ComputedHeight)
        {
            clipWidth -= ScrollbarWidth;
        }

        if (ShowHorizontalScrollbar && AreScrollbarsVisible() && ContentWidth > ComputedWidth)
        {
            clipHeight -= ScrollbarWidth;
        }

        return (clipX, clipY, clipWidth, clipHeight);
    }
}
