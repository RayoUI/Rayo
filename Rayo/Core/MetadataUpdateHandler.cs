using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Rayo.Core.MetadataUpdateHandler))]

namespace Rayo.Core
{
    /// <summary>
    /// Handler de actualizaci�n de metadatos para .NET Hot Reload.
    /// Este m�todo se llama autom�ticamente cuando Visual Studio aplica cambios con Hot Reload.
    /// </summary>
    internal static class MetadataUpdateHandler
    {
        /// <summary>
        /// Llamado por el runtime de .NET cuando los metadatos se actualizan durante Hot Reload.
        /// </summary>
        /// <param name="types">Tipos que fueron actualizados</param>
        public static void UpdateApplication(Type[]? types)
        {
            Console.WriteLine($"[MetadataUpdateHandler] Cambios detectados en {types?.Length ?? 0} tipos");

            // Notificar al HotReloadManager
            HotReloadManager.UpdateApplication(types);
        }
    }
}