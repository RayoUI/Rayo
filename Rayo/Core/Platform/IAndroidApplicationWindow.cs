namespace Rayo.Core.Platform;

/// <summary>
/// Runtime Android chrome / display options accessible from components via
/// <c>UIApplication.Current.Window.Android</c>.
/// </summary>
public interface IAndroidApplicationWindow
{
    /// <summary>
    /// Whether to use immersive (full screen) mode.
    /// </summary>
    bool ImmersiveMode { get; set; }

    /// <summary>
    /// Whether to keep the screen on while the app is running.
    /// </summary>
    bool KeepScreenOn { get; set; }

    /// <summary>
    /// The screen orientation mode.
    /// </summary>
    ScreenOrientation Orientation { get; set; }

    /// <summary>
    /// Whether to hide the navigation bar.
    /// </summary>
    bool HideNavigationBar { get; set; }

    /// <summary>
    /// Whether to hide the status bar.
    /// </summary>
    bool HideStatusBar { get; set; }

    /// <summary>
    /// Status bar color (Android 5.0+), ARGB.
    /// </summary>
    uint? StatusBarColor { get; set; }

    /// <summary>
    /// Navigation bar color (Android 5.0+), ARGB.
    /// </summary>
    uint? NavigationBarColor { get; set; }
}
