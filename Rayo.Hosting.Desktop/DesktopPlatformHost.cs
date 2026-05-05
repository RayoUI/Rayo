using Rayo.Hosting.Abstractions;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Rendering.SkiaSharp;
using System.Runtime.InteropServices;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Desktop platform host implementation for Windows, Linux, and macOS.
/// Uses Silk.NET for cross-platform windowing and OpenGL rendering.
/// </summary>
public class DesktopPlatformHost : PlatformHostBase
{
    private readonly DesktopPlatformCapabilities _capabilities;

    public DesktopPlatformHost()
    {
        _capabilities = new DesktopPlatformCapabilities();
    }

    public override IPlatformCapabilities Capabilities => _capabilities;

    public override void Run(
        Action<IPlatformApplicationContext> configureApp,
        Action<IPlatformWindowConfiguration>? configureWindow = null)
    {
        // Create default window configuration
        var windowConfig = CreateDefaultConfiguration();
        var platformConfig = new DesktopWindowConfiguration(windowConfig);

        // Apply platform-specific defaults
        ApplyPlatformDefaults(platformConfig);

        // Allow user customization
        configureWindow?.Invoke(platformConfig);

        OnBeforeRun();

        // Create the application (without initializing the window yet)
        using var app = new UIApplication(windowConfig);
        var appContext = new DesktopApplicationContext(app);

        // Set SkiaSharp as the default renderer.
        // User code inside configureApp() may override by calling SetGraphicsContext() again.
        app.SetGraphicsContext(new SkiaSharpGraphicsContext());

        // Configure the application BEFORE initializing the window
        // This ensures SetUI, ConfigureAssets, etc. are applied before window creation
        configureApp(appContext);

        // Now initialize the window with all configuration applied
        app.Initialize();

        // Wire the SkiaSharp→OpenGL presenter once the GL context is ready.
        app.OnGLInitialized += () =>
        {
            if (app.GraphicsContext is SkiaSharpGraphicsContext skiaCtx && skiaCtx.Renderer is { } renderer)
            {
                var presenter = new SkiaSharpGLPresenter(app.GL!, renderer);
                app.WindowPresenter = (w, h) => presenter.Present(w, h);
                app.DisposeWindowPresenter = presenter.Dispose;
            }
        };

        // Enable DevTools if requested (must be after renderer is created in OnLoad)
        if (appContext.EnableDevTools)
        {
            app.OnGLInitialized += () =>
            {
                if (app.Renderer != null)
                {
                    DevToolExtensions.EnableDevTools(app.Tree, app.Renderer, appContext.DevToolsPort);
                }
            };
        }

        // Run the application
        app.Run();

        OnAfterRun();
    }

    protected override void ApplyPlatformDefaults(IPlatformWindowConfiguration config)
    {
        if (config is not DesktopWindowConfiguration desktopConfig)
            return;

        var nativeConfig = desktopConfig.NativeConfiguration;

        // Apply OS-specific defaults
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            nativeConfig.Windows.ShowInTaskbar = true;
            nativeConfig.Windows.PreferDarkMode = true;
            nativeConfig.Windows.UseImmersiveDarkMode = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            nativeConfig.MacOS.ShowInDock = true;
            nativeConfig.MacOS.Appearance = MacOSAppearance.Dark;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            nativeConfig.Linux.PreferWayland = true;
        }
    }

    /// <summary>
    /// Creates the default desktop window configuration.
    /// </summary>
    public static WindowConfiguration CreateDefaultConfiguration()
    {
        return new WindowConfiguration
        {
            Title = "Rayo Application",
            Width = 800,
            Height = 600,
            StartupLocation = WindowStartupLocation.Manual, // Don't center by default
            CanResize = true,
            VSync = true,
            Samples = 4
        };
    }

    /// <summary>
    /// Provides access to the underlying WindowConfiguration for advanced scenarios.
    /// </summary>
    public WindowConfiguration? GetNativeWindowConfiguration(IPlatformWindowConfiguration config)
    {
        return (config as DesktopWindowConfiguration)?.NativeConfiguration;
    }

    /// <summary>
    /// Provides access to the underlying UIApplication for advanced scenarios.
    /// </summary>
    public UIApplication? GetNativeApplication(IPlatformApplicationContext context)
    {
        return (context as DesktopApplicationContext)?.NativeApplication;
    }
}
