using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace CrossPlatformApp;

/// <summary>
/// Main View for the cross-platform demo application.
/// This UI code is shared across all platforms.
/// </summary>
public class MainView : ViewBase<MainViewModel>
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(30))
            .Alignment(Alignment.Center)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                BuildHeader(),
                BuildPlatformInfo(),
                BuildCounter(),
                BuildFooter()
            );
    }

    private VisualElement BuildHeader()
    {
        var greetingText = new Computed<string>(() => ViewModel?.Greeting.Value ?? "Hello!");

        return new VStack()
            .Spacing(10)
            .Alignment(Alignment.Center)
            .Children(
                new Label()
                    .Text("RayoII")
                    .FontSize(28)
                    .Foreground(Color.White)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center),
                new Loading()
                    .Type(SpinnerType.Circle)
                    .Size(60)
                    .Color(new Color(59, 130, 246))
                    .Text("Loading")
                    .Margin(new Thickness(0, 0, 0, 15))
                    .HorizontalAlignment(HorizontalAlignment.Center),
                new Label()
                    .Text(greetingText)
                    .FontSize(18)
                    .Foreground(new Color(180, 180, 180))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
            );
    }

    private VisualElement BuildPlatformInfo()
    {
        var platformText = new Computed<string>(() =>
            $"Running on: {ViewModel?.PlatformName.Value ?? "Unknown"}");

        var platformTypeText = new Computed<string>(() =>
        {
            if (ViewModel?.IsDesktop.Value ?? false)
                return "Platform Type: Desktop";
            if (ViewModel?.IsMobile.Value ?? false)
                return "Platform Type: Mobile";
            return "Platform Type: Other";
        });

        return new Frame()
            .Padding(new Thickness(20))
            .Background(new Color(45, 45, 48))
            .BorderRadius(10)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Content(
                new VStack()
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label()
                            .Text(platformText)
                            .FontSize(16)
                            .Foreground(new Color(100, 200, 255))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new Label()
                            .Text(platformTypeText)
                            .FontSize(14)
                            .Foreground(new Color(150, 150, 150))
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                    )
            );
    }

    private VisualElement BuildCounter()
    {
        var counterText = new Computed<string>(() =>
            $"Counter: {ViewModel?.Counter.Value ?? 0}");

        return new VStack()
            .Spacing(15)
            .Alignment(Alignment.Center)
            .Children(
                new Label()
                    .Text(counterText)
                    .FontSize(32)
                    .Foreground(Color.White)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center),
                new HStack()
                    .Spacing(15)
                    .Height(80)
                    .JustifyContent(JustifyContent.Center)
                    .Children(
                        // Decrement button
                        new Button()
                            .Text("-")
                            .Width(60)
                            .Height(60)
                            .OnTapped(() => ViewModel.DecrementCounter()),
                        
                        // Reset button
                        new Button()
                            .Text("Reset")
                            .Width(90)
                            .Height(60)
                            .OnTapped(() => ViewModel.ResetCounter()),
                        
                        // Increment button
                        new Button()
                            .Text("+")
                            .Width(60)
                            .Height(60)
                            .OnTapped(() => ViewModel.IncrementCounter())
                    )
            );
    }

    private VisualElement BuildFooter()
    {
        return new VStack()
            .Spacing(5)
            .Alignment(Alignment.Center)
            .VerticalAlignment(VerticalAlignment.Bottom)
            .Children(
                new Label()
                    .Text("Rayo Framework")
                    .FontSize(12)
                    .Foreground(new Color(100, 100, 100))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center),
                new Label()
                    .Text("Shared UI across all platforms")
                    .FontSize(10)
                    .Foreground(new Color(80, 80, 80))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
            );
    }
}
