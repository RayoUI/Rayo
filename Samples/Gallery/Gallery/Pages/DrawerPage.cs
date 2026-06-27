using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class DrawerPage : Component
{
    private Drawer _leftDrawer = null!;
    private Drawer _rightDrawer = null!;
    private Drawer _topDrawer = null!;
    private Drawer _bottomDrawer = null!;

    public override VisualElement Build()
    {
        // Create drawers
        _leftDrawer = new Drawer()
            .Position(DrawerPosition.Left)
            .DrawerWidth(280)
            .Content(BuildDrawerContent("Left Drawer", "This drawer slides in from the left side."));

        _rightDrawer = new Drawer()
            .Position(DrawerPosition.Right)
            .DrawerWidth(320)
            .Background(GalleryPalette.Surface)
            .Content(BuildDrawerContent("Right Drawer", "This drawer slides in from the right side."));

        _topDrawer = new Drawer()
            .Position(DrawerPosition.Top)
            .DrawerHeight(200)
            .Content(BuildHorizontalDrawerContent("Top Drawer"));

        _bottomDrawer = new Drawer()
            .Position(DrawerPosition.Bottom)
            .DrawerHeight(250)
            .Background(GalleryPalette.Surface)
            .Content(BuildHorizontalDrawerContent("Bottom Drawer"));

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Drawer", "Slide-out Frames from screen edges"),

                Helper.CreateExampleSection("Drawer Positions",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new Label("Click buttons to open drawers from different positions:")
                                .FontSize(14)
                                .Foreground(GalleryPalette.Muted),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    new Button()
                                        .Text("Left Drawer")
                                        .Background(GalleryPalette.Primary)
                                        .HoverBackground(GalleryPalette.PrimaryHover)
                                        .TextColor(GalleryPalette.OnPrimary)
                                        .BorderThickness(0)
                                        .Padding(new Thickness(16, 10, 16, 10))
                                        .OnTapped(() => _leftDrawer?.Open()),

                                    new Button()
                                        .Text("Right Drawer")
                                        .Background(GalleryPalette.Success)
                                        .HoverBackground(GalleryPalette.Success)
                                        .TextColor(GalleryPalette.OnSuccess)
                                        .BorderThickness(0)
                                        .Padding(new Thickness(16, 10, 16, 10))
                                        .OnTapped(() => _rightDrawer?.Open()),

                                    new Button()
                                        .Text("Top Drawer")
                                        .Background(GalleryPalette.Info)
                                        .HoverBackground(GalleryPalette.Info)
                                        .TextColor(GalleryPalette.OnInfo)
                                        .BorderThickness(0)
                                        .Padding(new Thickness(16, 10, 16, 10))
                                        .OnTapped(() => _topDrawer?.Open()),

                                    new Button()
                                        .Text("Bottom Drawer")
                                        .Background(GalleryPalette.Warning)
                                        .HoverBackground(GalleryPalette.Warning)
                                        .TextColor(GalleryPalette.OnWarning)
                                        .BorderThickness(0)
                                        .Padding(new Thickness(16, 10, 16, 10))
                                        .OnTapped(() => _bottomDrawer?.Open())
                                )
                        )
                ),

                Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Click overlay to close drawer"),
                            CreateFeatureItem("Supports Left, Right, Top, Bottom positions"),
                            CreateFeatureItem("Customizable size and colors"),
                            CreateFeatureItem("Auto-closes when another drawer opens"),
                            CreateFeatureItem("Events for open/close state changes")
                        )
                ),

                // Include drawer controls in the tree (they render as overlays)
                _leftDrawer,
                _rightDrawer,
                _topDrawer,
                _bottomDrawer
            );
    }

    private VisualElement BuildDrawerContent(string title, string description)
    {
        return new VStack()
            .Spacing(16)
            .Padding(new Thickness(20))
            .Children(
                new Label(title)
                    .FontSize(20)
                    .Foreground(GalleryPalette.OnSurface),

                new Label(description)
                    .FontSize(14)
                    .Foreground(GalleryPalette.Muted),

                new Frame()
                    .Height(1)
                    .Background(GalleryPalette.Border)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),

                new Label("Navigation")
                    .FontSize(12)
                    .Foreground(GalleryPalette.Muted),

                CreateNavItem("Home", true),
                CreateNavItem("Profile", false),
                CreateNavItem("Settings", false),
                CreateNavItem("Help", false),

                new Frame()
                    .VerticalAlignment(VerticalAlignment.Stretch),

                new Button()
                    .Text("Close Drawer")
                    .Background(GalleryPalette.Danger)
                    .HoverBackground(GalleryPalette.Danger)
                    .TextColor(GalleryPalette.OnDanger)
                    .BorderThickness(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Padding(new Thickness(12, 10, 12, 10))
                    .OnTapped(() => Drawer.CloseCurrentDrawer())
            );
    }

    private VisualElement BuildHorizontalDrawerContent(string title)
    {
        return new VStack()
            .Spacing(12)
            .Padding(new Thickness(20))
            .Children(
                new HStack()
                    .Spacing(20)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label(title)
                            .FontSize(18)
                            .Foreground(GalleryPalette.OnSurface),

                        new Frame().Width(1),

                        new Button()
                            .Text("Close")
                            .Background(GalleryPalette.Danger)
                            .HoverBackground(GalleryPalette.Danger)
                            .TextColor(GalleryPalette.OnDanger)
                            .BorderThickness(0)
                            .Padding(new Thickness(12, 8, 12, 8))
                            .OnTapped(() => Drawer.CloseCurrentDrawer())
                    ),

                new Label("This is a horizontal drawer that slides from the edge of the screen.")
                    .FontSize(14)
                    .Foreground(GalleryPalette.Muted),

                new HStack()
                    .Spacing(12)
                    .Children(
                        CreateQuickAction("Action 1"),
                        CreateQuickAction("Action 2"),
                        CreateQuickAction("Action 3"),
                        CreateQuickAction("Action 4")
                    )
            );
    }

    private VisualElement CreateNavItem(string text, bool isSelected)
    {
        var bgColor = isSelected ? GalleryPalette.Primary : Color.Transparent;
        var hoverColor = isSelected ? GalleryPalette.PrimaryHover : GalleryPalette.SurfaceHover;

        return new Button()
            .Text(text)
            .Background(bgColor)
            .HoverBackground(hoverColor)
            .TextColor(isSelected ? GalleryPalette.OnPrimary : GalleryPalette.OnSurface)
            .BorderThickness(0)
            .BorderRadius(8)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(new Thickness(12, 10, 12, 10));
    }

    private VisualElement CreateQuickAction(string text)
    {
        return new Button()
            .Text(text)
            .Background(GalleryPalette.SurfaceHover)
            .HoverBackground(GalleryPalette.SurfacePressed)
            .TextColor(GalleryPalette.OnSurface)
            .BorderThickness(0)
            .Padding(new Thickness(16, 12, 16, 12));
    }

    private VisualElement CreateFeatureItem(string text)
    {
        return new HStack()
            .Spacing(8)
            .Children(
                new Label("*")
                    .FontSize(14)
                    .Foreground(GalleryPalette.Primary),
                new Label(text)
                    .FontSize(14)
                    .Foreground(GalleryPalette.Muted)
            );
    }
}
