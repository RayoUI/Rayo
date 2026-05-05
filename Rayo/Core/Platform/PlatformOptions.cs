namespace Rayo.Core.Platform;

/// <summary>
/// Base class for platform-specific options.
/// Options that don't apply to the current platform are ignored.
/// </summary>
public abstract class PlatformOptions
{
    /// <summary>
    /// Gets the target platform for these options.
    /// </summary>
    public abstract PlatformType Platform { get; }
}

/// <summary>
/// Represents the target platform types.
/// </summary>
public enum PlatformType
{
    Windows,
    Linux,
    MacOS,
    Android,
    iOS,
    WebAssembly
}

/// <summary>
/// Windows-specific window options.
/// These options are only applied when running on Windows.
/// </summary>
public class WindowsPlatformOptions : PlatformOptions
{
    public override PlatformType Platform => PlatformType.Windows;

    /// <summary>
    /// Whether to use Mica or Acrylic material effect (Windows 11+).
    /// </summary>
    public bool UseMicaBackdrop { get; set; } = false;

    /// <summary>
    /// Whether to show the window in taskbar.
    /// </summary>
    public bool ShowInTaskbar { get; set; } = true;

    /// <summary>
    /// The window icon path.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Whether to enable dark mode for window chrome.
    /// </summary>
    public bool PreferDarkMode { get; set; } = false;

    /// <summary>
    /// Whether to use immersive dark mode (affects title bar).
    /// </summary>
    public bool UseImmersiveDarkMode { get; set; } = false;
}

/// <summary>
/// Linux-specific window options.
/// These options are only applied when running on Linux.
/// </summary>
public class LinuxPlatformOptions : PlatformOptions
{
    public override PlatformType Platform => PlatformType.Linux;

    /// <summary>
    /// The WM_CLASS for the window (used by window managers).
    /// </summary>
    public string? WmClass { get; set; }

    /// <summary>
    /// Whether to use client-side decorations.
    /// </summary>
    public bool UseClientSideDecorations { get; set; } = false;

    /// <summary>
    /// Whether to enable Wayland support if available.
    /// </summary>
    public bool PreferWayland { get; set; } = true;
}

/// <summary>
/// macOS-specific window options.
/// These options are only applied when running on macOS.
/// </summary>
public class MacOSPlatformOptions : PlatformOptions
{
    public override PlatformType Platform => PlatformType.MacOS;

    /// <summary>
    /// Whether to show the window in Dock.
    /// </summary>
    public bool ShowInDock { get; set; } = true;

    /// <summary>
    /// Whether the title bar is transparent.
    /// </summary>
    public bool TransparentTitleBar { get; set; } = false;

    /// <summary>
    /// Whether to extend content into the title bar area.
    /// </summary>
    public bool ExtendClientAreaToDecorationsHint { get; set; } = false;

    /// <summary>
    /// Appearance hint for macOS (light, dark, auto).
    /// </summary>
    public MacOSAppearance Appearance { get; set; } = MacOSAppearance.Auto;
}

/// <summary>
/// macOS appearance modes.
/// </summary>
public enum MacOSAppearance
{
    Auto,
    Light,
    Dark
}

/// <summary>
/// Android-specific options.
/// These options are only applied when running on Android.
/// </summary>
public class AndroidPlatformOptions : PlatformOptions
{
    public override PlatformType Platform => PlatformType.Android;

    /// <summary>
    /// Whether to use immersive (full screen) mode.
    /// </summary>
    public bool ImmersiveMode { get; set; } = false;

    /// <summary>
    /// Whether to keep the screen on while the app is running.
    /// </summary>
    public bool KeepScreenOn { get; set; } = false;

    /// <summary>
    /// The screen orientation mode.
    /// </summary>
    public ScreenOrientation Orientation { get; set; } = ScreenOrientation.Unspecified;

    /// <summary>
    /// Whether to hide the navigation bar.
    /// </summary>
    public bool HideNavigationBar { get; set; } = false;

    /// <summary>
    /// Whether to hide the status bar.
    /// </summary>
    public bool HideStatusBar { get; set; } = false;

    /// <summary>
    /// Status bar color (Android 5.0+).
    /// </summary>
    public uint? StatusBarColor { get; set; }

    /// <summary>
    /// Navigation bar color (Android 5.0+).
    /// </summary>
    public uint? NavigationBarColor { get; set; }
}

/// <summary>
/// Screen orientation modes for mobile platforms.
/// </summary>
public enum ScreenOrientation
{
    Unspecified,
    Portrait,
    Landscape,
    PortraitReverse,
    LandscapeReverse,
    Sensor,
    SensorPortrait,
    SensorLandscape
}

/// <summary>
/// iOS-specific options.
/// These options are only applied when running on iOS.
/// </summary>
public class iOSPlatformOptions : PlatformOptions
{
    public override PlatformType Platform => PlatformType.iOS;

    /// <summary>
    /// Whether to use safe area insets.
    /// </summary>
    public bool UseSafeAreaInsets { get; set; } = true;

    /// <summary>
    /// The preferred status bar style.
    /// </summary>
    public iOSStatusBarStyle StatusBarStyle { get; set; } = iOSStatusBarStyle.Default;

    /// <summary>
    /// Whether to hide the home indicator.
    /// </summary>
    public bool HideHomeIndicator { get; set; } = false;

    /// <summary>
    /// The screen orientation mode.
    /// </summary>
    public ScreenOrientation Orientation { get; set; } = ScreenOrientation.Unspecified;
}

/// <summary>
/// iOS status bar styles.
/// </summary>
public enum iOSStatusBarStyle
{
    Default,
    LightContent,
    DarkContent
}
