#if NET10_0_OR_GREATER
using System;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Rayo.Hosting.Abstractions.MetadataUpdateHandler))]

namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Receives .NET Hot Reload notifications and forwards them to active platform hosts.
/// </summary>
internal static class MetadataUpdateHandler
{
    public static void ClearCache(Type[]? updatedTypes)
    {
    }

    public static void UpdateApplication(Type[]? updatedTypes)
    {
        HotReloadMediator.NotifyReload(updatedTypes);
    }
}
#endif
