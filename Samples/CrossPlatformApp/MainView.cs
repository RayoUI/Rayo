using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace CrossPlatformApp;

/// <summary>
/// Shared UI that demonstrates <c>UIApplication.Current.Window</c> controls
/// for desktop, Android, and iOS from the same codebase.
/// </summary>
public class MainView : ViewBase<MainViewModel>
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                // Pads content below the status bar / notch on mobile (hidden on desktop).
                new Frame()
                    .Height(ViewModel.SafeAreaTop)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .IsVisible(ViewModel.IsMobile)
                    .Background(new Color(15, 23, 42))
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                new ScrollView()
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Content(
                        new VStack()
                            .Spacing(18)
                            .Padding(new Thickness(24))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Children(
                                BuildHeader(),
                                BuildStatusCard(),
                                BuildPlatformPanel(),
                                BuildFooter()
                            )
                    )
            );
    }

    private VisualElement BuildHeader()
    {
        var platformText = new Computed<string>(() =>
            $"Running on {ViewModel?.PlatformName.Value ?? "Unknown"}");

        return new VStack()
            .Spacing(6)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                new Label()
                    .Text("Window API Demo")
                    .FontSize(26)
                    .Foreground(Color.White),
                new Label()
                    .Text(platformText)
                    .FontSize(14)
                    .Foreground(new Color(148, 163, 184)),
                new Label()
                    .Text("Mutate the native window from shared components via UIApplication.Current.Window")
                    .FontSize(13)
                    .LineHeight(1.3f)
                    .Foreground(new Color(100, 116, 139))
            );
    }

    private VisualElement BuildStatusCard()
    {
        var status = new Computed<string>(() => ViewModel?.StatusMessage.Value ?? "");
        var size = new Computed<string>(() => $"Size: {ViewModel?.WindowSizeText.Value ?? "—"}");
        var topSafe = new Computed<string>(() => ViewModel?.SafeAreaText.Value ?? "SafeArea.Top: 0");

        return Card(
            new VStack()
                .Spacing(8)
                .Children(
                    new Label()
                        .Text("Live status")
                        .FontSize(12)
                        .Foreground(new Color(96, 165, 250)),
                    new Label()
                        .Text(status)
                        .FontSize(15)
                        .Foreground(Color.White),
                    new Label()
                        .Text(size)
                        .FontSize(13)
                        .Foreground(new Color(148, 163, 184)),
                    new Label()
                        .Text(topSafe)
                        .FontSize(13)
                        .Foreground(new Color(148, 163, 184))
                )
        );
    }

    private VisualElement BuildPlatformPanel()
    {
        if (ViewModel?.IsDesktop.Value == true)
        {
            return BuildDesktopPanel();
        }

        if (ViewModel?.IsAndroid.Value == true)
        {
            return BuildAndroidPanel();
        }

        if (ViewModel?.IsiOS.Value == true)
        {
            return BuildiOSPanel();
        }

        return Card(
            new Label()
                .Text("No platform-specific window controls for this runtime.")
                .Foreground(new Color(148, 163, 184))
        );
    }

    private VisualElement BuildDesktopPanel()
    {
        var title = new Computed<string>(() => $"Title: {ViewModel?.WindowTitle.Value}");
        var maximized = new Computed<string>(() =>
            ViewModel?.IsMaximized.Value == true ? "State: Maximized" : "State: Normal");
        var topmost = new Computed<string>(() =>
            $"Topmost: {(ViewModel?.IsTopmost.Value == true ? "on" : "off")}");
        var canResize = new Computed<string>(() =>
            $"CanResize: {(ViewModel?.CanResize.Value == true ? "on" : "off")}");

        return Card(
            new VStack()
                .Spacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Children(
                    SectionTitle("Desktop — Window.*"),
                    InfoLabel(title),
                    InfoLabel(maximized),
                    InfoLabel(topmost),
                    InfoLabel(canResize),
                    ButtonRow(
                        ActionButton("Set title", () => ViewModel!.SetTitle($"Rayo Demo · {DateTime.Now:HH:mm:ss}")),
                        ActionButton("Maximize / Restore", () => ViewModel!.ToggleMaximize())
                    ),
                    ButtonRow(
                        ActionButton("Toggle topmost", () => ViewModel!.ToggleTopmost()),
                        ActionButton("Toggle resize", () => ViewModel!.ToggleCanResize())
                    ),
                    ButtonRow(
                        ActionButton("Center", () => ViewModel!.CenterWindow()),
                        ActionButton("Compact size", () => ViewModel!.ApplyCompactSize())
                    ),
                    ActionButton("Wide size (900×640)", () => ViewModel!.ApplyWideSize())
                )
        );
    }

    private VisualElement BuildAndroidPanel()
    {
        var keepOn = new Computed<string>(() =>
            $"KeepScreenOn: {(ViewModel?.KeepScreenOn.Value == true ? "on" : "off")}");
        var immersive = new Computed<string>(() =>
            $"ImmersiveMode: {(ViewModel?.ImmersiveMode.Value == true ? "on" : "off")}");
        var hideStatus = new Computed<string>(() =>
            $"HideStatusBar: {(ViewModel?.HideStatusBar.Value == true ? "on" : "off")}");
        var orientation = new Computed<string>(() =>
            $"Orientation: {ViewModel?.OrientationLabel.Value}");
        var topSafe = new Computed<string>(() =>
            $"Window.SafeArea.Top = {ViewModel?.SafeAreaTop.Value:0.#} (logical px)");

        return Card(
            new VStack()
                .Spacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Children(
                    SectionTitle("Android — Window.Android.* + SafeArea"),
                    InfoLabel(topSafe),
                    InfoLabel(keepOn),
                    InfoLabel(immersive),
                    InfoLabel(hideStatus),
                    InfoLabel(orientation),
                    ButtonRow(
                        ActionButton("Keep screen on", () => ViewModel!.ToggleKeepScreenOn()),
                        ActionButton("Immersive", () => ViewModel!.ToggleImmersiveMode())
                    ),
                    ButtonRow(
                        ActionButton("Hide status bar", () => ViewModel!.ToggleHideStatusBar()),
                        ActionButton("Cycle orientation", () => ViewModel!.CycleAndroidOrientation())
                    ),
                    ButtonRow(
                        ActionButton("Status bar blue", () => ViewModel!.ApplyAndroidStatusBarColor(0xFF2563EB)),
                        ActionButton("Status bar slate", () => ViewModel!.ApplyAndroidStatusBarColor(0xFF1E293B))
                    )
                )
        );
    }

    private VisualElement BuildiOSPanel()
    {
        var safeArea = new Computed<string>(() =>
            $"UseSafeAreaInsets: {(ViewModel?.UseSafeAreaInsets.Value == true ? "on" : "off")}");
        var topSafe = new Computed<string>(() =>
            $"Window.SafeArea.Top = {ViewModel?.SafeAreaTop.Value:0.#} (logical px)");
        var home = new Computed<string>(() =>
            $"HideHomeIndicator: {(ViewModel?.HideHomeIndicator.Value == true ? "on" : "off")}");
        var style = new Computed<string>(() =>
            $"StatusBarStyle: {ViewModel?.StatusBarStyleLabel.Value}");
        var orientation = new Computed<string>(() =>
            $"Orientation: {ViewModel?.OrientationLabel.Value}");

        return Card(
            new VStack()
                .Spacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Children(
                    SectionTitle("iOS — Window.iOS.* + SafeArea"),
                    InfoLabel(topSafe),
                    InfoLabel(safeArea),
                    InfoLabel(home),
                    InfoLabel(style),
                    InfoLabel(orientation),
                    ButtonRow(
                        ActionButton("Safe area", () => ViewModel!.ToggleSafeAreaInsets()),
                        ActionButton("Home indicator", () => ViewModel!.ToggleHideHomeIndicator())
                    ),
                    ButtonRow(
                        ActionButton("Cycle status bar", () => ViewModel!.CycleStatusBarStyle()),
                        ActionButton("Cycle orientation", () => ViewModel!.CycleiOSOrientation())
                    )
                )
        );
    }

    private VisualElement BuildFooter()
    {
        return new VStack()
            .Spacing(4)
            .Margin(new Thickness(0, 8, 0, 0))
            .Children(
                new Label()
                    .Text("Startup config lives in Desktop Program.cs / Android MainActivity.")
                    .FontSize(11)
                    .Foreground(new Color(71, 85, 105)),
                new Label()
                    .Text("Mobile top inset: Window.SafeArea.Top (0 on desktop).")
                    .FontSize(11)
                    .Foreground(new Color(71, 85, 105))
            );
    }

    private static VisualElement Card(VisualElement content) =>
        new Frame()
            .Padding(new Thickness(18))
            .Background(new Color(30, 41, 59))
            .BorderRadius(12)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(content);

    private static Label SectionTitle(string text) =>
        new Label()
            .Text(text)
            .FontSize(14)
            .Foreground(new Color(96, 165, 250));

    private static Label InfoLabel(Computed<string> text) =>
        new Label()
            .Text(text)
            .FontSize(13)
            .Foreground(new Color(203, 213, 225));

    private static VisualElement ButtonRow(params VisualElement[] children) =>
        new HStack()
            .Spacing(10)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(children);

    private static Button ActionButton(string text, Action onTap) =>
        new Button()
            .Text(text)
            .Height(44)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .OnTapped(onTap);
}
