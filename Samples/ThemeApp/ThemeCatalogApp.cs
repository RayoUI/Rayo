using System.Numerics;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Gestures.Components;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
using Rayo.Controls.Shapes;
using RayoMenu = Rayo.Controls.Menu;
using RayoPath = Rayo.Controls.Shapes.Path;
using RayoRectangle = Rayo.Controls.Shapes.Rectangle;

namespace ThemeApp;

public sealed class ThemeCatalogApp : Component
{
    private Drawer? _drawer;

    public override VisualElement Build()
    {
        _drawer = new Drawer()
            .Position(DrawerPosition.Right)
            .DrawerWidth(320)
            .Content(
                new VStack()
                    .Spacing(12)
                    .Padding(new Thickness(24))
                    .Children(
                        new Label("Drawer").FontSize(22),
                        new Label("This surface follows the active Rayo theme."),
                        new Button().Text("Close").OnTapped(() => _drawer?.Close())));

        var themeSelector = new ButtonGroup()
            .AddItems("Light", "Dark", "Neon", "Obsidian", "High contrast", "System")
            .SelectedIndex(GetThemeIndex((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light)))
            .OnSelectedIndexChanged(ApplyTheme)
            .Width(680);

        var catalog = new VStack()
            .Spacing(18)
            .Padding(new Thickness(24))
            .Children(
                Intro(),
                Section("Theme scopes, density and typed tokens", ThemeSystemControls()),
                Section("Buttons and actions", Buttons()),
                Section("Text and input", TextInputs()),
                Section("Selection and values", SelectionControls()),
                Section("Dates, files and color", PickerControls()),
                Section("Feedback and status", FeedbackControls()),
                Section("Data and collections", DataControls()),
                Section("Navigation and containers", ContainerControls()),
                Section("Layout and interaction", LayoutControls()),
                Section("Menus, overlays and transient UI", OverlayControls()),
                Section("Images, icons and shapes", GraphicsControls()));

        return new ThemeBackground()
            .Content(
                new VStack()
                    .Children(
                        new ThemeToolbar()
                            .Padding(new Thickness(24, 16))
                            .Content(
                                new HStack()
                                    .Alignment(Alignment.Center)
                                    .JustifyContent(JustifyContent.SpaceBetween)
                                    .Children(
                                        new VStack()
                                            .Spacing(2)
                                            .Children(
                                                new ThemeLabel(colors => colors.OnSurface, "Rayo ThemeApp")
                                                    .FontSize(22)
                                                    .FontWeight(FontWeight.Bold),
                                                new ThemeLabel(colors => colors.OnDisabled, "Live catalog of the controls included with Rayo")
                                                    .FontSize(12)),
                                        themeSelector)),
                        new ScrollView(catalog)
                            .Height(720)));
    }

    private static VisualElement Intro() =>
        new VStack()
            .Spacing(6)
            .Children(
                new ThemeLabel(colors => colors.OnBackground, "Control catalog")
                    .FontSize(30)
                    .FontWeight(FontWeight.Bold),
                new ThemeLabel(
                        colors => colors.OnDisabled,
                        "Choose a theme above. Mounted controls, popups, dialogs and overlays update without rebuilding the page.")
                    .FontSize(14));

    private static VisualElement ThemeSystemControls()
    {
        var accentKey = new ThemeKey<Color>("demo.accent");
        var compact = RayoThemes.Light with
        {
            Name = "compact-preview",
            Density = ThemeDensity.Compact,
            Components = RayoThemes.Light.Components with
            {
                Buttons = RayoThemes.Light.Buttons with
                {
                    Padding = new Thickness(8, 3),
                    Radius = new CornerRadius(2),
                    MinHeight = 28,
                },
            },
        };
        compact = compact.WithToken(accentKey, new Color(139, 92, 246));

        return Wrap(
            ScopePreview("Light scope", RayoThemes.Light),
            ScopePreview("Dark scope", RayoThemes.Dark),
            ScopePreview(
                    $"Compact scope · token #{ToHex(compact.GetToken(accentKey))}",
                compact));
    }

    private static VisualElement ScopePreview(string title, ThemeData theme) =>
        new ThemeScope(
            theme,
            new ThemeFrame()
                .Width(250)
                .Padding(new Thickness(14))
                .Content(
                    new VStack()
                        .Spacing(8)
                        .Children(
                            new ThemeLabel(colors => colors.OnSurface, title)
                                .FontWeight(FontWeight.SemiBold),
                            new Entry().Placeholder("Scoped input"),
                            new Button().Text("Scoped action"),
                            new Button().Text("Disabled").IsEnabled(false))));

