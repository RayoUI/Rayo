using Gallery.Controls;
using Rayo;
using Rayo.Controls;
using Rayo.Controls.Shapes;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class AbsolutePage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Absolute", "Absolute positioning layout for free-form placement"),

                Helper.CreateExampleSection("Basic Absolute Positioning",
                    new Absolute()
                        .Height(200)
                        .Background(new Color(25, 28, 35))
                        .Children(
                            new Frame()
                                .AbsolutePosition(20, 20)
                                .Size(new Size(80, 80))
                                .Background(new Color(59, 130, 246))
                                .BorderRadius(8),

                            new Frame()
                                .AbsolutePosition(120, 40)
                                .Size(new Size(100, 60))
                                .Background(new Color(34, 197, 94))
                                .BorderRadius(8),

                            new Frame()
                                .AbsolutePosition(240, 80)
                                .Size(new Size(70, 70))
                                .Background(new Color(234, 179, 8))
                                .BorderRadius(35),

                            new Frame()
                                .AbsolutePosition(330, 30)
                                .Size(new Size(90, 90))
                                .Background(new Color(239, 68, 68))
                                .BorderRadius(8)
                        )
                ),

                Helper.CreateExampleSection("Overlapping Elements",
                    new Absolute()
                        .Height(180)
                        .Background(new Color(25, 28, 35))
                        .Children(
                            new Frame()
                                .AbsolutePosition(50, 30)
                                .Size(new Size(120, 120))
                                .Background(new Color(59, 130, 246, 0.8f))
                                .BorderRadius(12),

                            new Frame()
                                .AbsolutePosition(100, 50)
                                .Size(new Size(120, 120))
                                .Background(new Color(168, 85, 247, 0.8f))
                                .BorderRadius(12),

                            new Frame()
                                .AbsolutePosition(150, 70)
                                .Size(new Size(120, 120))
                                .Background(new Color(236, 72, 153, 0.8f))
                                .BorderRadius(12)
                        )
                ),

                Helper.CreateExampleSection("Mixed Content",
                    new Absolute()
                        .Height(200)
                        .Background(new Color(25, 28, 35))
                        .Children(
                            new Label("Top Left")
                                .AbsolutePosition(10, 10)
                                .FontSize(14)
                                .Foreground(Color.White),

                            new Label("Center")
                                .AbsolutePosition(200, 90)
                                .FontSize(16)
                                .Foreground(new Color(59, 130, 246)),

                            new Button()
                                .AbsolutePosition(320, 20)
                                .Text("Button")
                                .Background(new Color(34, 197, 94))
                                .TextColor(Color.White)
                                .BorderWidth(0)
                                .Padding(new Thickness(16, 8, 16, 8)),

                            new Icon(Icons.Star)
                                .AbsolutePosition(80, 80)
                                .Size(48)
                                .Color(new Color(234, 179, 8)),

                            new Icon(Icons.Heart)
                                .AbsolutePosition(150, 120)
                                .Size(36)
                                .Color(new Color(239, 68, 68)),

                            new Frame()
                                .AbsolutePosition(250, 120)
                                .Size(new Size(150, 60))
                                .Background(new Color(45, 48, 58))
                                .BorderRadius(8)
                                .Padding(new Thickness(12))
                                .Content(
                                    new Label("Nested Frame")
                                        .FontSize(13)
                                        .Foreground(new Color(180, 185, 195))
                                )
                        )
                ),

                Helper.CreateExampleSection("Vector Paths - Cubic Bezier Curves",
                    new Absolute()
                        .Height(220)
                        .Background(new Color(25, 28, 35))
                        .Children(
                            // Simple cubic bezier curve
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(10, 20)
                                .MoveTo(0, 80)
                                .CurveTo(40, 0, 80, 0, 120, 80)
                                .Stroke(new Color(59, 130, 246))
                                .StrokeThickness(2)
                                .Fill(Color.Transparent),
                            new Label("Simple S-Curve")
                                .AbsolutePosition(30, 110)
                                .FontSize(11)
                                .Foreground(new Color(59, 130, 246)),

                            // Complex bezier path
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(150, 20)
                                .MoveTo(0, 80)
                                .CurveTo(20, 0, 60, 0, 80, 40)
                                .CurveTo(100, 80, 120, 20, 140, 60)
                                .Stroke(new Color(168, 85, 247))
                                .StrokeThickness(2)
                                .Fill(Color.Transparent),
                            new Label("Wave Pattern")
                                .AbsolutePosition(180, 110)
                                .FontSize(11)
                                .Foreground(new Color(168, 85, 247)),

                            // Closed shape with bezier
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(310, 30)
                                .MoveTo(50, 0)
                                .CurveTo(80, 0, 100, 20, 100, 50)
                                .CurveTo(100, 80, 80, 100, 50, 100)
                                .CurveTo(20, 100, 0, 80, 0, 50)
                                .CurveTo(0, 20, 20, 0, 50, 0)
                                .ClosePath()
                                .Fill(new Color(236, 72, 153, 0.3f))
                                .Stroke(new Color(236, 72, 153))
                                .StrokeThickness(2),
                            new Label("Rounded Shape")
                                .AbsolutePosition(320, 140)
                                .FontSize(11)
                                .Foreground(new Color(236, 72, 153)),

                            // Heart shape using bezier
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(30, 150)
                                .MoveTo(50, 30)
                                .CurveTo(50, 20, 40, 10, 30, 10)
                                .CurveTo(10, 10, 0, 25, 0, 40)
                                .CurveTo(0, 55, 15, 65, 50, 90)
                                .CurveTo(85, 65, 100, 55, 100, 40)
                                .CurveTo(100, 25, 90, 10, 70, 10)
                                .CurveTo(60, 10, 50, 20, 50, 30)
                                .ClosePath()
                                .Fill(new Color(239, 68, 68, 0.8f))
                                .Stroke(new Color(239, 68, 68))
                                .StrokeThickness(2),
                            new Label("Heart")
                                .AbsolutePosition(55, 245)
                                .FontSize(11)
                                .Foreground(new Color(239, 68, 68))
                        )
                ),

                Helper.CreateExampleSection("Vector Paths - Quadratic Bezier Curves",
                    new Absolute()
                        .Height(200)
                        .Background(new Color(25, 28, 35))
                        .Children(
                            // Simple quadratic curve
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(20, 30)
                                .MoveTo(0, 60)
                                .QuadraticCurveTo(60, 0, 120, 60)
                                .Stroke(new Color(34, 197, 94))
                                .StrokeThickness(2)
                                .Fill(Color.Transparent),
                            new Label("Parabolic Arc")
                                .AbsolutePosition(40, 100)
                                .FontSize(11)
                                .Foreground(new Color(34, 197, 94)),

                            // Multiple connected quadratic curves
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(160, 30)
                                .MoveTo(0, 60)
                                .QuadraticCurveTo(30, 0, 60, 60)
                                .QuadraticCurveTo(90, 120, 120, 60)
                                .Stroke(new Color(234, 179, 8))
                                .StrokeThickness(2)
                                .Fill(Color.Transparent),
                            new Label("Connected Curves")
                                .AbsolutePosition(170, 165)
                                .FontSize(11)
                                .Foreground(new Color(234, 179, 8)),

                            // Filled shape with quadratic curves
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(310, 40)
                                .MoveTo(60, 0)
                                .QuadraticCurveTo(100, 30, 80, 60)
                                .QuadraticCurveTo(60, 90, 30, 80)
                                .QuadraticCurveTo(0, 70, 20, 40)
                                .QuadraticCurveTo(40, 10, 60, 0)
                                .ClosePath()
                                .Fill(new Color(59, 130, 246, 0.4f))
                                .Stroke(new Color(59, 130, 246))
                                .StrokeThickness(2),
                            new Label("Organic Shape")
                                .AbsolutePosition(320, 145)
                                .FontSize(11)
                                .Foreground(new Color(59, 130, 246))
                        )
                ),

                Helper.CreateExampleSection("SVG Path Syntax",
                    new Absolute()
                        .Height(200)
                        .Background(new Color(25, 28, 35))
                        .Children(
                            // Star using SVG syntax
                            new Rayo.Controls.Shapes.Path("M 50 0 L 61 35 L 98 35 L 68 57 L 79 91 L 50 70 L 21 91 L 32 57 L 2 35 L 39 35 Z")
                                .AbsolutePosition(20, 30)
                                .Fill(new Color(234, 179, 8))
                                .Stroke(new Color(234, 179, 8, 0.5f))
                                .StrokeThickness(1),
                            new Label("Star (SVG)")
                                .AbsolutePosition(35, 135)
                                .FontSize(11)
                                .Foreground(new Color(234, 179, 8)),

                            // Cloud using cubic bezier in SVG syntax
                            new Rayo.Controls.Shapes.Path("M 30 50 C 30 30 50 20 60 30 C 70 20 90 30 90 50 C 100 50 100 70 90 70 L 30 70 C 20 70 20 50 30 50 Z")
                                .AbsolutePosition(130, 40)
                                .Fill(new Color(168, 85, 247, 0.6f))
                                .Stroke(new Color(168, 85, 247))
                                .StrokeThickness(2),
                            new Label("Cloud")
                                .AbsolutePosition(160, 125)
                                .FontSize(11)
                                .Foreground(new Color(168, 85, 247)),

                            // Infinity symbol
                            new Rayo.Controls.Shapes.Path()
                                .AbsolutePosition(260, 60)
                                .Data("M 20 40 C 20 20 40 20 50 40 C 60 60 60 60 70 40 C 80 20 100 20 100 40 C 100 60 80 60 70 40 C 60 20 60 20 50 40 C 40 60 20 60 20 40 Z")
                                .Fill(new Color(236, 72, 153, 0.5f))
                                .Stroke(new Color(236, 72, 153))
                                .StrokeThickness(2),
                            new Label("Infinity")
                                .AbsolutePosition(290, 120)
                                .FontSize(11)
                                .Foreground(new Color(236, 72, 153)),

                            // Logo-like shape
                            new Rayo.Controls.Shapes.Path("M 50 10 Q 80 10 80 40 Q 80 70 50 70 Q 50 40 50 40 Q 50 10 20 10 Q 20 40 20 40 Q 20 70 50 70")
                                .AbsolutePosition(380, 40)
                                .Fill(Color.Transparent)
                                .Stroke(new Color(34, 197, 94))
                                .StrokeThickness(3),
                            new Label("Logo Style")
                                .AbsolutePosition(390, 125)
                                .FontSize(11)
                                .Foreground(new Color(34, 197, 94))
                        )
                ),

                Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Absolute X/Y positioning with CanvasPosition()"),
                            CreateFeatureItem("Elements can overlap freely"),
                            CreateFeatureItem("Z-order follows child order (last child on top)"),
                            CreateFeatureItem("Optional ClipToBounds for overflow handling"),
                            CreateFeatureItem("Works with any UI element type"),
                            CreateFeatureItem("Vector Path support with Cubic and Quadratic Bezier curves"),
                            CreateFeatureItem("SVG path syntax (M, L, C, Q, Z commands)"),
                            CreateFeatureItem("Programmatic API for building paths"),
                            CreateFeatureItem("Fill and Stroke with custom colors and thickness")
                        )
                )
            );
    }

    private VisualElement CreateFeatureItem(string text)
    {
        return new HStack()
            .Spacing(8)
            .Children(
                new Label("*")
                    .FontSize(14)
                    .Foreground(new Color(59, 130, 246)),
                new Label(text)
                    .FontSize(14)
                    .Foreground(new Color(180, 185, 195))
            );
    }
}
