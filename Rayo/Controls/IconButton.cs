namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Modern icon button control with unified pointer event support (mouse, touch, pen).
///
/// Features:
/// - Unified pointer events (IPointerHandler) - works with mouse, touch, and stylus
/// - Gesture recognition (ITappable) - single tap, double tap
/// - Icon display support
/// - Touch-friendly minimum size (44x44 recommended)
/// - Hybrid reactive approach for properties
///
/// Uses modern system:
/// - IPointerHandler for unified pointer events
/// - ITappable for tap gesture support
/// - IGestureRecognizerHost for gesture recognizers
/// </summary>
public class IconButton : Rayo.Core.View<IconButton>,
    IPointerHandler,           // Modern unified pointer events
    ITappable,                 // Tap gesture support
    IGestureRecognizerHost     // Hosts gesture recognizers
{
    private Brush _currentBackground = Color.Transparent;
    private readonly TapRecognizer _tapRecognizer;

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region IconData
    public IconData? IconData
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region IconColor
    public Brush IconColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region IconSize
    public float IconSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    }
    #endregion

    #region HoverBackground
    public Brush HoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () => UpdateVisualState());
    }
    #endregion

    #region PressedBackground
    public Brush PressedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () => UpdateVisualState());
    }
    #endregion

    #region BorderWidth
    [LayoutProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region IsHovered
    public new bool IsHovered
    {
        get => field;
        set => this.SetProperty(ref field, value, () => UpdateVisualState());
    }
    #endregion

    #region IsPressed
    /// <summary>
    /// Gets or sets whether the button is currently pressed.
    /// Automatically updates visual state when changed.
    /// </summary>
    public new bool IsPressed
    {
        get => field;
        set => this.SetProperty(ref field, value, () => UpdateVisualState());
    }
    #endregion

    // =========================================================================
    // EVENTS (Modern + Legacy)
    // =========================================================================

    #region Modern Events (Preferred)
    
    /// <summary>
    /// Fired when the button is tapped (unified click/touch event).
    /// Preferred over OnClick for cross-platform apps.
    /// </summary>
    public event Action<TapGestureEventArgs>? Tapped;

    #endregion

    #region Gesture Recognizer Host

    /// <summary>
    /// List of gesture recognizers attached to this button.
    /// </summary>
    public List<IGestureRecognizer> GestureRecognizers { get; } = new();

    #endregion

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public IconButton()
    {
        // Initialize reactive properties
        IconSize = 24;
        IconColor = Color.White;
        Background = new Color(70, 130, 180);
        HoverBackground = new Color(100, 150, 200);
        PressedBackground = new Color(50, 100, 150);
        BorderWidth = 0;
        BorderColor = new Color(40, 80, 120);

        // Touch-friendly sizing
        // Minimum recommended size for touch: 44x44 (iOS HIG) or 48x48 (Material Design)
        Padding = new Thickness(8);
        BorderRadius = new CornerRadius(4);

        UpdateVisualState();

        // Setup gesture recognizers
        _tapRecognizer = new TapRecognizer(
            maxMovementThreshold: 15f,  // Larger threshold for touch (finger is less precise)
            maxPressDurationMs: 500,
            doubleTapWindowMs: 300
        );
        _tapRecognizer.TapDetected += OnTapDetected;
        GestureRecognizers.Add(_tapRecognizer);
    }

    public IconButton(IconData iconData) : this()
    {
        IconData = iconData;
    }

    // =========================================================================
    // POINTER EVENT HANDLERS (Modern System)
    // =========================================================================

    /// <summary>
    /// Called when pointer enters the button bounds.
    /// Only fires for mouse, not touch (touch has no hover state).
    /// </summary>
    public void OnPointerEntered(PointerEventArgs e)
    {
        // Hover only meaningful for mouse
        if (e.PointerType == PointerType.Mouse)
        {
            IsHovered = true;
        }
    }

    /// <summary>
    /// Called when pointer exits the button bounds.
    /// </summary>
    public void OnPointerExited(PointerEventArgs e)
    {
        if (e.PointerType == PointerType.Mouse)
        {
            IsHovered = false;
            IsPressed = false; // Reset if released outside the element
        }
    }

    /// <summary>
    /// Called when pointer is pressed down on the button.
    /// Works for mouse, touch, and pen.
    /// </summary>
    public void OnPointerPressed(PointerEventArgs e)
    {
        IsPressed = true;
    }

    /// <summary>
    /// Called when pointer is released.
    /// Works for mouse, touch, and pen.
    /// </summary>
    public void OnPointerReleased(PointerEventArgs e)
    {
        IsPressed = false;
    }

    /// <summary>
    /// Called when a tap gesture is detected by the recognizer.
    /// </summary>
    private void OnTapDetected(TapGestureEventArgs e)
    {
        // Fire modern event
        Tapped?.Invoke(e);
    }

    // =========================================================================
    // VISUAL STATE MANAGEMENT
    // =========================================================================

    private void UpdateVisualState()
    {
        // Determine current background based on state
        _currentBackground = IsPressed ? PressedBackground :
                           IsHovered ? HoverBackground :
                           Background;
    }

    // =========================================================================
    // LAYOUT & RENDERING
    // =========================================================================

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Calculate size based on icon size + padding
        float totalPadding = Padding.Left + Padding.Right;
        float totalPaddingV = Padding.Top + Padding.Bottom;

        float contentWidth = IconSize;
        float contentHeight = IconSize;

        // Calculate desired size
        float measuredWidth = contentWidth + totalPadding;
        float measuredHeight = contentHeight + totalPaddingV;

        // Apply explicit size constraints if set
        if (HasExplicitWidth)
        {
            measuredWidth = Width;
        }
        else
        {
            // Respect min size for touch targets only when size is not explicitly set
            // This allows smaller icons when explicitly configured with .Size()
            if (measuredWidth < 44) measuredWidth = 44;
        }

        if (HasExplicitHeight)
        {
            measuredHeight = Height;
        }
        else
        {
            // Respect min size for touch targets only when size is not explicitly set
            if (measuredHeight < 44) measuredHeight = 44;
        }

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Render(IRenderer renderer)
    {
        if (ComputedWidth <= 0 || ComputedHeight <= 0) return;

        // Draw background — use DrawRoundedRect when corners are uniform so gradient
        // brushes render properly. Fall back to DrawPath for non-uniform corner radii.
        bool uniformRadius = BorderRadius.TopLeft == BorderRadius.TopRight
                          && BorderRadius.TopLeft == BorderRadius.BottomRight
                          && BorderRadius.TopLeft == BorderRadius.BottomLeft;

        if (uniformRadius)
        {
            renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight,
                BorderRadius.TopLeft, _currentBackground);
        }
        else
        {
            var bgPath = VectorPath.RoundedRectangle(
                ComputedX, ComputedY, ComputedWidth, ComputedHeight,
                BorderRadius.TopLeft, BorderRadius.TopRight,
                BorderRadius.BottomRight, BorderRadius.BottomLeft);
            renderer.DrawPath(bgPath, _currentBackground);
        }

        // Draw border if specified
        if (BorderWidth > 0)
        {
            renderer.DrawRoundedRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight,
                BorderRadius.TopLeft, BorderWidth, BorderColor);
        }

        // Draw icon
        if (IconData != null)
        {
            RenderVectorIcon(renderer);
        }
    }

    private void RenderVectorIcon(IRenderer renderer)
    {
        if (IconData == null) return;

        float scaleX = IconSize / IconData.ViewBoxWidth;
        float scaleY = IconSize / IconData.ViewBoxHeight;
        float scale = Math.Min(scaleX, scaleY);

        float offsetX = 0;
        float offsetY = 0;

        if (scaleX > scaleY)
        {
            offsetX = (IconSize - (IconData.ViewBoxWidth * scale)) / 2;
        }
        else if (scaleY > scaleX)
        {
            offsetY = (IconSize - (IconData.ViewBoxHeight * scale)) / 2;
        }

        // Center icon in button
        float iconX = ComputedX + Padding.Left + (ComputedWidth - Padding.Left - Padding.Right - IconSize) / 2;
        float iconY = ComputedY + Padding.Top + (ComputedHeight - Padding.Top - Padding.Bottom - IconSize) / 2;

        float renderX = iconX + offsetX;
        float renderY = iconY + offsetY;

        foreach (var command in IconData.Commands)
        {
            command.Draw(renderer, renderX, renderY, scale, IconColor.PrimaryColor);
        }
    }

    // =========================================================================
    // FLUENT API EXTENSIONS
    // =========================================================================

    //public IconButton IconData(IconData? iconData)
    //{
    //    IconData = iconData;
    //    return this;
    //}

    //public IconButton IconColor(Brush color)
    //{
    //    IconColor = color;
    //    return this;
    //}

    //public IconButton IconSize(float size)
    //{
    //    IconSize = size;
    //    return this;
    //}

    //public IconButton Background(Brush color)
    //{
    //    Background = color;
    //    return this;
    //}

    //public IconButton HoverBackground(Brush color)
    //{
    //    HoverBackground = color;
    //    return this;
    //}

    //public IconButton PressedBackground(Brush color)
    //{
    //    PressedBackground = color;
    //    return this;
    //}

    //public IconButton BorderWidth(float width)
    //{
    //    BorderWidth = width;
    //    return this;
    //}

    //public IconButton BorderColor(Brush color)
    //{
    //    BorderColor = color;
    //    return this;
    //}
}