    private static string ToHex(Color color) =>
        $"{(byte)MathF.Round(color.R * 255):X2}{(byte)MathF.Round(color.G * 255):X2}{(byte)MathF.Round(color.B * 255):X2}";

    private static VisualElement Buttons() =>
        Wrap(
            new Button().Text("Primary").Variant(ButtonVariant.Primary),
            new Button().Text("Secondary").Variant(ButtonVariant.Secondary),
            new Button().Text("Danger").Variant(ButtonVariant.Danger),
            new Button().Text("Ghost").Variant(ButtonVariant.Ghost),
            new ButtonIcon(Icons.Heart).WithTooltip("ButtonIcon"),
            new ButtonFloat(Icons.Add) { FloatSize = ButtonFloatSize.Small },
            new ButtonGroup().AddItems("Day", "Week", "Month").SelectedIndex(1));

    private static VisualElement TextInputs() =>
        new VStack()
            .Spacing(12)
            .Children(
                Wrap(
                    new Label("Label"),
                    new Link("Rayo repository", "https://github.com"),
                    new Entry().Placeholder("Entry").Width(240),
                    new EntryNumber(42).Width(160)),
                new Editor("Editor supports multiline text.")
                    .Width(520)
                    .Height(100));

    private static VisualElement SelectionControls() =>
        new VStack()
            .Spacing(14)
            .Children(
                Wrap(
                    new Checkbox("Checkbox") { IsChecked = true },
                    new RadioButton("Radio A", "theme-app") { IsChecked = true },
                    new RadioButton("Radio B", "theme-app"),
                    new ToggleSwitch { IsOn = true },
                    new ComboBox()
                        .AddItems("First option", "Second option", "Third option")
                        .SelectedIndex(0)),
                new Slider(0, 100, 62).Width(420),
                new ProgressBar().Value(62).Width(420),
                new Stepper().Minimum(0).Maximum(10).Value(4));

