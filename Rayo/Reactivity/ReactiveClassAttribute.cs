using System;

namespace Rayo.Reactivity
{
    /// <summary>
    /// Excludes a class or property from reactive code generation.
    /// 
    /// When applied to a class that inherits from VisualElement:
    /// - No fluent setters will be generated for any properties of that class
    /// - The entire class is skipped by the ReactivePropertyGenerator
    /// 
    /// When applied to a property:
    /// - That specific property will not have fluent setters generated
    /// - Other properties in the same class will still be generated normally
    /// 
    /// Use cases:
    /// - Properties with complex setters that should not be exposed as fluent APIs
    /// - Internal state properties that shouldn't be part of the public fluent interface
    /// - Classes that inherit from VisualElement but should not have reactive generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class NoReactiveAttribute : Attribute
    {
    }
}
