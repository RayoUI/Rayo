using MobileApp.Pages;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace MobileApp;

public enum AppRoute
{
    Home,
    Details,
    Profile,
    Settings
}

public class MainView : Component
{
    private readonly Signal<AppRoute> _currentRoute = new(AppRoute.Home);
    private readonly Signal<int> _counter = new(0);
    private readonly Computed<string> _counterText;
    private readonly Computed<string> _title;
    private Drawer? _drawer;
    private Frame? _contentHost;

    public MainView()
    {
        _counterText = UseComputed(() => _counter.Value.ToString());
        _title = UseComputed(() => _currentRoute.Value switch
        {
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
            .AddChild(BuildAppBar(), 0, 0)
            .AddChild(_contentHost, 1, 0);
    }

    private VisualElement BuildAppBar()
    {
        return new Frame()
            .Height(60)
            .Background(new Color(25, 39, 62))
            .Padding(new Thickness(8, 8))
            .Content(
                new HStack()
                    .Height(44)
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new ButtonIcon(Icons.Menu)
                            .Size(44)
                            .IconSize(24)
                            .IconColor(Color.White)
                            .Background(Color.Transparent)
                            .HoverBackground(new Color(43, 63, 94))
                            .PressedBackground(new Color(16, 27, 43))
                            .BorderWidth(0)
                            .OnTapped(() => _drawer?.Open()),
                        new Label()
                            .Text(_title)
                            .FontSize(18)
                            .Foreground(Color.White)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ));
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
            .BorderWidth(0)
            .BorderRadius(8)
            .OnTapped(() =>
            {
                Navigate(route);
                Drawer.CloseCurrentDrawer();
            });

        return button;
    }

    private void Navigate(AppRoute route)
    {
        if (_currentRoute.Value == route)
        {
            return;
        }

        _currentRoute.Value = route;
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
        AppRoute.Details => new DetailsPage(),
        AppRoute.Profile => new ProfilePage(),
        AppRoute.Settings => new SettingsPage(),
        _ => new HomePage(_counter, _counterText)
    };
}
