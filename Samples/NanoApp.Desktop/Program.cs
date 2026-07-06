using NanoApp;
using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

var host = new DesktopPlatformHost();

host.Run(
    configureApp: context =>
    {
        context.ConfigureServices(AppSetup.ConfigureServices);
        context.SetUI<MainView>();
    },
    configureWindow: config =>
    {
        config.Title = "NanoApp";
        config.Width = 763;
        config.Height = 391;
        config.CanResize = true;
        config.VSync = true;
        config.Samples = 4;

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
        }
    });
