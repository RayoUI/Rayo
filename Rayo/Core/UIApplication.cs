using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Rayo.Animation;
using Rayo.Core.Assets;
using Rayo.Core.Interfaces;
using Rayo.Core.Platform;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Styling;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Rayo.Core;

public class UIApplication : IDisposable
{
    private static ThemeData _fallbackTheme = RayoThemes.Light;
    private IWindow? _window;
    private GL? _gl;  // Kept for compatibility with IconFont and direct access
    private IGraphicsContext? _graphicsContext;
    private IRenderer? _renderer;
    private UITree _tree;
    private EventManager _eventManager;
    private HotReloadManager? _hotReload;
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private ExceptionDispatchInfo? _pendingRenderException;
    private readonly List<ThemeHotReloadWatcher> _themeWatchers = new();
    private WindowConfiguration _windowConfig;
    // Tracks the last polled window size so we detect maximise/restore on Windows,
    // where the Silk.NET Resize event may not fire for state-based size changes.
    private Vector2D<int> _lastPolledSize;
    private Silk.NET.Windowing.WindowState _lastPolledWindowState;
    private int _pendingWindowStateRelayoutFrames;
    
    public static UIApplication? Current { get; private set; }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// Application-level style sheet applied to every <see cref="Component"/> subtree.
    /// Set via <see cref="UseGlobalStyles"/> for a fluent setup. Component-level styles
    /// (from <c>Component.BuildStyles()</c>) are applied after and therefore take priority.
    /// </summary>
    public StyleSheet? GlobalStyles { get; private set; }

    // Overlay system
    private readonly List<VisualElement> _overlays = new();
    public IReadOnlyList<VisualElement> Overlays => _overlays;

    // Event manager access for focus management
    public EventManager EventManager => _eventManager;

    // Rendering optimization
    private bool _enableVSync = true;

    private const int ColorSchemePollIntervalMs = 5000;
    private long _nextColorSchemePollAtMs = Environment.TickCount64 + ColorSchemePollIntervalMs;

    private double _targetFrameTime = 1.0 / 60.0; // 60 FPS
    private double _accumulatedTime = 0;
    private bool _continuousRendering = false;
    private int _idleFrameCount = 0;
    private const int MaxIdleFrames = 5; // After 5 idle frames, reduce FPS
    private bool _isInIdleMode = false;
    private bool _isExiting = false;

    // FPS tracking — updated every second from OnRender deltaTime.
    private int _fpsFrameCount = 0;
    private double _fpsAccumulator = 0.0;
    private float _currentFps = 0f;
    private float _currentFrameTimeMs = 0f;
    private long _runtimeStatsWindowStart = Stopwatch.GetTimestamp();
    private int _runtimeUpdateCount = 0;
    private int _runtimeRenderCount = 0;
    private int _runtimePresentCount = 0;
    private RuntimeLoopStats _runtimeLoopStats;

    // Per-frame phase timings (ms) fed into PerformanceTracker.
    private float _phaseEventMs = 0f;
    private float _phaseLayoutMs = 0f;
    private float _phaseRenderMs = 0f;

    /// <summary>
    /// Shows or hides the built-in FPS counter overlay in the top-right corner.
    /// <summary>The most recently measured frames per second.</summary>
    public float CurrentFps => _currentFps;

    public UITree Tree => _tree;
    public IRenderer? Renderer => _renderer;
    public HotReloadManager HotReload => _hotReload ??= new HotReloadManager(this);
    public GL? GL => _gl;
    public IGraphicsContext? GraphicsContext => _graphicsContext;

    /// <summary>
    /// Called each frame after <see cref="IRenderer.EndFrame"/> to present the rendered content to the screen.
    /// Wired by the hosting layer (e.g. <c>SkiaSharpGLPresenter</c> in <c>Rayo.Hosting.Desktop</c>).
    /// </summary>
    public Action<int, int>? WindowPresenter { get; set; }

    /// <summary>
    /// Called on shutdown to release any resources owned by <see cref="WindowPresenter"/>.
    /// </summary>
    public Action? DisposeWindowPresenter { get; set; }

    /// <summary>
    /// Gets the application's asset manager for accessing registered fonts, images, and other resources.
    /// </summary>
    public AssetManager Assets => AssetManager.Instance;

    /// <summary>
    /// Gets the underlying Silk.NET window.
    /// </summary>
    public IWindow? NativeWindow => _window;

    // Window dimensions for component access
    public struct WindowDimensions
    {
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public WindowDimensions Window => new WindowDimensions
    {
        Width = _window?.Size.X ?? 800,
        Height = _window?.Size.Y ?? 600
    };

    public readonly record struct RuntimeLoopStats(
        float UpdatesPerSecond,
        float RendersPerSecond,
        float PresentsPerSecond,
        float RecommendedTickerFps,
        bool IsInIdleMode,
        bool ContinuousRendering);

    public RuntimeLoopStats CurrentRuntimeLoopStats
    {
        get
        {
            RefreshRuntimeLoopStats();
            return _runtimeLoopStats;
        }
    }

    /// <summary>
    /// Current window width in logical pixels. Used by <see cref="Rayo.Styling.BreakpointHelper"/>
    /// to evaluate responsive breakpoints in style rules.
    /// </summary>
    public float WindowWidth => _lastPolledSize.X > 0 ? _lastPolledSize.X : _window?.Size.X ?? 1024f;

