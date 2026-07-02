namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Graphics.VectorGraphics;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

/// <summary>
/// Tooltip positioning relative to target element
/// </summary>
public enum TooltipPlacement
{
    Top,
    Bottom,
    Left,
    Right,
    Auto
}

/// <summary>
/// Tooltip content Frame that appears on hover.
/// Internal use - use TooltipHost to attach tooltips to elements.
/// </summary>
internal class TooltipFrame : Frame
{
    private const float ArrowLength = 8f;
    private const float ArrowWidth = 12f;
    private const float HorizontalPadding = 10f;
    private const float VerticalPadding = 6f;

    public string Text { get; set; } = "";
    private Label? _label;
    private TooltipPlacement _actualPlacement = TooltipPlacement.Bottom;
    private float _arrowOffset;

    public TooltipFrame(string text)
    {
        InitializeTheme();
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Text = text;

        _label = new Label(text)
            .TextHorizontalAlignment(HorizontalAlignment.Center)
            .TextVerticalAlignment(VerticalAlignment.Center);

        this.Content(_label);
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        SetThemeValue(nameof(Background), (Brush)theme.Colors.SurfacePressed, value => Background = value);
        if (_label != null)
        {
            _label.Foreground = theme.Colors.OnSurface;
            _label.FontSize = theme.Typography.Caption.FontSize * theme.Preferences.TextScale;
        }
    }

    public void UpdateText(string text)
    {
        Text = text;
        if (_label != null)
        {
            _label.Text(text);
        }
    }

    public void ConfigureArrow(TooltipPlacement placement, float arrowOffset)
    {
        _actualPlacement = placement == TooltipPlacement.Auto ? TooltipPlacement.Bottom : placement;
        _arrowOffset = arrowOffset;

        Padding = _actualPlacement switch
        {
            TooltipPlacement.Top => new Thickness(HorizontalPadding, VerticalPadding, HorizontalPadding, VerticalPadding + ArrowLength),
            TooltipPlacement.Bottom => new Thickness(HorizontalPadding, VerticalPadding + ArrowLength, HorizontalPadding, VerticalPadding),
            TooltipPlacement.Left => new Thickness(HorizontalPadding, VerticalPadding, HorizontalPadding + ArrowLength, VerticalPadding),
            TooltipPlacement.Right => new Thickness(HorizontalPadding + ArrowLength, VerticalPadding, HorizontalPadding, VerticalPadding),
            _ => new Thickness(HorizontalPadding, VerticalPadding + ArrowLength, HorizontalPadding, VerticalPadding)
        };
    }

    public override void Render(IRenderer renderer)
    {
        var (bubbleX, bubbleY, bubbleWidth, bubbleHeight) = GetBubbleBounds();
        if (Background.PrimaryColor.A <= 0 || bubbleWidth <= 0 || bubbleHeight <= 0)
        {
            return;
        }

        float radius = Math.Max(0, BorderRadius.TopLeft);
        if (radius > 0)
        {
            renderer.DrawRoundedRect(bubbleX, bubbleY, bubbleWidth, bubbleHeight, radius, Background);
        }
        else
        {
            renderer.DrawRect(bubbleX, bubbleY, bubbleWidth, bubbleHeight, Background);
        }

        renderer.DrawPath(CreateArrowPath(bubbleX, bubbleY, bubbleWidth, bubbleHeight), Background);
    }

    private (float x, float y, float width, float height) GetBubbleBounds()
    {
        return _actualPlacement switch
        {
            TooltipPlacement.Top => (ComputedX, ComputedY, ComputedWidth, Math.Max(0, ComputedHeight - ArrowLength)),
            TooltipPlacement.Bottom => (ComputedX, ComputedY + ArrowLength, ComputedWidth, Math.Max(0, ComputedHeight - ArrowLength)),
            TooltipPlacement.Left => (ComputedX, ComputedY, Math.Max(0, ComputedWidth - ArrowLength), ComputedHeight),
            TooltipPlacement.Right => (ComputedX + ArrowLength, ComputedY, Math.Max(0, ComputedWidth - ArrowLength), ComputedHeight),
            _ => (ComputedX, ComputedY + ArrowLength, ComputedWidth, Math.Max(0, ComputedHeight - ArrowLength))
        };
    }

