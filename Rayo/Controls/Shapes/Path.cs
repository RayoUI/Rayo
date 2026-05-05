namespace Rayo.Controls.Shapes;

using Rayo.Reactivity;
using Rayo.Rendering;
using System.Numerics;
using System.Text.RegularExpressions;

/// <summary>
/// Path shape control - draws complex vector paths using path data
/// Supports a subset of SVG path syntax
/// </summary>
public partial class Path : Shape<Path>
{
    private List<PathSegment> _segments = new();
    private List<Vector2> _cachedPoints = new();
    private float _minX, _minY, _maxX, _maxY;
    private string _data = "";
    private FillRule _fillRule = FillRule.EvenOdd;

    public string Data
    {
        get => _data;
        set
        {
            if (_data != value)
            {
                _data = value;
                ParsePathData(value);
                MarkNeedsPaint();
            }
        }
    }

    public FillRule FillRule
    {
        get => _fillRule;
        set
        {
            if (_fillRule != value)
            {
                _fillRule = value;
                MarkNeedsPaint();
            }
        }
    }

    public Path()
    {
        Fill = Color.Transparent;
        Stroke = Color.Black;
        StrokeThickness = 1;
    }

    public Path(string data)
    {
        Fill = Color.Transparent;
        Stroke = Color.Black;
        StrokeThickness = 1;
        Data = data;
    }

    //public Path Data(string data)
    //{
    //    Data = data;
    //    return this;
    //}

    //public Path FillRule(FillRule fillRule)
    //{
    //    FillRule = fillRule;
    //    return this;
    //}

    // Fluent API for building paths programmatically

    public Path MoveTo(float x, float y)
    {
        _segments.Add(new PathSegment(PathCommand.MoveTo, x, y));
        RebuildCache();
        return this;
    }

    public Path LineTo(float x, float y)
    {
        _segments.Add(new PathSegment(PathCommand.LineTo, x, y));
        RebuildCache();
        return this;
    }

    public Path HorizontalLineTo(float x)
    {
        _segments.Add(new PathSegment(PathCommand.HorizontalLine, x));
        RebuildCache();
        return this;
    }

    public Path VerticalLineTo(float y)
    {
        _segments.Add(new PathSegment(PathCommand.VerticalLine, y));
        RebuildCache();
        return this;
    }

    public Path CurveTo(float x1, float y1, float x2, float y2, float x, float y)
    {
        _segments.Add(new PathSegment(PathCommand.CubicBezier, x1, y1, x2, y2, x, y));
        RebuildCache();
        return this;
    }

    public Path QuadraticCurveTo(float x1, float y1, float x, float y)
    {
        _segments.Add(new PathSegment(PathCommand.QuadraticBezier, x1, y1, x, y));
        RebuildCache();
        return this;
    }

    public Path ArcTo(float rx, float ry, float rotation, bool largeArc, bool sweep, float x, float y)
    {
        _segments.Add(new PathSegment(PathCommand.Arc, rx, ry, rotation, largeArc ? 1 : 0, sweep ? 1 : 0, x, y));
        RebuildCache();
        return this;
    }

    public Path ClosePath()
    {
        _segments.Add(new PathSegment(PathCommand.Close));
        RebuildCache();
        return this;
    }

    public Path ClearPath()
    {
        _segments.Clear();
        _cachedPoints.Clear();
        MarkNeedsPaint();
        return this;
    }

    private void ParsePathData(string data)
    {
        _segments.Clear();

        if (string.IsNullOrWhiteSpace(data))
        {
            RebuildCache();
            return;
        }

        // Simple SVG path parser
        var regex = PathDataRegex();
        var matches = regex.Matches(data);

        char currentCommand = 'M';
        var numbers = new List<float>();

        foreach (Match match in matches)
        {
            string value = match.Value;

            if (char.IsLetter(value[0]))
            {
                ProcessCommand(currentCommand, numbers);
                currentCommand = value[0];
                numbers.Clear();
            }
            else if (float.TryParse(value, out float num))
            {
                numbers.Add(num);
            }
        }

        ProcessCommand(currentCommand, numbers);
        RebuildCache();
    }

    [GeneratedRegex(@"([MmLlHhVvCcSsQqTtAaZz])|(-?\d+\.?\d*)", RegexOptions.Compiled)]
    private static partial Regex PathDataRegex();

