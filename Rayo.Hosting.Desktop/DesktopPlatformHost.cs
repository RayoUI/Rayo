using System.Runtime.InteropServices;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Hosting.Abstractions;
using Rayo.Rendering.OpenGL;
using Rayo.Rendering.SkiaSharp;
using Silk.NET.OpenGL;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Desktop platform host implementation for Windows, Linux, and macOS.
/// Uses Silk.NET for cross-platform windowing and GPU-first rendering.
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
        // Create default window configuration.
        var windowConfig = CreateDefaultConfiguration();
        var platformConfig = new DesktopWindowConfiguration(windowConfig);

        // Apply platform-specific defaults.
        ApplyPlatformDefaults(platformConfig);

        // Allow user customization.
        configureWindow?.Invoke(platformConfig);

        OnBeforeRun();

        // Create the application (without initializing the window yet).
        using var app = new UIApplication(windowConfig);
        var appContext = new DesktopApplicationContext(app);

        // Default to SkiaSharp on desktop because it is currently the most
        // stable backend for production usage. OpenGL remains available as an
        // opt-in experimental backend through RAYO_DESKTOP_RENDERER=opengl.
        // User code can still replace the backend inside configureApp().
        app.SetGraphicsContext(CreateDefaultGraphicsContext());

        // Configure the application BEFORE initializing the window.
        configureApp(appContext);

        // Now initialize the window with all configuration applied.
        app.Initialize();

        // Bind SkiaSharp directly to the active OpenGL framebuffer. Keep the
        // CPU upload presenter only as a compatibility fallback when the host
        // cannot create a hardware-backed Skia surface.
        app.OnGLInitialized += () =>
        {
            if (app.GraphicsContext is SkiaSharpGraphicsContext skiaCtx && skiaCtx.Renderer is { } renderer)
            {
                var gl = app.GL!;
                var gpuInitialized = renderer.TryInitializeGpu(
                    Math.Max(1, (int)app.WindowWidth),
                    Math.Max(1, (int)app.WindowHeight),
                    unchecked((uint)gl.GetInteger((GetPName)GLEnum.FramebufferBinding)),
                    Math.Max(0, gl.GetInteger(GetPName.Samples)),
                    Math.Max(0, gl.GetInteger((GetPName)0x0D57))); // GL_STENCIL_BITS

                if (!gpuInitialized)
                {
                    Console.Error.WriteLine(
                        $"[SkiaSharpRenderer] GPU initialization failed: " +
                        $"{renderer.LastGpuInitializationError ?? "Unknown error."}");
                    var presenter = new SkiaSharpGLPresenter(gl, renderer);
                    app.WindowPresenter = (w, h) => presenter.Present(w, h);
                    app.DisposeWindowPresenter = presenter.Dispose;
                }
            }
        };

        // Enable DevTools if requested (must be after renderer is created in OnLoad).
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

        void ReloadApplication(Type[]? updatedTypes) => HotReloadManager.UpdateApplication(updatedTypes);

        HotReloadMediator.ReloadRequested += ReloadApplication;
        try
        {
            // Run the application.
            app.Run();
        }
        finally
        {
            HotReloadMediator.ReloadRequested -= ReloadApplication;
        }

        OnAfterRun();
    }

    protected override void ApplyPlatformDefaults(IPlatformWindowConfiguration config)
    {
        if (config is not DesktopWindowConfiguration desktopConfig)
            return;

        var nativeConfig = desktopConfig.NativeConfiguration;

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

    private static Rayo.Rendering.IGraphicsContext CreateDefaultGraphicsContext()
    {
        var requested = Environment.GetEnvironmentVariable("RAYO_DESKTOP_RENDERER");
        if (string.Equals(requested, "opengl", StringComparison.OrdinalIgnoreCase))
        {
            return new OpenGLGraphicsContext();
        }

        if (string.Equals(requested, "skia", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(requested, "skiasharp", StringComparison.OrdinalIgnoreCase))
        {
            return new SkiaSharpGraphicsContext();
        }

        return new SkiaSharpGraphicsContext();
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
            StartupLocation = WindowStartupLocation.Manual,
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
