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
public class ButtonIcon : BorderView<ButtonIcon>,
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

    #region Variant
    [PaintProperty]
    public ButtonVariant Variant
    {
        get => field;
        set => this.SetProperty(ref field, value, ApplyActiveTheme);
    } = ButtonVariant.Primary;
    #endregion

    #region IconColor
    public Brush IconColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
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
    } = Color.Transparent;
    #endregion

    #region HoverBackground
    public Brush HoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () => UpdateVisualState());
    } = Color.Transparent;
    #endregion

    #region PressedBackground
    public Brush PressedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () => UpdateVisualState());
    } = Color.Transparent;
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

    public ButtonIcon()
    {
        // Initialize reactive properties
        IconSize = 24;
        InitializeTheme();

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

    public ButtonIcon(IconData iconData) : this()
    {
        IconData = iconData;
    }

    public ButtonIcon UseThemeDefaults()
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
        SetThemeValue(nameof(IconColor), (Brush)colors.Foreground, value => IconColor = value);
        SetThemeValue(nameof(BorderBrush), (Brush)colors.Border, value => BorderBrush = value);
        SetThemeValue(nameof(BorderThickness), new Thickness(0), value => BorderThickness = value);
        SetThemeValue(nameof(Padding), new Thickness(theme.Spacing.Md), value => Padding = value);
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

    // =========================================================================
    // LAYOUT & RENDERING
    // =========================================================================

    protected override void Measure(float availableWidth, float availableHeight)
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
            // Apply the platform/theme touch target only to auto-sized buttons.
            // Explicit widths must remain exact so the arranged visual cannot
            // overlap spacing reserved by a parent layout.
            measuredWidth = Math.Max(measuredWidth, EffectiveTheme.ControlHeight);
        }

        if (HasExplicitHeight)
        {
            measuredHeight = Height;
        }
        else
        {
            measuredHeight = Math.Max(measuredHeight, EffectiveTheme.ControlHeight);
        }

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Render(IRenderer renderer)
    {
        if (ComputedWidth <= 0 || ComputedHeight <= 0) return;

        // Draw background � use DrawRoundedRect when corners are uniform so gradient
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
        if (BorderThickness.Left > 0)
        {
            renderer.DrawRoundedRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight,
                BorderRadius.TopLeft, BorderThickness.Left, BorderBrush.PrimaryColor);
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
}
