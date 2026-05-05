namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// Defines the orientation of the Splitter.
/// </summary>
public enum SplitterOrientation
{
    /// <summary>
    /// Children are arranged horizontally (side by side). Splitters are vertical bars.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Children are arranged vertically (stacked). Splitters are horizontal bars.
    /// </summary>
    Vertical
}

/// <summary>
/// A container that lays out its children linearly with resizable splitters between them.
/// Migrated to new MAUI-like architecture: inherits from Rayo.Core.Layout<T>
/// </summary>
public class Splitter : Rayo.Core.Layout<Splitter>, IPointerHandler, IInputHandler
{
    // Properties from old Layout base class
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set
        {
            if (field.PrimaryColor.R != value.PrimaryColor.R || field.PrimaryColor.G != value.PrimaryColor.G || field.PrimaryColor.B != value.PrimaryColor.B || field.PrimaryColor.A != value.PrimaryColor.A)
            {
                this.SetProperty(ref field, value);
            }
        }
    } = Color.Transparent;

    public new bool ShouldExpand { get; set; } = false;
    protected internal new bool HasExplicitWidth { get; private set; } = false;
    protected internal new bool HasExplicitHeight { get; private set; } = false;

    private SplitterOrientation _orientation = SplitterOrientation.Horizontal;
    private float _splitterSize = 6f;
    private Brush _splitterColor = new Color(40, 44, 52);
    private Brush _hoverColor = new Color(60, 64, 72);
    private Brush _dragColor = new Color(59, 130, 246); // Blue-ish

    // Internal state
    private int _draggedSplitterIndex = -1;
    private int _hoveredSplitterIndex = -1;
    private float _dragStartSplitterPos;
    private float _dragStartMouseOffset;
    // Snapshots of child dimensions at start of drag
    private List<float> _dragStartSizes = new();
    // Store the actual arranged sizes from last Arrange call
    private List<float> _arrangedSizes = new();
    
    private readonly List<(float X, float Y, float W, float H)> _splitters = new();

    #region Properties

    public SplitterOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                MarkNeedsLayout();
            }
        }
    }

    public float SplitterSize
    {
        get => _splitterSize;
        set
        {
            if (_splitterSize != value)
            {
                _splitterSize = value;
                MarkNeedsLayout();
            }
        }
    }

    public Brush SplitterColor
    {
        get => _splitterColor;
        set
        {
            _splitterColor = value;
            MarkNeedsPaint();
        }
    }

    public Brush HoverColor
    {
        get => _hoverColor;
        set => _hoverColor = value;
    }

    public Brush DragColor
    {
        get => _dragColor;
        set => _dragColor = value;
    }

    #endregion

    public Splitter()
    {
        // Default behavior
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    #region IInputHandler

    public bool CanHandleInput => true;

    public bool HandleInput(InputEventArgs e)
    {
        switch (e.EventType)
        {
            case InputEventType.MouseDown:
                UpdateHoverState(e.Position);
                if (_hoveredSplitterIndex != -1)
                {
                    _draggedSplitterIndex = _hoveredSplitterIndex;
                    var splitterRect = _splitters[_draggedSplitterIndex];
                    var localPos = GetLocalPosition(e.Position);
                    _dragStartSplitterPos = (Orientation == SplitterOrientation.Horizontal)
                        ? splitterRect.X
                        : splitterRect.Y;
                    _dragStartMouseOffset = (Orientation == SplitterOrientation.Horizontal)
                        ? localPos.X - _dragStartSplitterPos
                        : localPos.Y - _dragStartSplitterPos;

                    // Snapshot the actual arranged sizes (most reliable source)
                    _dragStartSizes.Clear();
                    var visibleChildren = Children.Where(c => c.IsVisible).ToList();
                    for (int i = 0; i < visibleChildren.Count; i++)
                    {
                        var child = visibleChildren[i];
                        _dragStartSizes.Add(Orientation == SplitterOrientation.Horizontal
                            ? child.ComputedWidth
                            : child.ComputedHeight);
                    }

                    MarkNeedsPaint();
                    return true;
                }
                break;

            case InputEventType.MouseDrag:
                if (_draggedSplitterIndex != -1)
                {
                    // Calculate delta from drag start position
                    var localPos = GetLocalPosition(e.Position);
                    float mousePos = (Orientation == SplitterOrientation.Horizontal)
                        ? localPos.X
                        : localPos.Y;
                    float targetSplitterPos = mousePos - _dragStartMouseOffset;
                    float delta = targetSplitterPos - _dragStartSplitterPos;

                    ApplyResize(_draggedSplitterIndex, delta);
                    return true;
                }
                break;

            case InputEventType.MouseUp:
                if (_draggedSplitterIndex != -1)
                {
                    _draggedSplitterIndex = -1;
                    MarkNeedsPaint();
                    return true;
                }
                break;

            case InputEventType.MouseMove:
                UpdateHoverState(e.Position);
                break;
        }

        return false;
    }

    public void OnFocusGained() { }
    public void OnFocusLost() { }

    #endregion

    private void UpdateHoverState(Vector2 windowPosition)
    {
        // Convert window position to element-local coordinates
        var localPos = GetLocalPosition(windowPosition);

        int found = -1;
        for (int i = 0; i < _splitters.Count; i++)
        {
            var r = _splitters[i];
            // Increase hit area slightly for better UX (3px padding)
            float padding = 3f;
            if (localPos.X >= r.X - padding && localPos.X <= r.X + r.W + padding &&
                localPos.Y >= r.Y - padding && localPos.Y <= r.Y + r.H + padding)
            {
                found = i;
                break;
            }
        }

        if (_hoveredSplitterIndex != found)
        {
            _hoveredSplitterIndex = found;
            MarkNeedsPaint();
        }
    }

    #region Layout Overrides

    public override void Measure(float availableWidth, float availableHeight)
    {
        var visibleChildren = Children.Where(c => c.IsVisible).ToList();
        if (visibleChildren.Count == 0)
        {
            DesiredWidth = Padding.Horizontal;
            DesiredHeight = Padding.Vertical;
            return;
        }

        float totalSplitterSpace = Math.Max(0, visibleChildren.Count - 1) * SplitterSize;
        
        float availableForChildrenWidth = availableWidth - Padding.Horizontal;
        float availableForChildrenHeight = availableHeight - Padding.Vertical;

        // Reduce available space by splitters
        if (Orientation == SplitterOrientation.Horizontal)
        {
            availableForChildrenWidth = Math.Max(0, availableForChildrenWidth - totalSplitterSpace);
        }
        else
        {
            availableForChildrenHeight = Math.Max(0, availableForChildrenHeight - totalSplitterSpace);
        }

        // Measure children
        float measuredWidth = 0;
        float measuredHeight = 0;

        foreach (var child in visibleChildren)
        {
            if (Orientation == SplitterOrientation.Horizontal)
            {
                // Horizontal: Height is restricted, Width is flexible (unless fixed)
                child.Measure(float.PositiveInfinity, availableForChildrenHeight);
                measuredWidth += child.DesiredWidth;
                measuredHeight = Math.Max(measuredHeight, child.DesiredHeight);
            }
            else
            {
                // Vertical: Width is restricted, Height is flexible
                child.Measure(availableForChildrenWidth, float.PositiveInfinity);
                measuredWidth = Math.Max(measuredWidth, child.DesiredWidth);
                measuredHeight += child.DesiredHeight;
            }
        }

        // Add splitters back to desired size
        if (Orientation == SplitterOrientation.Horizontal)
        {
            measuredWidth += totalSplitterSpace;
        }
        else
        {
            measuredHeight += totalSplitterSpace;
        }

        // Padding
        measuredWidth += Padding.Horizontal;
        measuredHeight += Padding.Vertical;

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
        
        if (HorizontalAlignment == HorizontalAlignment.Stretch && !float.IsPositiveInfinity(availableWidth))
        {
            DesiredWidth = availableWidth;
        }
        if (VerticalAlignment == VerticalAlignment.Stretch && !float.IsPositiveInfinity(availableHeight))
        {
            DesiredHeight = availableHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _splitters.Clear();

        var visibleChildren = Children.Where(c => c.IsVisible).ToList();
        if (visibleChildren.Count == 0) return;

        // Use local coordinates (relative to element position)
        float contentX = Padding.Left;
        float contentY = Padding.Top;
        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        float totalSplitterSpace = Math.Max(0, visibleChildren.Count - 1) * SplitterSize;
        float availableSpace = (Orientation == SplitterOrientation.Horizontal) 
            ? contentWidth - totalSplitterSpace 
            : contentHeight - totalSplitterSpace;

        // Store the arranged sizes for drag operations
        _arrangedSizes.Clear();
        
        // Let's gather current requested sizes
        var childSizes = new float[visibleChildren.Count];
        float totalRequested = 0;
        int stretchCount = 0;

        for (int i = 0; i < visibleChildren.Count; i++)
        {
            var child = visibleChildren[i];
            float size = (Orientation == SplitterOrientation.Horizontal) 
                ? (child.Width > 0 ? child.Width : child.DesiredWidth) 
                : (child.Height > 0 ? child.Height : child.DesiredHeight);
            
            childSizes[i] = size;
            
            // Check for stretch
            bool isStretch = (Orientation == SplitterOrientation.Horizontal)
                ? child.HorizontalAlignment == HorizontalAlignment.Stretch && child.Width <= 0
                : child.VerticalAlignment == VerticalAlignment.Stretch && child.Height <= 0;

            if (isStretch)
            {
                stretchCount++;
            }
            else
            {
                totalRequested += size;
            }
        }

        // If we have extra space and stretch children
        float remaining = Math.Max(0, availableSpace - totalRequested);
        if (stretchCount > 0)
        {
            float perStretch = remaining / stretchCount;
            for (int i = 0; i < visibleChildren.Count; i++)
            {
                var child = visibleChildren[i];
                bool isStretch = (Orientation == SplitterOrientation.Horizontal)
                    ? child.HorizontalAlignment == HorizontalAlignment.Stretch && child.Width <= 0
                    : child.VerticalAlignment == VerticalAlignment.Stretch && child.Height <= 0;

                if (isStretch)
                {
                    childSizes[i] = perStretch;
                }
            }
        }
        else if (stretchCount == 0 && availableSpace > totalRequested)
        {
            if (totalRequested > 0)
            {
                float factor = availableSpace / totalRequested;
                for (int i = 0; i < visibleChildren.Count; i++)
                {
                    childSizes[i] *= factor;
                }
            }
        }

        // Now Layout
        float currentPos = (Orientation == SplitterOrientation.Horizontal) ? contentX : contentY;

        for (int i = 0; i < visibleChildren.Count; i++)
        {
            var child = visibleChildren[i];
            float size = childSizes[i];
            
            // Store the actual arranged size for drag operations
            _arrangedSizes.Add(size);

            if (Orientation == SplitterOrientation.Horizontal)
            {
                // Arrange child at absolute position
                child.Arrange(x + currentPos, y + contentY, size, contentHeight);
                currentPos += size;

                // Add Splitter in local coordinates if not last
                if (i < visibleChildren.Count - 1)
                {
                    _splitters.Add((currentPos, contentY, SplitterSize, contentHeight));
                    currentPos += SplitterSize;
                }
            }
            else
            {
                // Arrange child at absolute position
                child.Arrange(x + contentX, y + currentPos, contentWidth, size);
                currentPos += size;

                // Add Splitter in local coordinates
                if (i < visibleChildren.Count - 1)
                {
                    _splitters.Add((contentX, currentPos, contentWidth, SplitterSize));
                    currentPos += SplitterSize;
                }
            }
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Render background if it has color
        if (Background.PrimaryColor.A > 0)
        {
            float renderHeight = Math.Max(ComputedHeight, DesiredHeight);
            float renderWidth = Math.Max(ComputedWidth, DesiredWidth);

            renderer.DrawRect(ComputedX, ComputedY, renderWidth, renderHeight, Background);
        }

        // Draw Splitters (convert from local to absolute coordinates)
        for (int i = 0; i < _splitters.Count; i++)
        {
            var r = _splitters[i];
            Brush c = _splitterColor;

            if (i == _draggedSplitterIndex) c = _dragColor;
            else if (i == _hoveredSplitterIndex) c = _hoverColor;

            renderer.DrawRect(ComputedX + r.X, ComputedY + r.Y, r.W, r.H, c);
        }
    }

    #endregion

    #region IPointerHandler (Unified pointer events — covers both mouse hover and Android touch drag)

    public void OnPointerEntered(PointerEventArgs e) { }

    public void OnPointerExited(PointerEventArgs e)
    {
        if (_hoveredSplitterIndex != -1 && _draggedSplitterIndex == -1)
        {
            _hoveredSplitterIndex = -1;
            MarkNeedsPaint();
        }
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        if (_draggedSplitterIndex != -1)
        {
            // Handle active drag (important for Android touch where IInputHandler isn't used)
            var localPos = GetLocalPosition(e.Position);
            float mousePos = (Orientation == SplitterOrientation.Horizontal)
                ? localPos.X
                : localPos.Y;
            float targetSplitterPos = mousePos - _dragStartMouseOffset;
            float delta = targetSplitterPos - _dragStartSplitterPos;
            ApplyResize(_draggedSplitterIndex, delta);
        }
        else
        {
            UpdateHoverState(e.Position);
        }
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        // Start drag — needed for Android touch (IInputHandler not called on Android)
        UpdateHoverState(e.Position);
        if (_hoveredSplitterIndex != -1)
        {
            _draggedSplitterIndex = _hoveredSplitterIndex;
            var splitterRect = _splitters[_draggedSplitterIndex];
            var localPos = GetLocalPosition(e.Position);
            _dragStartSplitterPos = (Orientation == SplitterOrientation.Horizontal)
                ? splitterRect.X
                : splitterRect.Y;
            _dragStartMouseOffset = (Orientation == SplitterOrientation.Horizontal)
                ? localPos.X - _dragStartSplitterPos
                : localPos.Y - _dragStartSplitterPos;

            _dragStartSizes.Clear();
            var visibleChildren = Children.Where(c => c.IsVisible).ToList();
            for (int i = 0; i < visibleChildren.Count; i++)
            {
                var child = visibleChildren[i];
                _dragStartSizes.Add(Orientation == SplitterOrientation.Horizontal
                    ? child.ComputedWidth
                    : child.ComputedHeight);
            }

            MarkNeedsPaint();
        }
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (_draggedSplitterIndex != -1)
        {
            _draggedSplitterIndex = -1;
            MarkNeedsPaint();
        }
    }

    public void OnPointerCanceled(PointerEventArgs e)
    {
        if (_draggedSplitterIndex != -1)
        {
            _draggedSplitterIndex = -1;
            MarkNeedsPaint();
        }
    }

    #endregion


    private void ApplyResize(int index, float delta)
    {
        var visibleChildren = Children.Where(c => c.IsVisible).ToList();
        
        if (index < 0 || index >= visibleChildren.Count - 1) return;
        if (_dragStartSizes.Count != visibleChildren.Count) return;

        var leftSize = _dragStartSizes[index];
        var rightSize = _dragStartSizes[index + 1];

        // Calculate clamped delta
        float maxDelta = rightSize - 10; // Min size 10
        float minDelta = -(leftSize - 10);
        
        float clampedDelta = Math.Clamp(delta, minDelta, maxDelta);

        // Set ALL children to their snapshot sizes to prevent proportional re-scaling,
        // then adjust the two adjacent ones by the delta.
        for (int i = 0; i < visibleChildren.Count; i++)
        {
            float size = _dragStartSizes[i];
            if (i == index) size += clampedDelta;
            else if (i == index + 1) size -= clampedDelta;

            if (Orientation == SplitterOrientation.Horizontal)
                visibleChildren[i].Width = size;
            else
                visibleChildren[i].Height = size;
        }
        
        MarkNeedsLayout();
    }
}
