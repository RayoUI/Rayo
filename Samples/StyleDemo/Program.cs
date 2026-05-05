using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

namespace StyleDemo;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                context.EnableDevTools = true;
                context.SetUI<StyleDemoApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Rayo — Style Showcase";
                config.Width = 860;
                config.Height = 680;
                config.CanResize = true;
                config.VSync = true;

                if (host.GetNativeWindowConfiguration(config) is { } native)
                    native.StartupLocation = WindowStartupLocation.CenterScreen;
            }
        );
    }
}
