using System;

namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Central hot reload dispatcher for platforms that need to coordinate UI rebuilds.
/// Provides a bridge between .NET Hot Reload events and platform-specific UI updates.
/// </summary>
public static class HotReloadMediator
{
    /// <summary>
    /// Raised when a hot reload event occurs and the UI should be rebuilt.
    /// </summary>
    public static event Action<Type[]?>? ReloadRequested;

    /// <summary>
    /// Notifies all subscribers that a hot reload event has occurred.
    /// </summary>
    /// <param name="updatedTypes">The types that were updated, or null if unknown.</param>
    public static void NotifyReload(Type[]? updatedTypes)
    {
        try
        {
            ReloadRequested?.Invoke(updatedTypes);
        }
        catch (Exception ex)
        {
            // Log to console as we don't have logger infrastructure in abstractions
            Console.WriteLine($"[HotReloadMediator] Error notifying listeners: {ex.Message}");
        }
    }
}
