using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace WeatherApp.Pages;

public sealed class SettingsPage(WeatherStore store, Action appChanged) : Component
{
    private ScrollView? _scrollView;
    private float _savedScrollOffset;

    public override VisualElement Build()
    {
        _scrollView = new ScrollView()
            .Background(WeatherUi.Background(store.IsDark))
            .Content(new VStack()
                .Padding(new Thickness(OperatingSystem.IsAndroid() ? 18 : 30))
                .Spacing(16)
                .Children(
                    BuildProfile(),
                    BuildPreview(),
                    WeatherUi.Heading("Units", store.IsDark),
                    UnitChoice("Imperial", "°F / mph / miles / inches", "imperial"),
                    UnitChoice("Metric", "°C / km/h / km / millimeters", "metric"),
                    UnitChoice("Hybrid", "°C / mph / miles / millimeters", "hybrid"),
                    WeatherUi.Heading("More", store.IsDark),
                    SettingRow("Support", "Weather TwentyOne sample for RayoUI"),
                    new HStack()
                        .JustifyContent(JustifyContent.SpaceBetween)
                        .Children(
                            new VStack().Spacing(2).Children(
                                new Label("Dark theme").FontSize(15).Foreground(WeatherUi.Text(store.IsDark)),
                                new Label("Use the original deep-blue palette").FontSize(11).Foreground(WeatherUi.Muted(store.IsDark))),
                            new ToggleSwitch()
                                .IsOn(store.IsDark)
                                .OnToggled(value =>
                                {
                                    store.IsDark = value;
                                    appChanged();
                                }))));

        if (_savedScrollOffset > 0)
        {
            _scrollView.VerticalScrollOffset = _savedScrollOffset;
        }

        return _scrollView;
    }

    public void RebuildPreservingScroll()
    {
        _savedScrollOffset = _scrollView?.VerticalScrollOffset ?? _savedScrollOffset;
        Rebuild();
    }

    private VisualElement BuildProfile() =>
        WeatherUi.Card(store.IsDark,
            new HStack()
                .Spacing(18)
                .Children(
                    new Frame().Size(62).BorderRadius(31).BorderThickness(0).Background(WeatherUi.Accent)
                        .Content(new Label("DO").FontSize(20).Foreground(Color.White)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)),
                    new VStack().Spacing(3).Children(
                        new Label("David Ortinau").FontSize(18).Foreground(WeatherUi.Text(store.IsDark)),
                        new Label("Weather enthusiast").FontSize(11).Foreground(WeatherUi.Muted(store.IsDark)))));

    private VisualElement BuildPreview() =>
        new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                WeatherUi.WeatherImage("fluent_weather_moon_16_filled.svg", 132),
                new Label($"{store.DisplayTemperature(70)}{store.TemperatureUnit}")
                    .FontSize(25).Foreground(WeatherUi.Text(store.IsDark)),
                new Frame().Background(WeatherUi.Purple).BorderRadius(20).BorderThickness(0)
                    .Padding(new Thickness(16, 4)).Content(new Label("Clear").Foreground(Color.White).FontSize(12)));

    private VisualElement UnitChoice(string title, string detail, string value)
    {
        var selected = store.Units == value || (store.Units == "hybrid" && value == "hybrid");
        return new Button()
            .Height(66)
            .Text($"{(selected ? "✓  " : "    ")}{title}\n    {detail}")
            .TextAlignment(HorizontalAlignment.Left)
            .TextColor(selected ? WeatherUi.Accent : WeatherUi.Text(store.IsDark))
            .Background(WeatherUi.Surface(store.IsDark))
            .HoverBackground(WeatherUi.SurfaceAlt(store.IsDark))
            .BorderThickness(0)
            .BorderRadius(10)
            .OnTapped(() =>
            {
                store.Units = value;
                RebuildPreservingScroll();
            });
    }

    private VisualElement SettingRow(string title, string detail) =>
        WeatherUi.Card(store.IsDark,
            new VStack().Spacing(3).Children(
                new Label(title).FontSize(15).Foreground(WeatherUi.Text(store.IsDark)),
                new Label(detail).FontSize(11).Foreground(WeatherUi.Muted(store.IsDark))));
}
