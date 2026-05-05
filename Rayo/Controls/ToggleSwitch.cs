namespace Rayo.Controls;

using Rayo.Animation;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Toggle switch component for ON/OFF states.
/// Uses modern pointer event system:
/// - IPointerHandler for unified pointer events
/// - ITappable for tap gesture support
/// - IGestureRecognizerHost for gesture recognizers
/// </summary>
public class ToggleSwitch : Rayo.Core.View<ToggleSwitch>,
    IPointerHandler,           // Modern unified pointer events
    ITappable,                 // Tap gesture support
    IGestureRecognizerHost,    // Hosts gesture recognizers
    IFrameAnimation            // Per-frame animation updates
{
    private float _animationProgress = 0;
    private float _animationStartProgress = 0;
    private float _animationElapsed = 0f;
    private float _animationTargetProgress = 0f;
    private bool _isAnimating;
    private bool _isAnimationRegistered;
    private bool _hasBeenRendered = false;
    private const float AnimationDuration = 0.2f;


    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region OnColor
    [PaintProperty]
    public Brush OnColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 120, 215);
    #endregion

    #region OffColor
    [PaintProperty]
    public Brush OffColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region OnBorderColor
    [PaintProperty]
    public Brush OnBorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 120, 215);
    #endregion

    #region OffBorderColor
    [PaintProperty]
    public Brush OffBorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(150, 150, 150);
    #endregion

    #region ThumbColor
    [PaintProperty]
    public Brush ThumbColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(240, 240, 240);
    #endregion

    #region SwitchWidth
    [LayoutProperty]
    public float SwitchWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 50;
    #endregion

    #region SwitchHeight
    [LayoutProperty]
    public float SwitchHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 26;
    #endregion

    #region ThumbSize
    [LayoutProperty]
    public float ThumbSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 20;
    #endregion

    #region BorderThickness
    [PaintProperty]
    public float BorderThickness
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 2f;
    #endregion

    #region IsOn
    [PaintProperty]
    public bool IsOn
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;

                if (!_hasBeenRendered)
                {
                    _animationProgress = field ? 1f : 0f;
                    MarkNeedsPaint();
                }
                else
                {
                    _animationStartProgress = _animationProgress;
                    _animationTargetProgress = field ? 1f : 0f;
                    _animationElapsed = 0f;
                    _isAnimating = true;
                    RegisterForAnimation();
                    MarkNeedsPaint();
                }

                Toggled?.Invoke(field);
            }
        }
    }
    #endregion

    #region IsHovered
    [PaintProperty]
    public new bool IsHovered
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region IsPressed
    [PaintProperty]
    public new bool IsPressed
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    private readonly TapRecognizer _tapRecognizer;

    // =========================================================================
    // EVENTS
    // =========================================================================
    public event Action<bool>? Toggled;

    /// <summary>
    /// Fired when the toggle switch is tapped.
    /// </summary>
    public event Action<TapGestureEventArgs>? Tapped;

    #region Gesture Recognizer Host
    public List<IGestureRecognizer> GestureRecognizers { get; } = new();
    #endregion

    // =========================================================================
    // INITIALIZATION
    // =========================================================================
    public ToggleSwitch()
    {
        Width = 50;
        Height = 26;

        // Setup gesture recognizers
        _tapRecognizer = new TapRecognizer(
            maxMovementThreshold: 15f,
            maxPressDurationMs: 500,
            doubleTapWindowMs: 300
        );
        _tapRecognizer.TapDetected += OnTapDetected;
        GestureRecognizers.Add(_tapRecognizer);
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
        IsOn = !IsOn;
        Tapped?.Invoke(e);
    }

    protected override void OnUnmounted()
    {
        base.OnUnmounted();
        UnregisterFromAnimation();
    }

    private void RegisterForAnimation()
    {
        if (_isAnimationRegistered)
        {
            return;
        }

        _isAnimationRegistered = true;
        FrameAnimationTicker.Register(this);
    }

    private void UnregisterFromAnimation()
    {
        if (!_isAnimationRegistered)
        {
            return;
        }

        _isAnimationRegistered = false;
        FrameAnimationTicker.Unregister(this);
    }

    void IFrameAnimation.Tick(float deltaTime)
    {
        if (!_isAnimating)
        {
            return;
        }

        _animationElapsed += deltaTime;
        float normalizedTime = Math.Clamp(_animationElapsed / AnimationDuration, 0f, 1f);
        _animationProgress = _animationStartProgress + (_animationTargetProgress - _animationStartProgress) * normalizedTime;
        MarkNeedsPaint();
        UIApplication.Current?.Tree.MarkNeedsRender();

        if (normalizedTime >= 1f)
        {
            _isAnimating = false;
            _animationProgress = _animationTargetProgress;
            UnregisterFromAnimation();
        }
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = Width > 0 ? Width : SwitchWidth;
        DesiredHeight = Height > 0 ? Height : SwitchHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        if (!_hasBeenRendered)
        {
            _animationProgress = IsOn ? 1f : 0f;
            _hasBeenRendered = true;
        }

        float x = ComputedX;
        float y = ComputedY;
        float width = ComputedWidth;
        float height = ComputedHeight;

        Brush backgroundColor = InterpolateColor(OffColor, OnColor, _animationProgress);
        Brush borderColor = InterpolateColor(OffBorderColor, OnBorderColor, _animationProgress);

        float trackRadius = height / 2;
        renderer.DrawRoundedRect(x, y, width, height, trackRadius, backgroundColor);

        if (BorderThickness > 0)
        {
            renderer.DrawRoundedRectOutline(x, y, width, height, trackRadius, BorderThickness, borderColor);
        }

        float thumbRadius = ThumbSize / 2;
        float thumbY = y + height / 2;
        float thumbStartX = x + thumbRadius + 3;
        float thumbEndX = x + width - thumbRadius - 3;
        float thumbX = thumbStartX + (thumbEndX - thumbStartX) * _animationProgress;

        renderer.DrawCircle(thumbX, thumbY + 1, thumbRadius, new Color(0, 0, 0, 50));
        renderer.DrawCircle(thumbX, thumbY, thumbRadius, ThumbColor);
    }

    private Brush InterpolateColor(Brush from, Brush to, float t)
    {
        // Gradient brushes can't be arithmetically interpolated — snap to the nearest state
        if (from.IsGradient || to.IsGradient)
            return t >= 0.5f ? to : from;

        var f = from.PrimaryColor;
        var e = to.PrimaryColor;
        return new Color(
            f.R + (e.R - f.R) * t,
            f.G + (e.G - f.G) * t,
            f.B + (e.B - f.B) * t,
            f.A + (e.A - f.A) * t
        );
    }
}
