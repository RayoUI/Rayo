namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para builders de UI que soportan hot reload.
/// Implementa esta interfaz en una clase para poder recargar la UI automáticamente.
/// </summary>
public interface IUIBuilder
{
    /// <summary>
    /// Construye y retorna el árbol de elementos UI.
    /// Este método se llama cada vez que se recarga la UI.
    /// </summary>
    VisualElement Build();
}