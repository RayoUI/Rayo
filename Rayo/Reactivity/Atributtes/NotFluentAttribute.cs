using System;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Suppresses fluent-setter code generation for a specific property (or all properties of a
    /// class when applied at the class level). The property is also exempt from the SUI001 analyzer
    /// rule — no <c>[LayoutProperty]</c>, <c>[PaintProperty]</c>, or <c>[InertProperty]</c> is
    /// required alongside this attribute (though you may still add one for intent documentation).
    ///
    /// Use cases:
    /// - Computed helpers that delegate to another tracked property.
    /// - Internal state set by the framework (e.g. <c>IsFocused</c>, scroll offsets).
    /// - Properties whose setters have complex side-effects that must not be wrapped.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class NotFluent : Attribute
    {
    }
}
