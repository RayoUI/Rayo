using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class HStackPage : Component
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("HStack", "Arranges children in a single horizontal line"),

                Helper.CreateExampleSection("Basic Horizontal Layout",
                    new HStack()
                        .Spacing(10)
                        .Children(
                            CreateSquare(50, new Color(59, 130, 246)),
                            CreateSquare(50, new Color(34, 197, 94)),
                            CreateSquare(50, new Color(234, 179, 8))
                        )
                ),

                Helper.CreateExampleSection("Alignment - Vertical Center",
                    new Frame()
                        .Height(100)
                        .Background(GalleryPalette.Surface)
                        .Content(
                            new HStack()
                                .Spacing(15)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Top)
                                .Children(
                                    CreateSquare(30, new Color(239, 68, 68)),
                                    CreateSquare(60, new Color(168, 85, 247)),
                                    CreateSquare(40, new Color(236, 72, 153))
                                )
                        )
                ),

                Helper.CreateExampleSection("Alignment - Horizontal End",
                    new Frame()
                        .Background(GalleryPalette.Surface)
                        .Content(
                            new HStack()
                                .Spacing(10)
                                .HorizontalAlignment(HorizontalAlignment.Right)
                                .Children(
                                    CreateSquare(40, new Color(59, 130, 246)),
                                    CreateSquare(40, new Color(59, 130, 246)),
                                    CreateSquare(40, new Color(59, 130, 246))
                                )
                        )
                ),

                Helper.CreateExampleSection("Spacing and Padding",
                    new PaletteFrame(colors => colors.SurfaceHover)
                        .Padding(new Thickness(20))
                        .Content(
                            new HStack()
                                .Spacing(30)
                                .Children(
                                    CreatePaletteSquare(40, colors => colors.Primary),
                                    CreatePaletteSquare(40, colors => colors.Success),
                                    CreatePaletteSquare(40, colors => colors.Warning)
                                )
                        )
                ),

                Helper.CreateExampleSection("Nested Components",
                    new HStack()
                        .Spacing(10)
                        .Children(
                            new Button().Text("Login").Padding(new Thickness(12, 6, 12, 6)),
                            new Button().Text("Register").Padding(new Thickness(12, 6, 12, 6)),
                            new Icon(Icons.Settings).Size(24).Color(GalleryPalette.OnSurface)
                        )
                )
            );
    }

    private VisualElement CreateSquare(float size, Color color)
    {
        return new Frame()
            .Size(new Size(size, size))
            .Background(color)
            .BorderRadius(8);
    }

    private VisualElement CreatePaletteSquare(float size, Func<Rayo.Styling.ColorPalette, Color> color)
    {
        return new PaletteFrame(color)
            .Size(new Size(size, size))
            .BorderRadius(8);
    }
}
