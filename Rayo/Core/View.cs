namespace Rayo.Core;

using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// Base class for leaf visual elements that do not contain children.
/// Similar to MAUI View - represents controls like Button, Label, Entry, Image, etc.
/// </summary>
public abstract class View<T> : VisualElement<T> where T : View<T>
{
}


