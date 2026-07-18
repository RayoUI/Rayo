using Nano;
using Rayo.Hosting.Desktop;

const int MobileLogicalWidth = 390;
const int MobileLogicalHeight = 780;

// Keep Nano on Desktop's stable SkiaSharp backend. Set this explicitly so an
// inherited RAYO_DESKTOP_RENDERER value cannot switch Nano back to OpenGL.
Environment.SetEnvironmentVariable("RAYO_DESKTOP_RENDERER", "skia");
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
        // Skia performs analytic antialiasing itself. The multisampled default
        // framebuffer created by Silk on Desktop cannot be wrapped by Ganesh,
        // so request a single-sample backbuffer to keep rendering GPU-backed.
        config.Samples = 0;
        config.SetIconFromFile(Path.Combine(AppContext.BaseDirectory, "Assets/AppIcon", "AppIcon.png"));

        if (host.GetNativeWindowConfiguration(config) is { } nativeConfig)
        {
            nativeConfig.StartupLocation = Rayo.Core.Platform.WindowStartupLocation.CenterScreen;
        }
    });
