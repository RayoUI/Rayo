namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para elementos que soportan scrolling (desplazamiento).
/// Permite al EventManager manejar scroll de forma gen�rica sin conocer tipos espec�ficos.
/// </summary>
public interface IScrollable
{
    /// <summary>
    /// Desplaza el contenido en la direcci�n Y (vertical).
    /// </summary>
    /// <param name="deltaY">Cantidad de desplazamiento (positivo = abajo, negativo = arriba)</param>
    void Scroll(float deltaY);

    /// <summary>
    /// Desplaza el contenido en la direcci�n X (horizontal).
    /// Default implementation is a no-op for elements that don't support horizontal scrolling.
    /// </summary>
    /// <param name="deltaX">Cantidad de desplazamiento (positivo = derecha, negativo = izquierda)</param>
    void ScrollHorizontal(float deltaX) { }

    /// <summary>
    /// Ancho del contenido total (puede ser mayor que el �rea visible).
    /// </summary>
    float ContentWidth { get; }

    /// <summary>
    /// Altura del contenido total (puede ser mayor que el �rea visible).
    /// </summary>
    float ContentHeight { get; }
}