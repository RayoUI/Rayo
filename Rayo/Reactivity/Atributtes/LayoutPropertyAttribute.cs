using System;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Compatibility alias for a property that requires a new measure pass when its value changes.
    ///
    /// The source generator reads this attribute and registers the property
    /// via <see cref="Rayo.Core.VisualElement.RegisterLayoutProperties"/> so that
    /// <c>InvalidateMeasure()</c> is called automatically on change — no explicit
    /// callback in the setter is needed.
    ///
    /// Use this when the property affects the element's measured size,
    /// and the generator's built-in heuristics do not classify it correctly.
    ///
    /// Example:
    /// <code>
    /// [LayoutProperty]
    /// public int ColumnCount { get; set; }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class LayoutPropertyAttribute : Attribute
    {
    }
}
