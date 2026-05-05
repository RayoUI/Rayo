namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para elementos que tienen comportamiento personalizado de hit-testing.
/// Permite a los componentes definir regiones de hit complejas o no rectangulares.
/// </summary>
public interface ICustomHitTest
{
    /// <summary>
    /// Determina si un punto est� dentro del elemento usando l�gica personalizada.
    /// </summary>
    /// <param name="point">Punto en coordenadas globales</param>
    /// <returns>True si el punto est� dentro del elemento</returns>
    bool HitTest(System.Numerics.Vector2 point);

    /// <summary>
    /// Obtiene la regi�n de hit-testing en coordenadas locales.
    /// �til para formas no rectangulares (c�rculos, pol�gonos, etc.).
    /// </summary>
    HitTestGeometry GetHitTestGeometry();
}

/// <summary>
/// Representa la geometr�a de un elemento para hit-testing.
/// </summary>
public abstract class HitTestGeometry
{
    /// <summary>
    /// Verifica si un punto est� dentro de esta geometr�a.
    /// </summary>
    public abstract bool Contains(System.Numerics.Vector2 point);
}

/// <summary>
/// Geometr�a rectangular para hit-testing.
/// </summary>
public class RectangleGeometry : HitTestGeometry
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public RectangleGeometry(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public override bool Contains(System.Numerics.Vector2 point)
    {
        return point.X >= X && point.X <= X + Width &&
    point.Y >= Y && point.Y <= Y + Height;
    }
}

/// <summary>
/// Geometr�a circular para hit-testing.
/// </summary>
public class CircleGeometry : HitTestGeometry
{
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float Radius { get; set; }

    public CircleGeometry(float centerX, float centerY, float radius)
    {
        CenterX = centerX;
        CenterY = centerY;
        Radius = radius;
    }

    public override bool Contains(System.Numerics.Vector2 point)
    {
        float dx = point.X - CenterX;
        float dy = point.Y - CenterY;
        return (dx * dx + dy * dy) <= (Radius * Radius);
    }
}

/// <summary>
/// Geometr�a combinada (uni�n de m�ltiples geometr�as).
/// </summary>
public class CombinedGeometry : HitTestGeometry
{
    private List<HitTestGeometry> _geometries = new();

    public void Add(HitTestGeometry geometry)
    {
        _geometries.Add(geometry);
    }

    public override bool Contains(System.Numerics.Vector2 point)
    {
        foreach (var geometry in _geometries)
        {
            if (geometry.Contains(point))
                return true;
        }
        return false;
    }
}