namespace Rayo.Controls.Shapes;

using Rayo.Reactivity;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// Polygon shape control - draws a closed polygon from a series of points
/// </summary>
public class Polygon : Shape<Polygon>
{
    //private List<Vector2> Points = new();
    private float _minX, _minY, _maxX, _maxY;
    //private FillRule _fillRule = FillRule.EvenOdd;

    //public IReadOnlyList<Vector2> Points => Points;

    [PaintProperty]
    public List<Vector2> Points
    {
        get => field;
        set => this.SetProperty(ref field, value ?? [], UpdateBounds);
    } = [];

    [PaintProperty]
    public FillRule FillRule
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = FillRule.EvenOdd;

    public Polygon()
    {
        Fill = Color.Transparent;
        Stroke = Color.Black;
        StrokeThickness = 1;
    }

    public Polygon(params Vector2[] points): this()
    {
        Points = points.ToList();
    }


    public Polygon AddPoint(float x, float y)
    {
        Points.Add(new Vector2(x, y));
        UpdateBounds();
        MarkNeedsPaint();
        return this;
    }

    public Polygon ClearPoints()
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
        if (Points.Count < 3) return;

        // Offset by half stroke thickness to ensure stroke is fully visible
        float strokeOffset = StrokeThickness / 2;
        float baseX = ComputedX - _minX + strokeOffset;
        float baseY = ComputedY - _minY + strokeOffset;

        var adjustedPoints = Points
            .Select(p => (baseX + p.X, baseY + p.Y))
            .ToList();

        // Draw fill
        if (Fill.PrimaryColor.A > 0)
        {
            renderer.DrawPolygon(adjustedPoints, Fill.PrimaryColor);
        }

        // Draw stroke
        if (Stroke.PrimaryColor.A > 0 && StrokeThickness > 0)
        {
            DrawPolygonStroke(renderer, adjustedPoints);
        }
    }

    private void DrawPolygonStroke(IRenderer renderer, List<(float, float)> points)
    {
        var strokeColor = Stroke.PrimaryColor;
        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];
            DrawLineSegment(renderer, p1.Item1, p1.Item2, p2.Item1, p2.Item2, strokeColor);
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

    // Factory methods for common polygons

    /// <summary>
    /// Creates a regular polygon with the specified number of sides
    /// </summary>
    public static Polygon Regular(int sides, float radius, float centerX = 0, float centerY = 0)
    {
        var polygon = new Polygon();
        var points = new Vector2[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = 2 * MathF.PI * i / sides - MathF.PI / 2;
            points[i] = new Vector2(
                centerX + radius * MathF.Cos(angle),
                centerY + radius * MathF.Sin(angle)
            );
        }

        return polygon.Points(points);
    }

    /// <summary>
    /// Creates a triangle
    /// </summary>
    public static Polygon Triangle(float width, float height)
    {
        return new Polygon(
            new Vector2(width / 2, 0),
            new Vector2(width, height),
            new Vector2(0, height)
        );
    }

    /// <summary>
    /// Creates a star shape
    /// </summary>
    public static Polygon Star(int points, float outerRadius, float innerRadius, float centerX = 0, float centerY = 0)
    {
        var polygon = new Polygon();
        var starPoints = new Vector2[points * 2];

        for (int i = 0; i < points * 2; i++)
        {
            float angle = MathF.PI * i / points - MathF.PI / 2;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;
            starPoints[i] = new Vector2(
                centerX + radius * MathF.Cos(angle),
                centerY + radius * MathF.Sin(angle)
            );
        }

        polygon.Points = starPoints.ToList();

        return polygon;
    }
}

/// <summary>
/// Specifies how the interior of a closed shape is determined
/// </summary>
public enum FillRule
{
    /// <summary>
    /// Alternating regions are filled (standard)
    /// </summary>
    EvenOdd,

    /// <summary>
    /// All enclosed regions are filled
    /// </summary>
    NonZero
}
