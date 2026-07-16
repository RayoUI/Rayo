using Nano.Views.ProjectAssetStore.Components;
using Nano.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore;

public sealed class ProjectAssetExplorerView : ViewBase<ProjectAssetExplorerViewModel>
{
    private readonly ITextAssetHost _documentHost;
    private readonly ISpriteAssetHost _spriteHost;
    private readonly Action _closeDrawer;
    private readonly AssetActionsPopup _actionsPopup = new();

    public ProjectAssetExplorerView(
        ITextAssetHost documentHost,
        ISpriteAssetHost spriteHost,
        Action closeDrawer)
    {
        _documentHost = documentHost;
        _spriteHost = spriteHost;
        _closeDrawer = closeDrawer;
    }

    public override VisualElement Build()
    {
        return new Frame()
            .Padding(new Thickness(0))
            .BorderThickness(0)
            .Background(new Color(20, 27, 40))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(BuildExplorerContent())
            .React(
                ViewModel.Revision,
                (host, _) => UIUpdateQueue.EnqueueUIUpdate(() =>
                {
                    _actionsPopup.Close();
                    host.Content(BuildExplorerContent());
                }));
    }

    private VisualElement BuildExplorerContent() =>
        new VStack()
            .Background(new Color(20, 27, 40))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                BuildHeader(),
                BuildToolbar(),
                BuildBrowser());

    private VisualElement BuildHeader() =>
        new Frame()
            .Height(112)
            .Background(new Color(25, 39, 62))
            .Padding(new Thickness(20, 26, 20, 22))
            .Content(
                new VStack()
                    .Spacing(6)
                    .Children(
                        new Label("Nano assets")
                            .FontSize(20)
                            .Foreground(Color.White),
                        new Label()
                            .Text(ViewModel.ProjectSubtitle)
                            .FontSize(13)
                            .Foreground(new Color(196, 210, 232))));

    private VisualElement BuildToolbar()
    {
        ButtonIcon? moreButton = null;
        moreButton = new ButtonIcon(Icons.MoreVert)
            .Size(34)
            .IconSize(18)
            .IconColor(new Color(203, 213, 225))
            .Variant(ButtonVariant.Ghost)
            .OnTapped(() => _actionsPopup.Toggle(
                moreButton!,
                ShowCreateFolderDialog,
                ShowCreateSpriteDialog,
                ViewModel.SetViewMode,
                this));

        return new Grid()
            .Columns(GridLength.Star, GridLength.Auto)
            .Rows(GridLength.Pixels(34))
            .Height(48)
            .Padding(new Thickness(12, 14, 12, 0))
            .AddChild(
                new AssetBreadcrumb(
                    ViewModel.CurrentDirectory.Value,
                    ViewModel.NavigateTo),
                0,
                0)
            .AddChild(moreButton, 0, 1);
    }

    private VisualElement BuildBrowser() =>
        new ScrollView()
            .Padding(new Thickness(12, 16))
            .Content(
                new AssetCollectionView(
                    ViewModel.ViewMode.Value,
                    ViewModel.CurrentDirectory.Value,
                    ViewModel.Assets,
                    ViewModel.NavigateUp,
                    OpenAsset))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

    private void ShowCreateFolderDialog() =>
        NewFolderDialog.Show(
            ViewModel.TryCreateDirectory,
            ViewModel.IsValidDirectoryName);

    private void ShowCreateSpriteDialog() =>
        NewSpriteDialog.Show(ViewModel.TryCreateSprite, OpenAsset);

    private void OpenAsset(VirtualAsset asset)
    {
        var result = ViewModel.OpenAsset(asset);
        if (result.Kind == AssetOpenKind.Directory)
            return;

        if (result.Kind == AssetOpenKind.Binary)
        {
            ToastService.ShowInfo("Binary asset preview is not implemented yet.");
            return;
        }

        if (result.Kind == AssetOpenKind.Sprite)
        {
            _spriteHost.OpenSpriteAsset(
                result.Path!,
                result.Text!,
                result.Save!);
            _closeDrawer();
            return;
        }

        _documentHost.OpenTextAsset(
            result.Path!,
            result.Text!,
            result.Save!);
        _closeDrawer();
    }
}
