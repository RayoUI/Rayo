using Microsoft.Extensions.DependencyInjection;
using Rayo.Core.Platform;

namespace Nano;

public static class App
{
    public static WindowConfiguration CreateDefaultConfiguration()
    {
        return new WindowConfiguration
        {
            Title = "Nano",
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
