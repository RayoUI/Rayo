using System;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Marks a property as requiring a new measure pass when its value changes.
    /// This implies a later arrange pass and repaint for the affected subtree.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class MeasurePropertyAttribute : Attribute
    {
    }
}
