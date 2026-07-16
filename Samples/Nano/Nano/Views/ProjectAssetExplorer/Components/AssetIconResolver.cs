using Rayo;
using Rayo.Controls;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal static class AssetIconResolver
{
    public static IconData GetIcon(string name, bool isDirectory)
    {
        if (isDirectory)
        {
            return Icons.Folder;
        }

        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => Icons.Image,
            ".sprite" => Icons.Image,
            _ => Icons.File
        };
    }

    public static Color GetColor(bool isDirectory) =>
        isDirectory ? new Color(250, 204, 21) : new Color(148, 163, 184);
}
