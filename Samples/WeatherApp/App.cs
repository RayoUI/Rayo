using Microsoft.Extensions.DependencyInjection;
using Rayo.Core.Assets;
using Rayo.Core.Platform;

namespace WeatherApp;

public static class App
{
    public static WindowConfiguration CreateDefaultConfiguration() => new()
    {
        Title = "Weather TwentyOne",
        Width = 1180,
        Height = 760,
        StartupLocation = WindowStartupLocation.CenterScreen,
        CanResize = true,
        VSync = true,
        Samples = 4,
        Android =
        {
            KeepScreenOn = false,
            Orientation = ScreenOrientation.Portrait
        }
    };

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WeatherStore>();
    }

    public static void ConfigureAssets(AssetConfiguration assets)
    {
        assets.AddSearchPath("Assets");
        assets.ConfigureImages(images =>
        {
            images.AddImage("Images/weather_partly_cloudy_day.svg", "CurrentWeather");
            images.AddImage("Images/compass_background.svg", "CompassBackground");
            images.AddImage("Images/compass_needle.svg", "CompassNeedle");
        });
    }
}
