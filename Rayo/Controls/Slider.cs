namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Slider control for selecting numeric values.
/// Uses IInputHandler for drag interactions and IPointerHandler for hover.
/// </summary>
public class Slider : Rayo.Core.View<Slider>,
    IInputHandler,             // Drag interactions
    IPointerHandler            // Modern hover support
{

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Value
    [PaintProperty]
    public float Value
    {
        get => field;
        set
        {
            float newValue = Math.Clamp(value, MinValue, MaxValue);
            if (this.SetProperty(ref field, newValue))
            {
                ValueChanged?.Invoke(field);
                MarkNeedsPaint();
            }
        }
    } = 0;
    #endregion

    #region MinValue
    [PaintProperty]
    public float MinValue
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                Value = Math.Clamp(Value, field, MaxValue);
                MarkNeedsPaint();
            }
        }
    } = 0;
    #endregion

    #region MaxValue
    [PaintProperty]
    public float MaxValue
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                Value = Math.Clamp(Value, MinValue, field);
                MarkNeedsPaint();
            }
        }
    } = 100;
    #endregion

    #region TrackBackground
    [PaintProperty]
    public Brush TrackBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(60, 60, 60);
    #endregion

    #region TrackFillColor
    [PaintProperty]
    public Brush TrackFillColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region ThumbColor
    [PaintProperty]
    public Brush ThumbColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region ThumbHoverColor
    [PaintProperty]
    public Brush ThumbHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(220, 220, 220);
    #endregion

    #region TrackHeight
    [LayoutProperty]
    public float TrackHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 4;
    #endregion

    #region ThumbSize
    [LayoutProperty]
    public float ThumbSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 16;
    #endregion

    #region IsHovered
    [NotFluent]
    public new bool IsHovered { get; set; }
    #endregion
    
    #region IsDragging
    [NotFluent]
    public bool IsDragging { get; internal set; }
    #endregion

 
    public bool CanHandleInput => true;
    public bool WantsMouseCapture => IsDragging;



    // =========================================================================
    // EVENTS
    // =========================================================================
    public event Action<float>? ValueChanged;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public Slider()
    {
        Width = 200;
        Height = 30;
    }

    public Slider(float minValue, float maxValue, float value = 0) : this()
    {
        MinValue = minValue;
        MaxValue = maxValue;
        Value = Math.Clamp(value, minValue, maxValue);
    }

    public float GetNormalizedValue()
    {
        if (MaxValue == MinValue) return 0;
        return (Value - MinValue) / (MaxValue - MinValue);
    }

    public void NormalizedValue(float normalized)
    {
        Value = MinValue + (MaxValue - MinValue) * Math.Clamp(normalized, 0, 1);
    }

    public void ValueFromPosition(float mouseX)
    {
        float trackWidth = ComputedWidth - ThumbSize;
        if (trackWidth <= 0) return;

        float relativeX = mouseX - (ComputedX + ThumbSize / 2);
        float normalized = Math.Clamp(relativeX / trackWidth, 0, 1);
        NormalizedValue(normalized);
    }

    public bool HandleInput(InputEventArgs args)
    {
        switch (args.EventType)
        {
            case InputEventType.MouseDown:
                if (IsPointOnThumb(args.Position) || IsPointOnTrack(args.Position))
                {
                    IsDragging = true;
                    ValueFromPosition(args.Position.X);
                    return true;
                }
                break;

            case InputEventType.MouseDrag:
                if (IsDragging)
                {
                    ValueFromPosition(args.Position.X);
                    MarkNeedsPaint();
                    return true;
                }
                break;

            case InputEventType.MouseUp:
                if (IsDragging)
                {
                    IsDragging = false;
                    MarkNeedsPaint();
                    return true;
                }
                break;
        }

        return false;
    }

    public void OnFocusGained() { }

    public void OnFocusLost()
    {
        if (IsDragging)
        {
            IsDragging = false;
            MarkNeedsPaint();
        }
    }

    private bool IsPointOnThumb(System.Numerics.Vector2 position)
    {
        float thumbX = ComputedX + (ComputedWidth - ThumbSize) * GetNormalizedValue();
        float thumbY = ComputedY + (ComputedHeight - ThumbSize) / 2;
        return position.X >= thumbX && position.X <= thumbX + ThumbSize &&
               position.Y >= thumbY && position.Y <= thumbY + ThumbSize;
    }

    private bool IsPointOnTrack(System.Numerics.Vector2 position)
    {
        float trackY = ComputedY + (ComputedHeight - TrackHeight) / 2;
        return position.X >= ComputedX && position.X <= ComputedX + ComputedWidth &&
               position.Y >= trackY && position.Y <= trackY + TrackHeight;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (Width == 0) Width = 200;
        if (Height == 0) Height = Math.Max(TrackHeight, ThumbSize) + 10;

        DesiredWidth = Width;
        DesiredHeight = Height;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        float trackY = ComputedY + (ComputedHeight - TrackHeight) / 2;

        renderer.DrawRoundedRect(
            ComputedX, trackY, ComputedWidth, TrackHeight,
            TrackHeight / 2, TrackBackground
        );

        float fillWidth = (ComputedWidth - ThumbSize) * GetNormalizedValue() + ThumbSize / 2;
        if (fillWidth > 0)
        {
            renderer.DrawRoundedRect(
                ComputedX, trackY, fillWidth, TrackHeight,
                TrackHeight / 2, TrackFillColor
            );
        }

        float thumbX = ComputedX + (ComputedWidth - ThumbSize) * GetNormalizedValue();
        float thumbY = ComputedY + (ComputedHeight - ThumbSize) / 2;
        var thumbColor = (IsHovered || IsDragging) ? ThumbHoverColor : ThumbColor;

        renderer.DrawRoundedRect(
            thumbX, thumbY, ThumbSize, ThumbSize,
            ThumbSize / 2, thumbColor
        );
    }

    // =========================================================================
    // IPOINTERHANDLER IMPLEMENTATION (for hover support)
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
        }
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        // Handled by IInputHandler
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        // Handled by IInputHandler
    }
}
