namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Checkbox with label and state management.
/// Uses modern pointer event system:
/// - IPointerHandler for unified pointer events
/// - ITappable for tap gesture support
/// - IGestureRecognizerHost for gesture recognizers
/// </summary>
public class Checkbox : Rayo.Core.View<Checkbox>,
    IPointerHandler,           // Modern unified pointer events
    ITappable,                 // Tap gesture support
    IGestureRecognizerHost     // Hosts gesture recognizers
{
    // Backing fields for generated/reactive properties
    private string _label = "";
    private Brush _background = new Color(40, 40, 40);
    private Brush _checkedBackground = new Color(59, 130, 246);
    private Brush _hoverBackground = new Color(50, 50, 50);
    private Brush _checkmarkColor = Color.White;
    private Brush _labelColor = Color.White;
    private float _boxSize = 20f;
    private float _labelSpacing = 10f;

    #region Label
    [LayoutProperty]
    public string Label
    {
        get => _label;
        set => this.SetProperty(ref _label, value);
    }
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => _background;
        set => this.SetProperty(ref _background, value, UpdateVisualState);
    }
    #endregion

    #region CheckedBackground
    [PaintProperty]
    public Brush CheckedBackground
    {
        get => _checkedBackground;
        set => this.SetProperty(ref _checkedBackground, value, UpdateVisualState);
    }
    #endregion

    #region HoverBackground
    [PaintProperty]
    public Brush HoverBackground
    {
        get => _hoverBackground;
        set => this.SetProperty(ref _hoverBackground, value, UpdateVisualState);
    }
    #endregion

    #region CheckmarkColor
    [PaintProperty]
    public Brush CheckmarkColor
    {
        get => _checkmarkColor;
        set => this.SetProperty(ref _checkmarkColor, value);
    }
    #endregion

    #region LabelColor
    [PaintProperty]
    public Brush LabelColor
    {
        get => _labelColor;
        set => this.SetProperty(ref _labelColor, value);
    }
    #endregion

    #region BoxSize
    [LayoutProperty]
    public float BoxSize
    {
        get => _boxSize;
        set => this.SetProperty(ref _boxSize, value);
    }
    #endregion

    #region LabelSpacing
    [LayoutProperty]
    public float LabelSpacing
    {
        get => _labelSpacing;
        set => this.SetProperty(ref _labelSpacing, value);
    }
    #endregion

    #region BoxPadding
    [LayoutProperty]
    public float BoxPadding
    {
        get => field;
        set
        {
            if (Math.Abs(field - value) > 0.01f)
            {
                field = value;
                MarkNeedsLayout();
            }
        }
    } = 2f;
    #endregion

    #region FontSize
    [LayoutProperty]
    public float FontSize
    {
        get => field;
        set
        {
            if (Math.Abs(field - value) > 0.01f)
            {
                field = value;
                MarkNeedsLayout();
            }
        }
    } = 14f;
    #endregion

    /// <summary>
    /// Gets or sets the checked state.
    /// </summary>
    #region IsChecked
    [PaintProperty]
    public bool IsChecked
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                Changed?.Invoke(value);
                UpdateVisualState();
                MarkNeedsPaint();
            }
        }
    } = false;
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
    [PaintProperty]
    public new bool IsPressed
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateVisualState);
    }
    #endregion

    private Brush _currentBackground = Color.Transparent;
    private readonly TapRecognizer _tapRecognizer;


    // =========================================================================
    // EVENTS
    // =========================================================================

    public event Action<bool>? Changed;

    /// <summary>
    /// Fired when the checkbox is tapped (unified click/touch event).
    /// </summary>
    public event Action<TapGestureEventArgs>? Tapped;

    #region Gesture Recognizer Host
    /// <summary>
    /// List of gesture recognizers attached to this checkbox.
    /// </summary>
    public List<IGestureRecognizer> GestureRecognizers { get; } = new();
    #endregion

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public Checkbox()
    {
        BorderRadius = new CornerRadius(4);

        // Setup gesture recognizers
        _tapRecognizer = new TapRecognizer(
            maxMovementThreshold: 15f,
            maxPressDurationMs: 500,
            doubleTapWindowMs: 300
        );
        _tapRecognizer.TapDetected += OnTapDetected;
        GestureRecognizers.Add(_tapRecognizer);

        UpdateVisualState();
    }

    public Checkbox(string label) : this()
    {
        Label = label;
    }

    private void Toggle()
    {
        IsChecked = !IsChecked;
    }

    // =========================================================================
    // POINTER EVENT HANDLERS (Modern System)
    // =========================================================================

    public void OnPointerEntered(PointerEventArgs e)
    {
        if (e.PointerType == PointerType.Mouse)
        {
            IsHovered = true;
        }
    }

    public void OnPointerExited(PointerEventArgs e)
    {
        if (e.PointerType == PointerType.Mouse)
        {
            IsHovered = false;
            IsPressed = false; // Reset if released outside the element
        }
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        IsPressed = true;
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        IsPressed = false;
    }

    private void OnTapDetected(TapGestureEventArgs e)
    {
        Toggle();
        Tapped?.Invoke(e);
    }

    // =========================================================================
    // VISUAL STATE MANAGEMENT
    // =========================================================================

    private void UpdateVisualState()
    {
        _currentBackground = IsPressed ? new Color(30, 30, 30) :
                           IsHovered ? HoverBackground :
                           (IsChecked ? CheckedBackground : Background);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float textWidth = EstimateTextWidth(Label);
        float boxAreaWidth = BoxSize + BoxPadding * 2;
        float contentWidth = boxAreaWidth;

        if (!string.IsNullOrEmpty(Label))
        {
            contentWidth += LabelSpacing + textWidth;
        }

        float boxAreaHeight = BoxSize + BoxPadding * 2;
        float desiredWidth = Width > 0 ? Width : contentWidth;
        float desiredHeight = Height > 0 ? Height : Math.Max(boxAreaHeight, FontSize);

        DesiredWidth = desiredWidth;
        DesiredHeight = desiredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        float boxAreaWidth = BoxSize + BoxPadding * 2;
        float boxAreaHeight = BoxSize + BoxPadding * 2;
        float boxAreaTop = ComputedY + (ComputedHeight - boxAreaHeight) / 2;
        float boxX = ComputedX + BoxPadding;
        float boxY = boxAreaTop + BoxPadding;

        // Draw checkbox box using precomputed background
        renderer.DrawRoundedRect(boxX, boxY, BoxSize, BoxSize, 4, _currentBackground);

        // Dibujar X si está checked
        if (IsChecked)
        {
            // Dibujar una X clara y visible
            float xSize = BoxSize * 0.5f;
            float xCenterX = boxX + BoxSize / 2;
            float xCenterY = boxY + BoxSize / 2;
            float xThickness = 2.5f;

            // Calcular los puntos de la X
            float halfSize = xSize / 2;

            // Línea diagonal de arriba-izquierda a abajo-derecha (\)
            float x1 = xCenterX - halfSize;
            float y1 = xCenterY - halfSize;
            float x2 = xCenterX + halfSize;
            float y2 = xCenterY + halfSize;

            // Dibujar línea \ (usando rectángulos rotados visualmente)
            for (int i = 0; i <= (int)xSize; i++)
            {
                float t = i / xSize;
                float px = x1 + (x2 - x1) * t;
                float py = y1 + (y2 - y1) * t;
                renderer.DrawRect(px - xThickness / 2, py - xThickness / 2, xThickness, xThickness, CheckmarkColor);
            }

            // Línea diagonal de arriba-derecha a abajo-izquierda (/)
            float x3 = xCenterX + halfSize;
            float y3 = xCenterY - halfSize;
            float x4 = xCenterX - halfSize;
            float y4 = xCenterY + halfSize;

            // Dibujar línea /
            for (int i = 0; i <= (int)xSize; i++)
            {
                float t = i / xSize;
                float px = x3 + (x4 - x3) * t;
                float py = y3 + (y4 - y3) * t;
                renderer.DrawRect(px - xThickness / 2, py - xThickness / 2, xThickness, xThickness, CheckmarkColor);
            }
        }

        // Dibujar etiqueta
        if (!string.IsNullOrEmpty(Label))
        {
            float labelX = ComputedX + boxAreaWidth + LabelSpacing;
            var textSize = renderer.MeasureText(Label, FontSize);
            float labelY = ComputedY + (ComputedHeight - textSize.Y) / 2;

            renderer.DrawText(Label, labelX, labelY, LabelColor, FontSize);
        }
    }

    private float EstimateTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        float width = 0;
        foreach (var ch in text)
        {
            if (ch == ' ')
            {
                width += FontSize * 0.4f;
            }
            else if (!char.IsControl(ch))
            {
                width += FontSize * 0.6f;
            }
        }

        return width;
    }
}
