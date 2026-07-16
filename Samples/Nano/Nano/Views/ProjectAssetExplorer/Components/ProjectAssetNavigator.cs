using Nano.Views.ProjectAssetStore;

namespace Nano.Views.ProjectAssetStore.Components;

public enum AssetViewMode
{
    List,
    Grid
}

public sealed class ProjectAssetNavigator
{
    private readonly IProjectAssetStore _store;

    public ProjectAssetNavigator(IProjectAssetStore store)
    {
        _store = store;
    }

    public string CurrentDirectory { get; private set; } = string.Empty;

    public AssetViewMode ViewMode { get; private set; } = AssetViewMode.List;

    public IReadOnlyList<VirtualAsset> GetCurrentAssets() =>
        _store.GetChildren(CurrentDirectory);

    public bool SetViewMode(AssetViewMode mode)
    {
        if (ViewMode == mode)
        {
            return false;
        }

        ViewMode = mode;
        return true;
    }

    public void NavigateTo(string directory)
    {
        CurrentDirectory = NormalizeDirectory(directory);
    }

    public void NavigateUp()
    {
        var separator = CurrentDirectory.LastIndexOf('/');
        NavigateTo(separator < 0 ? string.Empty : CurrentDirectory[..separator]);
    }

    public void OpenDirectory(VirtualAsset asset)
    {
        if (!asset.IsDirectory)
        {
            throw new ArgumentException("The asset must be a directory.", nameof(asset));
        }

        NavigateTo(asset.Path);
    }

    public void CreateDirectory(string name) =>
        _store.CreateDirectory(CurrentDirectory, name);

    public static bool IsValidDirectoryName(string name)
    {
        var trimmed = name.Trim();
        return !string.IsNullOrWhiteSpace(trimmed) &&
               !trimmed.Contains('/') &&
               !trimmed.Contains('\\') &&
               trimmed is not "." and not "..";
    }

    private static string NormalizeDirectory(string directory) =>
        directory.Trim().Trim('/').Replace('\\', '/');
}
