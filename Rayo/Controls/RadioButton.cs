namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Radio button with label. Used in groups where only one can be selected.
/// Uses modern pointer event system:
/// - IPointerHandler for unified pointer events
/// - ITappable for tap gesture support
/// - IGestureRecognizerHost for gesture recognizers
/// </summary>
public class RadioButton : Rayo.Core.View<RadioButton>,
    IPointerHandler,           // Modern unified pointer events
    ITappable,                 // Tap gesture support
    IGestureRecognizerHost     // Hosts gesture recognizers
{
    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Label
    public string Label
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = "";
    #endregion

    #region GroupName
    public string GroupName
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = "default";
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(40, 40, 40);
    #endregion

    #region CheckedBackground
    public Brush CheckedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region HoverBackground
    public Brush HoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(50, 50, 50);
    #endregion

    #region DotColor
    public Brush DotColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region LabelColor
    public Brush LabelColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region CircleSize
    public float CircleSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 20;
    #endregion

    #region LabelSpacing
    public float LabelSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 10;
    #endregion

    #region BorderWidth
    [LayoutProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 2;
    #endregion

    #region CirclePadding
    public float CirclePadding
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 2;
    #endregion

    #region FontSize
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 14;
    #endregion

    #region IsChecked
    public bool IsChecked
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;

                if (value && Parent != null)
                {
                    UncheckSiblings();
                }

                Changed?.Invoke(value);
                MarkNeedsPaint();
            }
        }
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
    /// Fired when the radio button is tapped.
    /// </summary>
    public event Action<TapGestureEventArgs>? Tapped;

    #region Gesture Recognizer Host
    public List<IGestureRecognizer> GestureRecognizers { get; } = new();
    #endregion

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public RadioButton()
    {
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

    public RadioButton(string label, string groupName = "default") : this()
    {
        Label = label;
        GroupName = groupName;
    }

    // =========================================================================
    // POINTER EVENT HANDLERS
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
        IsChecked = true;
        Tapped?.Invoke(e);
    }

    // =========================================================================
    // VISUAL STATE MANAGEMENT
    // =========================================================================

    private void UpdateVisualState()
    {
        _currentBackground = IsPressed ? new Color(30, 30, 30) :
                           IsHovered ? HoverBackground :
                           Background;
    }

    private void UncheckSiblings()
    {
        if (Parent == null) return;

        // Use GetChildren() instead of Children property
        foreach (var sibling in Parent.GetChildren().ToArray())
        {
            if (sibling is RadioButton otherRadio &&
                otherRadio != this &&
                otherRadio.GroupName == this.GroupName &&
                otherRadio.IsChecked)
            {
                otherRadio.IsChecked = false;
            }
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float textWidth = EstimateTextWidth(Label);
        float circleAreaWidth = CircleSize + CirclePadding * 2;
        float contentWidth = circleAreaWidth;

        if (!string.IsNullOrEmpty(Label))
        {
            contentWidth += LabelSpacing + textWidth;
        }

        float circleAreaHeight = CircleSize + CirclePadding * 2;

        DesiredWidth = Width > 0 ? Width : contentWidth;
        DesiredHeight = Height > 0 ? Height : Math.Max(circleAreaHeight, FontSize);
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        float circleAreaWidth = CircleSize + CirclePadding * 2;
        float circleAreaHeight = CircleSize + CirclePadding * 2;
        float circleRadius = CircleSize / 2;
        float circleCenterX = ComputedX + CirclePadding + circleRadius;
        float circleAreaTop = ComputedY + (ComputedHeight - circleAreaHeight) / 2;
        float circleCenterY = circleAreaTop + CirclePadding + circleRadius;

        // Use DrawCircle so gradient brushes render correctly
        renderer.DrawCircle(circleCenterX, circleCenterY, circleRadius, _currentBackground);

        if (!IsChecked)
        {
            renderer.DrawCircleOutline(circleCenterX, circleCenterY, circleRadius, BorderWidth, BorderColor);
        }
        else
        {
            renderer.DrawCircle(circleCenterX, circleCenterY, circleRadius, CheckedBackground);
            float dotRadius = circleRadius * 0.5f;
            renderer.DrawCircle(circleCenterX, circleCenterY, dotRadius, DotColor);
        }

        if (!string.IsNullOrEmpty(Label))
        {
            float labelX = ComputedX + circleAreaWidth + LabelSpacing;
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
                width += FontSize * 0.4f;
            else if (!char.IsControl(ch))
                width += FontSize * 0.6f;
        }

        return width;
    }
}
