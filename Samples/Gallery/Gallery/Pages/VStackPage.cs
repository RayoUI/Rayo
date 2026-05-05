using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class VStackPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("VStack", "Arranges children in a single vertical line"),

                Helper.CreateExampleSection("Basic Vertical Layout",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateRect(150, 40, new Color(59, 130, 246)),
                            CreateRect(150, 40, new Color(34, 197, 94)),
                            CreateRect(150, 40, new Color(234, 179, 8))
                        )
                ),

                Helper.CreateExampleSection("Alignment - Horizontal Center",
                    new Frame()
                        .Width(300)
                        .Background(new Color(30, 30, 35))
                        .Content(
                            new VStack()
                                .Spacing(15)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Children(
                                    CreateRect(100, 30, new Color(239, 68, 68)),
                                    CreateRect(180, 30, new Color(168, 85, 247)),
                                    CreateRect(140, 30, new Color(236, 72, 153))
                                )
                        )
                ),

                Helper.CreateExampleSection("Alignment - Vertical End",
                    new Frame()
                        .Height(200)
                        .Background(new Color(30, 30, 35))
                        .Content(
                            new VStack()
                                .Spacing(10)
                                .VerticalAlignment(VerticalAlignment.Bottom)
                                .Children(
                                    CreateRect(100, 30, new Color(59, 130, 246)),
                                    CreateRect(100, 30, new Color(59, 130, 246))
                                )
                        )
                ),

                Helper.CreateExampleSection("Form Example",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new Label("Email Address").Foreground(Color.White),
                            new Entry().Placeholder("Enter email...").Width(250),
                            new Label("Password").Foreground(Color.White),
                            new Entry().Placeholder("Enter password...").Width(250),
                            new Button().Text("Login").Width(100).Background(new Color(59, 130, 246))
                        )
                )
            );
    }

    private VisualElement CreateRect(float width, float height, Color color)
    {
        return new Frame()
            .Size(new Size(width, height))
            .Background(color)
            .BorderRadius(6);
    }
}