    private static VisualElement PickerControls() =>
        new VStack()
            .Spacing(12)
            .Children(
                Wrap(
                    new DatePicker().Width(220),
                    new TimePicker().Width(220),
                    new PathPicker().Width(340)),
                Wrap(
                    new Button().Text("Open ColorPicker")
                        .OnTapped(() => ColorPicker.ShowDialog((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Primary, _ => { })),
                    new Button().Text("Choose a folder")
                        .Variant(ButtonVariant.Secondary)
                        .OnTapped(() => PathPicker.ShowDialog(PathPickerMode.Folder, _ => { }))));

    private static VisualElement FeedbackControls() =>
        BuildFeedbackControls();

    private static VisualElement BuildFeedbackControls()
    {
        var loadingOverlay = new LoadingOverlay()
            .Content(new Label("LoadingOverlay content"))
            .Width(220)
            .Height(80);
        loadingOverlay.Show("Loading");

        return new VStack()
            .Spacing(14)
            .Children(
                Wrap(
                    new Badge("New"),
                    new Badge(12),
                    new BadgeContainer(
                        new ButtonIcon(Icons.Notification),
                        new Badge(3)),
                    new Loading().Size(32),
                    loadingOverlay,
                    new Button().Text("Hover for Tooltip").WithTooltip("Theme-aware Tooltip"),
                    new Button().Text("Show Toast")
                        .OnTapped(() => ToastService.ShowSuccess("ThemeData applied successfully"))),
                new ProgressBar().Value(78).Width(520));
    }

    private static VisualElement DataControls()
        => Wrap(
            new ListView<string>()
                .Items(["Alpha", "Beta", "Gamma", "Delta", "Epsilon"])
                .Width(250)
                .Height(250),
            CreateTreeViewPreview(),
            CreateDataGridPreview());

    private static VisualElement CreateTreeViewPreview()
    {
        var tree = new TreeView()
            .Width(420)
            .Height(250);
        var root = new TreeNode("Rayo controls") { Icon = Icons.Folder, IsExpanded = true };
        root.AddChild("Input").Icon = Icons.Folder;
        root.AddChild("Navigation").Icon = Icons.Folder;
        root.AddChild("Feedback").Icon = Icons.Folder;
        tree.AddRootNode(root);
        return tree;
    }

    private static VisualElement CreateDataGridPreview() =>
        new DataGrid()
            .AddColumn(new DataGridColumn("Control", "Name", 180))
            .AddColumn(new DataGridColumn("Category", "Category", 140))
            .AddColumn(new DataGridColumn("Ready", "Ready", 80))
            .Items(
            [
                new { Name = "Button", Category = "Action", Ready = "Yes" },
                new { Name = "DataGrid", Category = "Data", Ready = "Yes" },
                new { Name = "TreeView", Category = "Data", Ready = "Yes" },
            ])
            .Width(520)
            .Height(250);

    private static VisualElement LayoutControls()
    {
        var grid = new Grid()
            .Rows(GridLength.Star, GridLength.Star)
            .Columns(GridLength.Star, GridLength.Star)
            .RowSpacing(8)
            .ColumnSpacing(8)
            .Width(420)
            .Height(120)
            .AddChild(DemoTile("0,0"), 0, 0)
            .AddChild(DemoTile("0,1"), 0, 1)
            .AddChild(DemoTile("1,0"), 1, 0)
            .AddChild(DemoTile("1,1"), 1, 1);

        var flex = new Flex()
            .Direction(FlexDirection.Row)
            .Wrap(FlexWrap.Wrap)
            .Gap(8)
            .Width(420)
            .Height(120);
        flex.AddChild(DemoTile("Flex 1", 105, 48));
        flex.AddChild(DemoTile("Flex 2", 130, 48));
        flex.AddChild(DemoTile("Flex 3", 145, 48));
        flex.AddChild(DemoTile("Flex 4", 180, 48));

        var absolute = new Absolute()
            .Width(420)
            .Height(120)
            .Children(
                DemoTile("A", 110, 58).AbsolutePosition(12, 12),
                DemoTile("B", 130, 58).AbsolutePosition(105, 42),
                DemoTile("C", 110, 58).AbsolutePosition(225, 18));

        var horizontalFlex = new Flex
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            Gap = 6,
            RowGap = 6,
            AlignItems = Alignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
        };
        horizontalFlex.AddChild(DemoTile("F1", 70, 78));
        horizontalFlex.AddChild(DemoTile("F2", 70, 78));

        var stacks = Wrap(
            new VStack()
                .Spacing(6)
                .Children(DemoTile("V1", 90, 36), DemoTile("V2", 90, 36)),
            horizontalFlex,
            new LStack()
                .Orientation(Rayo.Layout.Orientation.Horizontal)
                .Spacing(6)
                .Alignment(Alignment.Center)
                .Children(DemoTile("L1", 70, 50), DemoTile("L2", 70, 70)));

        var gestureDetector = new GestureDetector(
                DemoTile("Tap GestureDetector", 260, 70))
            .OnTap((Vector2 _) => ToastService.ShowInfo("GestureDetector tapped"));

        var scrollContent = new VStack()
            .Spacing(6)
            .Children(
                DemoTile("Scrollable 1", 230, 42),
                DemoTile("Scrollable 2", 230, 42),
                DemoTile("Scrollable 3", 230, 42),
                DemoTile("Scrollable 4", 230, 42));

        return Wrap(
            NamedDemo("Grid", grid),
            NamedDemo("Flex", flex),
            NamedDemo("Absolute", absolute),
            NamedDemo("VStack, Flex and LStack", stacks),
            NamedDemo("GestureDetector", gestureDetector),
            NamedDemo(
                "ScrollView",
                new ScrollView(scrollContent)
                    .Width(270)
                    .Height(120)));
    }

