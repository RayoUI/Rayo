using Nano.Views.ProjectAssetStore;
using Nano.Views.ProjectAssetStore.Components;
using Rayo.Core;
using Rayo.Reactivity;

namespace Nano.ViewModels;

public sealed class ProjectAssetExplorerViewModel : ViewModelBase
{
    private readonly IProjectAssetStore _store;
    private readonly ProjectAssetNavigator _navigator;
    private readonly Action<string>? _currentDirectoryChanged;

    public ProjectAssetExplorerViewModel(
        IProjectAssetStore store,
        string initialDirectory = "",
        Action<string>? currentDirectoryChanged = null)
    {
        _store = store;
        _navigator = new ProjectAssetNavigator(store);
        _navigator.NavigateTo(initialDirectory);
        _currentDirectoryChanged = currentDirectoryChanged;
        CurrentDirectory = UseSignal(_navigator.CurrentDirectory);
        ViewMode = UseSignal(_navigator.ViewMode);
        Revision = UseSignal(0);
        ProjectSubtitle = UseComputed(
            () => $"{Path.GetFileName(_store.ArchivePath)} · ZIP project");
    }

    public Signal<string> CurrentDirectory { get; }
    public Signal<AssetViewMode> ViewMode { get; }
    public Signal<int> Revision { get; }
    public Computed<string> ProjectSubtitle { get; }
    public IReadOnlyList<VirtualAsset> Assets => _navigator.GetCurrentAssets();

    public void SetViewMode(AssetViewMode mode)
    {
        if (!_navigator.SetViewMode(mode))
            return;

        ViewMode.Value = _navigator.ViewMode;
        NotifyChanged();
    }

    public void NavigateTo(string directory)
    {
        _navigator.NavigateTo(directory);
        PublishCurrentDirectory();
    }

    public void NavigateUp()
    {
        _navigator.NavigateUp();
        PublishCurrentDirectory();
    }

    public AssetOpenResult OpenAsset(VirtualAsset asset)
    {
        if (asset.IsDirectory)
        {
            _navigator.OpenDirectory(asset);
            PublishCurrentDirectory();
            return AssetOpenResult.Directory;
        }

        if (_store.IsSpriteFile(asset.Path))
        {
            return AssetOpenResult.CreateSprite(
                asset.Path,
                _store.ReadText(asset.Path),
                text => _store.WriteText(asset.Path, text));
        }

        if (!_store.IsTextFile(asset.Path))
            return AssetOpenResult.Binary;

        return AssetOpenResult.CreateText(
            asset.Path,
            _store.ReadText(asset.Path),
            text => _store.WriteText(asset.Path, text));
    }

    public bool IsValidDirectoryName(string name) =>
        ProjectAssetNavigator.IsValidDirectoryName(name);

    public bool TryCreateDirectory(string name)
    {
        if (!IsValidDirectoryName(name))
            return false;

        try
        {
            _navigator.CreateDirectory(name);
            NotifyChanged();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public VirtualAsset? TryCreateSprite(string name)
    {
        try
        {
            var asset = _store.CreateSprite(_navigator.CurrentDirectory, name);
            NotifyChanged();
            return asset;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void NotifyChanged() => Revision.Value++;

    private void PublishCurrentDirectory()
    {
        CurrentDirectory.Value = _navigator.CurrentDirectory;
        _currentDirectoryChanged?.Invoke(_navigator.CurrentDirectory);
        NotifyChanged();
    }
}

public enum AssetOpenKind
{
    Directory,
    Text,
    Sprite,
    Binary
}

public sealed record AssetOpenResult(
    AssetOpenKind Kind,
    string? Path = null,
    string? Text = null,
    Action<string>? Save = null)
{
    public static AssetOpenResult Directory { get; } = new(AssetOpenKind.Directory);
    public static AssetOpenResult Binary { get; } = new(AssetOpenKind.Binary);

    public static AssetOpenResult CreateText(string path, string text, Action<string> save) =>
        new(AssetOpenKind.Text, path, text, save);

    public static AssetOpenResult CreateSprite(string path, string text, Action<string> save) =>
        new(AssetOpenKind.Sprite, path, text, save);
}
