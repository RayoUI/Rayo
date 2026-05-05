using Rayo.Example;
using Rayo.Hosting.Desktop;
using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

namespace Notepad;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("Rayo Notepad Started");
        Console.WriteLine("==========================================");

        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
#if DEBUG
                context.EnableDevTools = true;
#endif
                context.SetUI<NotepadApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Rayo Notepad";
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

