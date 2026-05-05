using VeldridApp;
using Rayo.Hosting.Desktop;
using Rayo.Core.Platform;

namespace VeldridApp;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                // The UI is rendered by the default SkiaSharp backend.
                // VeldridScene3D owns its own Veldrid GraphicsDevice for 3D rendering
                // and composites into the UI via CreateTextureFromPixels.
                context.SetUI<VeldridViewApp>();
            },
            configureWindow: config =>
            {
                config.Title     = "Veldrid + Rayo";
                config.Width     = 900;
                config.Height    = 720;
                config.CanResize = true;
                config.VSync     = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
            }
        );
    }
}
