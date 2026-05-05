using DirectXApp;
using Rayo.Hosting.Desktop;

namespace DirectXApp;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                // The UI is rendered by the default SkiaSharp backend.
                // DirectXScene3D owns its own D3D11 device for 3D rendering
                // and composites into the UI via CreateTextureFromPixels.
                context.SetUI<DirectXViewApp>();
            },
            configureWindow: config =>
            {
                config.Title     = "Direct3D 11 + Rayo";
                config.Width     = 900;
                config.Height    = 720;
                config.CanResize = true;
                config.VSync     = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                    nativeConfig.StartupLocation = Rayo.Core.Platform.WindowStartupLocation.CenterScreen;
            }
        );
    }
}
