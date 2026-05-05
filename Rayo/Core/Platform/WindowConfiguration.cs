namespace Rayo.Core.Platform;

/// <summary>
/// Configuration options for the application window.
/// Platform-specific options are only applied when running on the target platform.
/// </summary>
public class WindowConfiguration
{
    /// <summary>
    /// The window title.
    /// </summary>
    public string Title { get; set; } = "Rayo Application";

    /// <summary>
    /// The initial window width.
    /// </summary>
    public int Width { get; set; } = 800;

    /// <summary>
    /// The initial window height.
    /// </summary>
    public int Height { get; set; } = 600;

    /// <summary>
    /// The minimum window width. Set to 0 for no minimum.
    /// </summary>
    public int MinWidth { get; set; } = 0;

    /// <summary>
    /// The minimum window height. Set to 0 for no minimum.
    /// </summary>
    public int MinHeight { get; set; } = 0;

    /// <summary>
    /// The maximum window width. Set to 0 for no maximum.
    /// </summary>
    public int MaxWidth { get; set; } = 0;

    /// <summary>
    /// The maximum window height. Set to 0 for no maximum.
    /// </summary>
    public int MaxHeight { get; set; } = 0;

    /// <summary>
    /// The initial X position of the window. Only used when StartupLocation is Manual.
    /// </summary>
    public int? X { get; set; }

    /// <summary>
    /// The initial Y position of the window. Only used when StartupLocation is Manual.
    /// </summary>
    public int? Y { get; set; }

    /// <summary>
    /// The initial startup location of the window.
    /// </summary>
    public WindowStartupLocation StartupLocation { get; set; } = WindowStartupLocation.CenterScreen;

    /// <summary>
    /// The initial state of the window.
    /// </summary>
    public WindowState WindowState { get; set; } = WindowState.Normal;

    /// <summary>
    /// Whether the window can be resized.
    /// </summary>
    public bool CanResize { get; set; } = true;

    /// <summary>
    /// Whether the window should be topmost (always on top).
    /// </summary>
    public bool Topmost { get; set; } = false;

    /// <summary>
    /// Whether to show the window border and decorations.
    /// </summary>
    public SystemDecorations SystemDecorations { get; set; } = SystemDecorations.Full;

    /// <summary>
    /// Whether the window background is transparent.
    /// </summary>
    public bool TransparentBackground { get; set; } = false;

    /// <summary>
    /// The window background color (ARGB format).
    /// </summary>
    public uint BackgroundColor { get; set; } = 0xFF1E1E1E;

    /// <summary>
    /// Whether VSync is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// The MSAA sample count for anti-aliasing (0, 2, 4, 8, etc.).
    /// </summary>
    public int Samples { get; set; } = 4;

    /// <summary>
    /// Target frames per second.
    /// </summary>
    public int TargetFPS { get; set; } = 60;

    /// <summary>
    /// Windows-specific options.
    /// </summary>
    public WindowsPlatformOptions Windows { get; } = new();

    /// <summary>
    /// Linux-specific options.
    /// </summary>
    public LinuxPlatformOptions Linux { get; } = new();

    /// <summary>
    /// macOS-specific options.
    /// </summary>
    public MacOSPlatformOptions MacOS { get; } = new();

    /// <summary>
    /// Android-specific options.
    /// </summary>
    public AndroidPlatformOptions Android { get; } = new();

    /// <summary>
    /// iOS-specific options.
    /// </summary>
    public iOSPlatformOptions iOS { get; } = new();

    /// <summary>
    /// Creates a new WindowConfiguration with default values.
    /// </summary>
    public WindowConfiguration() { }

    /// <summary>
    /// Creates a new WindowConfiguration with specified title and size.
    /// </summary>
    public WindowConfiguration(string title, int width = 800, int height = 600)
    {
        Title = title;
        Width = width;
        Height = height;
    }
}
