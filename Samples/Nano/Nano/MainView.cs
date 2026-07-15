using Nano.Assets;
using Nano.Components;
using Nano.Pages;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Nano;

public class MainView : Component
{
    private enum AssetViewMode
    {
        List,
        Grid
    }

    private readonly NanoProjectStore _project = new();
    private readonly HomePage _homePage = new();
    private readonly Signal<string> _title = new("Nano");
    private readonly Signal<bool> _canGoBack = new(false);
    private Drawer? _drawer;
    private VisualElement? _assetActionsOverlay;
    private string _assetDirectory = string.Empty;
    private AssetViewMode _assetViewMode = AssetViewMode.List;

    public override VisualElement Build()
    {
        _drawer = new Drawer()
            .Position(DrawerPosition.Left)
            .DrawerWidth(320)
            .Background(new Color(20, 27, 40))
            .Content(BuildDrawerContent());

        return new Grid()
            .Rows(GridLength.Pixels(60), GridLength.Star)
            .Columns(GridLength.Star)
            .Background(new Color(12, 16, 24))
            .AddChild(
                new AppBar(
                    _title,
                    _canGoBack,
                    () => { },
                    () => _drawer?.Open(),
                    []),
                0,
                0)
            .AddChild(_homePage, 1, 0);
    }

