using Nano;
using Rayo.Hosting.Desktop;

const int MobileLogicalWidth = 390;
const int MobileLogicalHeight = 780;

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
        // Keep Desktop on the same logical viewport used by the Android build.
        // Android converts physical pixels to logical units before layout, while
        // Desktop currently uses a 1:1 logical coordinate system.
        config.Width = MobileLogicalWidth;
        config.Height = MobileLogicalHeight;
        config.CanResize = false;
        config.VSync = defaults.VSync;
        config.Samples = defaults.Samples;
        config.SetIconFromFile(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon", "AppIcon.png"));

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = Rayo.Core.Platform.WindowStartupLocation.CenterScreen;
        }
    });
