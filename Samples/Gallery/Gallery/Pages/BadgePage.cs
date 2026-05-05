using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class BadgePage : UserControl
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
                            new Badge("New")
                                .Background(new Color(59, 130, 246)),

                            new Badge("Sale")
                                .Background(new Color(239, 68, 68)),

                            new Badge("Pro")
                                .Background(new Color(168, 85, 247)),

                            new Badge("Beta")
                                .Background(new Color(234, 179, 8))
                                .TextColor(Color.Black),

                            new Badge("Free")
                                .Background(new Color(34, 197, 94))
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
                                    new Badge("Solid")
                                        .Variant(BadgeVariant.Solid)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Solid")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge("Outline")
                                        .Variant(BadgeVariant.Outline)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Outline")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge("Subtle")
                                        .Variant(BadgeVariant.Subtle)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Subtle")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
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
                                    new Badge("Small")
                                        .BadgeSize(BadgeSize.Small)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Small")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge("Medium")
                                        .BadgeSize(BadgeSize.Medium)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Medium")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge("Large")
                                        .BadgeSize(BadgeSize.Large)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Large")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
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
                                    new Badge("Rounded")
                                        .Shape(BadgeShape.Rounded)
                                        .Background(new Color(34, 197, 94)),
                                    new Label("Rounded")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge("Square")
                                        .Shape(BadgeShape.Square)
                                        .Background(new Color(34, 197, 94)),
                                    new Label("Square")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge("5")
                                        .Shape(BadgeShape.Circle)
                                        .Background(new Color(34, 197, 94)),
                                    new Label("Circle")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
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
                                    new Badge().Dot(true).BadgeSize(BadgeSize.Small)
                                        .Background(new Color(239, 68, 68)),
                                    new Label("Small")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge().Dot(true).BadgeSize(BadgeSize.Medium)
                                        .Background(new Color(34, 197, 94)),
                                    new Label("Medium")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Badge().Dot(true).BadgeSize(BadgeSize.Large)
                                        .Background(new Color(59, 130, 246)),
                                    new Label("Large")
                                        .FontSize(11)
                                        .Foreground(new Color(140, 145, 160))
                                )
                        )
                ),

                Helper.CreateExampleSection("Badge on Elements (BadgeContainer)",
                    new HStack()
                        .Spacing(32)
                        .Alignment(Alignment.Center)
                        .Children(
                            CreateBadgedIcon(Icons.Email, 5, new Color(239, 68, 68)),
                            CreateBadgedIcon(Icons.Notification, 12, new Color(59, 130, 246)),
                            CreateBadgedIcon(Icons.Folder, 99, new Color(34, 197, 94)),
                            CreateBadgedIconDot(Icons.Settings, new Color(234, 179, 8)),
                            CreateBadgedButton("Messages", 3)
                        )
                ),

                Helper.CreateExampleSection("Color Variants",
                    new HStack()
                        .Spacing(12)
                        .Children(
                            new Badge("Primary").Background(new Color(59, 130, 246)),
                            new Badge("Success").Background(new Color(34, 197, 94)),
                            new Badge("Warning").Background(new Color(234, 179, 8)).TextColor(Color.Black),
                            new Badge("Danger").Background(new Color(239, 68, 68)),
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
                new Badge()
                    .Count(count)
                    .ShowZero(showZero)
                    .Background(new Color(239, 68, 68)),
                new Label(count.ToString())
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateBadgedIcon(IconData icon, int count, Color badgeColor)
    {
        return new BadgeContainer()
            .Content(
                new Frame()
                    .Size(new Size(48, 48))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Icon(icon)
                            .Size(new Size(24, 24))
                            .Color(Color.White)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    )
            )
            .Badge(
                new Badge()
                    .Count(count)
                    .Background(badgeColor)
                    .BadgeSize(BadgeSize.Small)
            )
            .BadgeHorizontalPosition(HorizontalAlignment.Right)
            .BadgeVerticalPosition(VerticalAlignment.Top);
    }

    private VisualElement CreateBadgedIconDot(IconData icon, Color badgeColor)
    {
        return new BadgeContainer()
            .Content(
                new Frame()
                    .Size(new Size(48, 48))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Icon(icon)
                            .Size(24)
                            .Color(Color.White)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    )
            )
            .Badge(
                new Badge()
                    .Dot(true)
                    .Background(badgeColor)
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
                    .Background(new Color(55, 60, 75))
                    .HoverBackground(new Color(70, 75, 90))
                    .TextColor(Color.White)
                    .BorderWidth(0)
                    .Padding(new Thickness(16, 10, 16, 10))
            )
            .Badge(
                new Badge()
                    .Count(count)
                    .Background(new Color(239, 68, 68))
                    .BadgeSize(BadgeSize.Small)
            )
            .BadgeHorizontalPosition(HorizontalAlignment.Right)
            .BadgeVerticalPosition(VerticalAlignment.Top)
            .BadgeOffset(new Position(5, -4));
    }
}
