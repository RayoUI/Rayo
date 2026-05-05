namespace Rayo.Core.Interfaces;

using System.Numerics;

/// <summary>
/// Interfaz para elementos que clippean (recortan) su contenido.
/// Permite al EventManager verificar si un punto est� dentro del �rea visible.
/// </summary>
public interface IClippable
{
    /// <summary>
    /// Verifica si un punto est� dentro del �rea de clipping visible.
    /// </summary>
    /// <param name="position">Posici�n a verificar</param>
    /// <returns>true si el punto est� dentro del �rea visible</returns>
    bool IsPointInClipRegion(Vector2 position);

    /// <summary>
    /// Obtiene el rect de clipping (�rea visible) del elemento.
    /// </summary>
    /// <returns>(x, y, width, height) del �rea visible</returns>
    (float x, float y, float width, float height) GetClipRect();
}