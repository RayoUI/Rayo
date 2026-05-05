using Arkanoid;
using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

var host = new DesktopPlatformHost();

host.Run(
    configureApp: context =>
    {
#if DEBUG
        context.EnableDevTools = true;
#endif
        context.SetUI<ArkanoidApp>();
    },
    configureWindow: config =>
    {
        config.Title    = "Arkanoid — Rayo Game Engine";
        config.Width    = 600;
        config.Height   = 700;
        config.CanResize = true;
        config.VSync    = true;

        if (host.GetNativeWindowConfiguration(config) is { } native)
            native.StartupLocation = WindowStartupLocation.CenterScreen;
    }
);
