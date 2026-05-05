namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para controles que soportan edici�n de texto y pueden recibir caracteres.
/// Permite al EventManager delegar la l�gica de teclas al propio control.
/// </summary>
public interface ITextEditable : IFocusable
{
    /// <summary>
    /// Procesa una tecla presionada durante la edici�n.
    /// </summary>
    /// <param name="key">C�digo de la tecla</param>
    /// <param name="isShift">Si Shift est� presionado</param>
    /// <param name="isCtrl">Si Ctrl est� presionado</param>
    /// <param name="isAlt">Si Alt est� presionado</param>
    /// <returns>True si la tecla fue procesada y causa cambios visuales</returns>
    bool ProcessKeyDown(InputKey key, bool isShift, bool isCtrl, bool isAlt);

    /// <summary>
    /// Inserta un car�cter en la posici�n actual del cursor.
    /// </summary>
    /// <param name="character">Car�cter a insertar</param>
    /// <returns>True si el car�cter fue insertado y causa cambios visuales</returns>
    bool InsertCharacter(char character);

    /// <summary>
    /// Procesa la repetici�n autom�tica de una tecla.
    /// </summary>
    /// <param name="key">Tecla que se est� repitiendo</param>
    /// <param name="isShift">Si Shift est� presionado</param>
    /// <returns>True si caus� cambios visuales</returns>
    bool ProcessKeyRepeat(InputKey key, bool isShift);
}