namespace Rayo.Core.Platform;

internal sealed class AndroidApplicationWindow : IAndroidApplicationWindow
{
    private readonly Rayo.Core.UIApplication _app;

    internal AndroidApplicationWindow(Rayo.Core.UIApplication app)
    {
        _app = app;
    }

    private AndroidPlatformOptions Options => _app.WindowConfigurationInternal.Android;

    public bool ImmersiveMode
    {
        get => Options.ImmersiveMode;
        set
        {
            Options.ImmersiveMode = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }

    public bool KeepScreenOn
    {
        get => Options.KeepScreenOn;
        set
        {
            Options.KeepScreenOn = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }

    public ScreenOrientation Orientation
    {
        get => Options.Orientation;
        set
        {
            Options.Orientation = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }

    public bool HideNavigationBar
    {
        get => Options.HideNavigationBar;
        set
        {
            Options.HideNavigationBar = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }

    public bool HideStatusBar
    {
        get => Options.HideStatusBar;
        set
        {
            Options.HideStatusBar = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }

    public uint? StatusBarColor
    {
        get => Options.StatusBarColor;
        set
        {
            Options.StatusBarColor = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }

    public uint? NavigationBarColor
    {
        get => Options.NavigationBarColor;
        set
        {
            Options.NavigationBarColor = value;
            PlatformWindowControllers.ApplyAndroid(Options);
        }
    }
}
