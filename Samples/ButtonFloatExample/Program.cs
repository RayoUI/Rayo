using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

namespace ButtonFloatExample;

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
                context.SetUI<ButtonFloatApp>();
            },
            configureWindow: config =>
            {
                config.Title = "Rayo - ButtonFloat Example";
                config.Width = 420;
                config.Height = 720;
                config.CanResize = true;
                config.VSync = true;

                if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
                {
                    nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
                    nativeConfig.Topmost = true;
                }
            });
    }
}