    /// <summary>
    /// Current window height in logical pixels. Used by <see cref="Rayo.Styling.OrientationHelper"/>
    /// to determine portrait vs. landscape orientation.
    /// </summary>
    public float WindowHeight => _lastPolledSize.Y > 0 ? _lastPolledSize.Y : _window?.Size.Y ?? 768f;

    /// <summary>
    /// The currently active <see cref="Rayo.Styling.ThemeData"/>.
    /// Defaults to <see cref="Rayo.Styling.RayoThemes.Light"/>.
    /// </summary>
    public ThemeData ActiveTheme { get; private set; } = _fallbackTheme;

    public ThemeMode ThemeMode { get; private set; } = ThemeMode.Light;
    public HostThemePreferences HostThemePreferences { get; private set; } =
        HostThemePreferences.Default;

    internal static ThemeData FallbackTheme => _fallbackTheme;

    internal static void SetFallbackTheme(ThemeData theme)
    {
        _fallbackTheme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    /// <summary>
    /// Fired after <see cref="UseTheme"/> switches the active theme, so that already-built
    /// <see cref="Component"/> subtrees can re-apply styles with the new token values.
    /// </summary>
    public static event Action<ThemeData>? ThemeChanged;

    /// <summary>
    /// Sets the active theme at runtime. Fires <see cref="ThemeChanged"/> so that
    /// <see cref="Component"/> instances can re-apply their styles with the new token values.
    /// </summary>
    public UIApplication UseTheme(ThemeData theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ActiveTheme = theme;
        ThemeMode = theme.Brightness == ThemeBrightness.Dark
            ? ThemeMode.Dark
            : ThemeMode.Light;
        SetFallbackTheme(theme);
        NotifyThemeChanged(theme);

        _tree.Root?.NotifyThemeChanged(theme);
        foreach (var overlay in _overlays)
            overlay.NotifyThemeChanged(theme);
        foreach (var overlay in _tree.Overlays)
        {
            if (!_overlays.Contains(overlay))
                overlay.NotifyThemeChanged(theme);
        }

        return this;
    }

    /// <summary>
    /// Selects a built-in theme mode. System mode follows the live host color preference.
    /// </summary>
    public UIApplication UseThemeMode(ThemeMode mode)
    {
        if (mode == ThemeMode.System)
        {
            return UseSystemPreferences(PreferredColorSchemeHelper.CurrentHostPreferences);
        }

        UseTheme(mode == ThemeMode.Dark ? RayoThemes.Dark : RayoThemes.Light);
        ThemeMode = mode;
        return this;
    }

    /// <summary>Applies live appearance and accessibility preferences from a platform host.</summary>
    public UIApplication UseSystemPreferences(HostThemePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        preferences.Validate();
        HostThemePreferences = preferences;
        UseTheme(RayoThemes.ResolveSystem(preferences));
        ThemeMode = ThemeMode.System;
        return this;
    }

    /// <summary>
    /// Loads a JSON theme and applies future valid file updates atomically on the UI thread.
    /// Invalid updates leave the last valid theme active.
    /// </summary>
    public ThemeHotReloadWatcher WatchThemeFile(
        string path,
        Func<string, ThemeData?>? baseThemeResolver = null,
        Action<string>? onLoadFailed = null,
        ThemeJsonRegistry? registry = null)
    {
        var watcher = new ThemeHotReloadWatcher(path, baseThemeResolver, registry);
        watcher.ThemeChanged += theme => RunOnMainThread(() => UseTheme(theme));
        if (onLoadFailed != null)
            watcher.LoadFailed += error => RunOnMainThread(() => onLoadFailed(error));
        _themeWatchers.Add(watcher);
        UseTheme(watcher.Current);
        return watcher;
    }

    private void OnHostPreferencesChanged(HostThemePreferences preferences)
    {
        if (ThemeMode != ThemeMode.System)
            return;

        UseSystemPreferences(preferences);
    }

    internal static void NotifyThemeChanged(ThemeData theme)
    {
        ThemeChanged?.Invoke(theme);
    }

    /// <summary>
    /// Raised whenever <see cref="GlobalStyles"/> changes, so that already-built
    /// <see cref="Component"/> subtrees can re-apply styles without a full rebuild.
    /// </summary>
    public static event Action<StyleSheet>? GlobalStylesChanged;

    public Vector2D<float> GetMousePosition()
    {
        // Mouse position tracking - placeholder implementation
        // Components should track their own hover state via IHoverable
        return new Vector2D<float>(0, 0);
    }

    public event Action? OnGLInitialized;
    
    /// <summary>
    /// Event triggered every frame update.
    /// Provides the time elapsed since last frame in seconds.
    /// </summary>
    public event Action<float>? Updated;

    /// <summary>
    /// Allows injecting a custom graphics context before initializing the application.
    /// Must be called before Run() or RunManual().
    /// </summary>
    public void SetGraphicsContext(IGraphicsContext context)
    {
        if (_renderer != null)
        {
            throw new InvalidOperationException("Cannot change graphics context after initialization");
        }
        _graphicsContext = context;
    }

    /// <summary>
    /// Sets the UI Builder to use for the application.
    /// This automatically enables Hot Reload support for the builder.
    /// </summary>
    /// <typeparam name="T">The type of the UI Builder.</typeparam>
    public void SetUI<T>() where T : IUIBuilder, new()
    {
        HotReload.Enable<T>();
    }

    /// <summary>
    /// Configures application assets (fonts, images, etc.) using a fluent builder pattern.
    /// Similar to MAUI's asset configuration system.
    /// </summary>
    /// <param name="configure">Action to configure assets</param>
    /// <returns>The application instance for chaining</returns>
    /// <example>
    /// <code>
    /// app.ConfigureAssets(assets =>
    /// {
    ///     assets.AddFont("Assets/Fonts/Roboto-Regular.ttf", "Roboto");
    ///     assets.AddFont("Assets/Fonts/Lineicons.ttf", "Icons", 24);
    ///     assets.AddImage("Assets/Images/logo.png", "Logo");
    ///     assets.SetDefaultFont("Roboto");
    /// });
    /// </code>
    /// </example>
    public UIApplication ConfigureAssets(Action<AssetConfiguration> configure)
    {
        var configuration = new AssetConfiguration(AssetManager.Instance);
        configure(configuration);
        return this;
    }

    /// <summary>
    /// Configures the application to use the specified service provider for dependency injection.
    /// Services will be automatically injected into properties marked with [Inject] attribute.
    /// </summary>
    /// <param name="services">The service collection to build the provider from</param>
    /// <returns>The application instance for chaining</returns>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddSingleton&lt;IMyService, MyService&gt;();
    ///
    /// app.UseServiceProvider(services);
    /// </code>
    /// </example>
    public UIApplication UseServiceProvider(IServiceCollection services)
    {
        ServiceProvider = services.BuildServiceProvider();
        return this;
    }

    /// <summary>
    /// Configures the application to use an existing service provider for dependency injection.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use</param>
    /// <returns>The application instance for chaining</returns>
    public UIApplication UseServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        return this;
    }

