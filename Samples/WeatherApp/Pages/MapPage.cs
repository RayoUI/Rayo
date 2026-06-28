using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;

namespace WeatherApp.Pages;

public sealed class MapPage(WeatherStore store) : Component
{
    private int _zoom = 4;

    public override VisualElement Build()
    {
        var map = new Grid()
            .Columns(GridLength.Star)
            .Rows(GridLength.Star)
            .AddChild(new WindMapView().Zoom(_zoom), 0, 0)
            .AddChild(
                new VStack()
                    .Padding(new Thickness(14))
                    .Spacing(8)
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Children(
                        MapButton("+", () => { _zoom = Math.Min(8, _zoom + 1); Rebuild(); }),
                        MapButton("−", () => { _zoom = Math.Max(1, _zoom - 1); Rebuild(); })),
                0, 0);

        VisualElement body = map;
        if (!OperatingSystem.IsAndroid())
        {
            var metrics = new VStack().Padding(new Thickness(16)).Spacing(12);
            foreach (var metric in store.Metrics)
            {
                metrics.AddChild(WeatherUi.Card(store.IsDark,
                    new HStack().Spacing(12).Children(
                        WeatherUi.WeatherImage(metric.Icon, 34),
                        new VStack().Spacing(2).Children(
                            new Label(metric.Value).FontSize(18).Foreground(WeatherUi.Text(store.IsDark)),
                            new Label(metric.Title).FontSize(11).Foreground(WeatherUi.Muted(store.IsDark))))));
            }

            body = new Grid()
                .Columns(GridLength.Star, GridLength.Pixels(340))
                .Rows(GridLength.Star)
                .AddChild(map, 0, 0)
                .AddChild(new ScrollView().Background(WeatherUi.Background(store.IsDark)).Content(metrics), 0, 1);
        }

        return new Grid()
            .Rows(GridLength.Pixels(58), GridLength.Star)
            .Columns(GridLength.Star)
            .Background(WeatherUi.Background(store.IsDark))
            .AddChild(new Label("Wind Map")
                .FontSize(22)
                .Foreground(WeatherUi.Text(store.IsDark))
                .Padding(new Thickness(20, 14)), 0, 0)
            .AddChild(body, 1, 0);
    }

    private Button MapButton(string text, Action action) =>
        new Button()
            .Text(text)
            .Size(42)
            .FontSize(22)
            .Background(WeatherUi.Surface(store.IsDark))
            .TextColor(WeatherUi.Text(store.IsDark))
            .BorderThickness(0)
            .BorderRadius(21)
            .OnTapped(action);
}
