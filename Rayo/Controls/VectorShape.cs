using System;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using IRenderer = Rayo.Rendering.IRenderer;
using VectorPath = Rayo.Rendering.Graphics.VectorGraphics.VectorPath;
using PathCommandType = Rayo.Rendering.Graphics.VectorGraphics.PathCommandType;

namespace Rayo.Controls;

/// <summary>
/// Renders arbitrary vector paths with optional fill and stroke settings.
/// </summary>
public class VectorShape : Rayo.Core.View<VectorShape>
{
    #region Path
    public VectorPath? Path
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region FillColor
    [PaintProperty]
    public Brush FillColor
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                Fill = value.PrimaryColor.A > 0;
            }
        }
    } = Color.Transparent;
    #endregion

    #region StrokeColor
    public Brush StrokeColor
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                Stroke = value.PrimaryColor.A > 0;
                MarkNeedsPaint();
            }
        }
    } = Color.Black;
    #endregion

    #region StrokeWidth
    public float StrokeWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 1f;
    #endregion

    #region Fill
    [PaintProperty]
    public bool Fill
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    #region Stroke
    public bool Stroke
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;
    #endregion

    public VectorShape()
    {
        Width = 100;
        Height = 100;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (Width == 0)
        {
            Width = availableWidth;
        }

        if (Height == 0)
        {
            Height = availableHeight;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        if (Path == null)
        {
            return;
        }

        var translatedPath = TranslatePath(Path, ComputedX, ComputedY);

        if (Fill && Stroke)
        {
            renderer.DrawPathFillAndStroke(translatedPath, FillColor, StrokeColor, StrokeWidth);
        }
        else if (Fill)
        {
            renderer.DrawPath(translatedPath, FillColor);
        }
        else if (Stroke)
        {
            renderer.DrawPathStroke(translatedPath, StrokeColor, StrokeWidth);
        }
    }

    private static VectorPath TranslatePath(VectorPath path, float offsetX, float offsetY)
    {
        var newPath = new VectorPath();

        foreach (var cmd in path.Commands)
        {
            switch (cmd.Type)
            {
                case PathCommandType.MoveTo:
                    newPath.MoveTo(cmd.Point.X + offsetX, cmd.Point.Y + offsetY);
                    break;
                case PathCommandType.LineTo:
                    newPath.LineTo(cmd.Point.X + offsetX, cmd.Point.Y + offsetY);
                    break;
                case PathCommandType.QuadraticBezierTo:
                    newPath.QuadraticBezierTo(
                        cmd.ControlPoint1.X + offsetX,
                        cmd.ControlPoint1.Y + offsetY,
                        cmd.Point.X + offsetX,
                        cmd.Point.Y + offsetY);
                    break;
                case PathCommandType.CubicBezierTo:
                    newPath.CubicBezierTo(
                        cmd.ControlPoint1.X + offsetX,
                        cmd.ControlPoint1.Y + offsetY,
                        cmd.ControlPoint2.X + offsetX,
                        cmd.ControlPoint2.Y + offsetY,
                        cmd.Point.X + offsetX,
                        cmd.Point.Y + offsetY);
                    break;
                case PathCommandType.ArcTo:
                    newPath.ArcTo(
                        cmd.Point.X + offsetX,
                        cmd.Point.Y + offsetY,
                        cmd.Radius,
                        cmd.StartAngle,
                        cmd.SweepAngle,
                        cmd.LargeArc,
                        cmd.Clockwise);
                    break;
                case PathCommandType.Close:
                    newPath.Close();
                    break;
            }
        }

        return newPath;
    }
}

/// <summary>
/// Helper component for building common shapes.
/// </summary>
public class Shape : VectorShape
{
    public enum ShapeKind
    {
        Rectangle,
        RoundedRectangle,
        Circle,
        Ellipse,
        RegularPolygon,
        Star
    }

    private ShapeKind _shapeType;
    private float _cornerRadius;
    private int _sides = 5;
    private float _innerRadius = 0.5f;

    public Shape(ShapeKind type = ShapeKind.Rectangle)
    {
        _shapeType = type;
        UpdatePath();
    }

    public ShapeKind ShapeType
    {
        get => _shapeType;
        set
        {
            if (_shapeType == value)
            {
                return;
            }

            _shapeType = value;
            UpdatePath();
        }
    }

    public float CornerRadius
    {
        get => _cornerRadius;
        set
        {
            if (Math.Abs(_cornerRadius - value) < float.Epsilon)
            {
                return;
            }

            _cornerRadius = value;
            UpdatePath();
        }
    }

    public int Sides
    {
        get => _sides;
        set
        {
            var clamped = Math.Max(3, value);
            if (_sides == clamped)
            {
                return;
            }

            _sides = clamped;
            UpdatePath();
        }
    }

    public float InnerRadius
    {
        get => _innerRadius;
        set
        {
            var clamped = Math.Clamp(value, 0.1f, 0.9f);
            if (Math.Abs(_innerRadius - clamped) < float.Epsilon)
            {
                return;
            }

            _innerRadius = clamped;
            UpdatePath();
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        UpdatePath();
    }

    private void UpdatePath()
    {
        if (ComputedWidth <= 0 || ComputedHeight <= 0)
        {
            return;
        }

        Path = _shapeType switch
        {
            ShapeKind.Rectangle => VectorPath.Rectangle(0, 0, ComputedWidth, ComputedHeight),
            ShapeKind.RoundedRectangle => VectorPath.RoundedRectangle(0, 0, ComputedWidth, ComputedHeight, _cornerRadius),
            ShapeKind.Circle => VectorPath.Circle(ComputedWidth / 2, ComputedHeight / 2, Math.Min(ComputedWidth, ComputedHeight) / 2),
            ShapeKind.Ellipse => VectorPath.Ellipse(ComputedWidth / 2, ComputedHeight / 2, ComputedWidth / 2, ComputedHeight / 2),
            ShapeKind.RegularPolygon => VectorPath.RegularPolygon(ComputedWidth / 2, ComputedHeight / 2, Math.Min(ComputedWidth, ComputedHeight) / 2, _sides),
            ShapeKind.Star => VectorPath.Star(
                ComputedWidth / 2,
                ComputedHeight / 2,
                Math.Min(ComputedWidth, ComputedHeight) / 2,
                Math.Min(ComputedWidth, ComputedHeight) / 2 * _innerRadius,
                _sides),
            _ => VectorPath.Rectangle(0, 0, ComputedWidth, ComputedHeight)
        };

        MarkNeedsPaint();
    }
}