    private static VisualElement ContainerControls()
    {
        var accordion = new Accordion()
            .Width(440)
            .AddItem("Accordion item", new Label("Any VisualElement can be used as content."), true)
            .AddItem("Another item", new Checkbox("Nested control"));

        var expander = new Expander(
                "Standalone Expander",
                new Label("Expandable content outside an Accordion."))
            .Width(440)
            .IsExpanded(true);

        var tabs = new TabControl()
            .Width(440)
            .Height(180)
            .AddTab("Overview", CenteredText("TabControl content"))
            .AddTab("Settings", new Checkbox("Enable feature"));

        var carousel = new Carousel()
            .Size(new Size(440, 180))
            .AddSlides(
                CenteredText("Carousel slide 1"),
                CenteredText("Carousel slide 2"),
                CenteredText("Carousel slide 3"));

        var sideBarContentTitle = new ThemeLabel(colors => colors.OnSurface, "Home")
            .FontSize(20)
            .FontWeight(FontWeight.Bold);
        var sideBarContentDescription = new ThemeLabel(
            colors => colors.OnSurface,
            "Main content associated with the selected navigation item.");

        void ShowSideBarContent(string title, string description)
        {
            sideBarContentTitle.Text = title;
            sideBarContentDescription.Text = description;
        }

        var sideBar = new SideBar()
            .ExpandedWidth(180)
            .CollapsedWidth(60)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddCollapseToggle()
            .AddItem("Home", "H", () => ShowSideBarContent(
                "Home",
                "Main content associated with the selected navigation item."))
            .AddItem("Themes", "T", () => ShowSideBarContent(
                "Themes",
                "Theme configuration and live preview content."))
            .AddItem("Settings", "S", () => ShowSideBarContent(
                "Settings",
                "Application settings content."))
            .SelectedKey("Home");

        var sideBarContent = new ThemeFrame()
            .Padding(new Thickness(20))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(new VStack()
                .Spacing(8)
                .Children(sideBarContentTitle, sideBarContentDescription));

        var sideBarLayout = new HStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(sideBar, sideBarContent);

        var sideBarHost = new ThemeFrame()
            .Width(440)
            .Height(240)
            .Padding(0)
            .Content(sideBarLayout);

        var splitter = new Splitter()
            .Orientation(SplitterOrientation.Horizontal)
            .SplitterSize(6)
            .Width(440)
            .Height(150)
            .Children(
                new Frame().Width(180).Content(CenteredText("Resizable")),
                new Frame().Content(CenteredText("pane")));

        return new VStack()
            .Spacing(14)
            .Children(
                Wrap(
                    new Border(CenteredText("Border")).Width(180).Height(90),
                    new Frame().Content(CenteredText("Frame")).Width(180).Height(90),
                    new Card()
                        .Header(new Label("Card"))
                        .Content(new Label("Header and content"))
                        .Width(260)),
                NamedDemo("Accordion", accordion),
                NamedDemo("Expander", expander),
                NamedDemo("TabControl", tabs),
                NamedDemo("Carousel", carousel),
                NamedDemo("SideBar", sideBarHost),
                NamedDemo("Splitter", splitter));
    }

    private VisualElement OverlayControls()
    {
        var menu = new RayoMenu("Menu")
            .AddItem(new MenuItem("New", () => ToastService.ShowInfo("New selected")))
            .AddItem(new MenuItem("Save", () => ToastService.ShowSuccess("Saved")))
            .AddItem(new MenuItem("Delete", () => ToastService.ShowWarning("Delete selected")));

        var popupAnchor = new Button().Text("AnchoredPopup");
        popupAnchor.OnTapped(() =>
            AnchoredPopup.Show(
                popupAnchor,
                new ThemeFrame()
                    .Padding(new Thickness(16))
                    .Content(new Label("Anchored popup content"))));

        return Wrap(
            menu,
            popupAnchor,
            new Button().Text("Dialog")
                .OnTapped(() => Dialog.Show("ThemeApp", "Dialogs also receive the active theme.")),
            new Button().Text("Drawer")
                .OnTapped(() => _drawer?.Open()),
            new Button().Text("Toast")
                .OnTapped(() => ToastService.ShowInfo("Hello from ThemeApp")),
            new Button().Text("Overlay")
                .OnTapped(() =>
                {
                    if (UIApplication.Current is not { } app)
                    {
                        return;
                    }

                    Overlay? overlay = null;
                    var close = new Button()
                        .Text("Close overlay")
                        .OnTapped(() => overlay?.Hide());
                    overlay = new Overlay(
                        app,
                        new ThemeFrame()
                            .Width(300)
                            .Height(150)
                            .Padding(new Thickness(20))
                            .Content(
                                new VStack()
                                    .Spacing(16)
                                    .Children(
                                        new Label("Overlay component"),
                                        close)));
                    overlay.Show();
                }),
            _drawer!);
    }

