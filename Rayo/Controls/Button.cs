namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using Rayo.Styling;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Semantic visual variants supplied by the active theme.
/// </summary>
public enum ButtonVariant
{
    Primary,
    Secondary,
    Danger,
    Ghost,
}

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
public class Button : BorderView<Button>,
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

    #region Variant
    /// <summary>
    /// Selects the button color tokens from the active theme.
    /// </summary>
    [PaintProperty]
    public ButtonVariant Variant
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplyActiveTheme);
    } = ButtonVariant.Primary;
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    } = Color.Transparent;
    #endregion

    #region HoverBackground
    [PaintProperty]
    public Brush HoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    } = Color.Transparent;
    #endregion

    #region PressedBackground
    [PaintProperty]
    public Brush PressedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    } = Color.Transparent;
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
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

    /// <summary>
    /// Creates a button initialized with the active application theme.
    /// </summary>
    public Button()
    {
        // Initialize reactive properties (cannot use initializers on partial properties)
        Text = string.Empty;
        InitializeTheme();

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

    /// <summary>
    /// Creates a button with explicit colors derived from a semantic palette.
    /// Explicit palette colors are preserved when the global theme changes.
    /// </summary>
    public Button(ColorScheme palette) : this()
    {
        ApplyPalette(palette);
    }

    /// <summary>
    /// Applies the button-related roles from a semantic color palette.
    /// </summary>
    public Button ApplyPalette(ColorScheme palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        Background = palette.Primary;
        HoverBackground = palette.PrimaryHover;
        PressedBackground = palette.PrimaryPressed;
        TextColor = palette.OnPrimary;
        BorderBrush = palette.Border;

        return this;
    }

    /// <summary>
    /// Clears explicit color overrides and resumes following the active theme.
    /// </summary>
    public Button UseThemeDefaults()
    {
        ResetThemeValues();
        return this;
    }

    private void ApplyActiveTheme() =>
        OnThemeApplied(EffectiveTheme);

    protected override void OnThemeApplied(ThemeData theme)
    {
        var colors = Variant switch
        {
            ButtonVariant.Secondary => theme.Buttons.Secondary,
            ButtonVariant.Danger => theme.Buttons.Danger,
            ButtonVariant.Ghost => theme.Buttons.Ghost,
            _ => theme.Buttons.Primary,
        };

        SetThemeValue(nameof(Background), (Brush)colors.Background, value => Background = value);
        SetThemeValue(nameof(HoverBackground), (Brush)colors.HoverBackground, value => HoverBackground = value);
        SetThemeValue(nameof(PressedBackground), (Brush)colors.PressedBackground, value => PressedBackground = value);
        SetThemeValue(nameof(TextColor), (Brush)colors.Foreground, value => TextColor = value);
        SetThemeValue(nameof(BorderBrush), (Brush)colors.Border, value => BorderBrush = value);
        SetThemeValue(
            nameof(FontSize),
            theme.Buttons.Typography.FontSize * theme.Preferences.TextScale,
            value => FontSize = value);
        SetThemeValue(nameof(Padding), theme.Buttons.Padding, value => Padding = value);
        SetThemeValue(nameof(MinHeight), theme.Buttons.MinHeight, value => MinHeight = value);
        SetThemeValue(
            nameof(BorderThickness),
            new Thickness(theme.Buttons.BorderThickness),
            value => BorderThickness = value);
        SetThemeValue(nameof(BorderRadius), theme.Buttons.Radius, value => BorderRadius = value);
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
    /// Clears the pressed state when an ancestor takes ownership of the gesture,
    /// such as a ScrollView capturing a touch drag.
    /// </summary>
    public void OnPointerCanceled(PointerEventArgs e)
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
        var state = IsPressed
            ? ControlState.Pressed
            : IsHovered
                ? ControlState.Hovered
                : ControlState.Normal;
        _currentBackground = new StateMap<Brush>(Background)
            .With(ControlState.Hovered, HoverBackground)
            .With(ControlState.Pressed, PressedBackground)
            .Resolve(state);
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        // Button displays text directly - no children
        float contentWidth = string.IsNullOrEmpty(Text) ? 0 : Text.Length * (FontSize * 0.6f);
        float estimatedWidth = Math.Max(20, contentWidth + Padding.Horizontal);
        float estimatedHeight = (FontSize * 1.5f) + Padding.Vertical;

        DesiredWidth = Width > 0 ? Width : estimatedWidth;
        DesiredHeight = Height > 0 ? Height : estimatedHeight;

        OnMeasured(DesiredWidth, DesiredHeight);
    }

    protected override void Arrange(float x, float y, float width, float height)
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
