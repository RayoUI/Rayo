using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Shadow = Rayo.Controls.Shadow;

namespace Gallery.Pages;

public class BorderPage : Component
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Border", "Container with configurable border and shadow"),

                Helper.CreateExampleSection("Basic Border",
                    new PaletteBorder(colors => colors.Primary)
                        .BorderThickness(new Thickness(2))
                        .Padding(new Thickness(20))
                        .Content(
                            new PaletteLabel("Content inside a border", colors => colors.OnSurface)
                        )
                ),

                Helper.CreateExampleSection("Rounded Border",
                    new PaletteBorder(colors => colors.Secondary, colors => colors.SurfaceHover)
                        .BorderThickness(new Thickness(2))
                        .CornerRadius(new CornerRadius(12))
                        .Padding(new Thickness(20))
                        .Content(
                            new PaletteLabel("Rounded corners!", colors => colors.OnSurface)
                        )
                ),

                Helper.CreateExampleSection("With Shadow",
                    new PaletteBorder(colors => colors.Border, colors => colors.Surface)
                        .BorderThickness(new Thickness(1))
                        .CornerRadius(new CornerRadius(8))
                        .Padding(new Thickness(20))
                        .Shadow(new Shadow(new Color(0, 0, 0, 100), 4, 4, 12))
                        .Content(
                            new VStack()
                                .Spacing(10)
                                .Children(
                                    new PaletteLabel("Card with shadow", colors => colors.OnSurface)
                                        .FontSize(16),
                                    new PaletteLabel("This border has a drop shadow effect", colors => colors.OnDisabled)
                                )
                        )
                ),

                Helper.CreateExampleSection("Different Border Widths",
                    new HStack()
                        .Spacing(20)
                        .Children(
                            new PaletteBorder(colors => colors.Info)
                                .BorderThickness(new Thickness(1))
                                .Padding(new Thickness(15))
                                .Content(new PaletteLabel("1px", colors => colors.OnSurface)),

                            new PaletteBorder(colors => colors.Success)
                                .BorderThickness(new Thickness(2))
                                .Padding(new Thickness(15))
                                .Content(new PaletteLabel("2px", colors => colors.OnSurface)),

                            new PaletteBorder(colors => colors.Warning)
                                .BorderThickness(new Thickness(4))
                                .Padding(new Thickness(15))
                                .Content(new PaletteLabel("4px", colors => colors.OnSurface))
                        )
                ),

                Helper.CreateExampleSection("Colored Shadow",
                    new PaletteBorder(colors => colors.Info, colors => colors.Surface)
                        .BorderThickness(new Thickness(2))
                        .CornerRadius(new CornerRadius(12))
                        .Padding(new Thickness(25))
                        .Shadow(new Shadow(new Color(59, 130, 246, 150), 0, 0, 20))
                        .Content(
                            new PaletteLabel("Blue glow effect", colors => colors.OnSurface)
                        )
                )
            );
    }
}
