using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;

namespace WeatherApp.Pages;

public sealed class FavoritesPage(WeatherStore store) : Component
{
    private string _query = "";

    public override VisualElement Build()
    {
        var list = store.Favorites
            .Where(location => location.Name.Contains(_query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cards = new Grid()
            .Columns(GridLength.Star, GridLength.Star)
            .RowSpacing(12)
            .ColumnSpacing(12);

        var rowCount = Math.Max(1, (list.Count + 1) / 2);
        cards.Rows(Enumerable.Repeat(GridLength.Auto, rowCount).ToArray());

        for (var i = 0; i < list.Count; i++)
        {
            cards.AddChild(BuildFavorite(list[i]), i / 2, i % 2);
        }

        return new ScrollView()
            .Background(WeatherUi.Background(store.IsDark))
            .Content(new VStack()
                .Padding(new Thickness(OperatingSystem.IsAndroid() ? 16 : 28))
                .Spacing(20)
                .Children(
                    new Label("Favorites").FontSize(26).Foreground(WeatherUi.Text(store.IsDark)),
                    new Entry()
                        .Placeholder("Search locations")
                        .Text(_query)
                        .Background(WeatherUi.Surface(store.IsDark))
                        .TextColor(WeatherUi.Text(store.IsDark))
                        .PlaceholderColor(WeatherUi.Muted(store.IsDark))
                        .OnTextChanged(value =>
                        {
                            _query = value;
                            Rebuild();
                        }),
                    cards,
                    new Button()
                        .Text("+  Add a location")
                        .Height(52)
                        .Background(WeatherUi.Surface(store.IsDark))
                        .TextColor(WeatherUi.Accent)
                        .BorderThickness(0)
                        .OnTapped(() =>
                        {
                            if (store.Favorites.All(x => x.Name != "Seoul"))
                            {
                                store.Favorites.Add(new FavoriteLocation(
                                    "Seoul", "South Korea", "fluent_weather_sunny_20_filled.svg", 56, 4, 39));
                                Rebuild();
                            }
                        })));
    }

    private VisualElement BuildFavorite(FavoriteLocation location) =>
        WeatherUi.Card(store.IsDark,
            new VStack()
                .Height(154)
                .Spacing(8)
                .Children(
                    new HStack()
                        .JustifyContent(JustifyContent.SpaceBetween)
                        .Children(
                            new Label($"{store.DisplayTemperature(location.Fahrenheit)}°")
                                .FontSize(24).Foreground(WeatherUi.Text(store.IsDark)),
                            WeatherUi.WeatherImage(location.Icon, 58)),
                    new Label(location.Name).FontSize(14).Foreground(WeatherUi.Text(store.IsDark)),
                    new Label(location.Country).FontSize(11).Foreground(WeatherUi.Muted(store.IsDark)),
                    new HStack()
                        .Spacing(18)
                        .Children(
                            new Label($"☂ {location.Rain}%").FontSize(10).Foreground(WeatherUi.Muted(store.IsDark)),
                            new Label($"◉ {location.Humidity}%").FontSize(10).Foreground(WeatherUi.Muted(store.IsDark)))));
}
