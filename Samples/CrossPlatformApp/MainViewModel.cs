using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Reactivity;

namespace CrossPlatformApp;

/// <summary>
/// ViewModel that drives the shared window-API demo UI and mutates
/// <see cref="UIApplication.Current"/>.Window for the active platform.
/// </summary>
public class MainViewModel : ViewModelBase
{
    public Signal<string> PlatformName { get; } = new(GetPlatformName());
    public Signal<bool> IsDesktop { get; } = new(PlatformDetector.IsDesktop);
    public Signal<bool> IsAndroid { get; } = new(PlatformDetector.IsAndroid);
    public Signal<bool> IsiOS { get; } = new(PlatformDetector.IsiOS);
    public Signal<bool> IsMobile { get; } = new(PlatformDetector.IsMobile);
    public Signal<string> StatusMessage { get; } = new("Ready — use the controls below.");

    // Desktop live state
    public Signal<string> WindowTitle { get; } = new("Rayo Window API Demo");
    public Signal<bool> IsMaximized { get; } = new(false);
    public Signal<bool> IsTopmost { get; } = new(false);
    public Signal<bool> CanResize { get; } = new(true);
    public Signal<string> WindowSizeText { get; } = new("—");

    // Mobile safe area (0 on desktop)
    public Signal<float> SafeAreaTop { get; } = new(0f);
    public Signal<string> SafeAreaText { get; } = new("SafeArea.Top: 0");

    // Android live state
    public Signal<bool> KeepScreenOn { get; } = new(false);
    public Signal<bool> ImmersiveMode { get; } = new(false);
    public Signal<bool> HideStatusBar { get; } = new(false);
    public Signal<string> OrientationLabel { get; } = new(ScreenOrientation.Unspecified.ToString());

    // iOS live state
    public Signal<bool> UseSafeAreaInsets { get; } = new(true);
    public Signal<bool> HideHomeIndicator { get; } = new(false);
    public Signal<string> StatusBarStyleLabel { get; } = new(iOSStatusBarStyle.Default.ToString());

    private IApplicationWindow Window =>
        UIApplication.Current?.Window
        ?? throw new InvalidOperationException("No active UIApplication.");

    protected override void OnInitialized()
    {
        SafeArea.Changed += OnSafeAreaChanged;
        RegisterDisposable(new ActionDisposable(() => SafeArea.Changed -= OnSafeAreaChanged));

        RefreshFromWindow();
        StatusMessage.Value = $"Ready on {PlatformName.Value} — use the controls below.";
    }

    private void OnSafeAreaChanged() => RefreshSafeArea();

    private static string GetPlatformName() => PlatformDetector.CurrentPlatform switch
    {
        PlatformType.Windows => "Windows",
        PlatformType.Linux => "Linux",
        PlatformType.MacOS => "macOS",
        PlatformType.Android => "Android",
        PlatformType.iOS => "iOS",
        PlatformType.WebAssembly => "WebAssembly",
        _ => "Unknown"
    };

    private string FormatSize() => $"{Window.Width} × {Window.Height}";

    private static string FormatOrientation(ScreenOrientation orientation) => orientation.ToString();

    private static string FormatStatusBarStyle(iOSStatusBarStyle style) => style.ToString();

    private void RefreshSafeArea()
    {
        float top = Window.SafeArea.Top;
        SafeAreaTop.Value = top;
        SafeAreaText.Value = $"SafeArea.Top: {top:0.#}";
    }

    public void RefreshFromWindow()
    {
        var window = Window;

        WindowTitle.Value = window.Title;
        IsMaximized.Value = window.State == WindowState.Maximized;
        IsTopmost.Value = window.Topmost;
        CanResize.Value = window.CanResize;
        WindowSizeText.Value = FormatSize();

        RefreshSafeArea();

        KeepScreenOn.Value = window.Android.KeepScreenOn;
        ImmersiveMode.Value = window.Android.ImmersiveMode;
        HideStatusBar.Value = window.Android.HideStatusBar;
        OrientationLabel.Value = FormatOrientation(window.Android.Orientation);

        UseSafeAreaInsets.Value = window.iOS.UseSafeAreaInsets;
        HideHomeIndicator.Value = window.iOS.HideHomeIndicator;
        StatusBarStyleLabel.Value = FormatStatusBarStyle(window.iOS.StatusBarStyle);
    }

    // —— Desktop ——

    public void SetTitle(string title)
    {
        Window.Title = title;
        WindowTitle.Value = title;
        StatusMessage.Value = $"Title → {title}";
    }

    public void ToggleMaximize()
    {
        Window.State = Window.State == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        IsMaximized.Value = Window.State == WindowState.Maximized;
        WindowSizeText.Value = FormatSize();
        StatusMessage.Value = IsMaximized.Value ? "Window maximized" : "Window restored";
    }

