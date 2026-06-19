using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Gallery.Pages;

public class ButtonFloatPage : UserControl
{
    public override VisualElement Build()
    {
        var clickCount = UseSignal(0);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ButtonFloat", "Floating action button for primary contextual actions"),

                Helper.CreateExampleSection("Basic",
                    new HStack()
                        .Spacing(12)
                        .Alignment(Alignment.Center)
                        .Children(
                            new ButtonFloat(Icons.Add)
                                .OnTapped(() => clickCount.Value++),
                            new ButtonFloat(Icons.Edit)
                                .Background(new Color(34, 197, 94))
                                .HoverBackground(new Color(22, 163, 74))
                                .PressedBackground(new Color(21, 128, 61))
                                .OnTapped(() => clickCount.Value++),
                            new ButtonFloat(Icons.Save)
                                .Background(new Color(234, 179, 8))
                                .HoverBackground(new Color(202, 138, 4))
                                .PressedBackground(new Color(161, 98, 7))
                                .OnTapped(() => clickCount.Value++)
                        )
                ),

                Helper.CreateExampleSection("Sizes",
                    new HStack()
                        .Spacing(14)
                        .Alignment(Alignment.Center)
                        .Children(
                            new ButtonFloat(Icons.Add)
                                .FloatSize(ButtonFloatSize.Small),
                            new ButtonFloat(Icons.Add)
                                .FloatSize(ButtonFloatSize.Normal),
                            new ButtonFloat(Icons.Add)
                                .FloatSize(ButtonFloatSize.Large)
                        )
                ),

                Helper.CreateExampleSection("Floating Over Content",
                    CreateFloatingPreview(clickCount)
                ),

                Helper.CreateExampleSection("Code Example",
                    new Frame()
                        .Background(new Color(30, 33, 42))
                        .BorderRadius(8)
                        .Padding(new Thickness(12))
                        .Content(
                            new VStack()
                                .Spacing(4)
                                .Children(
                                    new Label("new Grid()")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddChild(content, 0, 0)")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    .AddChild(new ButtonFloat(Icons.Add).Dock(ButtonFloatPlacement.BottomRight), 0, 0)")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254))
                                )
                        )
                )
            );
    }

    private VisualElement CreateFloatingPreview(Signal<int> clickCount)
    {
        var content = new Frame()
            .Background(new Color(38, 42, 54))
            .BorderRadius(10)
            .Padding(new Thickness(18))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(10)
                    .Children(
                        new Label("Project Tasks")
                            .FontSize(18)
                            .Foreground(Color.White),
                        new Label()
                            .Text(clickCount.Map(count => $"Floating button taps: {count}"))
                            .FontSize(14)
                            .Foreground(new Color(147, 197, 253)),
                        CreateTaskRow("Design review", "Today"),
                        CreateTaskRow("API polish", "Tomorrow"),
                        CreateTaskRow("Release notes", "Friday")
                    )
            );

        return new Grid()
            .Rows(GridLength.Pixels(260))
            .Columns(GridLength.Star)
            .AddChild(content, 0, 0)
            .AddChild(
                new ButtonFloat(Icons.Add)
                    .Dock(ButtonFloatPlacement.BottomRight, 18)
                    .OnTapped(() => clickCount.Value++),
                0,
                0);
    }

    private VisualElement CreateTaskRow(string title, string due)
    {
        return new Frame()
            .Background(new Color(48, 54, 68))
            .BorderRadius(8)
            .Padding(new Thickness(12, 10))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .Spacing(10)
                    .Children(
                        new Label(title)
                            .FontSize(14)
                            .Foreground(new Color(229, 231, 235)),
                        new Label(due)
                            .FontSize(12)
                            .Foreground(new Color(156, 163, 175))
                    )
            );
    }
}
