using Microsoft.Extensions.DependencyInjection;
using Rayo.Core;
using Rayo.Core.Platform;

namespace CrossPlatformApp;

/// <summary>
/// Shared application setup for the window configuration demo.
/// Desktop and Android hosts call into this from their entry points.
/// </summary>
public static class App
{
    /// <summary>
    /// Creates the default window configuration shared by all platforms.
    /// Platform-specific hosts can copy these values into configureWindow
    /// and layer additional options (see Desktop Program / Android MainActivity).
    /// </summary>
    public static WindowConfiguration CreateDefaultConfiguration()
    {
        return new WindowConfiguration
        {
            Title = "Rayo Window API Demo",
            Width = 420,
            Height = 760,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = true,
            VSync = true,
            Samples = 4,
            WindowState = WindowState.Normal,
            Topmost = false,

            Windows =
            {
                ShowInTaskbar = true,
                PreferDarkMode = true
            },

            MacOS =
            {
                ShowInDock = true,
                Appearance = MacOSAppearance.Dark
            },

            Linux =
            {
                PreferWayland = true
            },

            Android =
            {
                KeepScreenOn = false,
                Orientation = ScreenOrientation.Unspecified,
                ImmersiveMode = false,
                HideStatusBar = false,
                HideNavigationBar = false,
                StatusBarColor = 0xFF1E293B
            },

            iOS =
            {
                UseSafeAreaInsets = true,
                StatusBarStyle = iOSStatusBarStyle.LightContent,
                HideHomeIndicator = false,
                Orientation = ScreenOrientation.Unspecified
            }
        };
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
    }

    public static void ConfigureApp(UIApplication app)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        app.UseServiceProvider(services);
        app.SetUI<MainView>();
    }
}
