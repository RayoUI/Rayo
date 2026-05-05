namespace Rayo.Controls.Shapes;

using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// Polyline shape control - draws an open path from a series of connected points
/// Unlike Polygon, the path is not closed
/// </summary>
public class Polyline : Shape<Polyline>
{
    //private List<Vector2> Points = new();
    private float _minX, _minY, _maxX, _maxY;

    //public IReadOnlyList<Vector2> Points => Points;

    [PaintProperty]
    public List<Vector2> Points
    {
        get => field;
        set => this.SetProperty(ref field, value ?? [], UpdateBounds);
    } = [];

    public Polyline()
    {
        Fill = Color.Transparent;
        Stroke = Color.Black;
        StrokeThickness = 1;
    }

    public Polyline(params Vector2[] points) : this()
    {
        Points = points.ToList();
    }

    //public Polyline Points(params Vector2[] points)
    //{
    //    Points = new List<Vector2>(points);
    //    UpdateBounds();
    //    MarkNeedsPaint();
    //    return this;
    //}

    //public Polyline Points(IEnumerable<(float x, float y)> points)
    //{
    //    Points = points.Select(p => new Vector2(p.x, p.y)).ToList();
    //    UpdateBounds();
    //    MarkNeedsPaint();
    //    return this;
    //}

    public Polyline AddPoint(float x, float y)
    {
        Points.Add(new Vector2(x, y));
        UpdateBounds();
        MarkNeedsPaint();
        return this;
    }

    public Polyline ClearPoints()
    {
        Points.Clear();
        UpdateBounds();
        MarkNeedsPaint();
        return this;
    }

    private void UpdateBounds()
    {
        if (Points.Count == 0)
        {
            _minX = _minY = _maxX = _maxY = 0;
            return;
        }

        _minX = Points.Min(p => p.X);
        _maxX = Points.Max(p => p.X);
        _minY = Points.Min(p => p.Y);
        _maxY = Points.Max(p => p.Y);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (Width > 0)
            DesiredWidth = Width;
        else
            DesiredWidth = _maxX - _minX + StrokeThickness;

        if (Height > 0)
            DesiredHeight = Height;
        else
            DesiredHeight = _maxY - _minY + StrokeThickness;
    }

    public override void Render(IRenderer renderer)
    {
        if (Points.Count < 2) return;

        // Offset by half stroke thickness to ensure stroke is fully visible
        float strokeOffset = StrokeThickness / 2;
        float baseX = ComputedX - _minX + strokeOffset;
        float baseY = ComputedY - _minY + strokeOffset;

        var adjustedPoints = Points
            .Select(p => (baseX + p.X, baseY + p.Y))
            .ToList();

        // Polyline doesn't have a fill (it's not closed)
        // But if fill is specified, draw a filled polygon (auto-close)
        if (Fill.PrimaryColor.A > 0)
        {
            renderer.DrawPolygon(adjustedPoints, Fill.PrimaryColor);
        }

        // Draw stroke (open path)
        if (Stroke.PrimaryColor.A > 0 && StrokeThickness > 0)
        {
            var strokeColor = Stroke.PrimaryColor;
            for (int i = 0; i < adjustedPoints.Count - 1; i++)
            {
                var p1 = adjustedPoints[i];
                var p2 = adjustedPoints[i + 1];
                DrawLineSegment(renderer, p1.Item1, p1.Item2, p2.Item1, p2.Item2, strokeColor);
            }
        }
    }

    private void DrawLineSegment(IRenderer renderer, float x1, float y1, float x2, float y2, Color color)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = MathF.Sqrt(dx * dx + dy * dy);

        if (length < 0.001f) return;

        float nx = dx / length;
        float ny = dy / length;

        float px = -ny * StrokeThickness / 2;
        float py = nx * StrokeThickness / 2;

        var linePoints = new List<(float, float)>
        {
            (x1 + px, y1 + py),
            (x2 + px, y2 + py),
            (x2 - px, y2 - py),
            (x1 - px, y1 - py)
        };

        renderer.DrawPolygon(linePoints, color);
    }
}
