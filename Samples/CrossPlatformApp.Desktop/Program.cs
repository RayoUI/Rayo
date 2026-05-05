using CrossPlatformApp;
using Rayo.Hosting.Desktop;
using Rayo.Hosting.Abstractions;

// Desktop entry point - now much simpler with platform abstraction!

var host = new DesktopPlatformHost();

host.Run(
    configureApp: context =>
    {
        // Configure services
        context.ConfigureServices(services =>
        {
            App.ConfigureServices(services);
        });

        // Configure the UI
        context.SetUI<MainView>();
    },
    configureWindow: config =>
    {
        // Customize window configuration
        var defaultConfig = App.CreateDefaultConfiguration();
        config.Title = defaultConfig.Title;
        config.Width = 370;
        config.Height = 700;
        config.CanResize = true;
        config.VSync = true;
        config.Samples = 4;
        
        // Access native configuration for desktop-specific settings
        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = Rayo.Core.Platform.WindowStartupLocation.CenterScreen;
            nativeConfig.Topmost = true;
        }
    }
);


