using Rayo;
using Rayo.Controls;
using Rayo.Controls.Shapes;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using System.Numerics;
using static Rayo.Core.UIHelpers;
using Path = Rayo.Controls.Shapes.Path;

namespace Gallery.Pages;

public class ShapesPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Shapes", "Geometric shape controls (Rectangle, Ellipse, Line, Polygon, Path)"),

                // Basic colored rectangles
                Helper.CreateExampleSection("Basic Rectangles",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            new Rectangle(100, 100).Fill(ColorDefault.Primary),
                            new Rectangle(100, 100).Fill(ColorDefault.Success),
                            new Rectangle(100, 100).Fill(ColorDefault.Warning),
                            new Rectangle(100, 100).Fill(ColorDefault.Danger)
                        )
                ),

                // Different sizes
                Helper.CreateExampleSection("Different Sizes",
                    new HStack()
                        .Spacing(15)
                        .Alignment(Alignment.End)
                        .Children(
                            new Rectangle(50, 50).Fill(ColorDefault.Info),
                            new Rectangle(75, 75).Fill(ColorDefault.Info),
                            new Rectangle(100, 100).Fill(ColorDefault.Info),
                            new Rectangle(125, 125).Fill(ColorDefault.Info)
                        )
                ),

                // Rounded rectangles
                Helper.CreateExampleSection("Rounded Rectangles",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Rectangle(80, 50)
                                .Fill(ColorDefault.Primary)
                                .Stroke(Color.White)
                                .StrokeThickness(2),

                            new Rectangle(60, 60)
                                .Fill(ColorDefault.Success)
                                .Radius(8),

                            new Rectangle(80, 60)
                                .Fill(ColorDefault.Warning)
                                .Radius(18),

                            new Rectangle(80, 50)
                                .Fill(Color.Transparent)
                                .Stroke(ColorDefault.Danger)
                                .StrokeThickness(3)
                                .Radius(12)
                        )
                ),

                // Borders
                Helper.CreateExampleSection("With Borders",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            new Rectangle(80, 80)
                                .Fill(ColorDefault.Primary)
                                .Stroke(ColorDefault.Danger)
                                .StrokeThickness(4),

                            new Rectangle(80, 80)
                                .Fill(ColorDefault.Success)
                                .Stroke(ColorDefault.Warning)
                                .StrokeThickness(6)
                                .Radius(20),

                            Ellipse.Circle(80)
                                .Fill(ColorDefault.Info)
                                .Stroke(ColorDefault.Secondary)
                                .StrokeThickness(5)
                        )
                ),

                // Lines as dividers
                Helper.CreateExampleSection("Lines as Dividers",
                    new VStack()
                        .Spacing(0)
                        .Width(400)
                        .Children(
                            new Label("Section 1")
                                .Padding(new Thickness(12))
                                .Foreground(Color.White),

                            new Rectangle(400, 2).Fill(ColorDefault.Secondary),

                            new Label("Section 2")
                                .Padding(new Thickness(12))
                                .Foreground(Color.White),

                            new Rectangle(400, 2).Fill(ColorDefault.Secondary),

                            new Label("Section 3")
                                .Padding(new Thickness(12))
                                .Foreground(Color.White)
                        )
                ),

                // Ellipse / Circle
                Helper.CreateExampleSection("Ellipse / Circle",
                    new HStack()
                        .Spacing(20)
                        .Alignment(Alignment.Center)
                        .Children(
                            Ellipse.Circle(60)
                                .Fill(ColorDefault.Info)
                                .Stroke(Color.White)
                                .StrokeThickness(2),

                            new Ellipse(100, 50)
                                .Fill(ColorDefault.Danger)
                                .Stroke(Color.White)
                                .StrokeThickness(2),

                            Ellipse.Circle(50)
                                .Fill(Color.Transparent)
                                .Stroke(ColorDefault.Warning)
                                .StrokeThickness(3)
                        )
                ),

                // Line
                Helper.CreateExampleSection("Line",
                    new HStack()
                        .Spacing(30)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Line(0, 0, 100, 0)
                                .Stroke(ColorDefault.Primary)
                                .StrokeThickness(2),

                            new Line(0, 0, 80, 60)
                                .Stroke(ColorDefault.Success)
                                .StrokeThickness(3),

                            new Line(0, 50, 100, 0)
                                .Stroke(ColorDefault.Danger)
                                .StrokeThickness(2),

                            new Line(0, 0, 100, 80)
                                .Stroke(ColorDefault.Info)
                                .StrokeThickness(6)
                        )
                ),

                // Polygon - Regular Shapes
                Helper.CreateExampleSection("Polygon - Regular Shapes",
                    new HStack()
                        .Spacing(30)
                        .Alignment(Alignment.Center)
                        .Children(
                            Polygon.Triangle(60, 50)
                                .Fill(ColorDefault.Warning)
                                .Stroke(Color.White)
                                .StrokeThickness(2),

                            Polygon.Regular(5, 35, 35, 35)
                                .Fill(ColorDefault.Info)
                                .Stroke(Color.White)
                                .StrokeThickness(2),

                            Polygon.Regular(6, 35, 35, 35)
                                .Fill(ColorDefault.Success)
                                .Stroke(Color.White)
                                .StrokeThickness(2),

                            Polygon.Regular(8, 40, 40, 40)
                                .Fill(ColorDefault.Primary)
                                .Stroke(ColorDefault.Secondary)
                                .StrokeThickness(2)
                        )
                ),

                // Polygon - Star
                Helper.CreateExampleSection("Polygon - Star",
                    new HStack()
                        .Spacing(30)
                        .Alignment(Alignment.Center)
                        .Children(
                            Polygon.Star(5, 40, 20, 40, 40)
                                .Fill(new Color(255, 215, 0))
                                .Stroke(new Color(255, 165, 0))
                                .StrokeThickness(2),

                            Polygon.Star(6, 35, 18, 35, 35)
                                .Fill(ColorDefault.Danger)
                                .Stroke(Color.White)
                                .StrokeThickness(1),

                            Polygon.Star(7, 40, 12, 40, 40)
                                .Fill(ColorDefault.Warning)
                                .Stroke(ColorDefault.Primary)
                                .StrokeThickness(2),

                            Polygon.Star(8, 35, 20, 35, 35)
                                .Fill(ColorDefault.Info)
                                .Stroke(Color.White)
                                .StrokeThickness(1)
                        )
                ),

                // Custom Colors
                Helper.CreateExampleSection("Custom Colors",
                    new HStack()
                        .Spacing(10)
                        .Children(
                            new Rectangle(80, 80).Fill(new Color(255, 100, 100)),
                            new Rectangle(80, 80).Fill(new Color(100, 255, 100)),
                            new Rectangle(80, 80).Fill(new Color(100, 100, 255)),
                            new Rectangle(80, 80).Fill(new Color(255, 255, 100)),
                            new Rectangle(80, 80).Fill(new Color(255, 100, 255))
                        )
                ),

                // Polyline
                Helper.CreateExampleSection("Polyline",
                    new Polyline()
                        .Points(
                            new Vector2(0, 40),
                            new Vector2(30, 0),
                            new Vector2(60, 30),
                            new Vector2(90, 10),
                            new Vector2(120, 40),
                            new Vector2(150, 20)
                        )
                        .Stroke(ColorDefault.Primary)
                        .StrokeThickness(3)
                ),

                // Path - SVG Data
                Helper.CreateExampleSection("Path - SVG Data",
                    new HStack()
                        .Spacing(30)
                        .Alignment(Alignment.Center)
                        .Children(
                            // Heart shape
                            new Path("M 25 10 C 15 0 0 10 0 25 C 0 40 25 50 25 50 C 25 50 50 40 50 25 C 50 10 35 0 25 10 Z")
                                .Fill(ColorDefault.Danger)
                                .Stroke(Color.White)
                                .StrokeThickness(1),

                            // Arrow
                            new Path("M 0 20 L 30 20 L 30 10 L 50 25 L 30 40 L 30 30 L 0 30 Z")
                                .Fill(ColorDefault.Success)
                                .Stroke(Color.White)
                                .StrokeThickness(1)
                        )
                ),

                // Path - Programmatic
                Helper.CreateExampleSection("Path - Programmatic",
                    new Path()
                        .MoveTo(0, 0)
                        .LineTo(50, 0)
                        .LineTo(60, 25)
                        .LineTo(50, 50)
                        .LineTo(0, 50)
                        .LineTo(10, 25)
                        .ClosePath()
                        .Fill(new Color(147, 112, 219))
                        .Stroke(Color.White)
                        .StrokeThickness(2)
                )
            );
    }
}
