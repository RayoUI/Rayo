namespace Rayo.Controls.Shapes;

using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Rectangle shape control
/// </summary>
public class Rectangle : Shape<Rectangle>
{
    private float _radiusX = 0;
    private float _radiusY = 0;

    public float RadiusX
    {
        get => _radiusX;
        set
        {
            value = Math.Max(0, value);
            if (_radiusX != value)
            {
                _radiusX = value;
                MarkNeedsPaint();
            }
        }
    }

    public float RadiusY
    {
        get => _radiusY;
        set
        {
            value = Math.Max(0, value);
            if (_radiusY != value)
            {
                _radiusY = value;
                MarkNeedsPaint();
            }
        }
    }

    public Rectangle() { }

    public Rectangle(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public Rectangle Radius(float radius)
    {
        RadiusX = radius;
        RadiusY = radius;
        return this;
    }

    public Rectangle Radius(float radiusX, float radiusY)
    {
        RadiusX = radiusX;
        RadiusY = radiusY;
        return this;
    }

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX;
        float y = ComputedY;
        float w = ComputedWidth;
        float h = ComputedHeight;

        // Use average radius for rounded corners
        float radius = (RadiusX + RadiusY) / 2;

        // Draw fill
        if (Fill.PrimaryColor.A > 0)
        {
            if (radius > 0)
            {
                renderer.DrawRoundedRect(x, y, w, h, radius, Fill);
            }
            else
            {
                renderer.DrawRect(x, y, w, h, Fill);
            }
        }

        // Draw stroke
        if (Stroke.PrimaryColor.A > 0 && StrokeThickness > 0)
        {
            DrawStroke(renderer, x, y, w, h, radius);
        }
    }

    private void DrawStroke(IRenderer renderer, float x, float y, float w, float h, float radius)
    {
        float thickness = StrokeThickness;
        var strokeColor = Stroke.PrimaryColor;

        if (radius > 0)
        {
            // Draw outer rect with stroke
            renderer.DrawRoundedRect(x, y, w, h, radius, strokeColor);
            // Draw inner rect to create stroke effect
            if (Fill.PrimaryColor.A > 0)
            {
                renderer.DrawRoundedRect(
                    x + thickness, y + thickness,
                    w - thickness * 2, h - thickness * 2,
                    Math.Max(0, radius - thickness), Fill);
            }
            else
            {
                renderer.DrawRoundedRect(
                    x + thickness, y + thickness,
                    w - thickness * 2, h - thickness * 2,
                    Math.Max(0, radius - thickness), Color.Transparent);
            }
        }
        else
        {
            // Draw border lines
            // Top
            renderer.DrawRect(x, y, w, thickness, strokeColor);
            // Bottom
            renderer.DrawRect(x, y + h - thickness, w, thickness, strokeColor);
            // Left
            renderer.DrawRect(x, y + thickness, thickness, h - thickness * 2, strokeColor);
            // Right
            renderer.DrawRect(x + w - thickness, y + thickness, thickness, h - thickness * 2, strokeColor);
        }
    }
}
