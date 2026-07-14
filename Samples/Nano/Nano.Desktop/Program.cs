using Nano;
using Rayo.Hosting.Desktop;

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
        config.SetIconFromFile(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon", "AppIcon.png"));

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = Rayo.Core.Platform.WindowStartupLocation.CenterScreen;
        }
    });
