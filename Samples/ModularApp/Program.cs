using Rayo.Hosting.Desktop;
using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

namespace ModularHooksApp;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("Rayo - MODULAR HOOKS APP");
        Console.WriteLine("==========================================");

        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                context.SetUI<ModularApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Rayo - Modular Hooks App";
                config.Width = 800;
                config.Height = 600;
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