    private static VisualElement GraphicsControls() =>
        Wrap(
            new Icon(Icons.Heart).Size(36),
            new Image("Assets/robot.png").Width(90).Height(70),
            new RayoRectangle(80, 55)
                .Fill((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Primary)
                .Stroke((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Focus)
                .StrokeThickness(2),
            new Ellipse(64, 64)
                .Fill((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Secondary),
            Polygon.Star(5, 32, 15, 32, 32)
                .Fill((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Warning),
            new Line(0, 0, 90, 50)
                .Stroke((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Info)
                .StrokeThickness(4),
            new Polyline()
                .Points(new Vector2(0, 35), new Vector2(35, 0), new Vector2(70, 35))
                .Stroke((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Success)
                .StrokeThickness(4),
            new RayoPath("M 0 20 L 40 0 L 80 20 L 40 50 Z")
                .Fill((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Colors.Danger));

    private static VisualElement Section(string title, VisualElement content) =>
        new Card()
            .Header(
                new ThemeLabel(colors => colors.OnSurface, title)
                    .FontSize(18)
                    .FontWeight(FontWeight.Bold))
            .Content(content)
            .Padding(new Thickness(18));

    private static VisualElement NamedDemo(string title, VisualElement content) =>
        new ThemeFrame()
            .Width(480)
            .Padding(new Thickness(14))
            .Content(
                new VStack()
                    .Spacing(10)
                    .Children(
                        new ThemeLabel(colors => colors.OnSurface, title)
                            .FontSize(14)
                            .FontWeight(FontWeight.Bold),
                        content));

    private static Frame DemoTile(string text, float width = 0, float height = 0)
    {
        Frame tile = new ThemeFrame();
        tile.Padding = new Thickness(8);
        tile.Content(
            new ThemeLabel(colors => colors.OnSurface, text)
                .TextHorizontalAlignment(HorizontalAlignment.Center)
                .TextVerticalAlignment(VerticalAlignment.Center));

        if (width > 0)
        {
            tile.Width = width;
        }

        if (height > 0)
        {
            tile.Height = height;
        }

        return tile;
    }

    private static Flex Wrap(params VisualElement[] children)
    {
        var flex = new Flex
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            Gap = 14,
            RowGap = 14,
            AlignItems = Alignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
        };

        foreach (var child in children)
            flex.AddChild(child);

        return flex;
    }

    private static VisualElement CenteredText(string text) =>
        new ThemeFrame()
            .Content(
                new ThemeLabel(colors => colors.OnSurface, text)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .TextVerticalAlignment(VerticalAlignment.Center));

    private static void ApplyTheme(int index)
    {
        ThemeData theme = index switch
        {
            1 => RayoThemes.Dark,
            2 => ThemeAppThemes.Neon,
            3 => ThemeAppThemes.Obsidian,
            4 => RayoThemes.HighContrast,
            _ => RayoThemes.Light,
        };

        if (index == 5 && UIApplication.Current is { } app)
            app.UseThemeMode(ThemeMode.System);
        else
            RayoThemes.UseTheme(theme);
    }

    private static int GetThemeIndex(ThemeData theme)
    {
        if (UIApplication.Current?.ThemeMode == ThemeMode.System)
            return 5;
        return theme.Name switch
        {
            "dark" => 1,
            "neon" => 2,
            "obsidian" => 3,
            "high-contrast" => 4,
            _ => 0,
        };
    }
}

internal sealed class ThemeBackground : Frame
{
    public ThemeBackground()
    {
        BorderThickness = 0;
        Padding = 0;
        InitializeTheme();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        Background = theme.Colors.Background;
    }
}

internal class ThemeFrame : Frame
{
    public ThemeFrame()
    {
        InitializeTheme();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        Background = theme.Colors.Surface;
        BorderBrush = theme.Colors.Border;
    }
}

internal sealed class ThemeToolbar : ThemeFrame
{
    protected override void OnThemeApplied(ThemeData theme)
    {
        base.OnThemeApplied(theme);
        BorderThickness = new Thickness(0, 0, 0, 1);
    }
}

internal sealed class ThemeLabel : Label
{
    private readonly Func<ColorScheme, Color>? _color;

    public ThemeLabel(Func<ColorScheme, Color> color, string text)
        : base(text)
    {
        _color = color;
        ResetThemeValues();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        if (_color == null)
        {
            return;
        }

        SetThemeValue(
            nameof(Foreground),
            _color(theme.Colors),
            value => Foreground = value);
    }
}