    private VectorPath CreateArrowPath(float bubbleX, float bubbleY, float bubbleWidth, float bubbleHeight)
    {
        float halfArrow = ArrowWidth / 2f;
        float minHorizontal = bubbleX + BorderRadius.TopLeft + halfArrow;
        float maxHorizontal = bubbleX + bubbleWidth - BorderRadius.TopRight - halfArrow;
        float minVertical = bubbleY + BorderRadius.TopLeft + halfArrow;
        float maxVertical = bubbleY + bubbleHeight - BorderRadius.BottomLeft - halfArrow;

        return _actualPlacement switch
        {
            TooltipPlacement.Top => CreateTriangle(
                Math.Clamp(ComputedX + _arrowOffset, minHorizontal, maxHorizontal),
                ComputedY + ComputedHeight,
                Math.Clamp(ComputedX + _arrowOffset - halfArrow, bubbleX, bubbleX + bubbleWidth),
                bubbleY + bubbleHeight,
                Math.Clamp(ComputedX + _arrowOffset + halfArrow, bubbleX, bubbleX + bubbleWidth),
                bubbleY + bubbleHeight),

            TooltipPlacement.Bottom => CreateTriangle(
                Math.Clamp(ComputedX + _arrowOffset, minHorizontal, maxHorizontal),
                ComputedY,
                Math.Clamp(ComputedX + _arrowOffset - halfArrow, bubbleX, bubbleX + bubbleWidth),
                bubbleY,
                Math.Clamp(ComputedX + _arrowOffset + halfArrow, bubbleX, bubbleX + bubbleWidth),
                bubbleY),

            TooltipPlacement.Left => CreateTriangle(
                ComputedX + ComputedWidth,
                Math.Clamp(ComputedY + _arrowOffset, minVertical, maxVertical),
                bubbleX + bubbleWidth,
                Math.Clamp(ComputedY + _arrowOffset - halfArrow, bubbleY, bubbleY + bubbleHeight),
                bubbleX + bubbleWidth,
                Math.Clamp(ComputedY + _arrowOffset + halfArrow, bubbleY, bubbleY + bubbleHeight)),

            TooltipPlacement.Right => CreateTriangle(
                ComputedX,
                Math.Clamp(ComputedY + _arrowOffset, minVertical, maxVertical),
                bubbleX,
                Math.Clamp(ComputedY + _arrowOffset - halfArrow, bubbleY, bubbleY + bubbleHeight),
                bubbleX,
                Math.Clamp(ComputedY + _arrowOffset + halfArrow, bubbleY, bubbleY + bubbleHeight)),

            _ => CreateTriangle(ComputedX + _arrowOffset, ComputedY, ComputedX + _arrowOffset - halfArrow, bubbleY, ComputedX + _arrowOffset + halfArrow, bubbleY)
        };
    }

    private static VectorPath CreateTriangle(float tipX, float tipY, float baseX1, float baseY1, float baseX2, float baseY2)
    {
        return new VectorPath()
            .MoveTo(tipX, tipY)
            .LineTo(baseX1, baseY1)
            .LineTo(baseX2, baseY2)
            .Close();
    }
}

/// <summary>
/// Host component that manages tooltip display for a target element.
/// Wraps the target and shows tooltip on hover.
/// Uses IPointerHandler for hover detection.
/// </summary>
public class TooltipHost : Rayo.Core.CompositeView<TooltipHost>, Rayo.Core.Input.IPointerHandler
{
    private readonly VisualElement _target;
    private readonly string _tooltipText;
    private readonly TooltipPlacement _placement;
    private TooltipFrame? _tooltipFrame;
    private bool _isShowing = false;
    private bool _isHovered = false;

    // Internal hover state management with custom setter to detect hover changes
    public new bool IsHovered
    {
        get => _isHovered;
        private set
        {
            if (_isHovered != value)
            {
                _isHovered = value;
                if (_isHovered)
                    OnHoverEnter();
                else
                    OnHoverExit();
            }
        }
    }

    public TooltipHost(VisualElement target, string tooltipText, TooltipPlacement placement = TooltipPlacement.Auto)
    {
        _target = target;
        _tooltipText = tooltipText;
        _placement = placement;

        // Wrap the target
        AddChild(_target);

        // Copy alignment from target
        HorizontalAlignment = _target.HorizontalAlignment;
        VerticalAlignment = _target.VerticalAlignment;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        // Measure target
        _target.MeasureUpdate(availableWidth, availableHeight);

        // Our desired size is the target's desired size
        DesiredWidth = _target.DesiredWidth;
        DesiredHeight = _target.DesiredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Arrange target to fill our space at the same absolute position
        _target.ArrangeUpdate(x, y, width, height);
    }

    private void OnHoverEnter()
    {
        ShowTooltip();
    }

    private void OnHoverExit()
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        var app = UIApplication.Current;
        if (app == null || string.IsNullOrWhiteSpace(_tooltipText)) return;

        _tooltipFrame = new TooltipFrame(_tooltipText);
        _tooltipFrame.ConfigureArrow(TooltipPlacement.Bottom, 0);
        _tooltipFrame.MeasureUpdate(float.PositiveInfinity, float.PositiveInfinity);

        var actualPlacement = ResolvePlacement();
        _tooltipFrame.ConfigureArrow(actualPlacement, 0);
        _tooltipFrame.MeasureUpdate(float.PositiveInfinity, float.PositiveInfinity);

        // Calculate tooltip position
        var (x, y, arrowOffset) = CalculateTooltipPosition(actualPlacement);
        _tooltipFrame.ConfigureArrow(actualPlacement, arrowOffset);

