using Rayo.DevTool;
using Rayo.Hosting.Desktop;
using Rayo.Core.Platform;

namespace Rayo.DevTool;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                context.SetUI<DevToolBuilder>();
            },
            configureWindow: config =>
            {
                config.Title = "Rayo DevTool";
                config.Width = 900;
                config.Height = 600;
                config.CanResize = true;
                config.VSync = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                {
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
                    nativeConfig.Topmost = true;
                }
            }
        );
    }
}