    public void ToggleTopmost()
    {
        Window.Topmost = !Window.Topmost;
        IsTopmost.Value = Window.Topmost;
        StatusMessage.Value = $"Topmost → {Window.Topmost}";
    }

    public void ToggleCanResize()
    {
        Window.CanResize = !Window.CanResize;
        CanResize.Value = Window.CanResize;
        StatusMessage.Value = $"CanResize → {Window.CanResize}";
    }

    public void CenterWindow()
    {
        Window.Center();
        StatusMessage.Value = "Window centered";
    }

    public void ApplyCompactSize()
    {
        Window.State = WindowState.Normal;
        Window.SetSize(420, 760);
        IsMaximized.Value = false;
        WindowSizeText.Value = FormatSize();
        StatusMessage.Value = "Size → 420 × 760";
    }

    public void ApplyWideSize()
    {
        Window.State = WindowState.Normal;
        Window.SetSize(900, 640);
        IsMaximized.Value = false;
        WindowSizeText.Value = FormatSize();
        StatusMessage.Value = "Size → 900 × 640";
    }

    // —— Android ——

    public void ToggleKeepScreenOn()
    {
        Window.Android.KeepScreenOn = !Window.Android.KeepScreenOn;
        KeepScreenOn.Value = Window.Android.KeepScreenOn;
        StatusMessage.Value = $"KeepScreenOn → {Window.Android.KeepScreenOn}";
    }

    public void ToggleImmersiveMode()
    {
        Window.Android.ImmersiveMode = !Window.Android.ImmersiveMode;
        ImmersiveMode.Value = Window.Android.ImmersiveMode;
        StatusMessage.Value = $"ImmersiveMode → {Window.Android.ImmersiveMode}";
    }

    public void ToggleHideStatusBar()
    {
        Window.Android.HideStatusBar = !Window.Android.HideStatusBar;
        HideStatusBar.Value = Window.Android.HideStatusBar;
        StatusMessage.Value = $"HideStatusBar → {Window.Android.HideStatusBar}";
    }

    public void CycleAndroidOrientation()
    {
        Window.Android.Orientation = Window.Android.Orientation switch
        {
            ScreenOrientation.Unspecified => ScreenOrientation.Portrait,
            ScreenOrientation.Portrait => ScreenOrientation.Landscape,
            ScreenOrientation.Landscape => ScreenOrientation.Sensor,
            _ => ScreenOrientation.Unspecified
        };
        OrientationLabel.Value = FormatOrientation(Window.Android.Orientation);
        StatusMessage.Value = $"Orientation → {Window.Android.Orientation}";
    }

    public void ApplyAndroidStatusBarColor(uint argb)
    {
        Window.Android.StatusBarColor = argb;
        StatusMessage.Value = $"StatusBarColor → 0x{argb:X8}";
    }

    // —— iOS ——

    public void ToggleSafeAreaInsets()
    {
        Window.iOS.UseSafeAreaInsets = !Window.iOS.UseSafeAreaInsets;
        UseSafeAreaInsets.Value = Window.iOS.UseSafeAreaInsets;
        StatusMessage.Value = $"UseSafeAreaInsets → {Window.iOS.UseSafeAreaInsets}";
    }

    public void ToggleHideHomeIndicator()
    {
        Window.iOS.HideHomeIndicator = !Window.iOS.HideHomeIndicator;
        HideHomeIndicator.Value = Window.iOS.HideHomeIndicator;
        StatusMessage.Value = $"HideHomeIndicator → {Window.iOS.HideHomeIndicator}";
    }

    public void CycleStatusBarStyle()
    {
        Window.iOS.StatusBarStyle = Window.iOS.StatusBarStyle switch
        {
            iOSStatusBarStyle.Default => iOSStatusBarStyle.LightContent,
            iOSStatusBarStyle.LightContent => iOSStatusBarStyle.DarkContent,
            _ => iOSStatusBarStyle.Default
        };
        StatusBarStyleLabel.Value = FormatStatusBarStyle(Window.iOS.StatusBarStyle);
        StatusMessage.Value = $"StatusBarStyle → {Window.iOS.StatusBarStyle}";
    }

    public void CycleiOSOrientation()
    {
        Window.iOS.Orientation = Window.iOS.Orientation switch
        {
            ScreenOrientation.Unspecified => ScreenOrientation.Portrait,
            ScreenOrientation.Portrait => ScreenOrientation.Landscape,
            _ => ScreenOrientation.Unspecified
        };
        OrientationLabel.Value = FormatOrientation(Window.iOS.Orientation);
        StatusMessage.Value = $"iOS Orientation → {Window.iOS.Orientation}";
    }

    private sealed class ActionDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _dispose, null);
            action?.Invoke();
        }
    }
}