    /// <summary>
    /// Sets application-level styles that are applied to every <see cref="Component"/> subtree.
    /// Use this to define a global theme. Component-level styles take priority over these.
    /// </summary>
    /// <example>
    /// <code>
    /// app.UseGlobalStyles(
    /// [
    ///     new Style&lt;Button&gt;().Height(36).BorderRadius(8),
    /// ]);
    /// </code>
    /// </example>
    public UIApplication UseGlobalStyles(StyleSheet styles)
    {
        GlobalStyles = styles;
        GlobalStylesChanged?.Invoke(styles);
        return this;
    }

    /// <summary>
    /// Enables or disables continuous rendering.
    /// If false, only renders when there are changes (more efficient).
    /// </summary>
    public bool ContinuousRendering
    {
        get => _continuousRendering;
        set => _continuousRendering = value;
    }

    /// <summary>
    /// Enables or disables VSync.
    /// </summary>
    public bool EnableVSync
    {
        get => _enableVSync;
        set
        {
            _enableVSync = value;
            if (_window != null)
            {
                _window.VSync = value;
            }
        }
    }

    /// <summary>
    /// Target FPS when VSync is disabled.
    /// </summary>
    public double TargetFPS
    {
        get => 1.0 / _targetFrameTime;
        set => _targetFrameTime = 1.0 / value;
    }

    /// <summary>
    /// Creates a new UIApplication with basic parameters.
    /// </summary>
    public UIApplication(string title = "Rayo Application", int width = 800, int height = 600)
        : this(new WindowConfiguration(title, width, height))
    {
    }

    /// <summary>
    /// Creates a new UIApplication with full window configuration.
    /// Platform-specific options are only applied when running on the target platform.
    /// Call Initialize() after configuring the application to create the window.
    /// </summary>
    /// <param name="config">The window configuration.</param>
    public UIApplication(WindowConfiguration config)
    {
        Current = this;
        _windowConfig = config;
        _lastPolledSize = new Vector2D<int>(config.Width, config.Height);
        _tree = new UITree();
        _tree.OnNeedsRenderChanged = OnTreeNeedsRender;
        _eventManager = new EventManager(_tree, this);
        PreferredColorSchemeHelper.HostPreferencesChanged += OnHostPreferencesChanged;

        _enableVSync = config.VSync;
        _targetFrameTime = 1.0 / config.TargetFPS;
    }

    private bool HasActiveAnimationWork =>
        AnimationManager.Instance.HasActiveAnimations || FrameAnimationTicker.HasActiveAnimations;

    private void WakeRenderLoop()
    {
        if (_isInIdleMode)
        {
            _isInIdleMode = false;
            _idleFrameCount = 0;
        }
    }

    private void PollColorSchemeIfDue()
    {
        long nowMs = Environment.TickCount64;
        if (nowMs < _nextColorSchemePollAtMs)
        {
            return;
        }

        _nextColorSchemePollAtMs = nowMs + ColorSchemePollIntervalMs;
        Rayo.Styling.PreferredColorSchemeHelper.NotifyIfChanged();
    }

    private void WaitForNextFrame(double remainingMs)
    {
        if (remainingMs <= 0)
        {
            return;
        }

        if (remainingMs > 5)
        {
            Thread.Sleep((int)(remainingMs / 2));
        }
        else if (remainingMs > 1)
        {
            Thread.Yield();
        }
        else if (remainingMs > 0.1)
        {
            Thread.SpinWait(100);
        }
    }

    private double GetTargetFrameMilliseconds()
    {
        const double targetFps = 60.0;
        const double targetFrameMs = 1000.0 / targetFps;
        const double idleFrameMs = 33.0;

        return _isInIdleMode ? idleFrameMs : targetFrameMs;
    }

    private void RecordUpdatePass()
    {
        _runtimeUpdateCount++;
        RefreshRuntimeLoopStats();
    }

