namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// Preferred vertical placement for an <see cref="AnchoredPopup"/>.
/// </summary>
public enum AnchoredPopupPlacement
{
    Auto,
    Below,
    Above
}

/// <summary>
/// Horizontal alignment of a popup relative to its anchor.
/// </summary>
public enum AnchoredPopupAlignment
{
    Start,
    Center,
    End
}

/// <summary>
/// Reusable overlay host that positions arbitrary content beside an anchor
/// after measuring its real size.
/// </summary>
public sealed class AnchoredPopup : Frame, IGlobalPointerHandler
{
    private bool _isOpen;

    public AnchoredPopup(VisualElement anchor, VisualElement content)
    {
        Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
        ArgumentNullException.ThrowIfNull(content);

        Background = Color.Transparent;
        BorderThickness = 0;
        Padding = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Content = content;
    }

    /// <summary>
    /// Element used as the positioning reference.
    /// </summary>
    public VisualElement Anchor { get; }

    [ArrangeProperty]
    public AnchoredPopupPlacement Placement
    {
        get => field;
        set => this.SetProperty(ref field, value, InvalidateArrange);
    } = AnchoredPopupPlacement.Auto;

    [ArrangeProperty]
    public AnchoredPopupAlignment AnchorAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value, InvalidateArrange);
    } = AnchoredPopupAlignment.Start;

    [ArrangeProperty]
    public float Gap
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(0, value), InvalidateArrange);
    } = 6f;

    [ArrangeProperty]
    public float EdgeInset
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(0, value), InvalidateArrange);
    } = 8f;

    [ArrangeProperty]
    public float OffsetX
    {
        get => field;
        set => this.SetProperty(ref field, value, InvalidateArrange);
    }

    [ArrangeProperty]
    public float OffsetY
    {
        get => field;
        set => this.SetProperty(ref field, value, InvalidateArrange);
    }

    /// <summary>
    /// Optional dynamic window-space position used by overlays whose anchor point
    /// can move while they are open, such as text selection handles.
    /// </summary>
    internal Func<float, float, Vector2>? WindowPositionProvider { get; set; }

    /// <summary>
    /// Closes the popup when the pointer is pressed outside it.
    /// </summary>
    public bool DismissOnOutsideClick { get; set; } = true;

    /// <summary>
    /// Restores focus to the anchor after interacting with popup content.
    /// </summary>
    public bool RestoreAnchorFocusOnInteraction { get; set; }

    public bool IsOpen => _isOpen;

    public event Action? Opened;
    public event Action? Closed;

    /// <summary>
    /// Creates and opens a popup in the global overlay layer.
    /// </summary>
    public static AnchoredPopup Show(
        VisualElement anchor,
        VisualElement content,
        Action<AnchoredPopup>? configure = null)
    {
        var popup = new AnchoredPopup(anchor, content);
        configure?.Invoke(popup);
        popup.Open();
        return popup;
    }

    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        OverlayManager.AddOverlay(this, Anchor);
        OverlayManager.EventManager?.RegisterGlobalPointerHandler(this);
        Opened?.Invoke();
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        OverlayManager.RemoveOverlay(this);
        OverlayManager.EventManager?.UnregisterGlobalPointerHandler(this);
        _isOpen = false;
        Closed?.Invoke();
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public bool HandleGlobalPointer(Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen)
        {
            return false;
        }

        if (ContainsWindowPoint(position))
        {
            return true;
        }

        if (Anchor.ContainsWindowPoint(position))
        {
            Close();
            return true;
        }

        if (DismissOnOutsideClick)
        {
            Close();
        }

        return false;
    }

    public bool HandleGlobalPointerReleased(Vector2 position, VisualElement? hitElement)
    {
        if (!_isOpen || !RestoreAnchorFocusOnInteraction || !ContainsWindowPoint(position))
            return false;

        OverlayManager.EventManager?.SetFocus(Anchor);
        return true;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        var bounds = GetAnchorBounds();
        var app = UIApplication.Current;
        float windowWidth = app?.Window.Width ?? OverlayManager.WindowWidth;
        float windowHeight = app?.Window.Height ?? OverlayManager.WindowHeight;

        float popupX;
        float popupY;
        if (WindowPositionProvider is { } positionProvider)
        {
            var position = positionProvider(width, height);
            popupX = position.X;
            popupY = position.Y;
        }
        else
        {
            popupX = AnchorAlignment switch
            {
                AnchoredPopupAlignment.Center => bounds.Left + (bounds.Right - bounds.Left - width) / 2f,
                AnchoredPopupAlignment.End => bounds.Right - width,
                _ => bounds.Left
            };

            bool placeAbove = Placement == AnchoredPopupPlacement.Above ||
                (Placement == AnchoredPopupPlacement.Auto &&
                 windowHeight > 0 &&
                 bounds.Bottom + Gap + height > windowHeight - EdgeInset);

            popupY = placeAbove
                ? bounds.Top - height - Gap
                : bounds.Bottom + Gap;

            popupX += OffsetX;
            popupY += OffsetY;
        }

        if (windowWidth > 0)
        {
            popupX = Math.Clamp(
                popupX,
                EdgeInset,
                Math.Max(EdgeInset, windowWidth - width - EdgeInset));
        }

        if (windowHeight > 0)
        {
            popupY = Math.Clamp(
                popupY,
                EdgeInset,
                Math.Max(EdgeInset, windowHeight - height - EdgeInset));
        }

        base.Arrange(popupX, popupY, width, height);
    }

    private (float Left, float Top, float Right, float Bottom) GetAnchorBounds()
    {
        var transform = Anchor.GetWorldRenderTransform();
        var topLeft = Vector2.Transform(
            new Vector2(Anchor.ComputedX, Anchor.ComputedY),
            transform);
        var bottomRight = Vector2.Transform(
            new Vector2(
                Anchor.ComputedX + Anchor.ComputedWidth,
                Anchor.ComputedY + Anchor.ComputedHeight),
            transform);

        return (
            Math.Min(topLeft.X, bottomRight.X),
            Math.Min(topLeft.Y, bottomRight.Y),
            Math.Max(topLeft.X, bottomRight.X),
            Math.Max(topLeft.Y, bottomRight.Y));
    }
}
