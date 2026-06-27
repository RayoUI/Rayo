using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class LStackPage : Component
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("LStack", "Versatile stack layout with flex-like distribution"),

                Helper.CreateExampleSection("Vertical Distribution (JustifyContent)",
                    new Frame()
                        .Height(250)
                        .Background(GalleryPalette.Surface)
                        .Content(
                            new HStack()
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Children(
                                    CreateJustifyColumn("Start", JustifyContent.Start),
                                    CreateJustifyColumn("Center", JustifyContent.Center),
                                    CreateJustifyColumn("End", JustifyContent.End),
                                    CreateJustifyColumn("Between", JustifyContent.SpaceBetween),
                                    CreateJustifyColumn("Around", JustifyContent.SpaceAround),
                                    CreateJustifyColumn("Evenly", JustifyContent.SpaceEvenly)
                                )
                        )
                ),

                Helper.CreateExampleSection("Horizontal Alignment (Cross-Axis)",
                    new Frame()
                        .Height(200)
                        .Background(GalleryPalette.Surface)
                        .Content(
                            new VStack()
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Children(
                                    CreateAlignmentRow("Start", Alignment.Start),
                                    CreateAlignmentRow("Center", Alignment.Center),
                                    CreateAlignmentRow("End", Alignment.End),
                                    CreateAlignmentRow("Stretch", Alignment.Stretch)
                                )
                        )
                ),

                Helper.CreateExampleSection("Mixed Content with Spacing",
                    new LStack()
                        .Orientation(Rayo.Layout.Orientation.Horizontal)
                        .Spacing(15)
                        .Alignment(Alignment.Center)
                        .Children(
                            new Icon(Icons.Info).Size(32).Color(new Color(59, 130, 246)),
                            new VStack()
                                .Children(
                                    new Label("Information Title").FontSize(16).Foreground(GalleryPalette.OnSurface),
                                    new Label("This is a description inside a LStack.").FontSize(13).Foreground(GalleryPalette.Muted)
                                ),
                            new Button().Text("Dismiss").Margin(new Thickness(20, 0, 0, 0))
                        )
                )
            );
    }

    private VisualElement CreateJustifyColumn(string title, JustifyContent justify)
    {
        return new VStack()
            .Children(
                new Label(title).FontSize(10).HorizontalAlignment(HorizontalAlignment.Center).Foreground(GalleryPalette.Muted),
                new LStack()
                    .Orientation(Rayo.Layout.Orientation.Vertical)
                    .JustifyContent(justify)
                    .Background(GalleryPalette.SurfaceHover)
                    .Margin(new Thickness(5))
                    .Children(
                        CreateSmallSquare(new Color(59, 130, 246)),
                        CreateSmallSquare(new Color(34, 197, 94)),
                        CreateSmallSquare(new Color(234, 179, 8))
                    )
            );
    }

    private VisualElement CreateAlignmentRow(string title, Alignment alignment)
    {
        return new HStack()
            .Height(40)
            .Children(
                new Label(title).Width(60).VerticalAlignment(VerticalAlignment.Center).Foreground(GalleryPalette.Muted),
                new LStack()
                    .Orientation(Rayo.Layout.Orientation.Vertical)
                    .Alignment(alignment)
                    .Background(GalleryPalette.SurfaceHover)
                    .Children(
                        new Frame().Height(10).Width(30).Background(new Color(168, 85, 247)).BorderRadius(2)
                    )
            );
    }

    private VisualElement CreateSmallSquare(Color color)
    {
        return new Frame()
            .Size(new Size(20, 20))
            .Background(color)
            .BorderRadius(4);
    }
}