    private void RecordRenderPass()
    {
        _runtimeRenderCount++;
        RefreshRuntimeLoopStats();
    }

    private void RecordPresentPass()
    {
        _runtimePresentCount++;
        RefreshRuntimeLoopStats();
    }

    private void RefreshRuntimeLoopStats()
    {
        long now = Stopwatch.GetTimestamp();
        double elapsedSeconds = (now - _runtimeStatsWindowStart) / (double)Stopwatch.Frequency;
        if (elapsedSeconds < 1.0)
        {
            return;
        }

        _runtimeLoopStats = new RuntimeLoopStats(
            UpdatesPerSecond: (float)(_runtimeUpdateCount / elapsedSeconds),
            RendersPerSecond: (float)(_runtimeRenderCount / elapsedSeconds),
            PresentsPerSecond: (float)(_runtimePresentCount / elapsedSeconds),
            RecommendedTickerFps: FrameAnimationTicker.RecommendedFps,
            IsInIdleMode: _isInIdleMode,
            ContinuousRendering: _continuousRendering);

        _runtimeStatsWindowStart = now;
        _runtimeUpdateCount = 0;
        _runtimeRenderCount = 0;
        _runtimePresentCount = 0;
    }

    /// <summary>
    /// Initializes the window with the configured settings.
    /// Must be called after all application configuration (SetUI, ConfigureAssets, etc.) is complete.
    /// </summary>
    public void Initialize()
    {
        if (_window != null)
        {
            throw new InvalidOperationException("Window has already been initialized");
        }

        var config = _windowConfig;
        var options = WindowOptions.Default;
        options.Title = config.Title;
        options.Size = new Vector2D<int>(config.Width, config.Height);
        options.VSync = config.VSync;
        options.UpdatesPerSecond = config.TargetFPS;
        options.FramesPerSecond = config.TargetFPS;
        options.Samples = config.Samples;
        options.API = _graphicsContext?.RequiresNativeWindow == true
            ? GraphicsAPI.None
            : new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(3, 3));

        // Apply window state
        options.WindowState = config.WindowState switch
        {
            Platform.WindowState.Maximized => Silk.NET.Windowing.WindowState.Maximized,
            Platform.WindowState.Minimized => Silk.NET.Windowing.WindowState.Minimized,
            Platform.WindowState.FullScreen => Silk.NET.Windowing.WindowState.Fullscreen,
            _ => Silk.NET.Windowing.WindowState.Normal
        };

        // Apply decorations
        options.WindowBorder = config.SystemDecorations switch
        {
            SystemDecorations.None => WindowBorder.Hidden,
            SystemDecorations.BorderOnly => WindowBorder.Fixed,
            _ => config.CanResize ? WindowBorder.Resizable : WindowBorder.Fixed
        };

        // Apply transparent background
        options.TransparentFramebuffer = config.TransparentBackground;

        // Apply topmost
        options.TopMost = config.Topmost;

        // Apply position (if manual)
        if (config.StartupLocation == WindowStartupLocation.Manual && config.X.HasValue && config.Y.HasValue)
        {
            options.Position = new Vector2D<int>(config.X.Value, config.Y.Value);
        }

        // Centering only applies to Normal windows. Setting Position on a Maximized/
        // Fullscreen window restores it to Normal on GLFW/Win32, which previously
        // cancelled WindowState.Maximized when StartupLocation was CenterScreen.
        var shouldCenterOnLoad =
            config.StartupLocation == WindowStartupLocation.CenterScreen
            && config.WindowState == Platform.WindowState.Normal;

        // Hide window initially if we need to center it (to avoid visible repositioning)
        if (shouldCenterOnLoad)
        {
            options.IsVisible = false;
        }

        _window = Silk.NET.Windowing.Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += HandleRenderCallback;
        _window.Resize += OnResize;
        _window.Closing += OnClosing;
        _window.Update += OnUpdate;
        _window.FocusChanged += OnFocusChanged;

        // Apply startup location after window creation
        if (shouldCenterOnLoad)
        {
            _window.Load += CenterWindowOnScreen;
        }
        else if (config.WindowState != Platform.WindowState.Normal)
        {
            // Some backends ignore maximize/fullscreen hints until after Load.
            // Re-apply the configured state once the native window is ready.
            _window.Load += ApplyConfiguredWindowState;
        }
    }

    private void ApplyConfiguredWindowState()
    {
        if (_window == null) return;

        WindowStateValue = _windowConfig.WindowState;
    }

    private void CenterWindowOnScreen()
    {
        if (_window == null) return;

        // Remember if window was initially hidden for centering
        bool wasHidden = !_window.IsVisible;

        // Setting Position restores Maximized/Fullscreen windows to Normal on GLFW/Win32.
        if (_window.WindowState == Silk.NET.Windowing.WindowState.Normal)
        {
            var monitor = _window.Monitor;
            if (monitor != null)
            {
                var bounds = monitor.Bounds;
                var windowSize = _window.Size;
                var x = bounds.Origin.X + (bounds.Size.X - windowSize.X) / 2;
                var y = bounds.Origin.Y + (bounds.Size.Y - windowSize.Y) / 2;
                _window.Position = new Vector2D<int>(x, y);
            }
        }

        // Show window after positioning (only if it was hidden for initial centering)
        if (wasHidden)
        {
            _window.IsVisible = true;
        }
    }

    /// <summary>
    /// Gets the current window configuration.
    /// </summary>
    public WindowConfiguration Configuration => _windowConfig;

