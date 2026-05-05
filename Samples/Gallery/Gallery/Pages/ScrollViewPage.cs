using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class ScrollViewPage : UserControl
{
    public override VisualElement Build()
    {
        var longContent = new VStack().Spacing(10);
        for (int i = 1; i <= 50; i++)
        {
            longContent.AddChild(
                new Label($"Item {i}")
                    .Foreground(i % 2 == 0 ? Color.White : ColorDefault.Secondary)
                    .Padding(new Thickness(8)
            ));
        }

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ScrollView", "Scrollable content container"),

                Helper.CreateExampleSection("Vertical Scroll",
                    new ScrollView()
                        .Width(300)
                        .Height(200)
                        .Content(longContent)
                ),

                Helper.CreateExampleSection("With Custom Content",
                    new ScrollView()
                        .Width(400)
                        .Height(250)
                        .Content(
                            new VStack()
                                .Spacing(15)
                                .Padding(new Thickness(16))
                                .Children(
                                    new Label("Scrollable Frame")
                                        .FontSize(18)
                                        .Foreground(Color.White),

                                    new Frame()
                                        .Background(ColorDefault.Primary)
                                        .BorderRadius(8)
                                        .Padding(new Thickness(12))
                                        .Content(
                                            new Label("This is a Frame inside a scroll view")
                                                .Foreground(Color.White)
                                        ),

                                    new Frame()
                                        .Background(ColorDefault.Success)
                                        .BorderRadius(8)
                                        .Padding(new Thickness(12))
                                        .Content(
                                            new Label("You can scroll to see more content")
                                                .Foreground(Color.White)
                                        ),

                                    new Frame()
                                        .Background(ColorDefault.Warning)
                                        .BorderRadius(8)
                                        .Padding(new Thickness(12))
                                        .Content(
                                            new Label("Add as many items as you need")
                                                .Foreground(Color.White)
                                        ),

                                    new Frame()
                                        .Background(ColorDefault.Danger)
                                        .BorderRadius(8)
                                        .Padding(new Thickness(12))
                                        .Content(
                                            new Label("The scrollbar appears automatically")
                                                .Foreground(Color.White)
                                        ),

                                    new Frame()
                                        .Background(ColorDefault.Info)
                                        .BorderRadius(8)
                                        .Padding(new Thickness(12))
                                        .Content(
                                            new Label("Last item - scroll to see it")
                                                .Foreground(Color.White)
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Compact ScrollView",
                    new Frame()
                        .Background(new Color(40, 40, 50))
                        .BorderRadius(8)
                        .Padding(new Thickness(16))
                        .Content(
                            new VStack()
                                .Spacing(12)
                                .Children(
                                    new Label("Recent Activity")
                                        .FontSize(16)
                                        .Foreground(Color.White),

                                    new ScrollView()
                                        .Width(350)
                                        .Height(150)
                                        .Content(
                                            new VStack()
                                                .Spacing(8)
                                                .Children(
                                                    new Label("✓ Task completed")
                                                        .Foreground(ColorDefault.Success),
                                                    new Label("✓ File uploaded")
                                                        .Foreground(ColorDefault.Success),
                                                    new Label("⚠ Warning: Low disk space")
                                                        .Foreground(ColorDefault.Warning),
                                                    new Label("✓ User logged in")
                                                        .Foreground(ColorDefault.Success),
                                                    new Label("✗ Error: Connection timeout")
                                                        .Foreground(ColorDefault.Danger),
                                                    new Label("✓ Backup completed")
                                                        .Foreground(ColorDefault.Success),
                                                    new Label("ℹ System update available")
                                                        .Foreground(ColorDefault.Info)
                                                )
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Horizontal Scroll (Orientation='Horizontal')",
                    new ScrollView()
                        .Orientation(ScrollOrientation.Horizontal)
                        .HorizontalScrollBarVisibility(ScrollBarVisibility.Auto)
                        .Width(400)
                        .Height(120)
                        .Content(
                            new HStack()
                                .Spacing(10)
                                .Padding(new Thickness(10))
                                .Children(
                                    CreateColoredFrame(new Color(59, 130, 246), "1"),
                                    CreateColoredFrame(new Color(34, 197, 94), "2"),
                                    CreateColoredFrame(new Color(234, 179, 8), "3"),
                                    CreateColoredFrame(new Color(239, 68, 68), "4"),
                                    CreateColoredFrame(new Color(168, 85, 247), "5"),
                                    CreateColoredFrame(new Color(236, 72, 153), "6"),
                                    CreateColoredFrame(new Color(6, 182, 212), "7"),
                                    CreateColoredFrame(new Color(34, 197, 94), "8"),
                                    CreateColoredFrame(new Color(234, 179, 8), "9"),
                                    CreateColoredFrame(new Color(239, 68, 68), "10")
                                )
                        )
                ),

                Helper.CreateExampleSection("Bidirectional Scroll (Orientation='Both')",
                    new ScrollView()
                        .Orientation(ScrollOrientation.Both)
                        .Width(400)
                        .Height(300)
                        .Content(
                            new VStack() // Rows
                                .Spacing(10)
                                .Children(
                                    Enumerable.Range(1, 10).Select(row => 
                                        new HStack() // Columns
                                            .Spacing(10)
                                            .Children(
                                                Enumerable.Range(1, 10).Select(col => 
                                                    new Frame()
                                                        .Size(new Size(100, 60))
                                                        .Background(new Color(row * 20, col * 20, 150))
                                                        .BorderRadius(4)
                                                        .Content(
                                                            new Label($"R{row} C{col}")
                                                                .Foreground(Color.White)
                                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                                .VerticalAlignment(VerticalAlignment.Center)
                                                        )
                                                ).ToArray()
                                            )
                                    ).ToArray()
                                )
                        )
                )
            );
    }

    private VisualElement CreateColoredFrame(Color color, string text)
    {
        return new Frame()
            .Size(new Size(80, 80))
            .Background(color)
            .BorderRadius(8)
            .Content(
                new Label(text)
                    .Foreground(Color.White)
                    .FontSize(16)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
            );
    }
}
