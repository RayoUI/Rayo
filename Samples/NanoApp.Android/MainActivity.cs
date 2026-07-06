using Android.App;
using Android.Content.PM;
using NanoApp;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Android;

namespace NanoApp.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/Theme.NanoApp",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class MainActivity : AndroidPlatformHost
{
    protected override void ConfigureApp(IPlatformApplicationContext context)
    {
        context.ConfigureServices(AppSetup.ConfigureServices);
        context.SetUI<MainView>();
    }

    protected override void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        base.ConfigureWindow(config);

        config.Title = "NanoApp";
        config.VSync = true;
        config.Samples = 4;

        if (config is AndroidWindowConfiguration androidConfig)
        {
            androidConfig.NativeConfiguration.Android.ImmersiveMode = true;
            androidConfig.NativeConfiguration.Android.Orientation =
                Rayo.Core.Platform.ScreenOrientation.Landscape;
        }
    }
}
