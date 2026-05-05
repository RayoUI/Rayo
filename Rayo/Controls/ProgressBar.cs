namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Progress bar component that shows progress of an operation.
/// Supports both determinate (0-100%) and indeterminate (animated) modes.
/// Migrated to new MAUI-like architecture: inherits from View<ProgressBar>
/// </summary>
public class ProgressBar : Rayo.Core.View<ProgressBar>
{
    private float _animationOffset = 0;
    private const float AnimationSpeed = 100f; // pixels per second

    #region ForegroundColor
    [PaintProperty]
    public Brush ForegroundColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 120, 215);
    #endregion

    #region BarHeight
    [LayoutProperty]
    public float BarHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 4;
    #endregion

    #region CornerRadius
    [PaintProperty]
    public float CornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 2;
    #endregion

    #region Value
    /// <summary>
    /// Current progress value (between Minimum and Maximum)
    /// </summary>
    [PaintProperty]
    public float Value
    {
        get => field;
        set => this.SetProperty(ref field, Math.Clamp(value, Minimum, Maximum));
    } = 0;
    #endregion

    #region Minimum
    /// <summary>
    /// Minimum value (default: 0)
    /// </summary>
    [PaintProperty]
    public float Minimum
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                Value = Math.Clamp(Value, field, Maximum);
                MarkNeedsPaint();
            }
        }
    } = 0;
    #endregion

    #region Maximum
    /// <summary>
    /// Maximum value (default: 100)
    /// </summary>
    [PaintProperty]
    public float Maximum
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                Value = Math.Clamp(Value, Minimum, field);
                MarkNeedsPaint();
            }
        }
    } = 100;
    #endregion

    #region IsIndeterminate
    /// <summary>
    /// If true, shows animated indeterminate progress (for unknown duration operations)
    /// </summary>
    [PaintProperty]
    public bool IsIndeterminate
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = false;
    #endregion


    public ProgressBar Range(float minimum, float maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
        return this;
    }

    public ProgressBar()
    {
        Background = new Color(200, 200, 200);
        Height = 4;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = Width;
        float measuredHeight = Height > 0 ? Height : BarHeight;

        if (Width <= 0)
        {
            // ProgressBar typically stretches horizontally
            measuredWidth = availableWidth;
        }

        DesiredWidth = measuredWidth;
        DesiredHeight = measuredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public void Update(float deltaTime)
    {
        if (IsIndeterminate)
        {
            _animationOffset += AnimationSpeed * deltaTime;

            // Reset when animation completes a cycle
            if (_animationOffset > ComputedWidth + 100)
            {
                _animationOffset = -100;
            }

            MarkNeedsPaint();
        }
    }

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX;
        float y = ComputedY;
        float width = ComputedWidth;
        float height = ComputedHeight;

        // Center the bar vertically if height is larger than BarHeight
        float barY = y;
        float barHeight = Math.Min(height, BarHeight);
        if (height > BarHeight)
        {
            barY = y + (height - BarHeight) / 2;
        }

        // Draw background
        if (CornerRadius > 0)
        {
            renderer.DrawRoundedRect(x, barY, width, barHeight, CornerRadius, Background);
        }
        else
        {
            renderer.DrawRect(x, barY, width, barHeight, Background);
        }

        // Draw foreground
        if (IsIndeterminate)
        {
            // Indeterminate: animated bar segment
            float segmentWidth = 100;
            float segmentX = x + _animationOffset;

            // Clamp to visible area
            if (segmentX < x)
            {
                segmentWidth -= (x - segmentX);
                segmentX = x;
            }
            if (segmentX + segmentWidth > x + width)
            {
                segmentWidth = x + width - segmentX;
            }

            if (segmentWidth > 0)
            {
                if (CornerRadius > 0)
                {
                    renderer.DrawRoundedRect(segmentX, barY, segmentWidth, barHeight, CornerRadius, ForegroundColor);
                }
                else
                {
                    renderer.DrawRect(segmentX, barY, segmentWidth, barHeight, ForegroundColor);
                }
            }
        }
        else
        {
            // Determinate: show progress based on value
            float progress = (Maximum - Minimum) != 0 ? (Value - Minimum) / (Maximum - Minimum) : 0;
            float fillWidth = width * progress;

            if (fillWidth > 0)
            {
                if (CornerRadius > 0)
                {
                    renderer.DrawRoundedRect(x, barY, fillWidth, barHeight, CornerRadius, ForegroundColor);
                }
                else
                {
                    renderer.DrawRect(x, barY, fillWidth, barHeight, ForegroundColor);
                }
            }
        }
    }
}
