using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class FramePage : UserControl
{
    public override VisualElement Build()
    {
        return
            new VStack()
                .Spacing(20)
                .Padding(new Thickness(20))
                .Children(
                    Helper.CreatePageHeader("Frame", "Single-content container with styling, perfect for wrapping ScrollView and other controls"),

                    Helper.CreateInfoCard(
                        "Frame vs Frame",
                        "Frame is optimized for a single child (Content property), while Frame supports multiple children. " +
                        "Use Frame when you want to wrap a single control like ScrollView with visual styling."
                    ),

                    CreateBasicSection(),
                    CreateScrollViewSection(),
                    CreateStyledSection(),
                    CreateAlignmentSection(),
                    CreateComplexSection()
                );

    }

    private VisualElement CreateBasicSection()
    {
        var frame1 = new Frame();
        frame1.Width(300);
        frame1.Height(100);
        frame1.Background(new Color(40, 40, 50));
        frame1.BorderColor(new Color(100, 100, 120));
        frame1.BorderWidth(2);
        frame1.BorderRadius(8);
        frame1.Padding(new Thickness(20));
        frame1.Content(
            new Label("This is content inside a Frame")
                .Foreground(Color.White)
                .FontSize(14)
        );

        var frame2 = new Frame();
        frame2.Width(300);
        frame2.Height(100);
        frame2.Background(new Color(59, 130, 246));
        frame2.BorderRadius(12);
        frame2.Padding(new Thickness(20));
        frame2.Content(
            new VStack()
                .Spacing(8)
                .Children(
                    new Label("Styled Frame")
                        .FontSize(16)
                        .Foreground(Color.White),
                    new Label("With padding and rounded corners")
                        .FontSize(12)
                        .Foreground(new Color(220, 220, 255))
                )
        );

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Basic Frame"),
                Helper.CreateExampleSection("Simple Frame with Content", frame1),
                Helper.CreateExampleSection("Frame with Gradient Background", frame2)
            );
    }

    private VisualElement CreateScrollViewSection()
    {
        // Create a long list of items
        var itemsStack = new VStack().Spacing(8);
        for (int i = 1; i <= 30; i++)
        {
            itemsStack.AddChild(
                new Frame()
                    .Background(new Color(50, 50, 60))
                    .BorderRadius(6)
                    .Padding(new Thickness(12))
                    .Content(
                        new HStack()
                            .Spacing(12)
                            .Alignment(Alignment.Center)
                            .Children(
                                new Frame()
                                    .Width(40)
                                    .Height(40)
                                    .Background(new Color(59, 130, 246))
                                    .BorderRadius(20),
                                new VStack()
                                    .Spacing(4)
                                    .Children(
                                        new Label($"Item {i}")
                                            .FontSize(14)
                                            .Foreground(Color.White),
                                        new Label($"Description for item {i}")
                                            .FontSize(11)
                                            .Foreground(new Color(180, 180, 180))
                                    )
                            )
                    )
            );
        }

        var frame1 = new Frame();
        frame1.Width(400);
        frame1.Height(300);
        frame1.Background(new Color(30, 30, 35));
        frame1.BorderColor(new Color(70, 70, 80));
        frame1.BorderWidth(1);
        frame1.BorderRadius(10);
        frame1.Padding(new Thickness(12));
        frame1.Content(new ScrollView(itemsStack));

        var frame2 = new Frame();
        frame2.Width(400);
        frame2.Background(new Color(30, 30, 35));
        frame2.BorderColor(new Color(70, 70, 80));
        frame2.BorderWidth(1);
        frame2.BorderRadius(10);
        frame2.Padding(new Thickness(12));
        frame2.Content(
            new ScrollView(
                new VStack()
                    .Spacing(8)
                    .Children(
                        new Label("Smart Height").Foreground(Color.White).FontSize(16),
                        new Label("ScrollView automatically sizes to content (100-400px max)")
                            .Foreground(new Color(180, 180, 180))
                            .FontSize(12),
                        CreateDummyList(10)
                    )
            )
        );

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Frame with ScrollView"),
                Helper.CreateExampleSection("ScrollView inside Frame (Fixed Height)", frame1),
                Helper.CreateExampleSection("ScrollView inside Frame (Auto Height with Clamp)", frame2)
            );
    }

    private VisualElement CreateStyledSection()
    {
        var frame1 = new Frame();
        frame1.Width(150);
        frame1.Height(120);
        frame1.Background(new Color(45, 55, 72));
        frame1.BorderColor(new Color(59, 130, 246));
        frame1.BorderWidth(3);
        frame1.BorderRadius(12);
        frame1.Padding(new Thickness(16));
        frame1.Content(
            new Label("Thick Border")
                .Foreground(Color.White)
                .FontSize(13)
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
        );

        var frame2 = new Frame();
        frame2.Width(150);
        frame2.Height(120);
        frame2.Background(new Color(45, 55, 72));
        frame2.BorderColor(new Color(34, 197, 94));
        frame2.BorderWidth(1);
        frame2.BorderRadius(4);
        frame2.Padding(new Thickness(16));
        frame2.Content(
            new Label("Thin Border")
                .Foreground(Color.White)
                .FontSize(13)
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
        );

        var frame3 = new Frame();
        frame3.Width(150);
        frame3.Height(120);
        frame3.Background(new Color(45, 55, 72));
        frame3.BorderRadius(20);
        frame3.Padding(new Thickness(16));
        frame3.Content(
            new Label("No Border")
                .Foreground(Color.White)
                .FontSize(13)
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
        );

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Styled Frames"),
                Helper.CreateExampleSection("Different Border Styles",
                    new HStack()
                        .Spacing(15)
                        .Children(frame1, frame2, frame3)
                )
            );
    }

    private VisualElement CreateAlignmentSection()
    {
        var stretchFrame = new Frame();
        stretchFrame.Width(350);
        stretchFrame.Height(120);
        stretchFrame.Background(new Color(30, 30, 35));
        stretchFrame.BorderColor(new Color(70, 70, 80));
        stretchFrame.BorderWidth(1);
        stretchFrame.BorderRadius(8);
        stretchFrame.Padding(new Thickness(12));
        stretchFrame.Content(
            new Frame()
                .Background(new Color(59, 130, 246))
                .BorderRadius(6)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Content(
                    new Label("Content stretches to fill Frame")
                        .Foreground(Color.White)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .VerticalAlignment(VerticalAlignment.Center)
                )
        );

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Content Alignment"),

                Helper.CreateExampleSection("Different Content Alignments",
                    new HStack()
                        .Spacing(15)
                        .Children(
                            CreateAlignedFrame("Top Left", HorizontalAlignment.Left, VerticalAlignment.Top),
                            CreateAlignedFrame("Center", HorizontalAlignment.Center, VerticalAlignment.Center),
                            CreateAlignedFrame("Bottom Right", HorizontalAlignment.Right, VerticalAlignment.Bottom)
                        )
                ),

                Helper.CreateExampleSection("Stretch Content", stretchFrame)
            );
    }

    private VisualElement CreateComplexSection()
    {
        var innerFrame = new Frame();
        innerFrame.Height(150);
        innerFrame.Background(new Color(40, 40, 50));
        innerFrame.BorderRadius(8);
        innerFrame.Padding(new Thickness(12));
        innerFrame.Content(
            new ScrollView(
                new Label(
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                    "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris. " +
                    "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum."
                )
                .Foreground(new Color(200, 200, 200))
                .FontSize(13)
            )
        );

        var outerFrame = new Frame();
        outerFrame.Width(350);
        outerFrame.Background(new Color(30, 30, 35));
        outerFrame.BorderColor(new Color(70, 70, 80));
        outerFrame.BorderWidth(1);
        outerFrame.BorderRadius(12);
        outerFrame.Padding(new Thickness(20));
        outerFrame.Content(
            new VStack()
                .Spacing(16)
                .Children(
                    // Header
                    new HStack()
                        .Spacing(12)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Frame()
                                .Width(50)
                                .Height(50)
                                .Background(new Color(59, 130, 246))
                                .BorderRadius(25),
                            new VStack()
                                .Spacing(4)
                                .Children(
                                    new Label("User Name")
                                        .FontSize(16)
                                        .Foreground(Color.White),
                                    new Label("Online")
                                        .FontSize(12)
                                        .Foreground(new Color(34, 197, 94))
                                )
                        ),

                    // Content in scrollable area
                    innerFrame,

                    // Footer
                    new HStack()
                        .Spacing(8)
                        .Children(
                            new Button()
                                .Text("Action 1")
                                .Width(100)
                                .Background(new Color(59, 130, 246)),
                            new Button()
                                .Text("Action 2")
                                .Width(100)
                                .Background(new Color(100, 100, 120))
                        )
                )
        );

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Complex Examples"),
                Helper.CreateExampleSection("Card with Frame", outerFrame)
            );
    }

    // Helper Methods

    private VisualElement CreateAlignedFrame(string text, HorizontalAlignment hAlign, VerticalAlignment vAlign)
    {
        var content = new Frame();
        content.Width(80);
        content.Height(60);
        content.Background(new Color(59, 130, 246));
        content.BorderRadius(6);
        content.HorizontalAlignment(hAlign);
        content.VerticalAlignment(vAlign);
        content.Content(
            new Label(text)
                .Foreground(Color.White)
                .FontSize(11)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
        );

        var frame = new Frame();
        frame.Width(150);
        frame.Height(120);
        frame.Background(new Color(30, 30, 35));
        frame.BorderColor(new Color(70, 70, 80));
        frame.BorderWidth(1);
        frame.BorderRadius(8);
        frame.Padding(new Thickness(12));
        frame.Content(content);
        return frame;
    }

    private VisualElement CreateDummyList(int count)
    {
        var stack = new VStack().Spacing(6);
        for (int i = 1; i <= count; i++)
        {
            stack.AddChild(
                new Frame()
                    .Background(new Color(50, 50, 60))
                    .BorderRadius(4)
                    .Padding(new Thickness(10))
                    .Content(
                        new Label($"List item {i}")
                            .Foreground(Color.White)
                            .FontSize(12)
                    )
            );
        }
        return stack;
    }
}
