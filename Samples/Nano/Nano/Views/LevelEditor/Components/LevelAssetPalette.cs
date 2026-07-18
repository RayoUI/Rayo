using Nano.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.LevelEditor.Components;

internal sealed class LevelAssetPalette : Component
{
    private readonly LevelEditorViewModel _viewModel;
    private readonly HStack _items = new();
    private Button? _tilesButton;
    private Button? _objectsButton;

    public LevelAssetPalette(LevelEditorViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public override VisualElement Build()
    {
        _tilesButton = CreateCategoryButton("Tiles", _viewModel.ShowTiles);
        _objectsButton = CreateCategoryButton("Objects", _viewModel.ShowObjects);
        Refresh();

        return new Frame()
            .Background(new Color(20, 27, 40))
            .BorderBrush(new Color(45, 55, 72))
            .BorderThickness(new Thickness(0, 1, 0, 0))
            .Padding(new Thickness(12, 10))
            .Content(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new HStack()
                            .Spacing(8)
                            .Children(_tilesButton, _objectsButton),
                        new ScrollView
                        {
                            Orientation = ScrollOrientation.Horizontal,
                            ShowHorizontalScrollbar = false
                        }.Content(_items)));
    }

    private Button CreateCategoryButton(string text, Action action) =>
        new Button()
            .Text(text)
            .Height(34)
            .MinWidth(90)
            .OnTapped(() =>
            {
                action();
                Refresh();
            });

    private void Refresh()
    {
        if (_tilesButton is null || _objectsButton is null)
            return;

        var showingTiles = _viewModel.SelectedCategory.Value == LevelAssetCategory.Tiles;
        _tilesButton.Variant(showingTiles ? ButtonVariant.Primary : ButtonVariant.Secondary);
        _objectsButton.Variant(showingTiles ? ButtonVariant.Secondary : ButtonVariant.Primary);
        _items.ClearChildren();
        _items.Spacing(8);

        if (showingTiles)
        {
            foreach (var tile in _viewModel.Tiles)
            {
                _items.AddChild(CreateAssetButton(
                    tile.Name,
                    tile.Color,
                    ReferenceEquals(tile, _viewModel.SelectedTile.Value),
                    () =>
                    {
                        _viewModel.SelectTile(tile);
                        Refresh();
                    }));
            }
        }
        else
        {
            foreach (var definition in _viewModel.Objects)
            {
                _items.AddChild(CreateAssetButton(
                    definition.Name,
                    definition.Color,
                    ReferenceEquals(definition, _viewModel.SelectedObject.Value),
                    () =>
                    {
                        _viewModel.SelectObject(definition);
                        Refresh();
                    }));
            }
        }
    }

    private static VisualElement CreateAssetButton(
        string name,
        Color color,
        bool selected,
        Action select) =>
        new Button()
            .Text(name)
            .Width(104)
            .Height(52)
            .Background(color)
            .TextColor(Color.White)
            .BorderBrush(selected ? Color.White : new Color(71, 85, 105))
            .BorderThickness(selected ? 3 : 1)
            .BorderRadius(7)
            .OnTapped(select);
}
