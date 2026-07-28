namespace Rayo.Core.Platform;

internal sealed class iOSApplicationWindow : IiOSApplicationWindow
{
    private readonly Rayo.Core.UIApplication _app;

    internal iOSApplicationWindow(Rayo.Core.UIApplication app)
    {
        _app = app;
    }

    private iOSPlatformOptions Options => _app.WindowConfigurationInternal.iOS;

    public bool UseSafeAreaInsets
    {
        get => Options.UseSafeAreaInsets;
        set
        {
            Options.UseSafeAreaInsets = value;
            PlatformWindowControllers.ApplyiOS(Options);
        }
    }

    public iOSStatusBarStyle StatusBarStyle
    {
        get => Options.StatusBarStyle;
        set
        {
            Options.StatusBarStyle = value;
            PlatformWindowControllers.ApplyiOS(Options);
        }
    }

    public bool HideHomeIndicator
    {
        get => Options.HideHomeIndicator;
        set
        {
            Options.HideHomeIndicator = value;
            PlatformWindowControllers.ApplyiOS(Options);
        }
    }

    public ScreenOrientation Orientation
    {
        get => Options.Orientation;
        set
        {
            Options.Orientation = value;
            PlatformWindowControllers.ApplyiOS(Options);
        }
    }
}
