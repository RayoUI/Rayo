using Rayo.Core.Platform;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Desktop;

namespace TestApp;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                context.EnableDevTools = true;
                context.SetUI<BasicApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Test App";
                config.Width = 400;
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

