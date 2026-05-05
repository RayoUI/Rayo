using FluentExamples;
using Rayo.Core.Platform;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Desktop;

namespace TestExamples;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                context.EnableDevTools = true;
                context.SetUI<TabControlApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Fluent Examples App";
                config.Width = 500;
                config.Height = 700;
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

