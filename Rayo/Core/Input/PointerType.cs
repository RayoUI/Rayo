namespace Rayo.Core.Input;

/// <summary>
/// Tipo de dispositivo de puntero (similar a PointerDeviceType en WinUI/UWP).
/// </summary>
public enum PointerType
{
    /// <summary>
    /// Mouse o trackpad tradicional
    /// </summary>
    Mouse,

    /// <summary>
    /// Entrada t�ctil (dedo en pantalla)
    /// </summary>
    Touch,

    /// <summary>
    /// L�piz �ptico o stylus (con presi�n)
    /// </summary>
    Pen,

    /// <summary>
    /// Tipo de puntero desconocido
    /// </summary>
    Unknown
}
