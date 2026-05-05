using Rayo.Hosting.Desktop;
using Rayo.Core.Platform;

namespace VisualScripting;

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
                context.SetUI<VisualScriptingView>();
            },
            configureWindow: config =>
            {
                config.Title = "Visual Scripting Editor - Rayo";
                config.Width = 1280;
                config.Height = 800;
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