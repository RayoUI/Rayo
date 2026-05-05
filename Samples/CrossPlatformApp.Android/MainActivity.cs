using Android.App;
using Android.Content.PM;
using Android.OS;
using Rayo.Hosting.Android;
using Rayo.Hosting.Abstractions;

namespace CrossPlatformApp.Android;

/// <summary>
/// Main activity - minimal boilerplate!
/// Just inherit from AndroidPlatformHost and configure the app.
/// All rendering, hot reload, and touch handling is in the hosting layer.
/// </summary>
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/Theme.CrossPlatformApp",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AndroidPlatformHost
{
    protected override void ConfigureApp(IPlatformApplicationContext context)
    {
        // Configure services from shared library
        context.ConfigureServices(services =>
        {
            CrossPlatformApp.App.ConfigureServices(services);
        });

        // Set the UI root - that's it!
        context.SetUI<CrossPlatformApp.MainView>();
    }

    protected override void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        base.ConfigureWindow(config);

        // Optional: Android-specific customization
        config.Title = "Rayo Cross-Platform Demo";
        config.VSync = true;
        config.Samples = 4;
    }
}


