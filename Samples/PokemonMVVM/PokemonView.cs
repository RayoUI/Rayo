#nullable enable

using Pokemon.Services.Interfaces;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

namespace Pokemon;

public class PokemonView : ViewBase<PokemonViewModel>
{
    [Inject] public ILoggerService? Logger { get; private set; }

    protected override StyleSheet? BuildStyles() =>
    [
        new Style<Button>("#load")
            .Background(Color.Red),
        new Style<Button>(".btn1")
            .TextColor(Color.Black)
            .TextAlignment(HorizontalAlignment.Center)
            .Background(Color.Gray),
        new Style<Button>(".btn2")
            .Background(Color.Blue)

    ];

    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(10)
            .Padding(new Thickness(20))
            .Alignment(Alignment.Stretch)
            .Children(
                BuildToolbar(),
                BuildContent()
            );
    }

    private VisualElement BuildToolbar()
    {
        return new HStack()
            .Spacing(10)
            .Height(40)
            .JustifyContent(JustifyContent.Center)
            .Children(
                new Button()
                    .Id("load")
                    .Classes("omega btn0")
                    .Text("Load Pokemons")
                    .OnTapped(async () => await ViewModel.LoadPokemonsAsync()),
                new Button()
                    .Classes("omega btn1")
                    .Text("Clear All")
                    .OnTapped(() => ViewModel.ClearAll()),
                new Button()
                    .Text("Remove First")
                    .Classes("omega btn2")
                    .OnTapped(() => ViewModel.RemoveFirstItem())
            );
    }

    private VisualElement BuildContent()
    {
        return new HStack()
            .Spacing(10)
            //.Padding(new Thickness(top: 20))
            .Children(
                BuildPokemonList(),
                BuildPokemonDetail()
            );
    }

    private VisualElement BuildPokemonList()
    {
        Logger?.Log($"BuildPokemonList called. ViewModel.Items count: {ViewModel.Items.Value.Count}");

        var _listView = new ListView<string>()
            .Items(ViewModel.Items)
            .Width(200)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .OnItemSelected((item, index) =>
            {
                _ = ViewModel.LoadPokemonSpriteAsync(item);
            });

        Logger?.Log($"ListView created and SetItems called");

        return new Frame()
            .Padding(new Thickness(10))
            .Background(Color.DarkGray)
            .BorderRadius(8)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(_listView);
    }

    private VisualElement BuildPokemonDetail()
    {
        return new VStack()
            .Padding(new Thickness(10))
            .Spacing(10)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                // Title at top
                new Label()
                    .Text(ViewModel.DisplayTitle)
                    .FontSize(18)
                    .Foreground(Color.Red)
                    .HorizontalAlignment(HorizontalAlignment.Center),
                // Content area with Loading and Image
                new VStack()
                    .Spacing(10)
                    .Alignment(Alignment.Center)
                    .JustifyContent(JustifyContent.Center)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Children(
                        // Loading spinner
                        new Loading()
                            .Type(SpinnerType.Circle)
                            .Size(60)
                            .Color(new Color(59, 130, 246))
                            .Text("Loading...")
                            .IsVisible(ViewModel.ShowLoading)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        // Pokemon image
                        new Image()
                            .Source(ViewModel.Avatar)
                            .IsVisible(ViewModel.ShowImage)
                            .Size(new Size(150, 150))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    )
            );
    }

    protected override void OnViewModelReady()
    {
        // Auto-load pokemons when view is ready
        _ = ViewModel.LoadPokemonsAsync();
    }
}
