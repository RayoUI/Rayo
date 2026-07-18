using Nano.Components;
using Nano.Views.ProjectAssetStore;
using Nano.GameEngine;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Nano.Views.Game;

public sealed class GamePage(IProjectAssetStore projectStore, Action goBack) : Component
{
    private readonly Signal<string> _title = new("Game");
    private readonly Signal<bool> _canGoBack = new(true);
    private readonly Signal<bool> _canPlay = new(false);
    private readonly NanoGameInputState _input = new();

    public override VisualElement Build()
    {
        return new Grid()
            .Rows(GridLength.Pixels(60), GridLength.Star)
            .Columns(GridLength.Star)
            .Background(new Color(7, 10, 16))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddChild(
                new AppBar(
                    _title,
                    _canGoBack,
                    goBack,
                    () => { },
                    _canPlay,
                    () => { },
                    []),
                0,
                0)
            .AddChild(
                new Grid()
                    .Rows(GridLength.Star)
                    .Columns(GridLength.Star)
                    .AddChild(
                        new NanoGameView(projectStore, _input)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch),
                        0,
                        0)
                    .AddChild(
                        new VirtualGameControls(_input)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch),
                        0,
                        0),
                1,
                0);
    }
}
