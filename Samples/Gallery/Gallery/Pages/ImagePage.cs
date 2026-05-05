using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class ImagePage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Image", "Display images with various stretch modes"),

                Helper.CreateExampleSection("Basic Image",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Image()
                                        .Source("Assets/Images/robot.png")
                                        .Size(new Size(120, 120)),
                                    new Label("robot.png")
                                        .FontSize(12)
                                        .Foreground(new Color(140, 145, 160))
                                ),
                            new VStack()
                                .Spacing(8)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Image()
                                        .Source("Assets/Images/super_robot.png")
                                        .Size(new Size(120, 120)),
                                    new Label("super_robot.png")
                                        .FontSize(12)
                                        .Foreground(new Color(140, 145, 160))
                                )
                        )
                ),

                Helper.CreateExampleSection("Stretch Modes",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            CreateStretchExample("None", StretchMode.None),
                            CreateStretchExample("Fill", StretchMode.Fill),
                            CreateStretchExample("Uniform", StretchMode.Uniform),
                            CreateStretchExample("UniformToFill", StretchMode.UniformToFill)
                        )
                ),

                Helper.CreateExampleSection("Image with Tint",
                    new HStack()
                        .Spacing(16)
                        .Children(
                            CreateTintedImage("Original", null),
                            CreateTintedImage("Red Tint", new Color(255, 100, 100)),
                            CreateTintedImage("Green Tint", new Color(100, 255, 100)),
                            CreateTintedImage("Blue Tint", new Color(100, 100, 255)),
                            CreateTintedImage("Gold Tint", new Color(255, 215, 0))
                        )
                ),

                Helper.CreateExampleSection("Different Sizes",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.End)
                        .Children(
                            CreateSizedImage(48),
                            CreateSizedImage(64),
                            CreateSizedImage(96),
                            CreateSizedImage(128)
                        )
                )
            );
    }

    private VisualElement CreateStretchExample(string label, StretchMode mode)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Frame()
                    .Size(new Size(100, 80))
                    .Background(new Color(45, 48, 58))
                    .BorderColor(new Color(70, 75, 90))
                    .BorderWidth(1)
                    .Content(
                        new Image()
                            .Source("Assets/Images/robot.png")
                            .Stretch(mode)
                            .Size(new Size(100, 80))
                    ),
                new Label(label)
                    .FontSize(12)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateTintedImage(string label, Color? tint)
    {
        var image = new Image()
            .Source("Assets/Images/robot.png")
            .Size(new Size(64, 64))
            .Stretch(StretchMode.Uniform);

        if (tint.HasValue)
        {
            image.Tint(tint.Value);
        }

        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                image,
                new Label(label)
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
            );
    }

    private VisualElement CreateSizedImage(int size)
    {
        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                new Image()
                    .Source("Assets/Images/super_robot.png")
                    .Size(new Size(size, size))
                    .Stretch(StretchMode.Uniform),
                new Label($"{size}x{size}")
                    .FontSize(11)
                    .Foreground(new Color(140, 145, 160))
            );
    }
}
