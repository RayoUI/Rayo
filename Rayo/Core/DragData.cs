namespace Rayo.Core;

/// <summary>
/// Encapsula los datos que se transfieren durante una operaci�n de drag & drop.
/// Soporta datos tipados y metadatos adicionales.
/// </summary>
public class DragData
{
    /// <summary>
    /// Tipo de datos que se est� arrastrando (ej: "text", "file", "item", "card", etc.)
    /// Usado para validaci�n en drop targets.
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// Los datos reales que se est�n arrastrando.
    /// Puede ser cualquier objeto.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// El elemento UIElement que se est� arrastrando.
    /// �til para operaciones visuales o de reordenamiento.
    /// </summary>
    public VisualElement? SourceElement { get; set; }

    /// <summary>
    /// Metadatos adicionales opcionales.
    /// Permite pasar informaci�n extra sin cambiar la estructura de DragData.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Posici�n inicial X donde comenz� el drag.
    /// </summary>
    public float StartX { get; set; }

    /// <summary>
    /// Posici�n inicial Y donde comenz� el drag.
    /// </summary>
    public float StartY { get; set; }

    /// <summary>
    /// Efectos de drop permitidos para esta operaci�n de drag.
    /// Por defecto permite Copy y Move.
    /// </summary>
    public DragDropEffect AllowedEffects { get; set; } = DragDropEffect.Copy | DragDropEffect.Move;

    /// <summary>
    /// El efecto de drop actual basado en las teclas modificadoras.
    /// Se actualiza autom�ticamente durante el drag.
    /// </summary>
    public DragDropEffect CurrentEffect { get; set; } = DragDropEffect.Move;

    /// <summary>
    /// Constructor con par�metros b�sicos.
    /// </summary>
    public DragData(string dataType, object? data, VisualElement? sourceElement = null)
    {
        DataType = dataType;
        Data = data;
        SourceElement = sourceElement;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Obtiene los datos con el tipo espec�fico.
    /// </summary>
    public T? GetData<T>() where T : class
    {
        return Data as T;
    }

    /// <summary>
    /// Agrega metadatos.
    /// </summary>
    public DragData WithMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Establece los efectos permitidos.
    /// </summary>
    public DragData WithAllowedEffects(DragDropEffect effects)
    {
        AllowedEffects = effects;
        return this;
    }

    /// <summary>
    /// Obtiene un metadato.
    /// </summary>
    public T? GetMetadata<T>(string key) where T : class
    {
        if (Metadata != null && Metadata.TryGetValue(key, out var value))
        {
            return value as T;
        }
        return null;
    }

    /// <summary>
    /// Verifica si tiene un metadato espec�fico.
    /// </summary>
    public bool HasMetadata(string key)
    {
        return Metadata != null && Metadata.ContainsKey(key);
    }
}