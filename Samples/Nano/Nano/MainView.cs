using Nano.Components;
using Nano.Navigation;
using Nano.Views;
using Nano.Views.Game;
using Nano.Views.ProjectAssetStore;
using Nano.ViewModels;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano;

public sealed class MainView : ViewBase<MainViewModel>
{
    private readonly HomePage _homePage;
    private readonly NanoNavigationStack _navigation = new();
    private Drawer? _drawer;
    private Frame? _navigationHost;

    public MainView()
        : this(new NanoProjectStore(), new HomePage())
    {
    }

    internal MainView(IProjectAssetStore projectStore, HomePage homePage)
    {
        _homePage = homePage;
        SetViewModel(new MainViewModel(projectStore));
    }

    public override VisualElement Build()
    {
        _drawer = new Drawer()
            .Position(DrawerPosition.Left)
            .DrawerWidth(320)
            .Background(new Color(20, 27, 40))
            .ContentFactory(CreateAssetExplorer);

        var rootPage = BuildRootPage();
        _navigation.SetRoot(rootPage);
        _navigationHost = new Frame()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(rootPage);
        return _navigationHost;
    }

    private VisualElement BuildRootPage()
    {
        return new Grid()
            .Rows(GridLength.Pixels(60), GridLength.Star)
            .Columns(GridLength.Star)
            .Background(new Color(12, 16, 24))
            .AddChild(
                new AppBar(
                    ViewModel.Title,
                    ViewModel.CanGoBack,
                    PopPage,
                    () => _drawer?.Open(),
                    ViewModel.CanPlay,
                    PushGamePage,
                    []),
                0,
                0)
            .AddChild(_homePage, 1, 0);
    }

    private void PushGamePage()
    {
        if (_navigationHost is null)
            return;

        var page = new GamePage(ViewModel.ProjectStore, PopPage);
        _navigation.Push(page);
        _navigationHost.Content = page;
    }

    private void PopPage()
    {
        if (_navigationHost is null)
            return;

        var previousPage = _navigation.Pop();
        if (previousPage is not null)
            _navigationHost.Content = previousPage;
    }

    private VisualElement CreateAssetExplorer()
    {
        var assetExplorer = new ProjectAssetExplorerView(
            _homePage,
            _homePage,
            () => Drawer.CloseCurrentDrawer());
        assetExplorer.SetViewModel(
            new ProjectAssetExplorerViewModel(ViewModel.ProjectStore));
        return assetExplorer;
    }
}
