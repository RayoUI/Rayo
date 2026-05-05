using Android.App;
using Android.Content;
using Android.Opengl;
using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;
using Android.Views;
using Android.OS;
using Android.Content.PM;

namespace Rayo.Hosting.Android;

/// <summary>
/// Android platform host implementation.
/// Provides an Activity base class that users can inherit from.
/// </summary>
public abstract class AndroidPlatformHost : Activity, IPlatformHost
{
    private RayoGLSurfaceView? _glSurfaceView;
    private readonly AndroidPlatformCapabilities _capabilities;
    private AndroidApplicationContext? _appContext;
    private AndroidWindowConfiguration? _windowConfig;

    protected AndroidPlatformHost()
    {
        _capabilities = new AndroidPlatformCapabilities();
    }

    public IPlatformCapabilities Capabilities => _capabilities;

    /// <summary>
    /// Override this method to configure your application.
    /// </summary>
    protected abstract void ConfigureApp(IPlatformApplicationContext context);

    /// <summary>
    /// Override this method to customize window configuration.
    /// </summary>
    protected virtual void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        // Default Android configuration
        config.VSync = true;
        config.Samples = 4;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Detect and set screen density for proper scaling
        DetectScreenDensity();

        // Create configuration
        var windowConfig = CreateDefaultConfiguration();
        _windowConfig = new AndroidWindowConfiguration(windowConfig);

        // Allow user customization first
        ConfigureWindow(_windowConfig);

        // Apply options that don't require DecorView
        ApplyWindowFlags();

        // Create application context
        _appContext = new AndroidApplicationContext();

        // Let user configure the app
        ConfigureApp(_appContext);

        // Create and configure the OpenGL surface view
        _glSurfaceView = new RayoGLSurfaceView(this, _appContext, windowConfig);

        // Set as content view
        SetContentView(_glSurfaceView);

        // Apply options that require DecorView (after SetContentView)
        ApplyDecorViewOptions();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _glSurfaceView?.OnResume();
    }

    protected override void OnPause()
    {
        base.OnPause();
        _glSurfaceView?.OnPause();
    }

    public void Run(
        Action<IPlatformApplicationContext> configureApp,
        Action<IPlatformWindowConfiguration>? configureWindow = null)
    {
        // Not used in Android - the Activity lifecycle handles this
        throw new NotSupportedException(
            "Android platform host uses Activity lifecycle. Override OnCreate instead of calling Run.");
    }

    private void DetectScreenDensity()
    {
        var displayMetrics = Resources?.DisplayMetrics;
        if (displayMetrics != null)
        {
            float densityDpi = (float)displayMetrics.DensityDpi;
            float scaleFactor = densityDpi / 160f; // Android baseline is 160 DPI
            
            global::Android.Util.Log.Info("Rayo", 
                $"Screen Density: {densityDpi} DPI, Scale Factor: {scaleFactor:F2}x");
            
            _capabilities.DpiScale = scaleFactor;
            Rayo.Rendering.SkiaSharp.SkiaSharpRenderer.SetDpiScaleFactor(scaleFactor);
        }
    }

    /// <summary>
    /// Apply window flags that don't require DecorView (before SetContentView)
    /// </summary>
    private void ApplyWindowFlags()
    {
        if (_windowConfig == null) return;

        var nativeConfig = _windowConfig.NativeConfiguration;
        var options = nativeConfig.Android;

        // Apply orientation
        RequestedOrientation = options.Orientation switch
        {
            Core.Platform.ScreenOrientation.Portrait => global::Android.Content.PM.ScreenOrientation.Portrait,
            Core.Platform.ScreenOrientation.Landscape => global::Android.Content.PM.ScreenOrientation.Landscape,
            Core.Platform.ScreenOrientation.PortraitReverse => global::Android.Content.PM.ScreenOrientation.ReversePortrait,
            Core.Platform.ScreenOrientation.LandscapeReverse => global::Android.Content.PM.ScreenOrientation.ReverseLandscape,
            Core.Platform.ScreenOrientation.Sensor => global::Android.Content.PM.ScreenOrientation.Sensor,
            Core.Platform.ScreenOrientation.SensorPortrait => global::Android.Content.PM.ScreenOrientation.SensorPortrait,
            Core.Platform.ScreenOrientation.SensorLandscape => global::Android.Content.PM.ScreenOrientation.SensorLandscape,
            _ => global::Android.Content.PM.ScreenOrientation.Unspecified
        };

        // Apply keep screen on
        if (options.KeepScreenOn)
        {
            Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        }

        // Apply hide status bar (fullscreen flag)
        if (options.HideStatusBar || options.ImmersiveMode)
        {
            Window?.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        }

        // Apply status bar color
        if (options.StatusBarColor.HasValue && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
#pragma warning disable CA1416
#pragma warning disable CA1422
            Window?.SetStatusBarColor(new global::Android.Graphics.Color((int)options.StatusBarColor.Value));
#pragma warning restore CA1422
#pragma warning restore CA1416
        }

        // Apply navigation bar color
        if (options.NavigationBarColor.HasValue && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
#pragma warning disable CA1416
#pragma warning disable CA1422
            Window?.SetNavigationBarColor(new global::Android.Graphics.Color((int)options.NavigationBarColor.Value));
#pragma warning restore CA1422
#pragma warning restore CA1416
        }
    }

    /// <summary>
    /// Apply options that require DecorView (after SetContentView)
    /// </summary>
    private void ApplyDecorViewOptions()
    {
        if (_windowConfig == null) return;

        var nativeConfig = _windowConfig.NativeConfiguration;
        var options = nativeConfig.Android;

        // Apply immersive mode if configured
        if (options.ImmersiveMode)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
#pragma warning disable CA1416
                Window?.InsetsController?.Hide(WindowInsets.Type.SystemBars());
                Window?.InsetsController?.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CS0618
                var decorView = Window?.DecorView;
                if (decorView != null)
                {
                    decorView.SystemUiVisibility = (StatusBarVisibility)(
                        SystemUiFlags.ImmersiveSticky |
                        SystemUiFlags.LayoutStable |
                        SystemUiFlags.LayoutHideNavigation |
                        SystemUiFlags.LayoutFullscreen |
                        SystemUiFlags.HideNavigation |
                        SystemUiFlags.Fullscreen);
                }
#pragma warning restore CS0618
            }
        }
        // Apply hide navigation bar (without immersive mode)
        else if (options.HideNavigationBar)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
#pragma warning disable CA1416
                Window?.InsetsController?.Hide(WindowInsets.Type.NavigationBars());
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CS0618
                var decorView = Window?.DecorView;
                if (decorView != null)
                {
                    var currentFlags = (SystemUiFlags)decorView.SystemUiVisibility;
                    decorView.SystemUiVisibility = (StatusBarVisibility)(
                        currentFlags |
                        SystemUiFlags.HideNavigation |
                        SystemUiFlags.LayoutHideNavigation);
                }
#pragma warning restore CS0618
            }
        }
    }

    private static WindowConfiguration CreateDefaultConfiguration()
    {
        return new WindowConfiguration
        {
            Title = "Rayo Android App",
            VSync = true,
            Samples = 4,
            Android =
            {
                KeepScreenOn = false,
                Orientation = Core.Platform.ScreenOrientation.Unspecified,
                ImmersiveMode = false,
            }
        };
    }
}
