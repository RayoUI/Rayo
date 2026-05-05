using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class IconPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Icon", "Scalable vector icons with customizable colors"),

                Helper.CreateExampleSection("Built-in Vector Icons",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            CreateVectorIcon("Check", Icons.Check),
                            CreateVectorIcon("Close", Icons.Close),
                            CreateVectorIcon("Add", Icons.Add),
                            CreateVectorIcon("Remove", Icons.Remove),
                            CreateVectorIcon("Edit", Icons.Edit),
                            CreateVectorIcon("Delete", Icons.Delete),
                            CreateVectorIcon("Save", Icons.Save),
                            CreateVectorIcon("Menu", Icons.Menu)
                        )
                ),

                Helper.CreateExampleSection("Navigation Icons",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            CreateVectorIcon("Up", Icons.ArrowUp),
                            CreateVectorIcon("Down", Icons.ArrowDown),
                            CreateVectorIcon("Left", Icons.ArrowLeft),
                            CreateVectorIcon("Right", Icons.ArrowRight),
                            CreateVectorIcon("Home", Icons.Home),
                            CreateVectorIcon("Search", Icons.Search),
                            CreateVectorIcon("Settings", Icons.Settings),
                            CreateVectorIcon("Refresh", Icons.Refresh)
                        )
                ),

                Helper.CreateExampleSection("Common Icons",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            CreateVectorIcon("File", Icons.File),
                            CreateVectorIcon("Folder", Icons.Folder),
                            CreateVectorIcon("Person", Icons.Person),
                            CreateVectorIcon("Email", Icons.Email),
                            CreateVectorIcon("Star", Icons.Star),
                            CreateVectorIcon("Heart", Icons.Heart),
                            CreateVectorIcon("Info", Icons.Info),
                            CreateVectorIcon("Warning", Icons.Warning)
                        )
                ),

                Helper.CreateExampleSection("Icon Sizes",
                    new HStack()
                        .Spacing(24)
                        .Alignment(Alignment.End)
                        .Children(
                            CreateSizedIcon(16, "16px"),
                            CreateSizedIcon(24, "24px"),
                            CreateSizedIcon(32, "32px"),
                            CreateSizedIcon(48, "48px"),
                            CreateSizedIcon(64, "64px")
                        )
                ),

                Helper.CreateExampleSection("Icon Colors",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            CreateColoredIcon("White", Color.White),
                            CreateColoredIcon("Primary", new Color(59, 130, 246)),
                            CreateColoredIcon("Success", new Color(34, 197, 94)),
                            CreateColoredIcon("Warning", new Color(234, 179, 8)),
                            CreateColoredIcon("Danger", new Color(239, 68, 68)),
                            CreateColoredIcon("Purple", new Color(168, 85, 247)),
                            CreateColoredIcon("Cyan", new Color(6, 182, 212))
                        )
                ),

                Helper.CreateExampleSection("Icons in Buttons",
                    new HStack()
                        .Spacing(12)
                        .Children(
                            CreateIconButton(Icons.Add, "Add Item", new Color(59, 130, 246)),
                            CreateIconButton(Icons.Edit, "Edit", new Color(234, 179, 8)),
                            CreateIconButton(Icons.Delete, "Delete", new Color(239, 68, 68)),
                            CreateIconButton(Icons.Save, "Save", new Color(34, 197, 94))
                        )
                ),

                Helper.CreateExampleSection("Icon Fonts via Label + FontFamily",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new Label("Use Label with FontFamily to render icons from registered fonts (like Lineicons)")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160)),

                            new HStack()
                                .Spacing(16)
                                .Children(
                                    CreateIconFontExample("\uEA1C", "Heart", new Color(239, 68, 68)),
                                    CreateIconFontExample("\uEA1D", "Star", new Color(234, 179, 8)),
                                    CreateIconFontExample("\uEA44", "Home", new Color(59, 130, 246)),
                                    CreateIconFontExample("\uEA54", "User", new Color(168, 85, 247)),
                                    CreateIconFontExample("\uEA5E", "Search", new Color(34, 197, 94)),
                                    CreateIconFontExample("\uEA5F", "Settings", new Color(6, 182, 212)),
                                    CreateIconFontExample("\uEC59", "Youtube", new Color(34, 197, 94)),
                                    CreateIconFontExample("\uEC4F", "X", new Color(239, 68, 68))
                                ),

                            new Frame()
                                .Background(new Color(30, 33, 42))
                                .BorderRadius(8)
                                .Padding(new Thickness(12))
                                .Content(
                                    new VStack()
                                        .Spacing(4)
                                        .Children(
                                            new Label("// Usage:")
                                                .FontSize(11)
                                                .Foreground(new Color(106, 153, 85)),
                                            new Label("new Label(\"\\uEA1C\").FontFamily(\"Lineicons\").FontSize(32)")
                                                .FontSize(11)
                                                .Foreground(new Color(156, 220, 254))
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Icon Font Sizes",
                    new HStack()
                        .Spacing(24)
                        .Alignment(Alignment.Start)
                        .Children(
                            CreateIconFontSizedExample("\uEA1D", 16, "16px"),
                            CreateIconFontSizedExample("\uEA1D", 24, "24px"),
                            CreateIconFontSizedExample("\uEA1D", 32, "32px"),
                            CreateIconFontSizedExample("\uEA1D", 48, "48px"),
                            CreateIconFontSizedExample("\uEA1D", 64, "64px")
                        )
                )
            );
    }

    private VisualElement CreateVectorIcon(string label, IconData iconData)
    {
        return new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(48, 48))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Icon(iconData)
                            .Size(24)
                            .Color(Color.White)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(10)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateSizedIcon(float size, string label)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(size + 16, size + 16))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Icon(Icons.Star)
                            .Size(size)
                            .Color(new Color(234, 179, 8))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateColoredIcon(string label, Color color)
    {
        return new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(48, 48))
                    .Background(new Color(35, 38, 48))
                    .BorderRadius(8)
                    .Content(
                        new Icon(Icons.Heart)
                            .Size(28)
                            .Color(color)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(10)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateIconButton(IconData iconData, string text, Color bgColor)
    {
        return new Frame()
            .Background(bgColor)
            .BorderRadius(8)
            .Padding(new Thickness(14, 10, 14, 10))
            .Content(
                new HStack()
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Icon(iconData)
                            .Size(18)
                            .Color(Color.White),
                        new Label(text)
                            .FontSize(14)
                            .Foreground(Color.White)
                    )
            );
    }

    private VisualElement CreateIconFontExample(string unicodeChar, string label, Color color)
    {
        return new VStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(48, 48))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Label(unicodeChar)
                            .FontFamily("Lineicons")
                            .FontSize(28)
                            .Foreground(color)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(10)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateIconFontSizedExample(string unicodeChar, float size, string label)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(size + 16, size + 16))
                    .Background(new Color(45, 48, 58))
                    .BorderRadius(8)
                    .Content(
                        new Label(unicodeChar)
                            .FontFamily("Lineicons")
                            .FontSize(size)
                            .Foreground(new Color(234, 179, 8))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),
                new Label(label)
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
            );
    }
}

