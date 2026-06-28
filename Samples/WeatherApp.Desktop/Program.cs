using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;
using WeatherApp;

var host = new DesktopPlatformHost();

host.Run(
    configureApp: context =>
    {
        context.EnableDevTools = true;
        context.ConfigureServices(App.ConfigureServices);
        context.SetUI<MainView>();
    },
    configureWindow: config =>
    {
        var defaults = App.CreateDefaultConfiguration();
        config.Title = defaults.Title;
        config.Width = defaults.Width;
        config.Height = defaults.Height;
        config.CanResize = defaults.CanResize;
        config.VSync = defaults.VSync;
        config.Samples = defaults.Samples;

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = WindowStartupLocation.CenterScreen;
        }
    });