    /// <summary>
    /// Gets or sets whether the window is topmost (always on top).
    /// </summary>
    public bool Topmost
    {
        get => _window?.TopMost ?? false;
        set
        {
            if (_window != null)
            {
                _window.TopMost = value;
            }
            _windowConfig.Topmost = value;
        }
    }

    /// <summary>
    /// Gets or sets the window state.
    /// </summary>
    public Platform.WindowState WindowStateValue
    {
        get
        {
            if (_window == null) return Platform.WindowState.Normal;
            return _window.WindowState switch
            {
                Silk.NET.Windowing.WindowState.Maximized => Platform.WindowState.Maximized,
                Silk.NET.Windowing.WindowState.Minimized => Platform.WindowState.Minimized,
                Silk.NET.Windowing.WindowState.Fullscreen => Platform.WindowState.FullScreen,
                _ => Platform.WindowState.Normal
            };
        }
        set
        {
            if (_window != null)
            {
                _window.WindowState = value switch
                {
                    Platform.WindowState.Maximized => Silk.NET.Windowing.WindowState.Maximized,
                    Platform.WindowState.Minimized => Silk.NET.Windowing.WindowState.Minimized,
                    Platform.WindowState.FullScreen => Silk.NET.Windowing.WindowState.Fullscreen,
                    _ => Silk.NET.Windowing.WindowState.Normal
                };
            }
            _windowConfig.WindowState = value;
        }
    }

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string Title
    {
        get => _window?.Title ?? _windowConfig.Title;
        set
        {
            if (_window != null)
            {
                _window.Title = value;
            }
            _windowConfig.Title = value;
        }
    }

    /// <summary>
    /// Centers the window on the screen.
    /// </summary>
    public void CenterOnScreen()
    {
        CenterWindowOnScreen();
    }

    /// <summary>
    /// Moves the window to the specified position.
    /// </summary>
    public void SetPosition(int x, int y)
    {
        if (_window != null)
        {
            _window.Position = new Vector2D<int>(x, y);
        }
        _windowConfig.X = x;
        _windowConfig.Y = y;
    }

    /// <summary>
    /// Resizes the window to the specified size.
    /// </summary>
    public void SetSize(int width, int height)
    {
        if (_window != null)
        {
            _window.Size = new Vector2D<int>(width, height);
        }
        _windowConfig.Width = width;
        _windowConfig.Height = height;
    }

    public void AddOverlay(VisualElement overlay, VisualElement? owner = null)
    {
        if (!_overlays.Contains(overlay))
        {
            overlay.CaptureDetachedTheme(owner);
            _overlays.Add(overlay);
            _tree.AddOverlay(overlay);
        }
    }

    public void RemoveOverlay(VisualElement overlay)
    {
        if (_overlays.Remove(overlay))
        {
            _tree.RemoveOverlay(overlay);
        }
    }

    private void OnLoad()
    {
        if (_graphicsContext == null)
        {
            throw new InvalidOperationException("No IGraphicsContext configured. Call SetGraphicsContext() before Initialize().");
        }

        ApplyWindowIcon();

        // Let backend bootstrap with the native window (e.g. Vulkan needs IVkSurface).
        _graphicsContext.OnWindowCreated(_window!);

        // Only acquire an OpenGL context for GL-based backends.
        if (_graphicsContext.RequiresNativeWindow == false)
        {
            _gl = _window!.CreateOpenGL();
            _graphicsContext.OnGraphicsDeviceCreated(_gl);
        }

        // Create renderer from graphics context
        _renderer = _graphicsContext.CreateRenderer();
        _renderer.Initialize(_window!.Size.X, _window.Size.Y);

        var input = _window.CreateInput();
        _eventManager.AttachInput(input);

        // Initialize asset manager
        AssetManager.Instance.Initialize();

        // Seed the polled-size tracker so the first OnUpdate doesn't treat load as a resize.
        _lastPolledSize = _window!.Size;
        _lastPolledWindowState = _window.WindowState;

        // Notify that graphics context is initialized
        OnGLInitialized?.Invoke();
    }

    private void ApplyWindowIcon()
    {
        if (_window == null || _windowConfig.Icon is not { } icon)
        {
            return;
        }

        var rawIcon = new RawImage(icon.Width, icon.Height, icon.Pixels.AsMemory());
        _window.SetWindowIcon(ref rawIcon);
    }

    private void OnResize(Vector2D<int> size)
    {
        _lastPolledSize = size;
        ApplyWindowSize(size);
    }

    private void OnFocusChanged(bool focused)
    {
        if (!focused)
        {
            _eventManager.ClearHoverStates();
        }
    }

    private void ApplyWindowSize(Vector2D<int> size)
    {
        if (size.X > 0 && size.Y > 0)
        {
            _renderer?.Resize(size.X, size.Y);
            _tree.MarkNeedsLayout();
            Rayo.Styling.BreakpointHelper.NotifyIfChanged();
            Rayo.Styling.OrientationHelper.NotifyIfChanged();
            _tree.MarkNeedsRender();
        }
    }

