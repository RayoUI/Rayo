using Rayo.Core.Interfaces;

namespace Rayo.Core;

/// <summary>
/// Extensiones para configurar input transparency en elementos UI.
/// Los elementos deben implementar IInputTransparent para usarlas.
/// </summary>
public static class InputTransparencyExtensions
{
    /// <summary>
    /// Marca el elemento como transparente a eventos de entrada.
    /// El elemento no capturar� eventos pero sus hijos s� podr�n.
    /// </summary>
    public static T SetInputTransparent<T>(this T element, bool isTransparent = true) where T : VisualElement, IInputTransparent
    {
        element.IsInputTransparent = isTransparent;
        return element;
    }

    /// <summary>
    /// Marca el elemento como transparente a eventos de entrada.
    /// Alias de SetInputTransparent(true) para mayor claridad en el DSL.
    /// </summary>
    public static T MakeInputTransparent<T>(this T element) where T : VisualElement, IInputTransparent
    {
        element.IsInputTransparent = true;
        return element;
    }
}