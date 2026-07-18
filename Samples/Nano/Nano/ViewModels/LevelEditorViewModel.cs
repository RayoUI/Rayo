using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Nano.ViewModels;

public enum LevelAssetCategory
{
    Tiles,
    Objects
}

public sealed record LevelTileDefinition(string Id, string Name, Color Color);
public sealed record LevelObjectDefinition(string Id, string Name, Color Color);
public sealed record LevelObjectInstance(string DefinitionId, int Column, int Row, Color Color);

public sealed class LevelEditorViewModel : ViewModelBase
{
    public LevelEditorViewModel()
    {
        Tiles =
        [
            new("grass", "Grass", new Color(82, 153, 88)),
            new("dirt", "Dirt", new Color(156, 108, 72)),
            new("water", "Water", new Color(54, 126, 190)),
            new("stone", "Stone", new Color(113, 124, 140))
        ];
        Objects =
        [
            new("player", "Player", new Color(62, 126, 214)),
            new("enemy", "Enemy", new Color(215, 72, 72)),
            new("tree", "Tree", new Color(45, 128, 79)),
            new("chest", "Chest", new Color(225, 142, 38))
        ];

        SelectedCategory = UseSignal(LevelAssetCategory.Tiles);
        SelectedTile = UseSignal<LevelTileDefinition?>(Tiles[0]);
        SelectedObject = UseSignal<LevelObjectDefinition?>(null);
        Revision = UseSignal(0);
    }

    public IReadOnlyList<LevelTileDefinition> Tiles { get; }
    public IReadOnlyList<LevelObjectDefinition> Objects { get; }
    public Dictionary<(int Column, int Row), LevelTileDefinition> TileMap { get; } = [];
    public List<LevelObjectInstance> ObjectInstances { get; } = [];
    public Signal<LevelAssetCategory> SelectedCategory { get; }
    public Signal<LevelTileDefinition?> SelectedTile { get; }
    public Signal<LevelObjectDefinition?> SelectedObject { get; }
    public Signal<int> Revision { get; }

    public void ShowTiles()
    {
        SelectedCategory.Value = LevelAssetCategory.Tiles;
        SelectedObject.Value = null;
        SelectedTile.Value ??= Tiles[0];
        NotifyChanged();
    }

    public void ShowObjects()
    {
        SelectedCategory.Value = LevelAssetCategory.Objects;
        SelectedTile.Value = null;
        SelectedObject.Value ??= Objects[0];
        NotifyChanged();
    }

    public void SelectTile(LevelTileDefinition tile)
    {
        SelectedCategory.Value = LevelAssetCategory.Tiles;
        SelectedObject.Value = null;
        SelectedTile.Value = tile;
        NotifyChanged();
    }

    public void SelectObject(LevelObjectDefinition definition)
    {
        SelectedCategory.Value = LevelAssetCategory.Objects;
        SelectedTile.Value = null;
        SelectedObject.Value = definition;
        NotifyChanged();
    }

    public void PlaceAt(int column, int row)
    {
        if (SelectedCategory.Value == LevelAssetCategory.Tiles &&
            SelectedTile.Value is { } tile)
        {
            if (TileMap.TryGetValue((column, row), out var current) &&
                ReferenceEquals(current, tile))
                return;

            TileMap[(column, row)] = tile;
            NotifyChanged();
            return;
        }

        if (SelectedObject.Value is not { } definition)
            return;

        ObjectInstances.RemoveAll(item => item.Column == column && item.Row == row);
        ObjectInstances.Add(new LevelObjectInstance(
            definition.Id,
            column,
            row,
            definition.Color));
        NotifyChanged();
    }

    public LevelTileDefinition? GetTileAt(int column, int row) =>
        TileMap.GetValueOrDefault((column, row));

    private void NotifyChanged() => Revision.Value++;
}