    private void HandleWindowSizeOrStateChange()
    {
        if (_window == null) return;

        bool needsApply = false;
        Vector2D<int> sizeToApply = _lastPolledSize;

        var currentState = _window.WindowState;
        if (currentState != _lastPolledWindowState)
        {
            _lastPolledWindowState = currentState;
            sizeToApply = _window.Size;
            _lastPolledSize = sizeToApply;
            needsApply = true;
            // Maximized/restored transitions can report an intermediate size before
            // the native client area fully settles. Manual border resize naturally
            // gets multiple updates; state transitions often do not, so keep
            // revalidating for a few frames.
            _pendingWindowStateRelayoutFrames = 3;
        }

        var currentSize = _window.Size;
        if (currentSize != _lastPolledSize)
        {
            _lastPolledSize = currentSize;
            sizeToApply = currentSize;
            needsApply = true;
        }

        if (_pendingWindowStateRelayoutFrames > 0)
        {
            sizeToApply = currentSize;
            needsApply = true;
            _pendingWindowStateRelayoutFrames--;
        }

        if (needsApply)
        {
            ApplyWindowSize(sizeToApply);
        }
    }

    private void OnTreeNeedsRender()
    {
        WakeRenderLoop();
    }

    // Stopwatch timestamp recorded at the start of each OnUpdate call (for event-phase timing).
    private long _updatePhaseStart;

    private void OnUpdate(double deltaTime)
    {
        if (_isExiting) return;

        RecordUpdatePass();
        _updatePhaseStart = System.Diagnostics.Stopwatch.GetTimestamp();

        // Poll window size every frame to catch size/state changes that don't fire the Resize event.
        HandleWindowSizeOrStateChange();

        PollColorSchemeIfDue();

        // Process actions on main thread (includes performance monitor callbacks)
        while (_mainThreadActions.TryDequeue(out var action))
        {
            action();
        }

        // Check if we need to exit idle mode (window property updates)
        // This is safe to do here in OnUpdate
        if (!_isInIdleMode && _window != null && _window.UpdatesPerSecond != 60)
        {
            _window.UpdatesPerSecond = 60;
            _window.FramesPerSecond = 60;
        }

        // Process automatic key repeat
        // CRITICAL: ProcessKeyRepeat returns true if a key is being tracked for repeat
        bool hasKeyRepeat = _eventManager.ProcessKeyRepeat();

        // Notify subscribers of update (e.g. ToastManager)
        Updated?.Invoke((float)deltaTime);

        // Advance animations (expects delta in milliseconds)
        AnimationManager.Instance.Update((float)(deltaTime * 1000.0));

        // Tick per-frame animation subscribers (delta in seconds)
        FrameAnimationTicker.Tick((float)deltaTime);

        // Record time spent in event/animation phase (everything before layout).
        _phaseEventMs = ElapsedMsSince(ref _updatePhaseStart);

        // Update tree (layout if needed)
        long _layoutStart = System.Diagnostics.Stopwatch.GetTimestamp();
        _tree.Update(_window!.Size.X, _window.Size.Y);

        // Update overlays layout
        // Create snapshot to avoid concurrent modification exception
        var overlaySnapshot = _overlays.ToList();
        foreach (var overlay in overlaySnapshot)
        {
            // Overlays always get the full window size to position themselves
            overlay.MeasureUpdate(_window!.Size.X, _window.Size.Y);

            // For overlays with Stretch alignment, use full window size
            // Otherwise use desired size and explicit position
            float x = overlay.X;
            float y = overlay.Y;
            float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? _window.Size.X : overlay.DesiredWidth;
            float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? _window.Size.Y : overlay.DesiredHeight;

            overlay.ArrangeUpdate(x, y, w, h);
        }

        // CRITICAL: Process pending UI updates HERE, after layout but BEFORE render
        // This allows event handlers to change signals without causing errors
        UIUpdateQueue.ProcessPendingUpdates();

        // If signals caused changes (MarkNeedsPaint was queued), we need another update
        // to recalculate layout if necessary
        _tree.Update(_window!.Size.X, _window.Size.Y);
        
        // Re-layout overlays if needed
        // Create snapshot to avoid concurrent modification exception
        overlaySnapshot = _overlays.ToList();
        foreach (var overlay in overlaySnapshot)
        {
            if (overlay.NeedsLayout)
            {
                overlay.MeasureUpdate(_window!.Size.X, _window.Size.Y);

                float x = overlay.X;
                float y = overlay.Y;
                float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? _window.Size.X : overlay.DesiredWidth;
                float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? _window.Size.Y : overlay.DesiredHeight;

                overlay.ArrangeUpdate(x, y, w, h);
            }
        }

        // Store layout phase timing (all measure/arrange work above).
        _phaseLayoutMs = ElapsedMsSince(ref _layoutStart);

        // CRITICAL: Do NOT enter idle mode if drag is active OR key repeat is active OR animations are running
        bool isDragActive = _eventManager.IsAnythingBeingDragged;
        bool hasActiveAnimations = HasActiveAnimationWork;

        // If no changes and not in continuous mode, increment idle counter
        if (!_tree.NeedsRender && !_continuousRendering && !isDragActive && !hasKeyRepeat && !hasActiveAnimations)
        {
            _idleFrameCount++;

            // Enter idle mode after MaxIdleFrames
            if (_idleFrameCount > MaxIdleFrames && !_isInIdleMode)
            {
                _isInIdleMode = true;
                // Reduce update frequency - 30 FPS is enough and keeps UI responsive
                _window!.UpdatesPerSecond = 30;
                _window.FramesPerSecond = 30;
            }
        }
        else
        {
            _idleFrameCount = 0;

            // Exit idle mode if there were changes OR drag is active OR animations are running
            if (_isInIdleMode)
            {
                _isInIdleMode = false;
                _window!.UpdatesPerSecond = 60;
                _window.FramesPerSecond = 60;
            }
        }
    }

