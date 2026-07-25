using Nano.Views.ProjectAssetStore;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal sealed class AssetCollectionView : Component
{
    private readonly AssetViewMode _viewMode;
    private readonly string _currentDirectory;
    private readonly IReadOnlyList<VirtualAsset> _assets;
    private readonly Action _navigateUp;
    private readonly Action<VirtualAsset> _openAsset;

    public AssetCollectionView(
        AssetViewMode viewMode,
        string currentDirectory,
        IReadOnlyList<VirtualAsset> assets,
        Action navigateUp,
        Action<VirtualAsset> openAsset)
    {
        _viewMode = viewMode;
        _currentDirectory = currentDirectory;
        _assets = assets;
        _navigateUp = navigateUp;
        _openAsset = openAsset;
    }

    public override VisualElement Build() =>
        _viewMode == AssetViewMode.List ? BuildList() : BuildGrid();

    private VisualElement BuildList()
    {
        const float itemHeight = 40;
        const float itemSpacing = 6;
        var entries = CreateEntries();
        if (entries.Count == 0)
        {
            return CreateEmptyLabel();
        }

        return new VStack()
            .Height(entries.Count * itemHeight + (entries.Count - 1) * itemSpacing)
            .Spacing(itemSpacing)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(entries
                .Select(entry => CreateListItem(entry.Name, entry.IsDirectory, entry.Action))
                .ToArray());
    }

    private VisualElement BuildGrid()
    {
        const int columnCount = 3;
        const float gridGap = 8;
        var entries = CreateEntries();
        if (entries.Count == 0)
        {
            return CreateEmptyLabel();
        }

        var grid = new Grid()
            .Columns(GridLength.Star, GridLength.Star, GridLength.Star)
            .ColumnSpacing(gridGap)
            .RowSpacing(gridGap)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        for (var row = 0; row < (int)Math.Ceiling(entries.Count / (double)columnCount); row++)
        {
            grid.RowDefinitions.Add(GridLength.Pixels(104));
        }

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            grid.AddChild(
                CreateGridItem(entry.Name, entry.IsDirectory, entry.Action),
                index / columnCount,
                index % columnCount);
        }

        return grid;
    }

    private List<AssetEntry> CreateEntries()
    {
        var entries = new List<AssetEntry>();
        if (!string.IsNullOrEmpty(_currentDirectory))
        {
            entries.Add(new AssetEntry("..", true, _navigateUp));
        }

        entries.AddRange(_assets.Select(asset =>
            new AssetEntry(asset.Name, asset.IsDirectory, () => _openAsset(asset))));
        return entries;
    }

    private static VisualElement CreateListItem(string text, bool isDirectory, Action action)
    {
        return new AssetTile(action)
            .Height(40)
            .BorderThickness(0)
            .BorderRadius(8)
            .Padding(new Thickness(10, 0))
            .Content(
                new Grid()
                    .Rows(GridLength.Star)
                    .Columns(GridLength.Pixels(28), GridLength.Star)
                    .ColumnSpacing(8)
                    .AddChild(
                        new Icon(AssetIconResolver.GetIcon(text, isDirectory))
                            .Size(19)
                            .Color(AssetIconResolver.GetColor(isDirectory))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center),
                        0,
                        0)
                    .AddChild(
                        CreateLabel(text, 14, HorizontalAlignment.Left).Height(40),
                        0,
                        1));
    }

    private static VisualElement CreateGridItem(string text, bool isDirectory, Action action)
    {
        return new AssetTile(action)
            .BorderBrush(new Color(51, 65, 85))
            .BorderThickness(1)
            .BorderRadius(8)
            .Padding(new Thickness(6))
            .Content(
                new VStack()
                    .Spacing(6)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Children(
                        new Icon(AssetIconResolver.GetIcon(text, isDirectory))
                            .Size(30)
                            .Color(AssetIconResolver.GetColor(isDirectory))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center),
                        CreateLabel(text, 12, HorizontalAlignment.Center)));
    }

    private static Label CreateLabel(string text, float fontSize, HorizontalAlignment textAlignment) =>
        new Label()
            .Text(text)
            .FontSize(fontSize)
            .Foreground(new Color(226, 232, 240))
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .TextHorizontalAlignment(textAlignment)
            .TextVerticalAlignment(VerticalAlignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center);

    private static Label CreateEmptyLabel() =>
        new Label("This folder is empty")
            .FontSize(13)
            .Foreground(new Color(148, 163, 184));

    private sealed record AssetEntry(string Name, bool IsDirectory, Action Action);
}
