using System;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Marks a property as requiring a new arrange pass when its value changes.
    /// This does not force a new measure pass unless the control invalidates it manually.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ArrangePropertyAttribute : Attribute
    {
    }
}
