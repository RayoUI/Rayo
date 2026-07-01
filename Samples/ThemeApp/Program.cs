using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

namespace ThemeApp;

public static class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                context.EnableDevTools = true;
                context.SetUI<ThemeCatalogApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Rayo Theme App";
                config.Width = 1180;
                config.Height = 820;
                config.CanResize = true;
                config.VSync = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                {
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
                }
            });
    }
}
