using Microsoft.Extensions.DependencyInjection;
using Rayo.Core.Platform;

namespace MobileApp;

public static class App
{
    public static WindowConfiguration CreateDefaultConfiguration()
    {
        return new WindowConfiguration
        {
            Title = "MobileApp",
            Width = 390,
            Height = 780,
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
    }

    public static void ConfigureServices(IServiceCollection services)
    {
    }
}
