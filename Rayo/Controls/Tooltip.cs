namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

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
    public string Text { get; set; } = "";
    private Label? _label;

    public TooltipFrame(string text)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Text = text;
        this.Background(new Color(50, 50, 50))
        .Padding(new Thickness(8, 4, 8, 4));
        BorderRadius = new CornerRadius(4);

        _label = new Label(text)
            .TextHorizontalAlignment(HorizontalAlignment.Center)
            .TextVerticalAlignment(VerticalAlignment.Center)
            .Foreground(Color.White)
            .FontSize(12);

        this.Content(_label);
    }

    public void UpdateText(string text)
    {
        Text = text;
        if (_label != null)
        {
            _label.Text(text);
        }
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

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Measure target
        _target.Measure(availableWidth, availableHeight);

        // Our desired size is the target's desired size
        DesiredWidth = _target.DesiredWidth;
        DesiredHeight = _target.DesiredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Arrange target to fill our space at the same absolute position
        _target.Arrange(x, y, width, height);
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

        // Measure tooltip to get its size
        _tooltipFrame.Measure(float.PositiveInfinity, float.PositiveInfinity);

        // Calculate tooltip position
        var (x, y) = CalculateTooltipPosition();

        // Set position using SetX/Y (like Menu does)
        _tooltipFrame.X(x);
        _tooltipFrame.Y(y);

        app.AddOverlay(_tooltipFrame);
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

    private (float x, float y) CalculateTooltipPosition()
    {
        var app = UIApplication.Current;
        if (_tooltipFrame == null || app == null)
            return (0, 0);

        float tooltipWidth = _tooltipFrame.DesiredWidth;
        float tooltipHeight = _tooltipFrame.DesiredHeight;
        float spacing = 8; // Gap between target and tooltip

        float x = 0, y = 0;

        TooltipPlacement actualPlacement = _placement;

        // Use target element's coordinates (already absolute in Rayo)
        float targetX = _target.ComputedX;
        float targetY = _target.ComputedY;
        float targetWidth = _target.ComputedWidth;
        float targetHeight = _target.ComputedHeight;

        // Auto placement: choose best position based on available space
        if (actualPlacement == TooltipPlacement.Auto)
        {
            float spaceTop = targetY;
            float spaceBottom = app.Window.Height - (targetY + targetHeight);
            float spaceLeft = targetX;
            float spaceRight = app.Window.Width - (targetX + targetWidth);

            // Prefer bottom, then top, then right, then left
            if (spaceBottom >= tooltipHeight + spacing)
                actualPlacement = TooltipPlacement.Bottom;
            else if (spaceTop >= tooltipHeight + spacing)
                actualPlacement = TooltipPlacement.Top;
            else if (spaceRight >= tooltipWidth + spacing)
                actualPlacement = TooltipPlacement.Right;
            else
                actualPlacement = TooltipPlacement.Left;
        }

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

        return (x, y);
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
