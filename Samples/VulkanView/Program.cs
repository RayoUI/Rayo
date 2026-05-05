using VulkanApp;
using Rayo.Hosting.Desktop;

namespace VulkanApp;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new DesktopPlatformHost();

        host.Run(
            configureApp: context =>
            {
                // The UI is rendered by the default SkiaSharp backend.
                // VulkanScene3D owns its own Vulkan device for 3D rendering
                // and composites into the UI via CreateTextureFromPixels.
                context.SetUI<VulkanViewApp>();
            },
            configureWindow: config =>
            {
                config.Title     = "Vulkan 3D + Rayo";
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
