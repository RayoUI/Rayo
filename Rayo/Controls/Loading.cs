namespace Rayo.Controls;

using Silk.NET.Core.Native;
using Rayo.Animation;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Loading spinner type
/// </summary>
public enum SpinnerType
{
    Circle,      // Rotating circle with gap
    Dots,        // Three pulsing dots
    Bars,        // Horizontal bars
    Ring         // Ring with rotating segment
}

/// <summary>
/// Loading spinner component - Animated loading indicator
/// </summary>
public class Loading : Rayo.Core.View<Loading>, IFrameAnimation
{
    private float _animationTime = 0;
    private bool _isAnimationRegistered;

    #region Type
    public SpinnerType Type
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = SpinnerType.Circle;
    #endregion

    #region Color
    public Brush Color
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 120, 215);
    #endregion

    #region StrokeWidth
    public float StrokeWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 4;
    #endregion

    #region AnimationSpeed
    public float AnimationSpeed
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 2f;
    #endregion

    #region Text
    public string? Text
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region TextColor
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(100, 100, 100);
    #endregion

    #region TextSize
    public float TextSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 14;
    #endregion

    public Loading()
    {
        Size(40f);
    }

    private void AdvanceAnimation(float deltaTime)
    {
        _animationTime += deltaTime * AnimationSpeed;
        if (_animationTime > Math.PI * 2)
        {
            _animationTime -= (float)Math.PI * 2;
        }
        if (!IsVisible)
        {
            return; // Skip invalidation when the spinner is hidden to avoid unnecessary redraws
        }

        MarkNeedsPaint();
        UIApplication.Current?.Tree.MarkNeedsRender();
    }

    protected override void OnMounted()
    {
        base.OnMounted();
        RegisterForAnimation();
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
        AdvanceAnimation(deltaTime);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width > 0 ? Width : 40f;
        float measuredHeight = Height > 0 ? Height : measuredWidth;

        if (!string.IsNullOrEmpty(Text))
        {
            measuredWidth = Math.Max(measuredWidth, EstimateTextWidth(Text, TextSize));
            measuredHeight += TextSize + TextSpacing;
        }

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX;
        float y = ComputedY;
        float centerX = x + ComputedWidth / 2;
        float centerY = y + Width / 2;

        switch (Type)
        {
            case SpinnerType.Circle:
                RenderCircleSpinner(renderer, centerX, centerY);
                break;

            case SpinnerType.Dots:
                RenderDotsSpinner(renderer, centerX, centerY);
                break;

            case SpinnerType.Bars:
                RenderBarsSpinner(renderer, centerX, centerY);
                break;

            case SpinnerType.Ring:
                RenderRingSpinner(renderer, centerX, centerY);
                break;
        }

        // Render text if present
        if (!string.IsNullOrEmpty(Text))
        {
            var textSize = renderer.MeasureText(Text, TextSize);
            float textX = centerX - textSize.X / 2f;
            float textY = y + Width + TextSpacing;
            renderer.DrawText(Text, textX, textY, TextColor, TextSize);
        }
    }

    private const float TextSpacing = 8f;

    private static float EstimateTextWidth(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        float width = 0f;
        foreach (char ch in text)
        {
            if (ch == ' ')
            {
                width += fontSize * 0.4f;
            }
            else if (char.IsControl(ch))
            {
                continue;
            }
            else
            {
                width += fontSize * 0.6f;
            }
        }

        return width;
    }

    private void RenderCircleSpinner(IRenderer renderer, float centerX, float centerY)
    {
        float radius = Width / 2 - StrokeWidth;

        // Draw rotating arc (simplified as multiple circles for now)
        // In a real implementation, this would draw an arc
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle = _animationTime + (i * (float)Math.PI * 2 / segments);
            float alpha = (i / (float)segments) * 255;

            float px = centerX + (float)Math.Cos(angle) * radius;
            float py = centerY + (float)Math.Sin(angle) * radius;

            Color dotColor = new Color(Color.PrimaryColor.R, Color.PrimaryColor.G, Color.PrimaryColor.B, (byte)alpha);
            renderer.DrawCircle(px, py, StrokeWidth / 2, dotColor);
        }
    }

    private void RenderDotsSpinner(IRenderer renderer, float centerX, float centerY)
    {
        float dotSize = Width / 8;
        float spacing = Width / 4;

        for (int i = 0; i < 3; i++)
        {
            float dotX = centerX - spacing + (i * spacing);
            float phase = _animationTime + (i * (float)Math.PI * 2 / 3);
            float scale = 0.5f + 0.5f * (float)Math.Sin(phase);

            float currentDotSize = dotSize * (0.5f + scale * 0.5f);
            byte alpha = (byte)(100 + 155 * scale);

            Color dotColor = new Color(Color.PrimaryColor.R, Color.PrimaryColor.G, Color.PrimaryColor.B, alpha);
            renderer.DrawCircle(dotX, centerY, currentDotSize, dotColor);
        }
    }

    private void RenderBarsSpinner(IRenderer renderer, float centerX, float centerY)
    {
        float barWidth = StrokeWidth;
        float spacing = barWidth * 2;
        int barCount = 5;

        for (int i = 0; i < barCount; i++)
        {
            float barX = centerX - (barCount - 1) * spacing / 2 + (i * spacing);
            float phase = _animationTime + (i * (float)Math.PI * 2 / barCount);
            float scale = 0.3f + 0.7f * (float)Math.Abs(Math.Sin(phase));

            float barHeight = Width * scale;
            float barY = centerY - barHeight / 2;

            renderer.DrawRect(barX - barWidth / 2, barY, barWidth, barHeight, Color);
        }
    }

    private void RenderRingSpinner(IRenderer renderer, float centerX, float centerY)
    {
        float radius = Width / 2 - StrokeWidth / 2;

        // Draw background ring
        Color bgColor = new Color(Color.PrimaryColor.R, Color.PrimaryColor.G, Color.PrimaryColor.B, 50);
        renderer.DrawCircleOutline(centerX, centerY, radius, StrokeWidth, bgColor);

        // Draw rotating segment (simplified as a circle for now)
        float angle = _animationTime;
        float segmentX = centerX + (float)Math.Cos(angle) * radius;
        float segmentY = centerY + (float)Math.Sin(angle) * radius;

        renderer.DrawCircle(segmentX, segmentY, StrokeWidth, Color);
    }
}

/// <summary>
/// Loading overlay that covers content with a spinner
/// </summary>
public class LoadingOverlay : Frame
{
    private Loading? _spinner;
    public bool IsLoading { get; private set; }

    public LoadingOverlay()
    {
        // Semi-transparent backdrop
        this.Background(new Color(255, 255, 255, 200));
    }

    public LoadingOverlay Show(string? text = null, SpinnerType type = SpinnerType.Circle)
    {
        IsLoading = true;

        ClearContent();

        // Create spinner
        _spinner = new Loading()
            .Type(type)
            .Size(new Size(50));

        if (!string.IsNullOrEmpty(text))
        {
            _spinner.Text(text);
        }

        // Center the spinner
        var container = new VStack()
            .Alignment(Alignment.Center)
            .Spacing(0)
            .Children(_spinner);

        this.Content(container);
        MarkNeedsLayout();
        return this;
    }

    public LoadingOverlay Hide()
    {
        IsLoading = false;
        ClearContent();
        MarkNeedsLayout();
        return this;
    }

    public void Update(float deltaTime)
    {
        if (IsLoading && _spinner != null)
        {
            if (_spinner is IFrameAnimation anim)
            {
                anim.Tick(deltaTime);
            }
        }
    }
}
