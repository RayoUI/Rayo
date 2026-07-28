using Android.App;
using Android.OS;
using Android.Views;
using Rayo.Core.Platform;
using Rayo.Rendering.SkiaSharp;

namespace Rayo.Hosting.Android;

/// <summary>
/// Applies <see cref="AndroidPlatformOptions"/> to a live <see cref="Activity"/>
/// and reports safe-area insets from the system window.
/// </summary>
internal sealed class AndroidWindowController : IAndroidWindowController
{
    private readonly Activity _activity;

    public AndroidWindowController(Activity activity)
    {
        _activity = activity;
    }

    public void Apply(AndroidPlatformOptions options)
    {
        if (_activity.IsFinishing || _activity.IsDestroyed)
        {
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
#pragma warning disable CA1416
            Looper.MainLooper?.IsCurrentThread == true)
#pragma warning restore CA1416
        {
            ApplyOnUiThread(options);
        }
        else
        {
            _activity.RunOnUiThread(() => ApplyOnUiThread(options));
        }
    }

    public SafeAreaInsets GetSafeAreaInsets()
    {
        if (_activity.IsFinishing || _activity.IsDestroyed)
        {
            return SafeAreaInsets.Empty;
        }

        var decorView = _activity.Window?.DecorView;
        if (decorView == null)
        {
            return SafeAreaInsets.Empty;
        }

        float scale = Math.Max(0.01f, SkiaSharpRenderer.GetDpiScaleFactor());
        float ToLogical(int pixels) => pixels / scale;

#pragma warning disable CA1416 // RootWindowInsets requires API 23+
        var rootInsets = Build.VERSION.SdkInt >= BuildVersionCodes.M
            ? decorView.RootWindowInsets
            : null;
#pragma warning restore CA1416
        if (rootInsets == null)
        {
            return SafeAreaInsets.Empty;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
#pragma warning disable CA1416
            var typeMask = WindowInsets.Type.StatusBars()
                           | WindowInsets.Type.DisplayCutout()
                           | WindowInsets.Type.NavigationBars();
            var bars = rootInsets.GetInsets(typeMask);
            return new SafeAreaInsets(
                ToLogical(bars.Top),
                ToLogical(bars.Right),
                ToLogical(bars.Bottom),
                ToLogical(bars.Left));
#pragma warning restore CA1416
        }

#pragma warning disable CS0618
#pragma warning disable CA1416
#pragma warning disable CA1422
        return new SafeAreaInsets(
            ToLogical(rootInsets.SystemWindowInsetTop),
            ToLogical(rootInsets.SystemWindowInsetRight),
            ToLogical(rootInsets.SystemWindowInsetBottom),
            ToLogical(rootInsets.SystemWindowInsetLeft));
#pragma warning restore CA1422
#pragma warning restore CA1416
#pragma warning restore CS0618
    }

    private void ApplyOnUiThread(AndroidPlatformOptions options)
    {
        if (_activity.IsFinishing || _activity.IsDestroyed)
        {
            return;
        }

        ApplyOrientation(options);
        ApplyKeepScreenOn(options);
        ApplyStatusAndNavigationColors(options);
        ApplySystemBars(options);
        SafeArea.NotifyChanged();

        // Insets often settle after the next layout pass following system-bar changes.
        _activity.Window?.DecorView?.Post(SafeArea.NotifyChanged);
    }

    private void ApplyOrientation(AndroidPlatformOptions options)
    {
        _activity.RequestedOrientation = options.Orientation switch
        {
            ScreenOrientation.Portrait => global::Android.Content.PM.ScreenOrientation.Portrait,
            ScreenOrientation.Landscape => global::Android.Content.PM.ScreenOrientation.Landscape,
            ScreenOrientation.PortraitReverse => global::Android.Content.PM.ScreenOrientation.ReversePortrait,
            ScreenOrientation.LandscapeReverse => global::Android.Content.PM.ScreenOrientation.ReverseLandscape,
            ScreenOrientation.Sensor => global::Android.Content.PM.ScreenOrientation.Sensor,
            ScreenOrientation.SensorPortrait => global::Android.Content.PM.ScreenOrientation.SensorPortrait,
            ScreenOrientation.SensorLandscape => global::Android.Content.PM.ScreenOrientation.SensorLandscape,
            _ => global::Android.Content.PM.ScreenOrientation.Unspecified
        };
    }

    private void ApplyKeepScreenOn(AndroidPlatformOptions options)
    {
        if (options.KeepScreenOn)
        {
            _activity.Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        }
        else
        {
            _activity.Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        }
    }

    private void ApplyStatusAndNavigationColors(AndroidPlatformOptions options)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
        {
            return;
        }

#pragma warning disable CA1416
#pragma warning disable CA1422
        if (options.StatusBarColor.HasValue)
        {
            _activity.Window?.SetStatusBarColor(
                new global::Android.Graphics.Color((int)options.StatusBarColor.Value));
        }

        if (options.NavigationBarColor.HasValue)
        {
            _activity.Window?.SetNavigationBarColor(
                new global::Android.Graphics.Color((int)options.NavigationBarColor.Value));
        }
#pragma warning restore CA1422
#pragma warning restore CA1416
    }

    private void ApplySystemBars(AndroidPlatformOptions options)
    {
        bool hideStatus = options.HideStatusBar || options.ImmersiveMode;
        bool hideNav = options.HideNavigationBar || options.ImmersiveMode;

        if (hideStatus)
        {
            _activity.Window?.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        }
        else
        {
            _activity.Window?.ClearFlags(WindowManagerFlags.Fullscreen);
        }

        // DecorView is only available after SetContentView. Accessing
        // Window.InsetsController earlier throws a Java NPE inside DecorView.
        var decorView = _activity.Window?.DecorView;
        if (decorView == null)
        {
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
#pragma warning disable CA1416
            var controller = decorView.WindowInsetsController;
            if (controller == null)
            {
                return;
            }

            if (options.ImmersiveMode)
            {
                controller.Hide(WindowInsets.Type.SystemBars());
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
            else
            {
                if (hideStatus)
                {
                    controller.Hide(WindowInsets.Type.StatusBars());
                }
                else
                {
                    controller.Show(WindowInsets.Type.StatusBars());
                }

                if (hideNav)
                {
                    controller.Hide(WindowInsets.Type.NavigationBars());
                }
                else
                {
                    controller.Show(WindowInsets.Type.NavigationBars());
                }
            }
#pragma warning restore CA1416
            return;
        }

#pragma warning disable CS0618
        if (options.ImmersiveMode)
        {
            decorView.SystemUiVisibility = (StatusBarVisibility)(
                SystemUiFlags.ImmersiveSticky |
                SystemUiFlags.LayoutStable |
                SystemUiFlags.LayoutHideNavigation |
                SystemUiFlags.LayoutFullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.Fullscreen);
        }
        else
        {
            SystemUiFlags flags = SystemUiFlags.LayoutStable;
            if (hideStatus)
            {
                flags |= SystemUiFlags.Fullscreen | SystemUiFlags.LayoutFullscreen;
            }

            if (hideNav)
            {
                flags |= SystemUiFlags.HideNavigation | SystemUiFlags.LayoutHideNavigation;
            }

            decorView.SystemUiVisibility = (StatusBarVisibility)flags;
        }
#pragma warning restore CS0618
    }
}
