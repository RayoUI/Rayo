using Microsoft.Extensions.DependencyInjection;
using Rayo.Core;
using Rayo.Core.Platform;

namespace CrossPlatformApp;

/// <summary>
/// Main application class that configures the shared application logic.
/// Platform-specific projects call this to set up the application.
/// </summary>
public static class App
{
    /// <summary>
    /// Creates the default window configuration.
    /// Platform-specific projects can override or extend this.
    /// </summary>
    public static WindowConfiguration CreateDefaultConfiguration()
    {
        return new WindowConfiguration
        {
            Title = "Rayo Cross-Platform Demo",
            Width = 370,
            Height = 700,
            StartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = true,
            VSync = true,
            Samples = 4,

            // Windows-specific options (only applied on Windows)
            Windows =
            {
                ShowInTaskbar = true,
                PreferDarkMode = true
            },

            // macOS-specific options (only applied on macOS)
            MacOS =
            {
                ShowInDock = true,
                Appearance = MacOSAppearance.Dark
            },

            // Linux-specific options (only applied on Linux)
            Linux =
            {
                PreferWayland = true
            },

            // Android-specific options (only applied on Android)
            Android =
            {
                KeepScreenOn = false,
                Orientation = ScreenOrientation.Unspecified
            }
        };
    }

    /// <summary>
    /// Configures the service collection with application services.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register application services here
        // services.AddSingleton<IMyService, MyService>();
    }

    /// <summary>
    /// Configures the UIApplication with the shared UI.
    /// </summary>
    public static void ConfigureApp(UIApplication app)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        app.UseServiceProvider(services);

        // Set the main view
        app.SetUI<MainView>();
    }

    /// <summary>
    /// Creates and runs the application with default configuration.
    /// </summary>
    public static void Run()
    {
        Run(CreateDefaultConfiguration());
    }

    /// <summary>
    /// Creates and runs the application with custom configuration.
    /// </summary>
    public static void Run(WindowConfiguration config)
    {
        using var app = new UIApplication(config);
        ConfigureApp(app);
        app.Run();
    }
}
