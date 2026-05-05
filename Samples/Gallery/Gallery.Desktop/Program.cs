using Rayo.Hosting.Desktop;
using Rayo.Hosting.Abstractions;
using Gallery;

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
        context.EnableDevTools = true;
        context.SetUI<GalleryBuilder>();

        // Configure assets using native application
        if (host.GetNativeApplication(context) is { } app)
        {
            app.ConfigureAssets(assets =>
            {
                App.ConfigureAssets(assets);
            });
        }
    },
    configureWindow: config =>
    {
        // Customize window configuration
        var defaultConfig = App.CreateDefaultConfiguration();
        config.Title = defaultConfig.Title;
        config.Width = defaultConfig.Width;
        config.Height = defaultConfig.Height;
        config.CanResize = true;
        config.VSync = true;
        config.Samples = 4;

        // Access native configuration for desktop-specific settings
        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = Rayo.Core.Platform.WindowStartupLocation.CenterScreen;
        }
    }
);


