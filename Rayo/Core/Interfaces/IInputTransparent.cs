namespace Rayo.Core.Interfaces;

/// <summary>
/// Interfaz para elementos que pueden ser "transparentes" a los eventos de entrada.
/// Similar a IsHitTestVisible en WPF o InputTransparent en MAUI.
/// </summary>
public interface IInputTransparent
{
    /// <summary>
    /// Si es true, el elemento no captura eventos pero sus hijos s� pueden.
    /// Si es false, el elemento y sus hijos capturan eventos normalmente.
    /// </summary>
    bool IsInputTransparent { get; set; }
}

/// <summary>
/// Comportamiento de input transparency m�s granular.
/// </summary>
public enum InputTransparencyMode
{
    /// <summary>
    /// El elemento captura eventos normalmente.
    /// </summary>
    None,

    /// <summary>
    /// El elemento no captura eventos pero sus hijos s�.
    /// </summary>
    Self,

    /// <summary>
    /// Ni el elemento ni sus hijos capturan eventos.
    /// </summary>
    SelfAndChildren,

    /// <summary>
    /// El elemento captura eventos pero sus hijos no.
    /// </summary>
    ChildrenOnly
}