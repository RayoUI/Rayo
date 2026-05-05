using FluentExamples;
using Rayo.Core.Platform;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Desktop;

namespace ToDoList;

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
                context.SetUI<FluentExamplesApp>();
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

