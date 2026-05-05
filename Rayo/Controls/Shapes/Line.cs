namespace Rayo.Controls.Shapes;

using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// Line shape control - draws a straight line between two points
/// </summary>
public class Line : Shape<Line>
{
    private float _x1 = 0;
    private float _y1 = 0;
    private float _x2 = 100;
    private float _y2 = 100;

    public float X1
    {
        get => _x1;
        set
        {
            if (_x1 != value)
            {
                _x1 = value;
                UpdateBounds();
                MarkNeedsPaint();
            }
        }
    }

    public float Y1
    {
        get => _y1;
        set
        {
            if (_y1 != value)
            {
                _y1 = value;
                UpdateBounds();
                MarkNeedsPaint();
            }
        }
    }

    public float X2
    {
        get => _x2;
        set
        {
            if (_x2 != value)
            {
                _x2 = value;
                UpdateBounds();
                MarkNeedsPaint();
            }
        }
    }

    public float Y2
    {
        get => _y2;
        set
        {
            if (_y2 != value)
            {
                _y2 = value;
                UpdateBounds();
                MarkNeedsPaint();
            }
        }
    }

    public Line()
    {
        Stroke = Color.Black;
        StrokeThickness = 1;
        UpdateBounds();
    }

    public Line(float x1, float y1, float x2, float y2)
    {
        _x1 = x1;
        _y1 = y1;
        _x2 = x2;
        _y2 = y2;
        Stroke = Color.Black;
        StrokeThickness = 1;
        UpdateBounds();
    }

    public Line Points(float x1, float y1, float x2, float y2)
    {
        _x1 = x1;
        _y1 = y1;
        _x2 = x2;
        _y2 = y2;
        UpdateBounds();
        MarkNeedsPaint();
        return this;
    }

    private void UpdateBounds()
    {
        float minX = Math.Min(_x1, _x2);
        float maxX = Math.Max(_x1, _x2);
        float minY = Math.Min(_y1, _y2);
        float maxY = Math.Max(_y1, _y2);

        if (Width <= 0)
            Width = maxX - minX + StrokeThickness;
        if (Height <= 0)
            Height = maxY - minY + StrokeThickness;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float minX = Math.Min(_x1, _x2);
        float maxX = Math.Max(_x1, _x2);
        float minY = Math.Min(_y1, _y2);
        float maxY = Math.Max(_y1, _y2);

        DesiredWidth = Width > 0 ? Width : Math.Max(maxX - minX + StrokeThickness, StrokeThickness);
        DesiredHeight = Height > 0 ? Height : Math.Max(maxY - minY + StrokeThickness, StrokeThickness);
    }

    public override void Render(IRenderer renderer)
    {
        if (Stroke.PrimaryColor.A == 0 || StrokeThickness <= 0)
            return;

        float baseX = ComputedX;
        float baseY = ComputedY;

        // Calculate actual line endpoints relative to computed position
        float startX = baseX + _x1;
        float startY = baseY + _y1;
        float endX = baseX + _x2;
        float endY = baseY + _y2;

        // Draw line as a thin rectangle rotated along the line direction
        DrawLine(renderer, startX, startY, endX, endY);
    }

    private void DrawLine(IRenderer renderer, float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = MathF.Sqrt(dx * dx + dy * dy);

        if (length < 0.001f) return;

        // Normalize direction
        float nx = dx / length;
        float ny = dy / length;

        // Perpendicular direction for thickness
        float px = -ny * StrokeThickness / 2;
        float py = nx * StrokeThickness / 2;

        // Create quad vertices
        var points = new List<(float, float)>
        {
            (x1 + px, y1 + py),
            (x2 + px, y2 + py),
            (x2 - px, y2 - py),
            (x1 - px, y1 - py)
        };

        renderer.DrawPolygon(points, Stroke.PrimaryColor);
    }
}
