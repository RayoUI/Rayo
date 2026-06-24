using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

namespace DiagramApp;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
#if DEBUG
                context.EnableDevTools = true;
#endif
                context.SetUI<DiagramApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Diagram App - Rayo";
                config.Width = 1200;
                config.Height = 780;
                config.CanResize = true;
                config.VSync = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                {
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
                }
            });
    }
}
