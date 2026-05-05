using SdlApp;
using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

var host = new DesktopPlatformHost();
host.Run(
    configureApp:    context => context.SetUI<SdlViewApp>(),
    configureWindow: config =>
    {
        config.Title     = "SDL2 Mandala — Rayo";
        config.Width     = 900;
        config.Height    = 720;
        config.CanResize = true;
        config.VSync     = true;

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
        }
    });
