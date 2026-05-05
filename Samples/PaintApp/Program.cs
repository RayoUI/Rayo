using PaintApp;
using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

namespace PaintApp;

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
                context.SetUI<PaintAppUI>();
            },
            configureWindow: config =>
            {
                config.Title = "Paint";
                config.Width = 1024;
                config.Height = 768;
                config.CanResize = true;
                config.VSync = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                {
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
        );
    }
}
