using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class CarouselPage : Component
{
    public override VisualElement Build()
    {
        var selectedSlide = new Signal<string>("Selected slide: 1");

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Carousel", "Display one slide at a time with navigation and indicators"),

                Helper.CreateExampleSection("Basic Carousel",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Carousel()
                                .Size(new Size(520, 280))
                                .TransitionMode(CarouselTransitionMode.Slide)
                                .TransitionDuration(0.32f)
                                .AddSlides(
                                    CreateFeatureSlide("Fast composition", "Build slides from any Rayo visual element.", colors => colors.Primary),
                                    CreateFeatureSlide("Clear navigation", "Previous and next actions keep content easy to browse.", colors => colors.Success),
                                    CreateFeatureSlide("Indicators", "Dots show progress and jump directly to each slide.", colors => colors.Warning))
                                .OnSelectedIndexChanged(index => selectedSlide.Value = $"Selected slide: {index + 1}"),
                            new Label()
                                .Text(selectedSlide)
                                .FontSize(13)
                        )
                ),

                Helper.CreateExampleSection("Image Carousel",
                    new Carousel()
                        .Size(new Size(520, 300))
                        .NavigationPlacement(CarouselNavigationPlacement.Overlay)
                        .OverlayNavigationButtonSize(58)
                        .OverlayNavigationIconSize(32)
                        .OverlayNavigationInset(16)
                        .TransitionDuration(0.36f)
                        .NavigationButtonBackground(new Color(0, 0, 0, 0.38f))
                        .NavigationButtonHoverBackground(new Color(0, 0, 0, 0.58f))
                        .AddSlides(
                            CreateImageSlide("Robot", "Assets/Images/robot.png", "Uniform image content inside a slide."),
                            CreateImageSlide("Super Robot", "Assets/Images/super_robot.png", "Slides can mix image and text layouts."),
                            CreateTextSlide("Reusable Content", "Each item is a normal VisualElement, so complex layouts work too."))
                ),

                Helper.CreateExampleSection("No Loop",
                    new Carousel()
                        .Size(new Size(420, 240))
                        .Loop(false)
                        .NavigationButtonBackground(new Color(67, 56, 202))
                        .NavigationButtonHoverBackground(new Color(79, 70, 229))
                        .IndicatorSelectedColor(new Color(129, 140, 248))
                        .AddSlides(
                            CreateStepSlide("Step 1", "Start", colors => colors.Primary),
                            CreateStepSlide("Step 2", "Review", colors => colors.Info),
                            CreateStepSlide("Step 3", "Finish", colors => colors.Success))
                )
            );
    }

    private static VisualElement CreateFeatureSlide(
        string title,
        string body,
        Func<Rayo.Styling.ColorScheme, Color> accent)
    {
        return new PaletteFrame(colors => colors.Surface)
            .BorderRadius(8)
            .Padding(new Thickness(28))
            .Content(
                new VStack()
                    .Spacing(14)
                    .Alignment(Alignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Children(
                        new PaletteFrame(accent)
                            .Size(new Size(72, 8))
                            .BorderRadius(4)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new PaletteLabel(title, colors => colors.OnSurface)
                            .FontSize(24)
                            .TextHorizontalAlignment(HorizontalAlignment.Center),
                        new PaletteLabel(body, colors => colors.OnDisabled)
                            .FontSize(14)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }

    private static VisualElement CreateImageSlide(string title, string source, string caption)
    {
        return new HStack()
            .Spacing(22)
            .Padding(new Thickness(28))
            .Alignment(Alignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                new PaletteFrame(colors => colors.SurfaceHover)
                    .Size(new Size(150, 150))
                    .BorderRadius(8)
                    .Content(
                        new Image()
                            .Source(source)
                            .Stretch(StretchMode.Uniform)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch)
                    ),
                new VStack()
                    .Spacing(10)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Children(
                        new PaletteLabel(title, colors => colors.OnSurface)
                            .FontSize(22),
                        new PaletteLabel(caption, colors => colors.OnDisabled)
                            .FontSize(14)
                    )
            );
    }

    private static VisualElement CreateTextSlide(string title, string body)
    {
        return new PaletteFrame(colors => colors.Surface)
            .BorderRadius(8)
            .Padding(new Thickness(30))
            .Content(
                new VStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new PaletteLabel(title, colors => colors.OnSurface)
                            .FontSize(24)
                            .TextHorizontalAlignment(HorizontalAlignment.Center),
                        new PaletteLabel(body, colors => colors.OnDisabled)
                            .FontSize(14)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }

    private static VisualElement CreateStepSlide(
        string step,
        string title,
        Func<Rayo.Styling.ColorScheme, Color> accent)
    {
        return new PaletteFrame(colors => colors.Surface)
            .BorderRadius(8)
            .Padding(new Thickness(24))
            .Content(
                new VStack()
                    .Spacing(10)
                    .Alignment(Alignment.Center)
                    .Children(
                        new PaletteLabel(step, accent)
                            .FontSize(13),
                        new PaletteLabel(title, colors => colors.OnSurface)
                            .FontSize(26)
                    )
            );
    }
}
