using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;

namespace Gallery.Pages;

public class BadgePage : Component
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Badge", "Small visual indicators for notifications and status"),

                Helper.CreateExampleSection("Basic Badges",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            new PaletteBadge("New", colors => colors.Primary, colors => colors.OnPrimary),

                            new PaletteBadge("Sale", colors => colors.Danger, colors => colors.OnDanger),

                            new Badge("Pro")
                                .Background(new Color(168, 85, 247)),

                            new PaletteBadge("Beta", colors => colors.Warning, colors => colors.OnWarning),

                            new PaletteBadge("Free", colors => colors.Success, colors => colors.OnSuccess)
                        )
                ),

                Helper.CreateExampleSection("Count Badges",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateCountExample(3),
                            CreateCountExample(12),
                            CreateCountExample(99),
                            CreateCountExample(150), // Will show 99+
                            CreateCountExample(0, showZero: true)
                        )
                ),

                Helper.CreateExampleSection("Badge Variants",
                    new HStack()
                        .Spacing(16)
                        .Alignment(Alignment.Center)
                        .Children(
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Solid", colors => colors.Primary, colors => colors.OnPrimary)
                                        .Variant(BadgeVariant.Solid),
                                    new Label("Solid")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Outline", colors => colors.Primary, colors => colors.OnPrimary)
                                        .Variant(BadgeVariant.Outline),
                                    new Label("Outline")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Subtle", colors => colors.Primary, colors => colors.OnPrimary)
                                        .Variant(BadgeVariant.Subtle),
                                    new Label("Subtle")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                )
                        )
                ),

                Helper.CreateExampleSection("Badge Sizes",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Small", colors => colors.Primary, colors => colors.OnPrimary)
                                        .BadgeSize(BadgeSize.Small),
                                    new Label("Small")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Medium", colors => colors.Primary, colors => colors.OnPrimary)
                                        .BadgeSize(BadgeSize.Medium),
                                    new Label("Medium")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Large", colors => colors.Primary, colors => colors.OnPrimary)
                                        .BadgeSize(BadgeSize.Large),
                                    new Label("Large")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                )
                        )
                ),

                Helper.CreateExampleSection("Badge Shapes",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Rounded", colors => colors.Success, colors => colors.OnSuccess)
                                        .Shape(BadgeShape.Rounded),
                                    new Label("Rounded")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("Square", colors => colors.Success, colors => colors.OnSuccess)
                                        .Shape(BadgeShape.Square),
                                    new Label("Square")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("5", colors => colors.Success, colors => colors.OnSuccess)
                                        .Shape(BadgeShape.Circle),
                                    new Label("Circle")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                )
                        )
                ),

                Helper.CreateExampleSection("Dot Badges",
                    new HStack()
                        .Spacing(24)
                        .Alignment(Alignment.Center)
                        .Children(
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("", colors => colors.Danger, colors => colors.OnDanger)
                                        .Dot(true).BadgeSize(BadgeSize.Small),
                                    new Label("Small")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("", colors => colors.Success, colors => colors.OnSuccess)
                                        .Dot(true).BadgeSize(BadgeSize.Medium),
                                    new Label("Medium")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new PaletteBadge("", colors => colors.Primary, colors => colors.OnPrimary)
                                        .Dot(true).BadgeSize(BadgeSize.Large),
                                    new Label("Large")
                                        .FontSize(11)
                                        .Foreground(GalleryPalette.Muted)
                                )
                        )
                ),

                Helper.CreateExampleSection("Badge on Elements (BadgeContainer)",
                    new HStack()
                        .Spacing(32)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateBadgedIcon(Icons.Email, 5, colors => colors.Danger, colors => colors.OnDanger),
                            CreateBadgedIcon(Icons.Notification, 12, colors => colors.Primary, colors => colors.OnPrimary),
                            CreateBadgedIcon(Icons.Folder, 99, colors => colors.Success, colors => colors.OnSuccess),
                            CreateBadgedIconDot(Icons.Settings, colors => colors.Warning, colors => colors.OnWarning),
                            CreateBadgedButton("Messages", 3)
                        )
                ),

                Helper.CreateExampleSection("Color Variants",
                    new HStack()
                        .Spacing(12)
                        .Children(
                            new PaletteBadge("Primary", colors => colors.Primary, colors => colors.OnPrimary),
                            new PaletteBadge("Success", colors => colors.Success, colors => colors.OnSuccess),
                            new PaletteBadge("Warning", colors => colors.Warning, colors => colors.OnWarning),
                            new PaletteBadge("Danger", colors => colors.Danger, colors => colors.OnDanger),
                            new Badge("Purple").Background(new Color(168, 85, 247)),
                            new Badge("Pink").Background(new Color(236, 72, 153)),
                            new Badge("Cyan").Background(new Color(6, 182, 212))
                        )
                )
            );
    }

    private VisualElement CreateCountExample(int count, bool showZero = false)
    {
        return new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                new PaletteBadge(count, colors => colors.Danger, colors => colors.OnDanger)
                    .ShowZero(showZero),
                new Label(count.ToString())
                    .FontSize(11)
                    .Foreground(GalleryPalette.Muted)
            );
    }

    private VisualElement CreateBadgedIcon(
        IconData icon,
        int count,
        Func<ColorScheme, Color> badgeColor,
        Func<ColorScheme, Color> textColor)
    {
        return new BadgeContainer()
            .Content(
                new PaletteFrame(colors => colors.SurfaceHover)
                    .Size(new Size(48, 48))
                    .BorderRadius(8)
                    .Content(
                        new PaletteIcon(icon, colors => colors.OnSurface)
                            .Size(new Size(24, 24))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    )
            )
            .Badge(
                new PaletteBadge(count, badgeColor, textColor)
                    .BadgeSize(BadgeSize.Small)
            )
            .BadgeHorizontalPosition(HorizontalAlignment.Right)
            .BadgeVerticalPosition(VerticalAlignment.Top);
    }

    private VisualElement CreateBadgedIconDot(
        IconData icon,
        Func<ColorScheme, Color> badgeColor,
        Func<ColorScheme, Color> textColor)
    {
        return new BadgeContainer()
            .Content(
                new PaletteFrame(colors => colors.SurfaceHover)
                    .Size(new Size(48, 48))
                    .BorderRadius(8)
                    .Content(
                        new PaletteIcon(icon, colors => colors.OnSurface)
                            .Size(24)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    )
            )
            .Badge(
                new PaletteBadge("", badgeColor, textColor)
                    .Dot(true)
            )
            .BadgeHorizontalPosition(HorizontalAlignment.Right)
            .BadgeVerticalPosition(VerticalAlignment.Top);
    }

    private VisualElement CreateBadgedButton(string text, int count)
    {
        return new BadgeContainer()
            .Content(
                new Button()
                    .Text(text)
                    .Variant(ButtonVariant.Secondary)
                    .BorderThickness(0)
                    .Padding(new Thickness(16, 10, 16, 10))
            )
            .Badge(
                new PaletteBadge(count, colors => colors.Danger, colors => colors.OnDanger)
                    .BadgeSize(BadgeSize.Small)
            )
            .BadgeHorizontalPosition(HorizontalAlignment.Right)
            .BadgeVerticalPosition(VerticalAlignment.Top)
            .BadgeOffset(new Position(5, -4));
    }
}
