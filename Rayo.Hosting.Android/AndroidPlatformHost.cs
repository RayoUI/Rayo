using Android.App;
using Android.Content;
using Android.Opengl;
using Rayo.Hosting.Abstractions;
using Rayo.Core;
using Rayo.Core.Platform;
using Android.Views;
using Android.OS;
using Android.Content.PM;
using Android.Content.Res;
using Android.Provider;
using Android.Widget;
using Rayo.Styling;

namespace Rayo.Hosting.Android;

/// <summary>
/// Android platform host implementation.
/// Provides an Activity base class that users can inherit from.
/// </summary>
public abstract class AndroidPlatformHost : Activity, IPlatformHost
{
    private const int StoragePermissionRequestCode = 4221;
    private RayoGLSurfaceView? _glSurfaceView;
    private FrameLayout? _contentHost;
    private global::Android.Views.View? _startupOverlay;
    private readonly AndroidPlatformCapabilities _capabilities;
    private AndroidApplicationContext? _appContext;
    private AndroidWindowConfiguration? _windowConfig;
    private AndroidWindowController? _windowController;
    private UIApplication? _application;

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

        // Create a headless UIApplication so UIApplication.Current.Window works
        // on Android (no Silk.NET window — chrome is applied via the controller).
        _application = new UIApplication(windowConfig);

        // Bridge runtime Window.Android.* mutations from components.
        _windowController = new AndroidWindowController(this);
        PlatformWindowControllers.SetAndroid(_windowController);
        SafeArea.SetProvider(_windowController);

        // Apply options that don't require DecorView
        ApplyWindowFlags();
        RequestStorageReadPermissions();

        // Theme preferences can now target UIApplication.Current.
        ApplyHostThemePreferences();

        // Create application context
        _appContext = new AndroidApplicationContext();

        // Android assets live inside the APK and are not regular filesystem files.
        // Route Rayo asset requests through the Activity's packaged AssetManager.
        Rayo.Core.Assets.AssetManager.Instance.AssetStreamProvider(
            path =>
            {
                var assetPath = path.Replace('\\', '/').TrimStart('/');
                if (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = assetPath["Assets/".Length..];
                }

                return Assets?.Open(assetPath);
            });

        Rayo.Core.Platform.UrlLauncher.SetService(new AndroidUrlLauncherService(this));

        // Let user configure the app
        ConfigureApp(_appContext);

        // Create and configure the OpenGL surface view
        _glSurfaceView = new RayoGLSurfaceView(this, _appContext, windowConfig);
        _glSurfaceView.FirstFramePresented += OnFirstFramePresented;

        // SurfaceView is backed by a separate compositor layer whose default
        // color is black until its first buffer is presented. Keep the themed
        // Android window background above it during renderer/UI initialization.
        _contentHost = new FrameLayout(this);
        _contentHost.AddView(
            _glSurfaceView,
            new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

        _startupOverlay = CreateStartupOverlay();
        _contentHost.AddView(
            _startupOverlay,
            new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));

        SetContentView(_contentHost);

