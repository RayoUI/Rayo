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
/// Modern button control with unified pointer event support (mouse, touch, pen).
///
/// Features:
/// - Unified pointer events (IPointerHandler) - works with mouse, touch, and stylus
/// - Gesture recognition (ITappable) - single tap, double tap
/// - Touch-friendly minimum size (44x44 recommended)
/// - Hybrid reactive approach for properties
///
/// Uses modern system:
/// - IPointerHandler for unified pointer events
/// - ITappable for tap gesture support
/// - IGestureRecognizerHost for gesture recognizers
/// </summary>
public class Button : Rayo.Core.View<Button>,
    IPointerHandler,           // Modern unified pointer events
    ITappable,                 // Tap gesture support
    IGestureRecognizerHost     // Hosts gesture recognizers
{
    private Brush _currentBackground = Color.Transparent;
    private readonly TapRecognizer _tapRecognizer;

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Text
    [LayoutProperty]
    public string Text
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
    [PaintProperty]
    public Brush HoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    }
    #endregion

    #region PressedBackground
    [PaintProperty]
    public Brush PressedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    }
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region FontSize
    [LayoutProperty]
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region TextAlignment
    /// <summary>
    /// Text alignment within the button (Left, Center, Right)
    /// </summary>
    [LayoutProperty]
    public HorizontalAlignment TextAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = HorizontalAlignment.Center;
    #endregion

    #region BorderWidth
    [PaintProperty]
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
    [PaintProperty]
    public new bool IsHovered
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    }
    #endregion

    #region IsPressed

    /// <summary>
    /// Gets or sets whether the button is currently pressed.
    /// Automatically updates visual state when changed.
    /// </summary>
    [PaintProperty]
    public new bool IsPressed
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    }
    #endregion

    // =========================================================================
    // EVENTS
    // =========================================================================

    #region Events
    
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

    public Button()
    {
        // Initialize reactive properties (cannot use initializers on partial properties)
        Text = string.Empty;
        FontSize = 14;
        Background = new Color(70, 130, 180);
        HoverBackground = new Color(100, 150, 200);
        PressedBackground = new Color(50, 100, 150);
        TextColor = Color.White;
        BorderWidth = 2;
        BorderColor = new Color(40, 80, 120);

        // Touch-friendly sizing
        // Minimum recommended size for touch: 44x44 (iOS HIG) or 48x48 (Material Design)
        // We don't enforce it here to allow flexibility, but callers should consider it
        Padding = new Thickness(12, 6, 12, 6);
        BorderRadius = new CornerRadius(4);

        // Visual state is updated directly in property setters

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

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Button displays text directly - no children
        float contentWidth = string.IsNullOrEmpty(Text) ? 0 : Text.Length * (FontSize * 0.6f);
        float estimatedWidth = Math.Max(20, contentWidth + Padding.Horizontal);
        float estimatedHeight = (FontSize * 1.5f) + Padding.Vertical;

        DesiredWidth = Width > 0 ? Width : estimatedWidth;
        DesiredHeight = Height > 0 ? Height : estimatedHeight;

        OnMeasured(DesiredWidth, DesiredHeight);
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        // Button is a leaf control - no children to arrange
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Use DrawRoundedRect for uniform corners so gradient brushes render properly.
        // Fall back to DrawPath for non-uniform corner radii (solid only).
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

        if (!string.IsNullOrEmpty(Text))
        {
            float maxTextWidth = ComputedWidth - Padding.Horizontal;
            string displayText = renderer.TruncateTextToFit(Text, maxTextWidth, FontSize);
            var textSize = renderer.MeasureText(displayText, FontSize);

            // Calculate X position based on TextAlignment
            float textX = TextAlignment switch
            {
                HorizontalAlignment.Left => ComputedX + Padding.Left,
                HorizontalAlignment.Right => ComputedX + ComputedWidth - textSize.X - Padding.Right,
                _ => ComputedX + (ComputedWidth - textSize.X) / 2 // Center
            };
            float textY = ComputedY + (ComputedHeight - textSize.Y) / 2;

            renderer.DrawText(displayText, textX, textY, TextColor, FontSize);
        }
    }
}
