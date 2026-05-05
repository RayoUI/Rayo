namespace Rayo.Core;

/// <summary>
/// Marks a property for automatic dependency injection.
/// Properties marked with this attribute will be automatically populated
/// from the registered IServiceProvider when a UserControl is initialized.
/// </summary>
/// <example>
/// <code>
/// public class MyComponent : UserControl
/// {
///     [Inject]
///     public IMyService? MyService { get; private set; }
///
///     public override UIElementBase Build()
///     {
///         // MyService is now available
///         return new Frame();
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InjectAttribute : Attribute
{
    /// <summary>
    /// When true, throws an exception if the service cannot be resolved.
    /// When false (default), the property remains null if service is not found.
    /// </summary>
    public bool Required { get; set; } = false;
}
