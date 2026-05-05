using Android.App;
using Android.Content.PM;
using Android.OS;
using Rayo.Core.Platform;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Android;

namespace Gallery.Android;

/// <summary>
/// Main activity - minimal boilerplate!
/// Just inherit from AndroidPlatformHost and configure the app.
/// All rendering, hot reload, and touch handling is in the hosting layer.
/// </summary>
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/Theme.Gallery",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AndroidPlatformHost
{
    protected override void ConfigureApp(IPlatformApplicationContext context)
    {
        // Configure services from shared library
        context.ConfigureServices(services =>
        {
            App.ConfigureServices(services);
        });

        Rayo.Core.Assets.AssetManager.Instance.AssetStreamProvider(path =>
        {
            // The APK's assets/ root corresponds to the project's Assets/ source folder.
            // Paths stored in the shared library are prefixed with "Assets/" (e.g.
            // "Assets/Images/robot.png"), but Android's AssetManager.Open() expects
            // the path relative to the assets/ root (e.g. "Images/robot.png").
            // Strip the leading "Assets/" prefix when present.
            const string prefix = "Assets/";
            var apkPath = path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(prefix.Length)
                : path;

            try
            {
                return Assets!.Open(apkPath);
            }
            catch (Java.IO.FileNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        });

        Rayo.Core.Assets.AssetManager.Instance.ConfigureAssets(assets =>
        {
            App.ConfigureAssets(assets);
        });

        context.EnableDevTools = true;

        // Set the UI root - that's it!
        context.SetUI<GalleryBuilder>();
    }

    protected override void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        base.ConfigureWindow(config);

        // Optional: Android-specific customization
        config.Title = "Rayo Gallery";
        config.VSync = true;
        config.Samples = 4;

        // Keep screen always on
        if (config is AndroidWindowConfiguration androidConfig)
        {
            var androidOptions = androidConfig.NativeConfiguration.Android;
            androidOptions.KeepScreenOn = true;
            androidOptions.Orientation = Rayo.Core.Platform.ScreenOrientation.Portrait;
            androidOptions.ImmersiveMode = true;
            androidOptions.HideNavigationBar = false;
            androidOptions.HideStatusBar = false;
        }
    }
}


