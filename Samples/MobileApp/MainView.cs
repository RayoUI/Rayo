using MobileApp.Components;
using MobileApp.Pages;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace MobileApp;

public class MainView : Component
{
    private readonly Signal<AppRoute> _currentRoute = new(AppRoute.Home);
    private readonly Signal<int> _counter = new(0);
    private readonly Signal<bool> _canGoBack = new(false);
    private readonly List<AppRoute> _backStack = new();
    private readonly Computed<string> _counterText;
    private readonly Computed<string> _title;
    private Drawer? _drawer;
    private Frame? _contentHost;

    public MainView()
    {
        _counterText = UseComputed(() => _counter.Value.ToString());
        _title = UseComputed(() => _currentRoute.Value switch
        {
            AppRoute.Counter => "Counter",
            AppRoute.Details => "Details",
            AppRoute.Profile => "Profile",
            AppRoute.Settings => "Settings",
            _ => "Home"
        });
    }

    protected override void OnInit()
    {
        UseSubscription(_currentRoute, _ => UIUpdateQueue.EnqueueUIUpdate(UpdateContent));
    }

    public override VisualElement Build()
    {
        _drawer = new Drawer()
            .Position(DrawerPosition.Left)
            .DrawerWidth(300)
            .Background(Color.White)
            .Content(BuildDrawerContent());

        _contentHost = new Frame()
            .Background(new Color(246, 248, 252))
            .Padding(new Thickness(0))
            .Content(CreatePage(_currentRoute.Value));

        return new Grid()
            .Rows(GridLength.Pixels(60), GridLength.Star)
            .Columns(GridLength.Star)
            .Background(new Color(246, 248, 252))
            .AddChild(
                new AppBar(
                    _title,
                    _canGoBack,
                    GoBack,
                    () => _drawer?.Open(),
                    CreateOverflowItems(_currentRoute.Value)),
                0,
                0)
            .AddChild(_contentHost, 1, 0);
    }

    private VisualElement BuildDrawerContent()
    {
        return new VStack()
            .Background(Color.White)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                new Frame()
                    .Height(104)
                    .Background(new Color(25, 39, 62))
                    .Padding(new Thickness(20, 26, 20, 22))
                    .Content(
                        new VStack()
                            .Spacing(6)
                            .Children(
                                new Label("MobileApp")
                                    .FontSize(20)
                                    .Foreground(Color.White),
                                new Label("Drawer navigation starter")
                                    .FontSize(13)
                                    .Foreground(new Color(196, 210, 232))
                            )),
                new VStack()
                    .Spacing(8)
                    .Padding(new Thickness(12, 16))
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Children(
                        CreateDrawerItem("Home", Icons.Home, AppRoute.Home),
                        CreateDrawerItem("Counter", Icons.Add, AppRoute.Counter),
                        CreateDrawerItem("Details", Icons.Info, AppRoute.Details),
                        CreateDrawerItem("Profile", Icons.Person, AppRoute.Profile),
                        CreateDrawerItem("Settings", Icons.Settings, AppRoute.Settings)
                    ));
    }

    private VisualElement CreateDrawerItem(string text, IconData icon, AppRoute route)
    {
        var button = new Button()
            .Height(48)
            .Text(text)
            .TextAlignment(HorizontalAlignment.Left)
            .Padding(new Thickness(16, 0, 16, 0))
            .TextColor(_currentRoute.Value == route ? Color.White : new Color(45, 55, 72))
            .Background(_currentRoute.Value == route ? new Color(62, 126, 214) : Color.Transparent)
            .HoverBackground(_currentRoute.Value == route ? new Color(62, 126, 214) : new Color(232, 238, 247))
            .PressedBackground(new Color(207, 219, 236))
            .BorderThickness(0)
            .BorderRadius(8)
            .OnTapped(() =>
            {
                NavigateRoot(route);
                Drawer.CloseCurrentDrawer();
            });

        return button;
    }

    private void Navigate(AppRoute route)
    {
        Navigate(route, trackHistory: true);
    }

    private void NavigateRoot(AppRoute route)
    {
        _backStack.Clear();
        _canGoBack.Value = false;
        Navigate(route, trackHistory: false);
    }

    private void Navigate(AppRoute route, bool trackHistory)
    {
        if (_currentRoute.Value == route)
        {
            return;
        }

        if (trackHistory)
        {
            _backStack.Add(_currentRoute.Value);
            _canGoBack.Value = true;
        }

        _currentRoute.Value = route;
    }

    private void GoBack()
    {
        if (_backStack.Count == 0)
        {
            return;
        }

        var previousRoute = _backStack[^1];
        _backStack.RemoveAt(_backStack.Count - 1);
        _canGoBack.Value = _backStack.Count > 0;
        Navigate(previousRoute, trackHistory: false);
    }

    private void UpdateContent()
    {
        if (_contentHost is null)
        {
            return;
        }

        _contentHost.Content(CreatePage(_currentRoute.Value));
        Rebuild();
    }

    private VisualElement CreatePage(AppRoute route) => route switch
    {
        AppRoute.Counter => new CounterPage(_counter, _counterText),
        AppRoute.Details => new DetailsPage(Navigate),
        AppRoute.Profile => new ProfilePage(),
        AppRoute.Settings => new SettingsPage(),
        _ => new HomePage()
    };

    private IReadOnlyList<AppBarOverflowItem> CreateOverflowItems(AppRoute route)
    {
        if (route == AppRoute.Counter)
        {
            return
            [
                new AppBarOverflowItem("Reset counter", Icons.Refresh, () => _counter.Value = 0),
                new AppBarOverflowItem("Open settings", Icons.Settings, () => NavigateRoot(AppRoute.Settings))
            ];
        }

        if (route != AppRoute.Home)
        {
            return [];
        }

        return
        [
            new AppBarOverflowItem("About MobileApp", Icons.Info, () => ToastService.ShowInfo("MobileApp starter template")),
            new AppBarOverflowItem("Open settings", Icons.Settings, () => NavigateRoot(AppRoute.Settings))
        ];
    }
}