        // Set position using SetX/Y (like Menu does)
        _tooltipFrame.X(x);
        _tooltipFrame.Y(y);

        app.AddOverlay(_tooltipFrame, this);
        _isShowing = true;
    }

    private void HideTooltip()
    {
        var app = UIApplication.Current;
        if (app != null && _tooltipFrame != null && _isShowing)
        {
            app.RemoveOverlay(_tooltipFrame);
            _isShowing = false;
            _tooltipFrame = null;
        }
    }

    private TooltipPlacement ResolvePlacement()
    {
        var app = UIApplication.Current;
        TooltipPlacement actualPlacement = _placement;

        if (app == null || actualPlacement != TooltipPlacement.Auto)
        {
            return actualPlacement == TooltipPlacement.Auto ? TooltipPlacement.Bottom : actualPlacement;
        }

        float targetX = _target.ComputedX;
        float targetY = _target.ComputedY;
        float targetWidth = _target.ComputedWidth;
        float targetHeight = _target.ComputedHeight;
        float spacing = 4;
        float tooltipWidth = _tooltipFrame?.DesiredWidth > 0 ? _tooltipFrame.DesiredWidth : 80;
        float tooltipHeight = _tooltipFrame?.DesiredHeight > 0 ? _tooltipFrame.DesiredHeight : 28;

        float spaceTop = targetY;
        float spaceBottom = app.Window.Height - (targetY + targetHeight);
        float spaceLeft = targetX;
        float spaceRight = app.Window.Width - (targetX + targetWidth);

        // Prefer bottom, then top, then right, then left
        if (spaceBottom >= tooltipHeight + spacing)
            return TooltipPlacement.Bottom;
        if (spaceTop >= tooltipHeight + spacing)
            return TooltipPlacement.Top;
        if (spaceRight >= tooltipWidth + spacing)
            return TooltipPlacement.Right;
        if (spaceLeft >= tooltipWidth + spacing)
            return TooltipPlacement.Left;

        return TooltipPlacement.Bottom;
    }

    private (float x, float y, float arrowOffset) CalculateTooltipPosition(TooltipPlacement actualPlacement)
    {
        var app = UIApplication.Current;
        if (_tooltipFrame == null || app == null)
            return (0, 0, 0);

        float tooltipWidth = _tooltipFrame.DesiredWidth;
        float tooltipHeight = _tooltipFrame.DesiredHeight;
        float spacing = 4;

        float x = 0, y = 0;

        // Use target element's coordinates (already absolute in Rayo)
        float targetX = _target.ComputedX;
        float targetY = _target.ComputedY;
        float targetWidth = _target.ComputedWidth;
        float targetHeight = _target.ComputedHeight;
        float targetCenterX = targetX + targetWidth / 2f;
        float targetCenterY = targetY + targetHeight / 2f;

        switch (actualPlacement)
        {
            case TooltipPlacement.Top:
                x = targetX + (targetWidth - tooltipWidth) / 2;
                y = targetY - tooltipHeight - spacing;
                break;

            case TooltipPlacement.Bottom:
                x = targetX + (targetWidth - tooltipWidth) / 2;
                y = targetY + targetHeight + spacing;
                break;

            case TooltipPlacement.Left:
                x = targetX - tooltipWidth - spacing;
                y = targetY + (targetHeight - tooltipHeight) / 2;
                break;

            case TooltipPlacement.Right:
                x = targetX + targetWidth + spacing;
                y = targetY + (targetHeight - tooltipHeight) / 2;
                break;
        }

        // Keep tooltip within window bounds
        x = Math.Clamp(x, 0, app.Window.Width - tooltipWidth);
        y = Math.Clamp(y, 0, app.Window.Height - tooltipHeight);

        float arrowOffset = actualPlacement is TooltipPlacement.Top or TooltipPlacement.Bottom
            ? Math.Clamp(targetCenterX - x, 0, tooltipWidth)
            : Math.Clamp(targetCenterY - y, 0, tooltipHeight);

        return (x, y, arrowOffset);
    }

    public override void Render(IRenderer renderer)
    {
        // We don't render anything ourselves - just the target
        // UITree will render our children automatically
    }

    // =========================================================================
    // IPOINTERHANDLER IMPLEMENTATION
    // =========================================================================

    void Rayo.Core.Input.IPointerHandler.OnPointerEntered(Rayo.Core.Input.PointerEventArgs e)
    {
        IsHovered = true;
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerExited(Rayo.Core.Input.PointerEventArgs e)
    {
        IsHovered = false;
    }
}

/// <summary>
/// Extension methods to easily add tooltips to any UIElement
/// </summary>
public static class TooltipExtensions
{
    /// <summary>
    /// Adds a tooltip to this element
    /// </summary>
    public static TooltipHost WithTooltip(this VisualElement element, string text, TooltipPlacement placement = TooltipPlacement.Auto)
    {
        return new TooltipHost(element, text, placement);
    }
}
