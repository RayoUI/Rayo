namespace Rayo.Controls.Shapes;

using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Ellipse shape control (also used for circles when Width == Height)
/// </summary>
public class Ellipse : Shape<Ellipse>
{
    public Ellipse() { }

    public Ellipse(float width, float height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a circle with the given diameter
    /// </summary>
    public static Ellipse Circle(float diameter)
    {
        return new Ellipse(diameter, diameter);
    }

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX;
        float y = ComputedY;
        float w = ComputedWidth;
        float h = ComputedHeight;

        float cx = x + w / 2;
        float cy = y + h / 2;

        bool isCircle = Math.Abs(w - h) < 0.001f;

        if (isCircle)
        {
            float radius = w / 2;

            // Draw fill
            if (Fill.PrimaryColor.A > 0)
            {
                renderer.DrawCircle(cx, cy, radius, Fill);
            }

            // Draw stroke
            if (Stroke.PrimaryColor.A > 0 && StrokeThickness > 0)
            {
                var strokeColor = Stroke.PrimaryColor;
                // Draw outer circle
                renderer.DrawCircle(cx, cy, radius, strokeColor);
                // Draw inner circle for stroke effect
                if (Fill.PrimaryColor.A > 0)
                {
                    renderer.DrawCircle(cx, cy, radius - StrokeThickness, Fill);
                }
                else
                {
                    renderer.DrawCircle(cx, cy, radius - StrokeThickness, Color.Transparent);
                }
            }
        }
        else
        {
            // For ellipse, approximate with polygon segments
            DrawEllipse(renderer, cx, cy, w / 2, h / 2);
        }
    }

    private void DrawEllipse(IRenderer renderer, float cx, float cy, float radiusX, float radiusY)
    {
        int segments = Math.Max(32, (int)Math.Max(radiusX, radiusY));
        var points = new List<(float, float)>();

        for (int i = 0; i < segments; i++)
        {
            float angle = 2 * MathF.PI * i / segments;
            float px = cx + radiusX * MathF.Cos(angle);
            float py = cy + radiusY * MathF.Sin(angle);
            points.Add((px, py));
        }

        // Draw fill
        if (Fill.PrimaryColor.A > 0)
        {
            renderer.DrawPolygon(points, Fill.PrimaryColor);
        }

        // Draw stroke
        if (Stroke.PrimaryColor.A > 0 && StrokeThickness > 0)
        {
            var strokeColor = Stroke.PrimaryColor;
            // Draw outer ellipse
            renderer.DrawPolygon(points, strokeColor);

            // Draw inner ellipse for stroke effect
            var innerPoints = new List<(float, float)>();
            float innerRadiusX = radiusX - StrokeThickness;
            float innerRadiusY = radiusY - StrokeThickness;

            if (innerRadiusX > 0 && innerRadiusY > 0)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = 2 * MathF.PI * i / segments;
                    float px = cx + innerRadiusX * MathF.Cos(angle);
                    float py = cy + innerRadiusY * MathF.Sin(angle);
                    innerPoints.Add((px, py));
                }

                renderer.DrawPolygon(innerPoints, Fill.PrimaryColor.A > 0 ? Fill.PrimaryColor : Color.Transparent);
            }
        }
    }
}
