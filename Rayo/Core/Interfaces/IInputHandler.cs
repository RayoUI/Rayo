namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para controles que quieren manejar eventos de entrada
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Si el control puede recibir eventos de entrada
    /// </summary>
    bool CanHandleInput { get; }

    /// <summary>
    /// Procesa un evento de entrada
    /// Retorna true si el evento fue manejado (consumed)
    /// </summary>
    bool HandleInput(InputEventArgs args);

    /// <summary>
    /// Se llama cuando el control recibe el foco
    /// </summary>
    void OnFocusGained()
    {
    }

    /// <summary>
    /// Se llama cuando el control pierde el foco
    /// </summary>
    void OnFocusLost()
    {
    }
}