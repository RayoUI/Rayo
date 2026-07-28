using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

namespace Rayo.Hosting.Android;

/// <summary>
/// Adapter that wraps Rayo's WindowConfiguration to implement the platform-agnostic interface,
/// and exposes Android chrome options for <c>ConfigureWindow</c>.
/// </summary>
public class AndroidWindowConfiguration : IPlatformWindowConfiguration
{
    private readonly WindowConfiguration _config;

    public AndroidWindowConfiguration(WindowConfiguration config)
    {
        _config = config;
    }

    public string Title
    {
        get => _config.Title;
        set => _config.Title = value;
    }

    public int Width
    {
        get => _config.Width;
        set => _config.Width = value;
    }

    public int Height
    {
        get => _config.Height;
        set => _config.Height = value;
    }

    public bool CanResize
    {
        get => _config.CanResize;
        set => _config.CanResize = value;
    }

    public bool VSync
    {
        get => _config.VSync;
        set => _config.VSync = value;
    }

    public int Samples
    {
        get => _config.Samples;
        set => _config.Samples = value;
    }

    public bool Topmost
    {
        get => _config.Topmost;
        set => _config.Topmost = value;
    }

    public WindowState WindowState
    {
        get => _config.WindowState;
        set => _config.WindowState = value;
    }

    public SystemDecorations SystemDecorations
    {
        get => _config.SystemDecorations;
        set => _config.SystemDecorations = value;
    }

    public bool ImmersiveMode
    {
        get => _config.Android.ImmersiveMode;
        set => _config.Android.ImmersiveMode = value;
    }

    public bool KeepScreenOn
    {
        get => _config.Android.KeepScreenOn;
        set => _config.Android.KeepScreenOn = value;
    }

    public ScreenOrientation Orientation
    {
        get => _config.Android.Orientation;
        set => _config.Android.Orientation = value;
    }

    public bool HideNavigationBar
    {
        get => _config.Android.HideNavigationBar;
        set => _config.Android.HideNavigationBar = value;
    }

    public bool HideStatusBar
    {
        get => _config.Android.HideStatusBar;
        set => _config.Android.HideStatusBar = value;
    }

    public uint? StatusBarColor
    {
        get => _config.Android.StatusBarColor;
        set => _config.Android.StatusBarColor = value;
    }

    public uint? NavigationBarColor
    {
        get => _config.Android.NavigationBarColor;
        set => _config.Android.NavigationBarColor = value;
    }

    /// <summary>
    /// Gets the underlying WindowConfiguration for Android-specific usage.
    /// </summary>
    public WindowConfiguration NativeConfiguration => _config;
}
