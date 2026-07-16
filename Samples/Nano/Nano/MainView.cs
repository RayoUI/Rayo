using Nano.Components;
using Nano.Views;
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
    private Drawer? _drawer;

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

        return new Grid()
            .Rows(GridLength.Pixels(60), GridLength.Star)
            .Columns(GridLength.Star)
            .Background(new Color(12, 16, 24))
            .AddChild(
                new AppBar(
                    ViewModel.Title,
                    ViewModel.CanGoBack,
                    () => { },
                    () => _drawer?.Open(),
                    []),
                0,
                0)
            .AddChild(_homePage, 1, 0);
    }

    private VisualElement CreateAssetExplorer()
    {
        var assetExplorer = new ProjectAssetExplorerView(
            _homePage,
            () => Drawer.CloseCurrentDrawer());
        assetExplorer.SetViewModel(
            new ProjectAssetExplorerViewModel(ViewModel.ProjectStore));
        return assetExplorer;
    }
}
