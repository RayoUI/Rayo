using Rayo;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Controls;

namespace Examples;

/// <summary>
/// Example showing tab positioning in different directions.
/// </summary>
internal class TabControlPositionExample : IUIBuilder
{
    public VisualElement Build() =>
        new VStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                new Frame()
                    .Background(new Color(20, 20, 20))
                    .Padding(new Thickness(10))
                    .Margin(new Thickness(5))
                    .Content(
                        new VStack()
                            .Spacing(5)
                            .Children(
                        new Label().Text("TabPosition.Top (default)").Margin(new Thickness(0, 0, 0, 5)),
                        new TabControl()
                            .Margin(new Thickness(0))
                            .Position(TabPosition.Top)
                            .AddTab("Tab A1", new Label().Text("Top - Content A1").Padding(10))
                            .AddTab("Tab A2", new Label().Text("Top - Content A2").Padding(10))
                            .AddTab("Tab A3", new Label().Text("Top - Content A3").Padding(10))
                            .Height(150)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            )
                    )
                    .Height(200)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),

                new Frame()
                    .Background(new Color(20, 20, 20))
                    .Padding(new Thickness(10))
                    .Margin(new Thickness(5))
                    .Content(
                        new VStack()
                            .Spacing(5)
                            .Children(
                        new Label().Text("TabPosition.Bottom").Margin(new Thickness(0, 0, 0, 5)),
                        new TabControl()
                            .Margin(new Thickness(0))
                            .Position(TabPosition.Bottom)
                            .AddTab("Tab B1", new Label().Text("Bottom - Content B1").Padding(10))
                            .AddTab("Tab B2", new Label().Text("Bottom - Content B2").Padding(10))
                            .AddTab("Tab B3", new Label().Text("Bottom - Content B3").Padding(10))
                            .Height(150)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            )
                    )
                    .Height(200)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),

                new HStack()
                    .Spacing(5)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Height(250)
                    .Children(
                        new Frame()
                            .Background(new Color(20, 20, 20))
                            .Padding(new Thickness(10))
                            .Margin(new Thickness(5))
                            .Content(
                                new VStack()
                                    .Spacing(5)
                                    .Children(
                                new Label().Text("TabPosition.Left").Margin(new Thickness(0, 0, 0, 5)),
                                new TabControl()
                                    .Margin(new Thickness(0))
                                    .Position(TabPosition.Left)
                                    .AddTab("Tab C1", new Label().Text("Left - Content C1").Padding(10))
                                    .AddTab("Tab C2", new Label().Text("Left - Content C2").Padding(10))
                                    .AddTab("Tab C3", new Label().Text("Left - Content C3").Padding(10))
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .VerticalAlignment(VerticalAlignment.Stretch)
                                    )
                            )
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch),

                        new Frame()
                            .Background(new Color(20, 20, 20))
                            .Padding(new Thickness(10))
                            .Margin(new Thickness(5))
                            .Content(
                                new VStack()
                                    .Spacing(5)
                                    .Children(
                                new Label().Text("TabPosition.Right").Margin(new Thickness(0, 0, 0, 5)),
                                new TabControl()
                                    .Margin(new Thickness(0))
                                    .Position(TabPosition.Right)
                                    .AddTab("Tab D1", new Label().Text("Right - Content D1").Padding(10))
                                    .AddTab("Tab D2", new Label().Text("Right - Content D2").Padding(10))
                                    .AddTab("Tab D3", new Label().Text("Right - Content D3").Padding(10))
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .VerticalAlignment(VerticalAlignment.Stretch)
                                    )
                            )
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch)
                    )
            );
}
