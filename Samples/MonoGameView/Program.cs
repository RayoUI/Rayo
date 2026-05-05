using MonoGameSample;
using Rayo.Core.Platform;
using Rayo.Hosting.Abstractions;
using Rayo.Hosting.Desktop;

namespace MonoGameSample;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                // DevTools causes the UI tree to be rebuilt, which mounts MonoGameView
                // multiple times and corrupts MonoGame's internal static _uiThread state.
                context.SetUI<MonoGameViewApp>();
            },
            configureWindow: config =>
            {
                config.Title    = "MonoGame + Rayo";
                config.Width    = 900;
                config.Height   = 720;
                config.CanResize = true;
                config.VSync    = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
            }
        );
    }
}