        // Apply options that require DecorView (after SetContentView)
        ApplyDecorViewOptions();
    }

    protected override void OnStart()
    {
        base.OnStart();
        _glSurfaceView?.ScheduleResumeRender();
    }

    protected override void OnRestart()
    {
        base.OnRestart();
        _glSurfaceView?.ScheduleResumeRender();
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplyHostThemePreferences();
        _glSurfaceView?.OnResume();
        _glSurfaceView?.ScheduleResumeRender();
        _glSurfaceView?.RestoreVirtualKeyboard();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        DetectScreenDensity();
        ApplyHostThemePreferences();
        SafeArea.NotifyChanged();
        _glSurfaceView?.ScheduleResumeRender();
    }

    protected override void OnPostResume()
    {
        base.OnPostResume();
        _glSurfaceView?.ScheduleResumeRender();
        _glSurfaceView?.RestoreVirtualKeyboard();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        if (hasFocus)
        {
            // Immersive / system-bar flags are often cleared when focus is lost.
            ApplyDecorViewOptions();
            _glSurfaceView?.ScheduleResumeRender();
            _glSurfaceView?.RestoreVirtualKeyboard();
        }
        else
        {
            _glSurfaceView?.NotifyWindowFocusLost();
        }
    }

    protected override void OnPause()
    {
        _glSurfaceView?.NotifyActivityPaused();
        base.OnPause();
        _glSurfaceView?.OnPause();
    }

    protected override void OnStop()
    {
        _glSurfaceView?.NotifyPaused();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        if (_windowController != null)
        {
            SafeArea.ClearProvider(_windowController);
            PlatformWindowControllers.ClearAndroid(_windowController);
            _windowController = null;
        }

        _application?.Dispose();
        _application = null;

        if (_glSurfaceView != null)
        {
            _glSurfaceView.FirstFramePresented -= OnFirstFramePresented;
        }

        RemoveStartupOverlay();
        _contentHost = null;
        base.OnDestroy();
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

    private global::Android.Views.View CreateStartupOverlay()
    {
        var overlay = new global::Android.Views.View(this);

        // Match the renderer's clear color if the Activity theme does not expose
        // a window background. Applications normally override this through
        // android:windowBackground in their launch theme.
        overlay.SetBackgroundColor(new global::Android.Graphics.Color(30, 30, 30));

        var windowBackground = new global::Android.Util.TypedValue();
        if (Theme?.ResolveAttribute(
                global::Android.Resource.Attribute.WindowBackground,
                windowBackground,
                true) == true)
        {
            if (windowBackground.ResourceId != 0)
            {
                overlay.SetBackgroundResource(windowBackground.ResourceId);
            }
            else
            {
                overlay.SetBackgroundColor(
                    new global::Android.Graphics.Color(windowBackground.Data));
            }
        }

        return overlay;
    }

    private void OnFirstFramePresented()
    {
        // OnDrawFrame completes immediately before GLSurfaceView swaps buffers.
        // Keep the cover for a few more display frames so SurfaceFlinger has
        // latched the rendered buffer before exposing the SurfaceView layer.
        _startupOverlay?.PostDelayed(RemoveStartupOverlay, 50);
    }

    private void RemoveStartupOverlay()
    {
        var overlay = _startupOverlay;
        if (overlay == null)
        {
            return;
        }

        _contentHost?.RemoveView(overlay);
        overlay.Dispose();
        _startupOverlay = null;
    }

    private void ApplyHostThemePreferences()
    {
        var configuration = Resources?.Configuration;
        var prefersDark = configuration != null &&
            (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
        var textScale = Math.Clamp(configuration?.FontScale ?? 1f, 0.5f, 3f);

        var highContrast = Settings.Secure.GetInt(
            ContentResolver,
            "high_text_contrast_enabled",
            0) != 0;

        var animationScale = Settings.Global.GetFloat(
            ContentResolver,
            "animator_duration_scale",
            1f);

        var preferences = new HostThemePreferences
        {
            PrefersDark = prefersDark,
            HighContrast = highContrast,
            ReduceMotion = animationScale == 0f,
            TextScale = textScale,
            Density = ThemeDensity.Touch,
        };

        if (Rayo.Core.UIApplication.Current is { } app)
            app.UseSystemPreferences(preferences);
        else
            RayoThemes.UseTheme(RayoThemes.ResolveSystem(preferences));
    }

    /// <summary>
    /// Apply window flags that don't require DecorView (before SetContentView)
    /// </summary>
    private void ApplyWindowFlags()
    {
        if (_windowConfig == null) return;

        // Keep the Rayo viewport above the IME. This also gives keyboard
        // accessory bars a stable bottom edge across Android keyboards.
        Window?.SetSoftInputMode(SoftInput.AdjustResize | SoftInput.StateAlwaysHidden);

        _windowController?.Apply(_windowConfig.NativeConfiguration.Android);
    }

    /// <summary>
    /// Apply options that require DecorView (after SetContentView)
    /// </summary>
    private void ApplyDecorViewOptions()
    {
        if (_windowConfig == null) return;

        _windowController?.Apply(_windowConfig.NativeConfiguration.Android);
    }

    private void RequestStorageReadPermissions()
    {
#pragma warning disable CA1416
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
        {
            return;
        }

        var permissions = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
            ? new[]
            {
                global::Android.Manifest.Permission.ReadMediaAudio,
                global::Android.Manifest.Permission.ReadMediaImages,
                global::Android.Manifest.Permission.ReadMediaVideo,
            }
            : new[]
            {
                global::Android.Manifest.Permission.ReadExternalStorage,
            };

        var missingPermissions = permissions
            .Where(permission => CheckSelfPermission(permission) != Permission.Granted)
            .ToArray();

        if (missingPermissions.Length > 0)
        {
            RequestPermissions(missingPermissions, StoragePermissionRequestCode);
        }
#pragma warning restore CA1416
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
