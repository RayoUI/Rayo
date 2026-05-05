namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para elementos que pueden recibir foco de teclado
/// </summary>
public interface IFocusable
{
    /// <summary>
    /// Indica si el elemento tiene el foco
    /// </summary>
    bool IsFocused { get; set; }
}