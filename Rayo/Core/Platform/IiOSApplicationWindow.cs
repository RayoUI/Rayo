namespace Rayo.Core.Platform;

/// <summary>
/// Runtime iOS chrome / display options accessible from components via
/// <c>UIApplication.Current.Window.iOS</c>.
/// </summary>
public interface IiOSApplicationWindow
{
    /// <summary>
    /// Whether to use safe area insets.
    /// </summary>
    bool UseSafeAreaInsets { get; set; }

    /// <summary>
    /// The preferred status bar style.
    /// </summary>
    iOSStatusBarStyle StatusBarStyle { get; set; }

    /// <summary>
    /// Whether to hide the home indicator.
    /// </summary>
    bool HideHomeIndicator { get; set; }

    /// <summary>
    /// The screen orientation mode.
    /// </summary>
    ScreenOrientation Orientation { get; set; }
}
