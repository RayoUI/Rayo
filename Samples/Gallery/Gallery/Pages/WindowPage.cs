using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class WindowPage : Component
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Window", "Floating, non-modal containers for independent tasks"),

                Helper.CreateExampleSection("Initial positioning",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Use InitialPosition to choose where a window first appears, or Centered() to open it in the middle of the viewport.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),
                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateWindowButton("Top-left", GalleryPalette.Primary, ShowInspector),
                                    CreateWindowButton("Offset", GalleryPalette.Success, ShowActivity),
                                    CreateWindowButton("Centered", GalleryPalette.Info, ShowNotes)))),

                Helper.CreateExampleSection("Window stack",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Open several windows, select one to bring it to the front, and drag its title bar to reposition it.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),
                            CreateWindowButton("Open all examples", new Color(139, 92, 246), ShowAllExamples))),

                Helper.CreateExampleSection("Drag behavior",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Use IsDragEnabled to keep a window in a fixed position while preserving its header actions and close button.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),
                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateWindowButton("Draggable", GalleryPalette.Primary, ShowInspector),
                                    CreateWindowButton("Fixed position", new Color(100, 116, 139), ShowFixedWindow)))),

                Helper.CreateExampleSection("Custom header actions",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("AddHeaderAction accepts buttons or any other VisualElement alongside the optional close button.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),
                            CreateWindowButton("Open editor window", new Color(14, 165, 233), ShowCustomHeaderWindow))),

                Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Non-modal overlay: the page remains interactive"),
                            CreateFeatureItem("Selecting a window brings it above the other windows"),
                            CreateFeatureItem("InitialPosition sets the initial top-left coordinate"),
                            CreateFeatureItem("Centered() places a window in the viewport center"),
                            CreateFeatureItem("IsDragEnabled can lock a window in place"),
                            CreateFeatureItem("Title bar with a built-in close action"),
                            CreateFeatureItem("Hosts arbitrary VisualElement content"),
                            CreateFeatureItem("Configurable width and screen position")))
            );
    }

    private static Button CreateWindowButton(string text, Color background, Action onTapped)
    {
        return new Button()
            .Text(text)
            .Background(background)
            .HoverBackground(background)
            .PressedBackground(background)
            .TextColor(GalleryPalette.OnPrimary)
            .BorderThickness(0)
            .BorderRadius(6)
            .Padding(new Thickness(16, 10, 16, 10))
            .OnTapped(onTapped);
    }

    private static VisualElement CreateFeatureItem(string text)
    {
        return new HStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Frame().Size(6).Background(GalleryPalette.Primary).BorderRadius(3),
                new Label(text).FontSize(14).Foreground(ColorDefault.Secondary));
    }

    private static void ShowInspector()
    {
        new Window("Inspector", new VStack()
            .Spacing(10)
            .Children(
                new Label("Selected layer").FontSize(13).Foreground(ColorDefault.Secondary),
                new Label("Hero image").FontSize(16).Foreground(GalleryPalette.OnSurface),
                new Label("1280 × 720 px\nVisible · Locked: no").FontSize(14).Foreground(ColorDefault.Secondary)),
            width: 320)
        {
            InitialPosition = new Position(56, 96)
        }.Show();
    }

    private static void ShowActivity()
    {
        new Window("Recent activity", new VStack()
            .Spacing(10)
            .Children(
                CreateActivityItem("Build completed", ColorDefault.Success),
                CreateActivityItem("Preview deployed", GalleryPalette.Info),
                CreateActivityItem("2 comments received", ColorDefault.Warning)),
            width: 360)
        {
            InitialPosition = new Position(412, 144)
        }.Show();
    }

    private static void ShowNotes()
    {
        new Window("Release notes", new VStack()
            .Spacing(10)
            .Children(
                new Label("v1.0.0").FontSize(16).Foreground(GalleryPalette.OnSurface),
                new Label("• Added non-modal windows\n• Improved overlay composition\n• Updated Gallery examples")
                    .FontSize(14)
                    .Foreground(ColorDefault.Secondary)),
            width: 340)
        .Centered()
        .Show();
    }

    private static void ShowAllExamples()
    {
        ShowInspector();
        ShowActivity();
        ShowNotes();
    }

    private static void ShowFixedWindow()
    {
        new Window("Fixed position", new VStack()
            .Spacing(10)
            .Children(
                new Label("This window cannot be moved from its title bar.")
                    .FontSize(14)
                    .Foreground(ColorDefault.Secondary),
                new Label("Its close button and all content controls remain interactive.")
                    .FontSize(14)
                    .Foreground(ColorDefault.Secondary)),
            width: 360)
        {
            InitialPosition = new Position(220, 240),
            IsDragEnabled = false
        }.Show();
    }

    private static void ShowCustomHeaderWindow()
    {
        new Window("Editor", new Label("Use the Refresh and Save controls in this window header.")
            .FontSize(14)
            .Foreground(ColorDefault.Secondary), width: 420)
        {
            InitialPosition = new Position(280, 180)
        }
        .AddHeaderActions(
            new ButtonIcon(Icons.Refresh)
                .Size(32)
                .Variant(ButtonVariant.Ghost)
                .OnTapped(() => ToastService.ShowInfo("Preview refreshed.")),
            new Button()
                .Text("Save")
                .Height(32)
                .Padding(new Thickness(12, 6))
                .OnTapped(() => ToastService.ShowInfo("Changes saved.")))
        .Show();
    }

    private static VisualElement CreateActivityItem(string text, Color color)
    {
        return new HStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Frame().Size(10).Background(color).BorderRadius(5),
                new Label(text).FontSize(14).Foreground(GalleryPalette.OnSurface));
    }
}
