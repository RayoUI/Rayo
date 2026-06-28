using Microsoft.Extensions.DependencyInjection;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using WeatherApp.Pages;

namespace WeatherApp;

public sealed class MainView : Component
{
    private readonly WeatherStore _store;
    private readonly Signal<AppRoute> _route = new(AppRoute.Home);
    private readonly HomePage _homePage;
    private readonly FavoritesPage _favoritesPage;
    private readonly MapPage _mapPage;
    private readonly SettingsPage _settingsPage;
    private Frame? _contentHost;

    public MainView()
    {
        _store = UIApplication.Current?.ServiceProvider?.GetService<WeatherStore>() ?? new WeatherStore();
        _homePage = new HomePage(_store);
        _favoritesPage = new FavoritesPage(_store);
        _mapPage = new MapPage(_store);
        _settingsPage = new SettingsPage(_store, ApplyTheme);
    }

    protected override void OnInit()
    {
        UseSubscription(_route, _ => UIUpdateQueue.EnqueueUIUpdate(RebuildShell));
    }

    public override VisualElement Build()
    {
        _contentHost = new Frame()
            .Background(WeatherUi.Background(_store.IsDark))
            .Padding(new Thickness(0))
            .BorderThickness(0)
            .Content(CreatePage());

        return OperatingSystem.IsAndroid() ? BuildPhoneShell() : BuildDesktopShell();
    }

    private VisualElement BuildDesktopShell() =>
        new Grid()
            .Columns(GridLength.Pixels(76), GridLength.Star)
            .Rows(GridLength.Star)
            .Background(WeatherUi.Background(_store.IsDark))
            .AddChild(BuildNavigation(vertical: true), 0, 0)
            .AddChild(_contentHost!, 0, 1);

    private VisualElement BuildPhoneShell() =>
        new Grid()
            .Columns(GridLength.Star)
            .Rows(GridLength.Star, GridLength.Pixels(72))
            .Background(WeatherUi.Background(_store.IsDark))
            .AddChild(_contentHost!, 0, 0)
            .AddChild(BuildNavigation(vertical: false), 1, 0);

    private VisualElement BuildNavigation(bool vertical)
    {
        var buttons = new[]
        {
            WeatherUi.NavButton("⌂", _route.Value == AppRoute.Home, _store.IsDark, () => Navigate(AppRoute.Home)),
            WeatherUi.NavButton("♡", _route.Value == AppRoute.Favorites, _store.IsDark, () => Navigate(AppRoute.Favorites)),
            WeatherUi.NavButton("⌖", _route.Value == AppRoute.Map, _store.IsDark, () => Navigate(AppRoute.Map)),
            WeatherUi.NavButton("⚙", _route.Value == AppRoute.Settings, _store.IsDark, () => Navigate(AppRoute.Settings))
        };

        if (vertical)
        {
            return new VStack()
                .Background(WeatherUi.Surface(_store.IsDark))
                .Padding(new Thickness(10, 22))
                .Spacing(18)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(buttons);
        }

        return new HStack()
            .Background(WeatherUi.Surface(_store.IsDark))
            .Padding(new Thickness(12, 8))
            .Spacing(12)
            .JustifyContent(JustifyContent.SpaceEvenly)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(buttons);
    }

    private VisualElement CreatePage() => _route.Value switch
    {
        AppRoute.Favorites => _favoritesPage,
        AppRoute.Map => _mapPage,
        AppRoute.Settings => _settingsPage,
        _ => _homePage
    };

    private void Navigate(AppRoute route)
    {
        if (_route.Value != route)
        {
            _route.Value = route;
        }
    }

    private void RebuildShell()
    {
        Rebuild();
    }

    private void ApplyTheme()
    {
        _homePage.Rebuild();
        _favoritesPage.Rebuild();
        _mapPage.Rebuild();
        _settingsPage.RebuildPreservingScroll();
        Rebuild();
    }
}
