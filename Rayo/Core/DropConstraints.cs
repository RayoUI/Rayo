namespace Rayo.Core;

/// <summary>
/// Define restricciones avanzadas para operaciones de drop.
/// Permite controlar qu� elementos pueden ser dropeados en qu� targets bajo qu� condiciones.
/// </summary>
public class DropConstraints
{
    /// <summary>
    /// Tipos de datos aceptados. Si es null, acepta todos los tipos.
    /// </summary>
    public HashSet<string>? AcceptedDataTypes { get; set; }

    /// <summary>
    /// Tipos de datos rechazados expl�citamente.
    /// Tiene prioridad sobre AcceptedDataTypes.
    /// </summary>
    public HashSet<string>? RejectedDataTypes { get; set; }

    /// <summary>
    /// Efectos de drop permitidos. Si es null, acepta todos.
    /// </summary>
    public DragDropEffect? AllowedEffects { get; set; }

    /// <summary>
    /// N�mero m�ximo de elementos que puede aceptar este drop target.
    /// Si es null, no hay l�mite.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// Funci�n personalizada de validaci�n.
    /// Si retorna false, el drop es rechazado.
    /// </summary>
    public Func<DragData, bool>? CustomValidator { get; set; }

    /// <summary>
    /// Si es true, solo acepta drops de elementos que provengan del mismo contenedor padre.
    /// </summary>
    public bool OnlySameParent { get; set; } = false;

    /// <summary>
    /// Si es true, no acepta drops de elementos que sean ancestros de este drop target.
    /// Previene crear ciclos en la jerarqu�a.
    /// </summary>
    public bool PreventAncestorDrop { get; set; } = true;

    /// <summary>
    /// Metadatos requeridos que debe tener el DragData.
    /// Si est� especificado, el DragData debe tener todos estos metadatos.
    /// </summary>
    public Dictionary<string, object>? RequiredMetadata { get; set; }

    /// <summary>
    /// Constructor por defecto con restricciones permisivas.
    /// </summary>
    public DropConstraints()
    {
    }

    /// <summary>
    /// Crea restricciones que solo aceptan tipos de datos espec�ficos.
    /// </summary>
    public static DropConstraints AcceptOnly(params string[] dataTypes)
    {
        return new DropConstraints
        {
            AcceptedDataTypes = new HashSet<string>(dataTypes)
        };
    }

    /// <summary>
    /// Crea restricciones que rechazan tipos de datos espec�ficos.
    /// </summary>
    public static DropConstraints RejectTypes(params string[] dataTypes)
    {
        return new DropConstraints
        {
            RejectedDataTypes = new HashSet<string>(dataTypes)
        };
    }

    /// <summary>
    /// Crea restricciones con un l�mite m�ximo de elementos.
    /// </summary>
    public static DropConstraints WithMaxItems(int maxItems)
    {
        return new DropConstraints
        {
            MaxItems = maxItems
        };
    }

    /// <summary>
    /// Crea restricciones con un validador personalizado.
    /// </summary>
    public static DropConstraints WithValidator(Func<DragData, bool> validator)
    {
        return new DropConstraints
        {
            CustomValidator = validator
        };
    }

    /// <summary>
    /// Valida si un DragData cumple con estas restricciones.
    /// </summary>
    /// <param name="dragData">Datos del drag a validar</param>
    /// <param name="dropTarget">El drop target que est� validando</param>
    /// <param name="currentItemCount">Cantidad actual de elementos en el drop target</param>
    /// <returns>True si pasa todas las validaciones, false en caso contrario</returns>
    public bool Validate(DragData dragData, VisualElement dropTarget, int currentItemCount = 0)
    {
        // 1. Validar tipos de datos rechazados
        if (RejectedDataTypes != null && RejectedDataTypes.Contains(dragData.DataType))
        {
            return false;
        }

        // 2. Validar tipos de datos aceptados
        if (AcceptedDataTypes != null && !AcceptedDataTypes.Contains(dragData.DataType))
        {
            return false;
        }

        // 3. Validar efectos permitidos
        if (AllowedEffects.HasValue && !AllowedEffects.Value.HasEffect(dragData.CurrentEffect))
        {
            return false;
        }

        // 4. Validar l�mite de elementos
        if (MaxItems.HasValue && currentItemCount >= MaxItems.Value)
        {
            return false;
        }

        // 5. Validar mismo padre
        if (OnlySameParent && dragData.SourceElement != null)
        {
            if (dragData.SourceElement.Parent != dropTarget.Parent)
            {
                return false;
            }
        }

        // 6. Prevenir drop de ancestros
        if (PreventAncestorDrop && dragData.SourceElement != null)
        {
            if (IsAncestor(dragData.SourceElement, dropTarget))
            {
                return false;
            }
        }

        // 7. Validar metadatos requeridos
        if (RequiredMetadata != null)
        {
            foreach (var kvp in RequiredMetadata)
            {
                if (!dragData.HasMetadata(kvp.Key))
                {
                    return false;
                }

                var value = dragData.GetMetadata<object>(kvp.Key);
                if (value == null || !value.Equals(kvp.Value))
                {
                    return false;
                }
            }
        }

        // 8. Validador personalizado
        if (CustomValidator != null && !CustomValidator(dragData))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Verifica si un elemento es ancestro de otro.
    /// </summary>
    private bool IsAncestor(VisualElement potential, VisualElement element)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current == potential)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// API fluent para agregar tipo aceptado.
    /// </summary>
    public DropConstraints AcceptType(string dataType)
    {
        AcceptedDataTypes ??= new HashSet<string>();
        AcceptedDataTypes.Add(dataType);
        return this;
    }

    /// <summary>
    /// API fluent para agregar tipo rechazado.
    /// </summary>
    public DropConstraints RejectType(string dataType)
    {
        RejectedDataTypes ??= new HashSet<string>();
        RejectedDataTypes.Add(dataType);
        return this;
    }

    /// <summary>
    /// API fluent para establecer efectos permitidos.
    /// </summary>
    public DropConstraints WithEffects(DragDropEffect effects)
    {
        AllowedEffects = effects;
        return this;
    }

    /// <summary>
    /// API fluent para establecer metadato requerido.
    /// </summary>
    public DropConstraints RequireMetadata(string key, object value)
    {
        RequiredMetadata ??= new Dictionary<string, object>();
        RequiredMetadata[key] = value;
        return this;
    }
}