    private VisualElement BuildDrawerContent()
    {
        return new VStack()
            .Background(new Color(20, 27, 40))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
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
                                new Label($"{Path.GetFileName(_project.ArchivePath)} · ZIP project")
                                    .FontSize(13)
                                    .Foreground(new Color(196, 210, 232))
                            )),
                BuildAssetToolbar(),
                BuildAssetBrowser());
    }

    private VisualElement BuildAssetToolbar()
    {
        ButtonIcon? moreButton = null;
        moreButton = new ButtonIcon(Icons.MoreVert)
            .Size(34)
            .IconSize(18)
            .IconColor(new Color(203, 213, 225))
            .Variant(ButtonVariant.Ghost)
            .OnTapped(() => ToggleAssetActionsMenu(moreButton!));

        return new Grid()
            .Columns(GridLength.Star, GridLength.Auto)
            .Rows(GridLength.Pixels(34))
            .Height(48)
            .Padding(new Thickness(12, 14, 12, 0))
            .AddChild(BuildBreadcrumb(), 0, 0)
            .AddChild(moreButton, 0, 1);
    }

    private VisualElement BuildBreadcrumb()
    {
        const float maximumSegmentWidth = 112;
        var segments = new List<VisualElement>();

        var currentPath = string.Empty;
        foreach (var segment in _assetDirectory.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
            var targetPath = currentPath;
            segments.Add(new Label("/")
                .FontSize(13)
                .Foreground(new Color(100, 116, 139))
                .VerticalAlignment(VerticalAlignment.Center));
            segments.Add(new BreadcrumbSegment(
                segment,
                maximumSegmentWidth,
                () => NavigateTo(targetPath)));
        }

        return new HStack()
            .Spacing(2)
            .Height(34)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .Children(
                new ButtonIcon(Icons.Home)
                    .Size(34)
                    .IconSize(18)
                    .IconColor(Color.White)
                    .Variant(ButtonVariant.Ghost)
                    .OnTapped(() => NavigateTo(string.Empty)),
                new ScrollView
                {
                    Orientation = ScrollOrientation.Horizontal,
                    ShowHorizontalScrollbar = false,
                    ShowVerticalScrollbar = false
                }
                .Height(34)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Center)
                .Content(
                    new HStack()
                        .Spacing(2)
                        .Height(34)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Children(segments.ToArray())));
    }

    private void ToggleAssetActionsMenu(VisualElement anchor)
    {
        if (_assetActionsOverlay is not null)
        {
            CloseAssetActionsMenu();
            return;
        }

        const float menuWidth = 176;
        var menu = new Frame()
            .X(Math.Max(8, anchor.ComputedX + anchor.ComputedWidth - menuWidth))
            .Y(anchor.ComputedY + anchor.ComputedHeight + 6)
            .Width(menuWidth)
            .Background(new Color(45, 55, 72))
            .BorderBrush(new Color(76, 89, 112))
            .BorderThickness(1)
            .BorderRadius(8)
            .Padding(new Thickness(6))
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new VStack()
                    .Spacing(2)
                    .Children(
                        CreateAssetAction("New folder", Icons.FolderCreateNew, () =>
                        {
                            CloseAssetActionsMenu();
                            ShowCreateFolderDialog();
                        }),
                        CreateAssetAction("List view", Icons.ListView, () =>
                        {
                            CloseAssetActionsMenu();
                            SetAssetViewMode(AssetViewMode.List);
                        }),
                        CreateAssetAction("Grid view", Icons.GridView, () =>
                        {
                            CloseAssetActionsMenu();
                            SetAssetViewMode(AssetViewMode.Grid);
                        })));

        _assetActionsOverlay = new AssetActionsOverlay(menu, CloseAssetActionsMenu)
            .Background(Color.Transparent)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        OverlayManager.AddOverlay(_assetActionsOverlay, this);
    }

    private static VisualElement CreateAssetAction(string text, IconData icon, Action action)
    {
        return new MenuItem(text, action)
            .IconOptions(new MenuItemIconOptions(icon, new Color(196, 210, 232), Size: 16f))
            .Height(40)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    private void CloseAssetActionsMenu()
    {
        if (_assetActionsOverlay is null)
            return;

        OverlayManager.RemoveOverlay(_assetActionsOverlay);
        _assetActionsOverlay = null;
    }

    private void SetAssetViewMode(AssetViewMode mode)
    {
        if (_assetViewMode == mode)
        {
            return;
        }

        _assetViewMode = mode;
        RefreshDrawer();
    }

    private VisualElement BuildAssetBrowser()
    {
        var assets = _project.GetChildren(_assetDirectory).ToList();
        var content = _assetViewMode == AssetViewMode.List
            ? BuildListView(assets)
            : BuildGridView(assets);

        return new ScrollView()
            .Padding(new Thickness(12, 16))
            .Content(content)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
    }

    private VisualElement BuildListView(IReadOnlyList<VirtualAsset> assets)
    {
        var items = new List<VisualElement>();
        if (!string.IsNullOrEmpty(_assetDirectory))
        {
            items.Add(CreateListAssetItem("..", true, NavigateUp));
        }

        foreach (var asset in assets)
        {
            items.Add(CreateListAssetItem(asset.Name, asset.IsDirectory, () => OpenAsset(asset)));
        }

        if (items.Count == 0)
        {
            items.Add(new Label("This folder is empty")
                .FontSize(13)
                .Foreground(new Color(148, 163, 184)));
        }

        return new VStack()
            .Spacing(6)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(items.ToArray());
    }

    private VisualElement BuildGridView(IReadOnlyList<VirtualAsset> assets)
    {
        const int columnCount = 3;
        const float gridGap = 8;
        var entries = new List<(string Name, bool IsDirectory, Action Action)>();
        if (!string.IsNullOrEmpty(_assetDirectory))
            entries.Add(("..", true, NavigateUp));
        entries.AddRange(assets.Select(asset => (asset.Name, asset.IsDirectory, (Action)(() => OpenAsset(asset)))));

        if (entries.Count == 0)
            return new Label("This folder is empty").FontSize(13).Foreground(new Color(148, 163, 184));

        var grid = new Grid()
            .Columns(GridLength.Star, GridLength.Star, GridLength.Star)
            .ColumnSpacing(gridGap)
            .RowSpacing(gridGap)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
        for (var row = 0; row < (int)Math.Ceiling(entries.Count / (double)columnCount); row++)
            grid.RowDefinitions.Add(GridLength.Pixels(104));

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            grid.AddChild(
                CreateGridAssetItem(entry.Name, entry.IsDirectory, entry.Action),
                index / columnCount,
                index % columnCount);
        }

        return grid;
    }

    private VisualElement CreateListAssetItem(string text, bool isDirectory, Action action)
    {
        var icon = GetAssetIcon(text, isDirectory);
        return new AssetGridTile(action)
            .Height(40)
            .BorderThickness(0)
            .BorderRadius(8)
            .Padding(new Thickness(10, 0))
            .Content(
                new Grid()
                    .Rows(GridLength.Star)
                    .Columns(GridLength.Pixels(28), GridLength.Star)
                    .ColumnSpacing(8)
                    .AddChild(
                        new Icon(icon)
                            .Size(19)
                            .Color(GetAssetIconColor(isDirectory))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center),
                        0,
                        0)
                    .AddChild(
                        new Label()
                            .Height(40)
                            .Text(text)
                            .FontSize(14)
                            .Foreground(new Color(226, 232, 240))
                            .TextTrimming(TextTrimming.CharacterEllipsis)
                            .TextHorizontalAlignment(HorizontalAlignment.Left)
                            .TextVerticalAlignment(VerticalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        0,
                        1));
    }

    private VisualElement CreateGridAssetItem(string text, bool isDirectory, Action action)
    {
        var icon = GetAssetIcon(text, isDirectory);
        return new AssetGridTile(action)
            .BorderBrush(new Color(51, 65, 85))
            .BorderThickness(1)
            .BorderRadius(8)
            .Padding(new Thickness(6))
            .Content(
                new VStack()
                    .Spacing(6)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Children(
                        new Icon(icon)
                            .Size(30)
                            .Color(GetAssetIconColor(isDirectory))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center),
                        new Label()
                            .Text(text)
                            .FontSize(12)
                            .Foreground(new Color(226, 232, 240))
                            .TextTrimming(TextTrimming.CharacterEllipsis)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Center)));
    }

    private static IconData GetAssetIcon(string name, bool isDirectory)
    {
        if (isDirectory)
            return Icons.Folder;

        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => Icons.Image,
            _ => Icons.File
        };
    }

    private static Color GetAssetIconColor(bool isDirectory) =>
        isDirectory ? new Color(250, 204, 21) : new Color(148, 163, 184);

    private void ShowCreateFolderDialog()
    {
        var folderName = new Entry()
            .Placeholder("Folder name")
            .Height(38);
        var content = new VStack()
            .Spacing(8)
            .Children(
                new Label("Create a folder in the current location.")
                    .FontSize(13)
                    .Foreground(new Color(148, 163, 184)),
                folderName);

        Dialog.Show(
            "New folder",
            content,
            showCancelButton: true,
            onAccepted: () => CreateDirectory(folderName.Text),
            validate: () => IsValidFolderName(folderName.Text),
            okText: "Create",
            cancelText: "Cancel");
    }

    private static bool IsValidFolderName(string name)
    {
        var trimmed = name.Trim();
        return !string.IsNullOrWhiteSpace(trimmed) &&
               !trimmed.Contains('/') &&
               !trimmed.Contains('\\') &&
               trimmed is not "." and not "..";
    }

    private void CreateDirectory(string name)
    {
        try
        {
            _project.CreateDirectory(_assetDirectory, name);
            RefreshDrawer();
        }
        catch (ArgumentException)
        {
            ToastService.ShowInfo("Enter a valid folder name.");
        }
    }

    private void NavigateUp()
    {
        var separator = _assetDirectory.LastIndexOf('/');
        NavigateTo(separator < 0 ? string.Empty : _assetDirectory[..separator]);
    }

    private void NavigateTo(string directory)
    {
        _assetDirectory = directory;
        RefreshDrawer();
    }

    private void OpenAsset(VirtualAsset asset)
    {
        if (asset.IsDirectory)
        {
            _assetDirectory = asset.Path;
            RefreshDrawer();
            return;
        }

        if (!_project.IsTextFile(asset.Path))
        {
            ToastService.ShowInfo("Binary asset preview is not implemented yet.");
            return;
        }

        _homePage.OpenTextAsset(asset.Path, _project.ReadText(asset.Path), text => _project.WriteText(asset.Path, text));
        Drawer.CloseCurrentDrawer();
    }

    private void RefreshDrawer()
    {
        CloseAssetActionsMenu();
        _drawer?.Content(BuildDrawerContent());
    }

    private sealed class AssetActionsOverlay : Absolute, IPointerHandler
    {
        private readonly VisualElement _menu;
        private readonly Action _close;

        public AssetActionsOverlay(VisualElement menu, Action close)
        {
            _menu = menu;
            _close = close;
            AddChild(menu);
        }

        public void OnPointerReleased(PointerEventArgs args)
        {
            var position = args.Position;
            var isInsideMenu = position.X >= _menu.ComputedX &&
                               position.X <= _menu.ComputedX + _menu.ComputedWidth &&
                               position.Y >= _menu.ComputedY &&
                               position.Y <= _menu.ComputedY + _menu.ComputedHeight;
            if (!isInsideMenu)
                _close();
        }
    }

    private sealed class BreadcrumbSegment : Frame, IPointerHandler
    {
        private static readonly Color HoverBackground = new(45, 55, 72);
        private readonly Action _onTapped;
        private bool _isTapPending;

        public BreadcrumbSegment(string text, float maximumWidth, Action onTapped)
        {
            _onTapped = onTapped;
            Height = 28;
            MaxWidth = maximumWidth;
            Padding = new Thickness(8, 0);
            Background = Color.Transparent;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Center;
            Content = new Label()
                .Text(text)
                .FontSize(14)
                .Foreground(new Color(191, 219, 254))
                .TextTrimming(TextTrimming.CharacterEllipsis)
                .TextHorizontalAlignment(HorizontalAlignment.Left)
                .TextVerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch);
        }

        public void OnPointerEntered(PointerEventArgs args)
        {
            if (args.PointerType == PointerType.Mouse)
                Background = HoverBackground;
        }

        public void OnPointerExited(PointerEventArgs args)
        {
            if (args.PointerType == PointerType.Mouse)
                Background = Color.Transparent;
        }

        public void OnPointerPressed(PointerEventArgs args)
        {
            // Do not show a pressed state: a touch drag belongs to the
            // surrounding ScrollView and must remain visually neutral.
            _isTapPending = args.Button == 0;
        }

        public void OnPointerReleased(PointerEventArgs args)
        {
            if (!_isTapPending)
                return;

            _isTapPending = false;
            _onTapped();
        }

        public void OnPointerCanceled(PointerEventArgs args)
        {
            _isTapPending = false;
            Background = Color.Transparent;
        }
    }

    private sealed class AssetGridTile : Frame, IPointerHandler
    {
        private static readonly Color NormalBackground = new(30, 41, 59);
        private static readonly Color HoverBackground = new(51, 65, 85);
        private static readonly Color PressedBackground = new(62, 126, 214);
        private readonly Action _action;
        private bool _isPressed;

        public AssetGridTile(Action action)
        {
            _action = action;
            Background = NormalBackground;
        }

        public void OnPointerEntered(PointerEventArgs args)
        {
            if (!_isPressed)
                Background = HoverBackground;
        }

        public void OnPointerExited(PointerEventArgs args) => ResetInteraction();

        public void OnPointerPressed(PointerEventArgs args)
        {
            if (args.Button != 0)
                return;

            _isPressed = true;
            Background = PressedBackground;
        }

        public void OnPointerReleased(PointerEventArgs args)
        {
            if (!_isPressed)
                return;

            _isPressed = false;
            Background = HoverBackground;
            _action();
        }

        public void OnPointerCanceled(PointerEventArgs args) => ResetInteraction();

        private void ResetInteraction()
        {
            _isPressed = false;
            Background = NormalBackground;
        }
    }
}
