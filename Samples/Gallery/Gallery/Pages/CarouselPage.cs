using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class CarouselPage : UserControl
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
                                    CreateFeatureSlide("Fast composition", "Build slides from any Rayo visual element.", new Color(59, 130, 246)),
                                    CreateFeatureSlide("Clear navigation", "Previous and next actions keep content easy to browse.", new Color(16, 185, 129)),
                                    CreateFeatureSlide("Indicators", "Dots show progress and jump directly to each slide.", new Color(245, 158, 11)))
                                .OnSelectedIndexChanged(index => selectedSlide.Value = $"Selected slide: {index + 1}"),
                            new Label()
                                .Text(selectedSlide)
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary)
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
                        .SlideBackground(new Color(24, 26, 32))
                        .BorderColor(new Color(76, 82, 96))
                        .IndicatorSelectedColor(new Color(34, 197, 94))
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
                            CreateStepSlide("Step 1", "Start", new Color(99, 102, 241)),
                            CreateStepSlide("Step 2", "Review", new Color(14, 165, 233)),
                            CreateStepSlide("Step 3", "Finish", new Color(34, 197, 94)))
                )
            );
    }

    private static VisualElement CreateFeatureSlide(string title, string body, Color accent)
    {
        return new Frame()
            .Background(new Color(31, 35, 44))
            .BorderRadius(8)
            .Padding(new Thickness(28))
            .Content(
                new VStack()
                    .Spacing(14)
                    .Alignment(Alignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Children(
                        new Frame()
                            .Size(new Size(72, 8))
                            .Background(accent)
                            .BorderRadius(4)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new Label(title)
                            .FontSize(24)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .Foreground(Color.White),
                        new Label(body)
                            .FontSize(14)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .Foreground(new Color(190, 198, 214))
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
                new Frame()
                    .Size(new Size(150, 150))
                    .Background(new Color(41, 45, 56))
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
                        new Label(title)
                            .FontSize(22)
                            .Foreground(Color.White),
                        new Label(caption)
                            .FontSize(14)
                            .Foreground(new Color(176, 184, 200))
                    )
            );
    }

    private static VisualElement CreateTextSlide(string title, string body)
    {
        return new Frame()
            .Background(new Color(39, 45, 59))
            .BorderRadius(8)
            .Padding(new Thickness(30))
            .Content(
                new VStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label(title)
                            .FontSize(24)
                            .Foreground(Color.White)
                            .TextHorizontalAlignment(HorizontalAlignment.Center),
                        new Label(body)
                            .FontSize(14)
                            .Foreground(new Color(198, 207, 222))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }

    private static VisualElement CreateStepSlide(string step, string title, Color accent)
    {
        return new Frame()
            .Background(new Color(31, 35, 44))
            .BorderRadius(8)
            .Padding(new Thickness(24))
            .Content(
                new VStack()
                    .Spacing(10)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label(step)
                            .FontSize(13)
                            .Foreground(accent),
                        new Label(title)
                            .FontSize(26)
                            .Foreground(Color.White)
                    )
            );
    }
}