    /// <summary>
    /// Returns elapsed milliseconds since <paramref name="startTimestamp"/> (a Stopwatch timestamp).
    /// Resets <paramref name="startTimestamp"/> to the current timestamp after reading.
    /// </summary>
    private static float ElapsedMsSince(ref long startTimestamp)
    {
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        float ms = (float)((now - startTimestamp) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
        startTimestamp = now;
        return ms;
    }

    // Timestamp recorded just before _tree.Render() in OnRender (for render-phase timing).
    private long _renderPhaseStart;

    /// <summary>
    /// Accumulates frame time samples and refreshes <see cref="_currentFps"/> once per second.
    /// Must be called on every rendered frame with the actual elapsed time.
    /// </summary>
    private void TrackFps(double deltaTime)
    {
        _fpsFrameCount++;
        _fpsAccumulator += deltaTime;
        _currentFrameTimeMs = (float)(deltaTime * 1000.0);

        if (_fpsAccumulator >= 1.0)
        {
            _currentFps = (float)(_fpsFrameCount / _fpsAccumulator);
            _fpsFrameCount = 0;
            _fpsAccumulator = 0.0;
        }
    }

    private void OnRender(double deltaTime)
    {
        if (_isExiting) return;

        // Adaptive rendering strategy
        bool shouldRender = _continuousRendering || _tree.NeedsRender;

        // Frame limiting if VSync is disabled
        if (!_enableVSync && shouldRender)
        {
            _accumulatedTime += deltaTime;
            if (_accumulatedTime < _targetFrameTime)
            {
                return; // Skip this frame
            }
            _accumulatedTime = 0;
        }

        // In deep idle mode, skip render completely
        // Window remains responsive because event loop is still active
        if (_isInIdleMode && !shouldRender)
        {
            return; // Do NOTHING of OpenGL in idle
        }

        // Only do render work if we are really going to render
        if (shouldRender || _idleFrameCount < MaxIdleFrames)
        {
            RecordRenderPass();
            TrackFps(deltaTime);

            _tree.NotifyRenderStarted();
            _renderer!.BeginFrame();
            _renderer.Clear(new Rayo.Rendering.Color(0.1f, 0.1f, 0.1f, 1.0f));

            // Render main tree (includes overlays via UITree)
            _renderPhaseStart = System.Diagnostics.Stopwatch.GetTimestamp();
            _tree.Render(_renderer, fullSurfaceCleared: true);
            _phaseRenderMs = ElapsedMsSince(ref _renderPhaseStart);

            // Commit performance data for this frame.
            Rayo.DevTools.PerformanceTracker.CommitFrame(
                _currentFps, _currentFrameTimeMs,
                _phaseLayoutMs, 0f,          // arrange is included in layout pass
                _phaseRenderMs, _phaseEventMs,
                CurrentRuntimeLoopStats);

            // Render DevTool highlight overlay (if DevTools enabled)
            Rayo.DevTools.DevToolExtensions.RenderDevToolOverlay(_renderer);

            _renderer.EndFrame();
            _tree.ClearRenderFlag();

            // Present rendered content to the screen via the hosting layer's presenter
            WindowPresenter?.Invoke(_window!.Size.X, _window!.Size.Y);
            RecordPresentPass();
        }
    }

    private void HandleRenderCallback(double deltaTime)
    {
        try
        {
            OnRender(deltaTime);
        }
        catch (Exception exception)
        {
            // Silk keeps the window marked as being inside DoRender until its
            // callback returns. Preserve the original exception and rethrow it
            // from the manual loop instead of letting Dispose mask it with
            // "You cannot call Reset inside of the render loop".
            _pendingRenderException = ExceptionDispatchInfo.Capture(exception);
        }
    }

    private void OnClosing()
    {
        _isExiting = true;
        _eventManager.Detach();
        _hotReload?.Dispose();
        _renderer?.Dispose();

        // Release presenter resources owned by the hosting layer
        DisposeWindowPresenter?.Invoke();

        _gl?.Dispose();
    }

    //public void Run()
    //{
    //    _window?.Run();
    //}

    /// <summary>
    /// Runs the application with a manual loop that allows full control over rendering.
    /// Perfect for retained mode UIs where you only want to render when there are changes.
    /// Automatically calls Initialize() if the window hasn't been created yet.
    /// </summary>
    public void Run()
    {
        // Initialize window if not already done
        if (_window == null)
        {
            Initialize();
        }

        var window = _window ?? throw new InvalidOperationException("Window initialization failed.");
        window.Initialize();

        // Use Stopwatch for more precise timing than DateTime
        var stopwatch = Stopwatch.StartNew();
        long lastFrameTime = 0;

        //Console.WriteLine("[Manual Loop] Started - Full control of render loop");
        //Console.WriteLine("[Manual Loop] Window only renders when there are changes");

        while (!_window.IsClosing && !_isExiting)
        {
            long currentTime = stopwatch.ElapsedMilliseconds;
            double elapsed = currentTime - lastFrameTime;

            double targetMs = GetTargetFrameMilliseconds();

            // Frame pacing: Only process if enough time has passed
            if (elapsed >= targetMs)
            {
                // === PHASE 1: EVENT PROCESSING ===
                _window.DoEvents();
                if (_isExiting || _window.IsClosing) break;
                RecordUpdatePass();

                // Poll window size/state to catch maximize/restore on Windows.
                HandleWindowSizeOrStateChange();

                PollColorSchemeIfDue();

                // Process automatic key repeat
                bool hasKeyRepeat = _eventManager.ProcessKeyRepeat();

                // Process queued actions (batching)
                int actionsProcessed = 0;
                while (_mainThreadActions.TryDequeue(out var action) && actionsProcessed < 100)
                {
                    action();
                    actionsProcessed++;
                }
                if (_isExiting || _window.IsClosing) break;

                // Fire Updated event for time-dependent components (like Toasts)
                Updated?.Invoke((float)(elapsed / 1000.0));

                // Advance animations in manual loop
                AnimationManager.Instance.Update((float)elapsed);
                FrameAnimationTicker.Tick((float)(elapsed / 1000.0));

                // === PHASE 2: LAYOUT ===
                _tree.Update(_window.Size.X, _window.Size.Y);

                // Update overlays layout
                // Create snapshot to avoid concurrent modification exception
                var overlaySnapshot = _overlays.ToList();
                foreach (var overlay in overlaySnapshot)
                {
                    overlay.MeasureUpdate(_window.Size.X, _window.Size.Y);

                    float x = overlay.X;
                    float y = overlay.Y;
                    float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? _window.Size.X : overlay.DesiredWidth;
                    float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? _window.Size.Y : overlay.DesiredHeight;

                    overlay.ArrangeUpdate(x, y, w, h);
                }

                // === PHASE 2B: PROCESS PENDING UI UPDATES ===
                // CRITICAL: Process pending UI updates after layout
                UIUpdateQueue.ProcessPendingUpdates();
                if (_isExiting || _window.IsClosing) break;

                // If signals caused changes, do another update to recalculate layout
                _tree.Update(_window.Size.X, _window.Size.Y);
                
                // Re-layout overlays if needed
                // Create snapshot to avoid concurrent modification exception
                overlaySnapshot = _overlays.ToList();
                foreach (var overlay in overlaySnapshot)
                {
                    if (overlay.NeedsLayout)
                    {
                        overlay.MeasureUpdate(_window.Size.X, _window.Size.Y);

                        float x = overlay.X;
                        float y = overlay.Y;
                        float w = overlay.HorizontalAlignment == HorizontalAlignment.Stretch ? _window.Size.X : overlay.DesiredWidth;
                        float h = overlay.VerticalAlignment == VerticalAlignment.Stretch ? _window.Size.Y : overlay.DesiredHeight;

                        overlay.ArrangeUpdate(x, y, w, h);
                    }
                }

                // === PHASE 3: IDLE MODE MANAGEMENT ===
                // CRITICAL: Do NOT enter idle mode if drag is active OR key repeat is active
                bool isDragActive = _eventManager.IsAnythingBeingDragged;

                bool hasActiveAnimations = HasActiveAnimationWork;

                if (!_tree.NeedsRender && !_continuousRendering && !isDragActive && !hasKeyRepeat && !hasActiveAnimations)
                {
                    _idleFrameCount++;
                    if (_idleFrameCount > MaxIdleFrames && !_isInIdleMode)
                    {
                        _isInIdleMode = true;
                        //Console.WriteLine("[Manual Loop] ⏸ IDLE mode activated (30 FPS)");
                    }
                }
                else
                {
                    if (_isInIdleMode)
                    {
                        _isInIdleMode = false;
                        //Console.WriteLine("[Manual Loop] ▶ ACTIVE mode (60 FPS)");
                    }
                    _idleFrameCount = 0;
                }

                // === PHASE 4: RENDERING ===
                if ((_tree.NeedsRender || _continuousRendering || _idleFrameCount < MaxIdleFrames) && !_isExiting && !_window.IsClosing)
                {
                    _window.DoRender();
                    var renderException = _pendingRenderException;
                    _pendingRenderException = null;
                    renderException?.Throw();
                    _tree.ClearRenderFlag();
                }

                lastFrameTime = currentTime;
            }
            else
            {
                WaitForNextFrame(targetMs - elapsed);
            }
        }
    }

    public void RunOnMainThread(Action action)
    {
        _mainThreadActions.Enqueue(action);
    }

    /// <summary>
    /// Renders an element and all its children recursively.
    /// This is needed for overlays which are not part of the main UITree.
    /// </summary>
    private void RenderElementRecursive(VisualElement element, IRenderer renderer)
    {
        if (!element.IsVisible) return;

        bool hasOpacity = element.Opacity < 1f;
        if (hasOpacity)
        {
            renderer.PushOpacity(element.Opacity);
        }

        try
        {
            // Render the element itself
            element.InvokeOnBeforeRender(renderer);
            element.Render(renderer);

            // Render children recursively
            if (!element.RendersChildrenManually)
            {
                foreach (var child in element.GetChildrenByZIndex())
                {
                    RenderElementRecursive(child, renderer);
                }
            }

            element.InvokeOnAfterRender(renderer);
        }
        finally
        {
            if (hasOpacity)
            {
                renderer.PopOpacity();
            }
        }
    }

    public void Dispose()
    {
        PreferredColorSchemeHelper.HostPreferencesChanged -= OnHostPreferencesChanged;
        foreach (var watcher in _themeWatchers)
            watcher.Dispose();
        _themeWatchers.Clear();
        if (Current == this) Current = null;
        _window?.Dispose();
    }

    public void Exit()
    {
        _isExiting = true;
        _window?.Close();
    }
}
