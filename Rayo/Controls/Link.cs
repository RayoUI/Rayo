namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.Core.Platform;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Inline text link with hover, pressed, visited, and tap support.
/// </summary>
public class Link : View<Link>,
    IPointerHandler,
    ITappable,
    IGestureRecognizerHost
{
    private readonly TapRecognizer _tapRecognizer;

    #region Text
    [LayoutProperty]
    public string Text
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = string.Empty;
    #endregion

    #region Url
    [PaintProperty]
    public string? Url
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
    } = 14f;
    #endregion

    #region NormalColor
    [PaintProperty]
    public Brush NormalColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region HoverColor
    [PaintProperty]
    public Brush HoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region PressedColor
    [PaintProperty]
    public Brush PressedColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region VisitedColor
    [PaintProperty]
    public Brush VisitedColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region IsVisited
    [PaintProperty]
    public bool IsVisited
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Underline
    [PaintProperty]
    public bool Underline
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    #region UnderlineOnHoverOnly
    [PaintProperty]
    public bool UnderlineOnHoverOnly
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region OpenUrlOnTap
    [PaintProperty]
    public bool OpenUrlOnTap
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    #region TextHorizontalAlignment
    [LayoutProperty]
    public HorizontalAlignment TextHorizontalAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = HorizontalAlignment.Left;
    #endregion

    public event Action<TapGestureEventArgs>? Tapped;
    public event Action<Link>? Activated;

    public List<IGestureRecognizer> GestureRecognizers { get; } = new();

    public Link()
    {
        InitializeTheme();
        Padding = new Thickness(0);
        Cursor = CursorShape.Hand;

        _tapRecognizer = new TapRecognizer(
            maxMovementThreshold: 15f,
            maxPressDurationMs: 500,
            doubleTapWindowMs: 300);
        _tapRecognizer.TapDetected += OnTapDetected;
        GestureRecognizers.Add(_tapRecognizer);
    }

    protected override void OnThemeApplied(Theme theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(NormalColor), (Brush)palette.Primary, value => NormalColor = value);
        SetThemeValue(nameof(HoverColor), (Brush)palette.PrimaryHover, value => HoverColor = value);
        SetThemeValue(nameof(PressedColor), (Brush)palette.PrimaryPressed, value => PressedColor = value);
        SetThemeValue(nameof(VisitedColor), (Brush)palette.Info, value => VisitedColor = value);
    }

    public Link(string text) : this()
    {
        Text = text;
    }

    public Link(string text, string url) : this(text)
    {
        Url = url;
    }

    public Link OnActivated(Action<Link> handler)
    {
        Activated += handler;
        return this;
    }

    public Link OnTapped(Action handler)
    {
        Tapped += _ => handler();
        return this;
    }

    public Link OnTapped(Action<TapGestureEventArgs> handler)
    {
        Tapped += handler;
        return this;
    }

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
            IsPressed = false;
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

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float textWidth = EstimateTextWidth(Text);
        float textHeight = FontSize * 1.35f;

        DesiredWidth = Width > 0 ? Width : textWidth + Padding.Horizontal;
        DesiredHeight = Height > 0 ? Height : textHeight + Padding.Vertical;
    }

    public override void Render(IRenderer renderer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        float maxTextWidth = Math.Max(0, ComputedWidth - Padding.Horizontal);
        string displayText = renderer.TruncateTextToFit(Text, maxTextWidth, FontSize);
        var textSize = renderer.MeasureText(displayText, FontSize);
        Brush color = GetCurrentColor();

        float textX = TextHorizontalAlignment switch
        {
            HorizontalAlignment.Center => ComputedX + Padding.Left + (maxTextWidth - textSize.X) / 2f,
            HorizontalAlignment.Right => ComputedX + ComputedWidth - Padding.Right - textSize.X,
            _ => ComputedX + Padding.Left
        };
        float textY = ComputedY + Padding.Top + Math.Max(0, ComputedHeight - Padding.Vertical - textSize.Y) / 2f;

        renderer.DrawText(displayText, textX, textY, color, FontSize);

        bool drawUnderline = Underline && (!UnderlineOnHoverOnly || IsHovered || IsPressed);
        if (drawUnderline && textSize.X > 0)
        {
            float thickness = Math.Max(1f, FontSize * 0.07f);
            float underlineY = textY + FontSize + thickness;
            renderer.DrawRect(textX, underlineY, textSize.X, thickness, color.PrimaryColor);
        }
    }

    private void OnTapDetected(TapGestureEventArgs e)
    {
        IsVisited = true;
        Activated?.Invoke(this);
        Tapped?.Invoke(e);

        if (OpenUrlOnTap && !string.IsNullOrWhiteSpace(Url))
        {
            UrlLauncher.Open(Url);
        }
    }

    private Brush GetCurrentColor()
    {
        if (IsPressed)
        {
            return PressedColor;
        }

        if (IsHovered)
        {
            return HoverColor;
        }

        return IsVisited ? VisitedColor : NormalColor;
    }

    private float EstimateTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        float width = 0;
        foreach (var ch in text)
        {
            width += ch == ' ' ? FontSize * 0.4f : FontSize * 0.6f;
        }

        return width;
    }
}
