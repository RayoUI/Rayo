namespace Rayo.Core;

/// <summary>
/// Representa el tipo de operaci�n de drop que se realizar�.
/// Similar al est�ndar HTML5 Drag and Drop.
/// </summary>
[Flags]
public enum DragDropEffect
{
    /// <summary>
    /// No se permite ninguna operaci�n de drop.
    /// </summary>
    None = 0,

    /// <summary>
    /// El elemento ser� copiado al drop target.
    /// El elemento original permanece en su lugar.
    /// </summary>
    Copy = 1 << 0,

    /// <summary>
    /// El elemento ser� movido al drop target.
    /// El elemento original ser� removido de su ubicaci�n.
    /// </summary>
    Move = 1 << 1,

    /// <summary>
    /// Se crear� un enlace/referencia al elemento en el drop target.
    /// Similar a un acceso directo.
    /// </summary>
    Link = 1 << 2,

    /// <summary>
    /// Permite todas las operaciones.
    /// El drop target decidir� cu�l usar.
    /// </summary>
    All = Copy | Move | Link
}

/// <summary>
/// Extensiones de utilidad para DragDropEffect.
/// </summary>
public static class DragDropEffectExtensions
{
    /// <summary>
    /// Verifica si un efecto contiene otro efecto espec�fico.
    /// </summary>
    public static bool HasEffect(this DragDropEffect effect, DragDropEffect flag)
    {
        return (effect & flag) == flag;
    }

    /// <summary>
    /// Obtiene el cursor apropiado para el efecto.
    /// </summary>
    public static string GetCursorName(this DragDropEffect effect)
    {
        return effect switch
        {
            DragDropEffect.Copy => "copy",
            DragDropEffect.Move => "move",
            DragDropEffect.Link => "alias",
            DragDropEffect.None => "no-drop",
            _ => "default"
        };
    }

    /// <summary>
    /// Obtiene el efecto m�s apropiado seg�n las teclas modificadoras.
    /// </summary>
    /// <param name="allowedEffects">Efectos permitidos por el draggable</param>
    /// <param name="isCtrlPressed">Si est� presionada la tecla Ctrl</param>
    /// <param name="isShiftPressed">Si est� presionada la tecla Shift</param>
    /// <param name="isAltPressed">Si est� presionada la tecla Alt</param>
    public static DragDropEffect GetEffectFromModifiers(
        this DragDropEffect allowedEffects,
        bool isCtrlPressed,
        bool isShiftPressed,
    bool isAltPressed)
    {
        // Ctrl = Copy, Shift = Move, Alt = Link
        if (isCtrlPressed && allowedEffects.HasEffect(DragDropEffect.Copy))
            return DragDropEffect.Copy;

        if (isShiftPressed && allowedEffects.HasEffect(DragDropEffect.Move))
            return DragDropEffect.Move;

        if (isAltPressed && allowedEffects.HasEffect(DragDropEffect.Link))
            return DragDropEffect.Link;

        // Por defecto, preferir Move si est� disponible
        if (allowedEffects.HasEffect(DragDropEffect.Move))
            return DragDropEffect.Move;

        if (allowedEffects.HasEffect(DragDropEffect.Copy))
            return DragDropEffect.Copy;

        if (allowedEffects.HasEffect(DragDropEffect.Link))
            return DragDropEffect.Link;

        return DragDropEffect.None;
    }
}