    private void ProcessCommand(char command, List<float> numbers)
    {
        switch (command)
        {
            case 'M':
            case 'm':
                for (int i = 0; i + 1 < numbers.Count; i += 2)
                    _segments.Add(new PathSegment(PathCommand.MoveTo, numbers[i], numbers[i + 1]));
                break;

            case 'L':
            case 'l':
                for (int i = 0; i + 1 < numbers.Count; i += 2)
                    _segments.Add(new PathSegment(PathCommand.LineTo, numbers[i], numbers[i + 1]));
                break;

            case 'H':
            case 'h':
                foreach (float x in numbers)
                    _segments.Add(new PathSegment(PathCommand.HorizontalLine, x));
                break;

            case 'V':
            case 'v':
                foreach (float y in numbers)
                    _segments.Add(new PathSegment(PathCommand.VerticalLine, y));
                break;

            case 'C':
            case 'c':
                for (int i = 0; i + 5 < numbers.Count; i += 6)
                    _segments.Add(new PathSegment(PathCommand.CubicBezier,
                        numbers[i], numbers[i + 1], numbers[i + 2], numbers[i + 3], numbers[i + 4], numbers[i + 5]));
                break;

            case 'Q':
            case 'q':
                for (int i = 0; i + 3 < numbers.Count; i += 4)
                    _segments.Add(new PathSegment(PathCommand.QuadraticBezier,
                        numbers[i], numbers[i + 1], numbers[i + 2], numbers[i + 3]));
                break;

            case 'A':
            case 'a':
                for (int i = 0; i + 6 < numbers.Count; i += 7)
                    _segments.Add(new PathSegment(PathCommand.Arc,
                        numbers[i], numbers[i + 1], numbers[i + 2], numbers[i + 3], numbers[i + 4], numbers[i + 5], numbers[i + 6]));
                break;

            case 'Z':
            case 'z':
                _segments.Add(new PathSegment(PathCommand.Close));
                break;
        }
    }

    private void RebuildCache()
    {
        _cachedPoints.Clear();

        if (_segments.Count == 0)
        {
            _minX = _minY = _maxX = _maxY = 0;
            return;
        }

        Vector2 currentPos = Vector2.Zero;
        Vector2 startPos = Vector2.Zero;

        foreach (var segment in _segments)
        {
            switch (segment.Command)
            {
                case PathCommand.MoveTo:
                    currentPos = new Vector2(segment.Values[0], segment.Values[1]);
                    startPos = currentPos;
                    _cachedPoints.Add(currentPos);
                    break;

                case PathCommand.LineTo:
                    currentPos = new Vector2(segment.Values[0], segment.Values[1]);
                    _cachedPoints.Add(currentPos);
                    break;

                case PathCommand.HorizontalLine:
                    currentPos = new Vector2(segment.Values[0], currentPos.Y);
                    _cachedPoints.Add(currentPos);
                    break;

                case PathCommand.VerticalLine:
                    currentPos = new Vector2(currentPos.X, segment.Values[0]);
                    _cachedPoints.Add(currentPos);
                    break;

                case PathCommand.CubicBezier:
                    AddBezierPoints(currentPos,
                        new Vector2(segment.Values[0], segment.Values[1]),
                        new Vector2(segment.Values[2], segment.Values[3]),
                        new Vector2(segment.Values[4], segment.Values[5]));
                    currentPos = new Vector2(segment.Values[4], segment.Values[5]);
                    break;

                case PathCommand.QuadraticBezier:
                    AddQuadraticBezierPoints(currentPos,
                        new Vector2(segment.Values[0], segment.Values[1]),
                        new Vector2(segment.Values[2], segment.Values[3]));
                    currentPos = new Vector2(segment.Values[2], segment.Values[3]);
                    break;

                case PathCommand.Arc:
                    // Simplified arc handling - approximate with line for now
                    currentPos = new Vector2(segment.Values[5], segment.Values[6]);
                    _cachedPoints.Add(currentPos);
                    break;

                case PathCommand.Close:
                    if (startPos != currentPos)
                        _cachedPoints.Add(startPos);
                    currentPos = startPos;
                    break;
            }
        }

        UpdateBounds();
    }

    private void AddBezierPoints(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        int segments = 20;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 point = uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
            _cachedPoints.Add(point);
        }
    }

    private void AddQuadraticBezierPoints(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        int segments = 16;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector2 point = uu * p0 + 2 * u * t * p1 + tt * p2;
            _cachedPoints.Add(point);
        }
    }

    private void UpdateBounds()
    {
        if (_cachedPoints.Count == 0)
        {
            _minX = _minY = _maxX = _maxY = 0;
            return;
        }

        _minX = _cachedPoints.Min(p => p.X);
        _maxX = _cachedPoints.Max(p => p.X);
        _minY = _cachedPoints.Min(p => p.Y);
        _maxY = _cachedPoints.Max(p => p.Y);
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
        if (_cachedPoints.Count < 2) return;

        // Offset by half stroke thickness to ensure stroke is fully visible
        float strokeOffset = StrokeThickness / 2;
        float baseX = ComputedX - _minX + strokeOffset;
        float baseY = ComputedY - _minY + strokeOffset;

        var adjustedPoints = _cachedPoints
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

/// <summary>
/// Path command types
/// </summary>
public enum PathCommand
{
    MoveTo,
    LineTo,
    HorizontalLine,
    VerticalLine,
    CubicBezier,
    QuadraticBezier,
    Arc,
    Close
}

/// <summary>
/// Represents a single path segment
/// </summary>
public struct PathSegment
{
    public PathCommand Command { get; }
    public float[] Values { get; }

    public PathSegment(PathCommand command, params float[] values)
    {
        Command = command;
        Values = values;
    }
}
