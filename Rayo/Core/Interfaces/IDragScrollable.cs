namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para elementos que soportan drag-scrolling.
/// Permite iniciar/cancelar scrolling mediante arrastre.
/// </summary>
public interface IDragScrollable
{
    /// <summary>
    /// Indica si el elemento est� esperando que se confirme un drag para hacer scroll.
    /// </summary>
    bool IsDragPending { get; }

    /// <summary>
    /// Inicia el estado de drag pendiente.
    /// </summary>
    void StartDragPending();

    /// <summary>
    /// Cancela un drag pendiente.
    /// �til cuando otro elemento captura el evento o se hace click sin arrastrar.
    /// </summary>
    void CancelDragPending();
}