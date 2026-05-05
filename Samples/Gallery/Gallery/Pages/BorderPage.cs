using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Shadow = Rayo.Controls.Shadow;

namespace Gallery.Pages;

public class BorderPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Border", "Container with configurable border and shadow"),

                Helper.CreateExampleSection("Basic Border",
                    new Border()
                        .BorderBrush(ColorDefault.Primary)
                        .BorderThickness(new Thickness(2))
                        .Padding(new Thickness(20))
                        .Content(
                            new Label()
                                .Text("Content inside a border")
                                .Foreground(Color.White)
                        )
                ),

                Helper.CreateExampleSection("Rounded Border",
                    new Border()
                        .BorderBrush(new Color(100, 100, 200))
                        .BorderThickness(new Thickness(2))
                        .CornerRadius(new CornerRadius(12))
                        .Padding(new Thickness(20))
                        .Background(new Color(40, 40, 60))
                        .Content(
                            new Label()
                                .Text("Rounded corners!")
                                .Foreground(Color.White)
                        )
                ),

                Helper.CreateExampleSection("With Shadow",
                    new Border()
                        .BorderBrush(new Color(80, 80, 80))
                        .BorderThickness(new Thickness(1))
                        .CornerRadius(new CornerRadius(8))
                        .Padding(new Thickness(20))
                        .Background(new Color(50, 50, 55))
                        .Shadow(new Shadow(new Color(0, 0, 0, 100), 4, 4, 12))
                        .Content(
                            new VStack()
                                .Spacing(10)
                                .Children(
                                    new Label()
                                        .Text("Card with shadow")
                                        .FontSize(16)
                                        .Foreground(Color.White),
                                    new Label()
                                        .Text("This border has a drop shadow effect")
                                        .Foreground(ColorDefault.Secondary)
                                )
                        )
                ),

                Helper.CreateExampleSection("Different Border Widths",
                    new HStack()
                        .Spacing(20)
                        .Children(
                            new Border()
                                .BorderBrush(ColorDefault.Info)
                                .BorderThickness(new Thickness(1))
                                .Padding(new Thickness(15))
                                .Content(new Label().Text("1px").Foreground(Color.White)),

                            new Border()
                                .BorderBrush(ColorDefault.Success)
                                .BorderThickness(new Thickness(2))
                                .Padding(new Thickness(15))
                                .Content(new Label().Text("2px").Foreground(Color.White)),

                            new Border()
                                .BorderBrush(ColorDefault.Warning)
                                .BorderThickness(new Thickness(4))
                                .Padding(new Thickness(15))
                                .Content(new Label().Text("4px").Foreground(Color.White))
                        )
                ),

                Helper.CreateExampleSection("Colored Shadow",
                    new Border()
                        .BorderBrush(new Color(59, 130, 246))
                        .BorderThickness(new Thickness(2))
                        .CornerRadius(new CornerRadius(12))
                        .Padding(new Thickness(25))
                        .Background(new Color(30, 40, 60))
                        .Shadow(new Shadow(new Color(59, 130, 246, 150), 0, 0, 20))
                        .Content(
                            new Label()
                                .Text("Blue glow effect")
                                .Foreground(Color.White)
                        )
                )
            );
    }
}
