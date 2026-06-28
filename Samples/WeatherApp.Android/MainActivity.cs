using Android.App;
using Android.Content.PM;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Android;

namespace WeatherApp.Android;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/Theme.WeatherApp",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class MainActivity : AndroidPlatformHost
{
    protected override void ConfigureApp(IPlatformApplicationContext context)
    {
        context.ConfigureServices(WeatherApp.App.ConfigureServices);
        context.SetUI<WeatherApp.MainView>();
    }

    protected override void ConfigureWindow(IPlatformWindowConfiguration config)
    {
        base.ConfigureWindow(config);
        var defaults = WeatherApp.App.CreateDefaultConfiguration();
        config.Title = defaults.Title;
        config.VSync = defaults.VSync;
        config.Samples = defaults.Samples;
    }
}
