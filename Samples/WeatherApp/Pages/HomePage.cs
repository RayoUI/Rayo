using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace WeatherApp.Pages;

public sealed class HomePage(WeatherStore store) : Component
{
    public override VisualElement Build()
    {
        var content = new VStack()
            .Spacing(28)
            .Padding(new Thickness(24, 22, 24, 36))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                BuildHeader(),
                BuildHero(),
                BuildHourly(),
                BuildDaily());

        return new ScrollView()
            .Background(WeatherUi.Background(store.IsDark))
            .Content(content);
    }

    private VisualElement BuildHeader() =>
        new HStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .JustifyContent(JustifyContent.SpaceBetween)
            .Children(
                new VStack().Spacing(3).Children(
                    new Label(store.Location).FontSize(20).Foreground(WeatherUi.Text(store.IsDark)),
                    new Label($"Updated {store.LastUpdated:h:mm tt}").FontSize(11).Foreground(WeatherUi.Muted(store.IsDark))),
                new Button()
                    .Text("Refresh")
                    .Background(WeatherUi.Surface(store.IsDark))
                    .TextColor(WeatherUi.Accent)
                    .BorderThickness(0)
                    .OnTapped(() =>
                    {
                        store.LastUpdated = DateTime.Now;
                        Rebuild();
                    }));

    private VisualElement BuildHero()
    {
        var current = new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                WeatherUi.WeatherImage("weather_partly_cloudy_day.svg", OperatingSystem.IsAndroid() ? 210 : 230),
                new Label($"{store.DisplayTemperature(52)}{store.TemperatureUnit}")
                    .FontSize(28).Foreground(WeatherUi.Text(store.IsDark)),
                new Frame()
                    .Background(WeatherUi.Purple)
                    .BorderRadius(24)
                    .BorderThickness(0)
                    .Padding(new Thickness(18, 5))
                    .Content(new Label("Clear").FontSize(13).Foreground(Color.White)));

        var wind = new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                new Grid()
                    .Rows(GridLength.Star)
                    .Columns(GridLength.Star)
                    .Size(210)
                    .AddChild(WeatherUi.WeatherImage("compass_background.svg", 210), 0, 0)
                    .AddChild(WeatherUi.WeatherImage("compass_needle.svg", 210).Rotate(-18), 0, 0),
                new Label("Winds").FontSize(12).Foreground(WeatherUi.Muted(store.IsDark)),
                new Label(store.Units == "metric" ? "23 | 40 km/h" : "14 | 25 mph")
                    .FontSize(19).Foreground(WeatherUi.Text(store.IsDark)));

        var metrics = new Grid()
            .Columns(GridLength.Star, GridLength.Star)
            .Rows(GridLength.Auto, GridLength.Auto)
            .ColumnSpacing(10)
            .RowSpacing(10);

        for (var i = 0; i < 4; i++)
        {
            metrics.AddChild(BuildMetric(store.Metrics[i]), i / 2, i % 2);
        }

        if (OperatingSystem.IsAndroid())
        {
            return new VStack().Spacing(24).Children(
                WeatherUi.Card(store.IsDark, current),
                metrics);
        }

        return new Grid()
            .Columns(GridLength.Stars(1.1f), GridLength.Stars(1.1f), GridLength.Stars(1.6f))
            .Rows(GridLength.Auto)
            .ColumnSpacing(24)
            .AddChild(WeatherUi.Card(store.IsDark, current), 0, 0)
            .AddChild(WeatherUi.Card(store.IsDark, wind), 0, 1)
            .AddChild(metrics, 0, 2);
    }

    private VisualElement BuildMetric(WeatherMetric metric) =>
        WeatherUi.Card(store.IsDark,
            new VStack()
                .Spacing(3)
                .Children(
                    WeatherUi.WeatherImage(metric.Icon, 58),
                    new Label(metric.Value).FontSize(20).Foreground(WeatherUi.Text(store.IsDark)),
                    new Label(metric.Title).FontSize(12).Foreground(WeatherUi.Text(store.IsDark)),
                    new Label($"From {metric.Station}").FontSize(10).Foreground(WeatherUi.Muted(store.IsDark))));

    private VisualElement BuildHourly()
    {
        var row = new HStack().Spacing(18).Padding(new Thickness(2, 4));
        foreach (var hour in store.Hours)
        {
            row.AddChild(WeatherUi.Card(store.IsDark, new VStack()
                .Width(70)
                .Spacing(5)
                .Alignment(Alignment.Center)
                .Children(
                    new Label(hour.Time).FontSize(10).Foreground(WeatherUi.Muted(store.IsDark)),
                    WeatherUi.WeatherImage(hour.Icon, 50),
                    new Label($"{store.DisplayTemperature(hour.Temperature)}°")
                        .FontSize(14).Foreground(WeatherUi.Text(store.IsDark)))));
        }

        return new VStack().Spacing(10).Children(
            WeatherUi.Heading("Next 24 Hours", store.IsDark),
            new ScrollView().Orientation(ScrollOrientation.Horizontal).Height(160).Content(row));
    }

    private VisualElement BuildDaily()
    {
        var row = new HStack().Spacing(12).Padding(new Thickness(2, 4));
        foreach (var day in store.Week)
        {
            row.AddChild(WeatherUi.Card(store.IsDark,
                new VStack()
                    .Width(84)
                    .Spacing(5)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label(day.Day).FontSize(13).Foreground(WeatherUi.Text(store.IsDark)),
                        new Label($"{store.DisplayTemperature(day.High)}°").FontSize(14).Foreground(WeatherUi.Text(store.IsDark)),
                        WeatherUi.WeatherImage(day.Icon, 52),
                        new Frame().Width(9).Height(Math.Max(18, day.High - day.Low + 20)).BorderRadius(5)
                            .BorderThickness(0).Background(WeatherUi.Accent),
                        new Label($"{store.DisplayTemperature(day.Low)}°").FontSize(12).Foreground(WeatherUi.Muted(store.IsDark)),
                        new Label($"☂ {day.RainChance}%").FontSize(10).Foreground(WeatherUi.Muted(store.IsDark)))));
        }

        return new VStack().Spacing(10).Children(
            WeatherUi.Heading("Daily Forecasts", store.IsDark),
            new ScrollView().Orientation(ScrollOrientation.Horizontal).Height(268).Content(row));
    }
}
