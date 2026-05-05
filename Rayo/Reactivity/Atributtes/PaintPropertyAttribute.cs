using System;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Marks a property as requiring only a repaint when its value changes.
    ///
    /// The source generator reads this attribute and registers the property
    /// via <see cref="Rayo.Core.VisualElement.RegisterPaintProperties"/> so that
    /// <c>MarkNeedsPaint()</c> is called automatically on change — no explicit
    /// callback in the setter is needed.
    ///
    /// Use this when the property only affects visual rendering (colors, opacity,
    /// decorations) and does not change the element's measured size or position,
    /// and the generator's built-in heuristics do not classify it correctly.
    ///
    /// Example:
    /// <code>
    /// [PaintProperty]
    /// public Color AccentColor { get; set; }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class PaintPropertyAttribute : Attribute
    {
    }
}
