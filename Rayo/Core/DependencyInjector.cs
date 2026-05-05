using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Rayo.Core;

/// <summary>
/// Helper class for injecting dependencies into objects using the [Inject] attribute.
/// </summary>
public static class DependencyInjector
{
    /// <summary>
    /// Global service provider for platforms where UIApplication is not available (e.g., Android).
    /// </summary>
    private static IServiceProvider? _globalServiceProvider;

    /// <summary>
    /// Sets the global service provider for dependency injection.
    /// Use this on platforms where UIApplication.Current is not available.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use globally.</param>
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _globalServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the current service provider (from UIApplication or global fallback).
    /// </summary>
    public static IServiceProvider? ServiceProvider =>
        UIApplication.Current?.ServiceProvider ?? _globalServiceProvider;

    /// <summary>
    /// Injects dependencies into properties marked with [Inject] attribute.
    /// </summary>
    /// <param name="target">The object to inject dependencies into.</param>
    /// <param name="serviceProvider">The service provider to resolve dependencies from. If null, uses UIApplication.Current?.ServiceProvider.</param>
    /// <returns>True if injection was performed, false if no service provider was available.</returns>
    public static bool Inject(object target, IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= ServiceProvider;
        if (serviceProvider == null)
            return false;

        var type = target.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var injectAttribute = property.GetCustomAttribute<InjectAttribute>();
            if (injectAttribute == null)
                continue;

            if (!property.CanWrite)
            {
                Console.WriteLine($"[DI] Warning: Property '{property.Name}' on '{type.Name}' is marked with [Inject] but has no setter.");
                continue;
            }

            var service = serviceProvider.GetService(property.PropertyType);

            if (service == null && injectAttribute.Required)
            {
                throw new InvalidOperationException(
                    $"Required service of type '{property.PropertyType.Name}' could not be resolved for property '{property.Name}' on '{type.Name}'.");
            }

            if (service != null)
            {
                property.SetValue(target, service);
            }
        }

        return true;
    }

    /// <summary>
    /// Creates an instance of T and injects dependencies.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <param name="serviceProvider">The service provider to resolve dependencies from.</param>
    /// <returns>A new instance of T with dependencies injected.</returns>
    public static T CreateAndInject<T>(IServiceProvider? serviceProvider = null) where T : new()
    {
        var instance = new T();
        Inject(instance, serviceProvider);
        return instance;
    }

    /// <summary>
    /// Creates an instance using the service provider's ActivatorUtilities (supports constructor injection)
    /// and then injects property dependencies.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <param name="serviceProvider">The service provider to resolve dependencies from.</param>
    /// <param name="parameters">Additional constructor parameters.</param>
    /// <returns>A new instance of T with dependencies injected.</returns>
    public static T CreateWithServices<T>(IServiceProvider? serviceProvider = null, params object[] parameters)
    {
        serviceProvider ??= ServiceProvider;
        if (serviceProvider == null)
            throw new InvalidOperationException("No service provider available.");

        var instance = ActivatorUtilities.CreateInstance<T>(serviceProvider, parameters);
        if (instance != null)
        {
            Inject(instance, serviceProvider);
        }
        return instance;
    }
}
