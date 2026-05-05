namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para controles que tienen un valor que puede ajustarse arrastrando.
/// Ejemplos: Slider, ScrollBar, ProgressBar editable.
/// </summary>
public interface IDraggableValue
{
    /// <summary>
    /// Indica si el control est� siendo arrastrado actualmente.
    /// </summary>
    bool IsDragging { get; set; }

    /// <summary>
    /// Inicia el arrastre del valor.
    /// </summary>
    /// <param name="mouseX">Posici�n X del mouse</param>
    /// <param name="mouseY">Posici�n Y del mouse</param>
    void StartDragging(float mouseX, float mouseY);

    /// <summary>
    /// Actualiza el valor basado en la posici�n del mouse durante el arrastre.
    /// </summary>
    /// <param name="mouseX">Posici�n X del mouse</param>
    /// <param name="mouseY">Posici�n Y del mouse</param>
    void UpdateDragging(float mouseX, float mouseY);

    /// <summary>
    /// Finaliza el arrastre del valor.
    /// </summary>
    void EndDragging();
}