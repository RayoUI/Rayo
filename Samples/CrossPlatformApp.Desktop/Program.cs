using CrossPlatformApp;
using Rayo.Core.Platform;
using Rayo.Hosting.Desktop;

// Desktop entry point — startup window options + shared UI that mutates Window at runtime.

var host = new DesktopPlatformHost();
var defaults = App.CreateDefaultConfiguration();

host.Run(
    configureApp: context =>
    {
        context.ConfigureServices(App.ConfigureServices);
        context.SetUI<MainView>();
    },
    configureWindow: config =>
    {
        // Shared / cross-platform options on IPlatformWindowConfiguration
        config.Title = defaults.Title;
        config.Width = defaults.Width;
        config.Height = defaults.Height;
        config.CanResize = defaults.CanResize;
        config.VSync = defaults.VSync;
        config.Samples = defaults.Samples;
        config.WindowState = defaults.WindowState;
        config.Topmost = defaults.Topmost;
        config.SystemDecorations = defaults.SystemDecorations;

        // Desktop-only options still live on the native WindowConfiguration
        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = defaults.StartupLocation;
            nativeConfig.Windows.ShowInTaskbar = defaults.Windows.ShowInTaskbar;
            nativeConfig.Windows.PreferDarkMode = defaults.Windows.PreferDarkMode;
            nativeConfig.MacOS.ShowInDock = defaults.MacOS.ShowInDock;
            nativeConfig.MacOS.Appearance = defaults.MacOS.Appearance;
            nativeConfig.Linux.PreferWayland = defaults.Linux.PreferWayland;
        }
    }
);
