using Android.App;
using Android.Content.PM;
using Rayo.Hosting.Android;
using Rayo.Hosting.Abstractions;

namespace MobileApp.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/Theme.MobileApp",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AndroidPlatformHost
{
    protected override void ConfigureApp(IPlatformApplicationContext context)
    {
        context.ConfigureServices(MobileApp.App.ConfigureServices);
        context.SetUI<MobileApp.MainView>();
    }

    protected override void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        base.ConfigureWindow(config);
        var defaults = MobileApp.App.CreateDefaultConfiguration();
        config.Title = defaults.Title;
        config.VSync = defaults.VSync;
        config.Samples = defaults.Samples;

        if (config is AndroidWindowConfiguration android)
        {
            android.KeepScreenOn = defaults.Android.KeepScreenOn;
            android.Orientation = defaults.Android.Orientation;
            android.ImmersiveMode = defaults.Android.ImmersiveMode;
            android.HideStatusBar = defaults.Android.HideStatusBar;
            android.HideNavigationBar = defaults.Android.HideNavigationBar;
            android.StatusBarColor = defaults.Android.StatusBarColor;
            android.NavigationBarColor = defaults.Android.NavigationBarColor;
        }
    }
}
