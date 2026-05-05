using Microsoft.Extensions.DependencyInjection;
using Rayo.Core;
using Rayo.Core.Assets;
using Rayo.Core.Platform;

namespace Gallery;

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
            Title = "Rayo Gallery",
            Width = 1000,
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
        // Register application services here
        // services.AddSingleton<IMyService, MyService>();
    }

    /// <summary>
    /// Configures assets for the application.
    /// </summary>
    public static void ConfigureAssets(AssetConfiguration assets)
    {
        assets.AddSearchPath("Assets");

        assets.ConfigureFonts(fonts =>
        {
            fonts.AddFont("Fonts/Lineicons.ttf", "Lineicons", 24);
        });

        assets.ConfigureImages(images =>
        {
            images.AddImage("Images/robot.png", "Robot");
            images.AddImage("Images/super_robot.png", "SuperRobot");
        });
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
        app.SetUI<GalleryBuilder>();
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
