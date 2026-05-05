namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para elementos que pueden aceptar drops de elementos draggables.
/// Implementa esta interfaz en cualquier UIElement que quieras que sea un drop target.
/// </summary>
public interface IDropTarget
{
    /// <summary>
    /// Llamado cuando un elemento draggable entra en el �rea de este drop target.
    /// </summary>
    /// <param name="dragData">Datos del elemento que se est� arrastrando</param>
    /// <returns>True si este target puede aceptar el drop, false en caso contrario</returns>
    bool OnDragEnter(DragData dragData);

    /// <summary>
    /// Llamado mientras un elemento draggable se mueve sobre este drop target.
    /// </summary>
    /// <param name="dragData">Datos del elemento que se est� arrastrando</param>
    /// <param name="mouseX">Posici�n X actual del mouse</param>
    /// <param name="mouseY">Posici�n Y actual del mouse</param>
    void OnDragOver(DragData dragData, float mouseX, float mouseY);

    /// <summary>
    /// Llamado cuando un elemento draggable sale del �rea de este drop target.
    /// </summary>
    /// <param name="dragData">Datos del elemento que se estaba arrastrando</param>
    void OnDragLeave(DragData dragData);

    /// <summary>
    /// Llamado cuando se suelta un elemento draggable sobre este drop target.
    /// </summary>
    /// <param name="dragData">Datos del elemento que se solt�</param>
    /// <param name="mouseX">Posici�n X donde se solt�</param>
    /// <param name="mouseY">Posici�n Y donde se solt�</param>
    /// <returns>True si el drop fue aceptado, false para rechazarlo</returns>
    bool OnDrop(DragData dragData, float mouseX, float mouseY);

    /// <summary>
    /// Indica si este drop target est� actualmente bajo un elemento draggable v�lido.
    /// Usado para feedback visual.
    /// </summary>
    bool IsDropTargetActive { get; set; }

    /// <summary>
    /// Determina si este drop target puede aceptar el tipo de datos dado.
    /// Permite filtrar qu� elementos pueden ser dropeados aqu�.
    /// </summary>
    /// <param name="dataType">Tipo de datos que se est� arrastrando</param>
    /// <returns>True si puede aceptar este tipo, false en caso contrario</returns>
    bool CanAcceptDataType(string dataType);

    /// <summary>
    /// Restricciones avanzadas para este drop target.
    /// Si es null, solo se usa CanAcceptDataType para validar.
    /// </summary>
    DropConstraints? Constraints { get; }

    /// <summary>
    /// Efectos de drop permitidos por este target.
    /// Si es null, acepta todos los efectos que permita el DragData.
    /// </summary>
    DragDropEffect? AllowedEffects { get; }
}