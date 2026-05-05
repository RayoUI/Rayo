using Rayo;
using Rayo.Controls;
using Rayo.Controls.Shapes;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using static Rayo.Core.UIHelpers;
using GradientStop = Rayo.Rendering.Brushes.GradientStop;
using Rectangle = Rayo.Controls.Shapes.Rectangle;

namespace Gallery.Pages;

public class BrushesPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Brushes", "Gradient and image brushes for backgrounds"),

                Helper.CreateExampleSection("Solid Color Brush",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(new SolidColorBrush(ColorDefault.Primary), "Primary"),
                            CreateBrushSample(new SolidColorBrush(ColorDefault.Success), "Success"),
                            CreateBrushSample(new SolidColorBrush(ColorDefault.Danger), "Danger")
                        )
                ),

                Helper.CreateExampleSection("Linear Gradient - Horizontal",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(
                                LinearGradientBrush.Horizontal(ColorDefault.Primary, ColorDefault.Info),
                                "Blue to Cyan"),
                            CreateBrushSample(
                                LinearGradientBrush.Horizontal(ColorDefault.Success, ColorDefault.Warning),
                                "Green to Yellow"),
                            CreateBrushSample(
                                LinearGradientBrush.Horizontal(ColorDefault.Danger, ColorDefault.Warning),
                                "Red to Yellow")
                        )
                ),

                Helper.CreateExampleSection("Linear Gradient - Vertical",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(
                                LinearGradientBrush.Vertical(new Color(59, 130, 246), new Color(147, 51, 234)),
                                "Blue to Purple"),
                            CreateBrushSample(
                                LinearGradientBrush.Vertical(new Color(16, 185, 129), new Color(59, 130, 246)),
                                "Teal to Blue"),
                            CreateBrushSample(
                                LinearGradientBrush.Vertical(new Color(251, 146, 60), new Color(239, 68, 68)),
                                "Orange to Red")
                        )
                ),

                Helper.CreateExampleSection("Linear Gradient - Diagonal",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(
                                LinearGradientBrush.Diagonal(new Color(236, 72, 153), new Color(251, 146, 60)),
                                "Pink to Orange"),
                            CreateBrushSample(
                                LinearGradientBrush.Diagonal(new Color(34, 197, 94), new Color(59, 130, 246)),
                                "Green to Blue"),
                            CreateBrushSample(
                                LinearGradientBrush.Diagonal(new Color(168, 85, 247), new Color(236, 72, 153)),
                                "Purple to Pink")
                        )
                ),

                Helper.CreateExampleSection("Linear Gradient - Multi-Stop",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(
                                new LinearGradientBrush(
                                    new GradientStop(Color.Red, 0f),
                                    new GradientStop(Color.Yellow, 0.5f),
                                    new GradientStop(Color.Green, 1f)
                                ) { StartPoint = new System.Numerics.Vector2(0, 0.5f), EndPoint = new System.Numerics.Vector2(1, 0.5f) },
                                "RGB Gradient"),
                            CreateBrushSample(
                                new LinearGradientBrush(
                                    new GradientStop(new Color(59, 130, 246), 0f),
                                    new GradientStop(new Color(147, 51, 234), 0.33f),
                                    new GradientStop(new Color(236, 72, 153), 0.66f),
                                    new GradientStop(new Color(251, 146, 60), 1f)
                                ) { StartPoint = new System.Numerics.Vector2(0, 0.5f), EndPoint = new System.Numerics.Vector2(1, 0.5f) },
                                "Rainbow")
                        )
                ),

                Helper.CreateExampleSection("Radial Gradient",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(
                                RadialGradientBrush.Circular(Color.White, ColorDefault.Primary),
                                "Center Light"),
                            CreateBrushSample(
                                RadialGradientBrush.Circular(ColorDefault.Warning, ColorDefault.Danger),
                                "Warm"),
                            CreateBrushSample(
                                RadialGradientBrush.Circular(new Color(59, 130, 246), new Color(30, 30, 40)),
                                "Blue Vignette")
                        )
                ),

                Helper.CreateExampleSection("Conic Gradient",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateBrushSample(
                                ConicGradientBrush.ColorWheel(),
                                "Color Wheel"),
                            CreateBrushSample(
                                ConicGradientBrush.Sweep(ColorDefault.Primary, ColorDefault.Success),
                                "Blue to Green"),
                            CreateBrushSample(
                                ConicGradientBrush.Sweep(ColorDefault.Danger, ColorDefault.Warning, 45),
                                "Red to Yellow")
                        )
                ),

                Helper.CreateExampleSection("Gradient Spread Methods",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new Label()
                                .Text("Pad | Repeat | Reflect")
                                .Foreground(ColorDefault.Secondary),
                            new HStack()
                                .Spacing(15)
                                .Children(
                                    CreateBrushSample(
                                        new LinearGradientBrush(ColorDefault.Primary, ColorDefault.Info)
                                        {
                                            StartPoint = new System.Numerics.Vector2(0.25f, 0.5f),
                                            EndPoint = new System.Numerics.Vector2(0.75f, 0.5f),
                                            SpreadMethod = GradientSpreadMethod.Pad
                                        },
                                        "Pad"),
                                    CreateBrushSample(
                                        new LinearGradientBrush(ColorDefault.Primary, ColorDefault.Info)
                                        {
                                            StartPoint = new System.Numerics.Vector2(0.25f, 0.5f),
                                            EndPoint = new System.Numerics.Vector2(0.75f, 0.5f),
                                            SpreadMethod = GradientSpreadMethod.Repeat
                                        },
                                        "Repeat"),
                                    CreateBrushSample(
                                        new LinearGradientBrush(ColorDefault.Primary, ColorDefault.Info)
                                        {
                                            StartPoint = new System.Numerics.Vector2(0.25f, 0.5f),
                                            EndPoint = new System.Numerics.Vector2(0.75f, 0.5f),
                                            SpreadMethod = GradientSpreadMethod.Reflect
                                        },
                                        "Reflect")
                                )
                        )
                ),

                Helper.CreateExampleSection("Brushes on Shapes",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            new VStack()
                                .Spacing(4)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Rectangle(100, 60)
                                        .Fill(LinearGradientBrush.Horizontal(ColorDefault.Primary, ColorDefault.Info))
                                        .Radius(8),
                                    new Label().Text("Rectangle").FontSize(12).Foreground(ColorDefault.Secondary)
                                ),
                            new VStack()
                                .Spacing(4)
                                .Alignment(Alignment.Center)
                                .Children(
                                    Ellipse.Circle(60)
                                        .Fill(RadialGradientBrush.Circular(Color.White, ColorDefault.Danger)),
                                    new Label().Text("Circle").FontSize(12).Foreground(ColorDefault.Secondary)
                                ),
                            new VStack()
                                .Spacing(4)
                                .Alignment(Alignment.Center)
                                .Children(
                                    new Frame()
                                        .Width(100)
                                        .Height(60)
                                        .Background(LinearGradientBrush.Diagonal(ColorDefault.Success, ColorDefault.Warning))
                                        .BorderRadius(new CornerRadius(8)),
                                    new Label().Text("Frame").FontSize(12).Foreground(ColorDefault.Secondary)
                                )
                        )
                )
            );
    }

    private VisualElement CreateBrushSample(Brush brush, string label)
    {
        var Frame = new Frame();
        Frame.Width(100);
        Frame.Height(60);
        Frame.Background(brush);
        Frame.ClipToBounds(true);
        Frame.BorderRadius(new CornerRadius(8));
        Frame.BorderColor(new Color(80, 80, 80));
        Frame.BorderWidth(1);

        return new VStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .Children(
                Frame,
                new Label()
                    .Text(label)
                    .FontSize(12)
                    .Foreground(ColorDefault.Secondary)
            );
    }
}
