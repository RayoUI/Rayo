using System;
using System.Reflection.Metadata;
using Rayo.Hosting.Abstractions;

[assembly: MetadataUpdateHandler(typeof(Gallery.MetadataUpdateHandler))]

namespace Gallery;

/// <summary>
/// Assembly-level handler that bridges .NET Hot Reload notifications to platform-specific listeners.
/// </summary>
internal static class MetadataUpdateHandler
{
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        Console.WriteLine($"[CrossPlatformApp] Hot reload update with {updatedTypes?.Length ?? 0} types");
        HotReloadMediator.NotifyReload(updatedTypes);
    }

    public static void ClearCache(Type[]? _)
    {
        // No cached resources yet, but keep method for runtime compliance.
    }
}

