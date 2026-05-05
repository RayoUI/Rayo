using Rayo.Example;
using Rayo.Hosting.Desktop;
using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

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
                context.SetUI<KanbanApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Kanban";
                config.Width = 900;
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

