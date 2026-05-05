using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class SplitterPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Splitter", "Resizable Frames with adjustable separators"),

                Helper.CreateExampleSection("Horizontal Splitter",
                    new Frame()
                        .Height(200)
                        .Background(new Color(25, 28, 35))
                        .Content(
                            new Splitter()
                                .Orientation(SplitterOrientation.Horizontal)
                                .SplitterSize(6)
                                .Children(
                                    // Left Frame (Fixed width initially)
                                    new Frame()
                                        .Width(150)
                                        .Background(new Color(59, 130, 246, 0.2f))
                                        .Content(
                                            new Label("Left (Fixed Width)")
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(Color.White)
                                        ),

                                    // Center Frame (Flexible)
                                    new Frame()
                                        .Background(new Color(34, 197, 94, 0.2f))
                                        .Content(
                                            new Label("Center (Star)")
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(Color.White)
                                        ),
                                    
                                    // Right Frame (Fixed width initially)
                                    new Frame()
                                        .Width(100)
                                        .Background(new Color(239, 68, 68, 0.2f))
                                        .Content(
                                            new Label("Right (Fixed)")
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(Color.White)
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Vertical Splitter",
                    new Frame()
                        .Height(300)
                        .Background(new Color(25, 28, 35))
                        .Content(
                            new Splitter()
                                .Orientation(SplitterOrientation.Vertical)
                                .SplitterSize(6)
                                .Children(
                                    // Top Frame
                                    new Frame()
                                        .Height(80)
                                        .Background(new Color(168, 85, 247, 0.2f))
                                        .Content(
                                            new Label("Top (Fixed Height)")
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(Color.White)
                                        ),

                                    // Bottom Frame (Flexible)
                                    new Frame()
                                        .Background(new Color(234, 179, 8, 0.2f))
                                        .Content(
                                            new Label("Bottom (Star)")
                                                .VerticalAlignment(VerticalAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .Foreground(Color.White)
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Nested Splitters (IDE Layout)",
                    new Frame()
                        .Height(300)
                        .Background(new Color(25, 28, 35))
                        .Content(
                            new Splitter()
                                .Orientation(SplitterOrientation.Horizontal)
                                .Children(
                                    // Explorer (Left)
                                    new Frame()
                                        .Width(150)
                                        .Background(new Color(30, 30, 35))
                                        .Content(
                                            new Label("Explorer")
                                                .Padding(new Thickness(5))
                                                .Foreground(new Color(150, 150, 150))
                                        ),

                                    // Main Area (Right)
                                    new Splitter()
                                        .Orientation(SplitterOrientation.Vertical)
                                        .Children(
                                            // Editor (Top)
                                            new Frame()
                                                .Background(new Color(20, 20, 24))
                                                .Content(
                                                    new Label("Code Editor")
                                                        .Padding(new Thickness(10))
                                                        .Foreground(Color.White)
                                                ),
                                            
                                            // Terminal (Bottom)
                                            new Frame()
                                                .Height(100)
                                                .Background(new Color(40, 40, 45))
                                                .Content(
                                                    new Label("Terminal")
                                                        .Padding(new Thickness(5))
                                                        .Foreground(new Color(180, 180, 180))
                                                )
                                        )
                                )
                        )
                ),
                
                 Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Horizontal and Vertical orientations"),
                            CreateFeatureItem("Drag handles to resize Frames"),
                            CreateFeatureItem("Support for fixed sized and flexible (star) Frames"),
                            CreateFeatureItem("Nested splitters for complex layouts"),
                            CreateFeatureItem("Customizable splitter size and colors")
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
