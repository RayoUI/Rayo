namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Rendering;

/// <summary>
/// Representa los datos de un icono, incluyendo su SVG path y viewBox.
/// Los iconos se definen usando comandos de dibujo simples.
/// </summary>
public class IconData
{
    /// <summary>
    /// Nombre del icono
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// ViewBox del SVG (width, height) - típicamente 24x24
    /// </summary>
    public float ViewBoxWidth { get; }

    public float ViewBoxHeight { get; }

    /// <summary>
    /// Comandos de dibujo del icono
    /// </summary>
    public List<IconDrawCommand> Commands { get; }

    public IconData(string name, float viewBoxWidth = 24, float viewBoxHeight = 24)
    {
        Name = name;
        ViewBoxWidth = viewBoxWidth;
        ViewBoxHeight = viewBoxHeight;
        Commands = new List<IconDrawCommand>();
    }

    public IconData AddPath(List<(float x, float y)> points, float strokeWidth = 2f)
    {
        Commands.Add(new IconPathCommand(points, strokeWidth));
        return this;
    }

    public IconData AddCircle(float cx, float cy, float radius, bool filled = false, float strokeWidth = 2f)
    {
        Commands.Add(new IconCircleCommand(cx, cy, radius, filled, strokeWidth));
        return this;
    }

    public IconData AddRect(float x, float y, float width, float height, bool filled = false, float strokeWidth = 2f)
    {
        Commands.Add(new IconRectCommand(x, y, width, height, filled, strokeWidth));
        return this;
    }

    public IconData AddLine(float x1, float y1, float x2, float y2, float strokeWidth = 2f)
    {
        Commands.Add(new IconLineCommand(x1, y1, x2, y2, strokeWidth));
        return this;
    }

    public IconData AddPolygon(List<(float x, float y)> points, bool filled = true)
    {
        Commands.Add(new IconPolygonCommand(points, filled));
        return this;
    }
}

/// <summary>
/// Comando de dibujo base para iconos
/// </summary>
public abstract class IconDrawCommand
{
    public abstract void Draw(IRenderer renderer, float x, float y, float scale, Color color);
}

/// <summary>
/// Comando para dibujar un path (serie de líneas conectadas)
/// </summary>
public class IconPathCommand : IconDrawCommand
{
    public List<(float x, float y)> Points { get; }
    public float StrokeWidth { get; }

    public IconPathCommand(List<(float x, float y)> points, float strokeWidth)
    {
        Points = points;
        StrokeWidth = strokeWidth;
    }

    public override void Draw(IRenderer renderer, float x, float y, float scale, Color color)
    {
        for (int i = 0; i < Points.Count - 1; i++)
        {
            var p1 = Points[i];
            var p2 = Points[i + 1];

            float x1 = x + p1.x * scale;
            float y1 = y + p1.y * scale;
            float x2 = x + p2.x * scale;
            float y2 = y + p2.y * scale;

            renderer.DrawLine(x1, y1, x2, y2, StrokeWidth * scale, color);
        }
    }
}

/// <summary>
/// Comando para dibujar un círculo
/// </summary>
public class IconCircleCommand : IconDrawCommand
{
    public float CenterX { get; }
    public float CenterY { get; }
    public float Radius { get; }
    public bool Filled { get; }
    public float StrokeWidth { get; }

    public IconCircleCommand(float cx, float cy, float radius, bool filled, float strokeWidth)
    {
        CenterX = cx;
        CenterY = cy;
        Radius = radius;
        Filled = filled;
        StrokeWidth = strokeWidth;
    }

    public override void Draw(IRenderer renderer, float x, float y, float scale, Color color)
    {
        float cx = x + CenterX * scale;
        float cy = y + CenterY * scale;
        float r = Radius * scale;

        if (Filled)
        {
            renderer.DrawCircle(cx, cy, r, color);
        }
        else
        {
            renderer.DrawCircleOutline(cx, cy, r, StrokeWidth * scale, color);
        }
    }
}

/// <summary>
/// Comando para dibujar un rectángulo
/// </summary>
public class IconRectCommand : IconDrawCommand
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public bool Filled { get; }
    public float StrokeWidth { get; }

    public IconRectCommand(float x, float y, float width, float height, bool filled, float strokeWidth)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Filled = filled;
        StrokeWidth = strokeWidth;
    }

    public override void Draw(IRenderer renderer, float x, float y, float scale, Color color)
    {
        float rx = x + X * scale;
        float ry = y + Y * scale;
        float rw = Width * scale;
        float rh = Height * scale;

        if (Filled)
        {
            renderer.DrawRect(rx, ry, rw, rh, color);
        }
        else
        {
            renderer.DrawRectOutline(rx, ry, rw, rh, StrokeWidth * scale, color);
        }
    }
}

/// <summary>
/// Comando para dibujar una línea
/// </summary>
public class IconLineCommand : IconDrawCommand
{
    public float X1 { get; }
    public float Y1 { get; }
    public float X2 { get; }
    public float Y2 { get; }
    public float StrokeWidth { get; }

    public IconLineCommand(float x1, float y1, float x2, float y2, float strokeWidth)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        StrokeWidth = strokeWidth;
    }

    public override void Draw(IRenderer renderer, float x, float y, float scale, Color color)
    {
        float sx1 = x + X1 * scale;
        float sy1 = y + Y1 * scale;
        float sx2 = x + X2 * scale;
        float sy2 = y + Y2 * scale;

        renderer.DrawLine(sx1, sy1, sx2, sy2, StrokeWidth * scale, color);
    }
}

/// <summary>
/// Comando para dibujar un polígono
/// </summary>
public class IconPolygonCommand : IconDrawCommand
{
    public List<(float x, float y)> Points { get; }
    public bool Filled { get; }

    public IconPolygonCommand(List<(float x, float y)> points, bool filled)
    {
        Points = points;
        Filled = filled;
    }

    public override void Draw(IRenderer renderer, float x, float y, float scale, Color color)
    {
        if (Filled)
        {
            // Dibujar polígono relleno usando triangulación simple (fan)
            if (Points.Count >= 3)
            {
                var scaledPoints = Points.Select(p => (x + p.x * scale, y + p.y * scale)).ToList();
                renderer.DrawPolygon(scaledPoints, color);
            }
        }
        else
        {
            // Dibujar contorno del polígono
            for (int i = 0; i < Points.Count; i++)
            {
                var p1 = Points[i];
                var p2 = Points[(i + 1) % Points.Count];

                float x1 = x + p1.x * scale;
                float y1 = y + p1.y * scale;
                float x2 = x + p2.x * scale;
                float y2 = y + p2.y * scale;

                renderer.DrawLine(x1, y1, x2, y2, 2f * scale, color);
            }
        }
    }
}