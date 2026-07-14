using Android.Content.PM;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Android;

namespace Nano.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/Theme.Nano",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : AndroidPlatformHost
{
    protected override void ConfigureApp(IPlatformApplicationContext context)
    {
        context.ConfigureServices(Nano.App.ConfigureServices);
        context.SetUI<Nano.MainView>();
    }

    protected override void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        base.ConfigureWindow(config);
        var defaults = Nano.App.CreateDefaultConfiguration();
        config.Title = defaults.Title;
        config.VSync = defaults.VSync;
        config.Samples = defaults.Samples;
    }
}
