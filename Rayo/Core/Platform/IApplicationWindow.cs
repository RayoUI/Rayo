namespace Rayo.Core.Platform;

/// <summary>
/// Runtime window control surface for components and application code.
/// Mutations apply to the live native window when it exists, and always
/// update <see cref="WindowConfiguration"/>.
/// </summary>
public interface IApplicationWindow
{
    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Gets or sets the window state (normal, maximized, minimized, fullscreen).
    /// </summary>
    WindowState State { get; set; }

    /// <summary>
    /// Gets or sets whether the window stays above other windows.
    /// </summary>
    bool Topmost { get; set; }

    /// <summary>
    /// Gets or sets whether the user can resize the window.
    /// Combined with <see cref="SystemDecorations"/> to choose the native border style.
    /// </summary>
    bool CanResize { get; set; }

    /// <summary>
    /// Gets or sets the system chrome / decorations.
    /// </summary>
    SystemDecorations SystemDecorations { get; set; }

    /// <summary>
    /// Gets or sets the window width in pixels.
    /// </summary>
    int Width { get; set; }

    /// <summary>
    /// Gets or sets the window height in pixels.
    /// </summary>
    int Height { get; set; }

    /// <summary>
    /// Gets or sets the window X position.
    /// </summary>
    int X { get; set; }

    /// <summary>
    /// Gets or sets the window Y position.
    /// </summary>
    int Y { get; set; }

    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// </summary>
    bool VSync { get; set; }

    /// <summary>
    /// Gets or sets whether the window is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Resizes the window.
    /// </summary>
    void SetSize(int width, int height);

    /// <summary>
    /// Moves the window to the specified screen position.
    /// </summary>
    void SetPosition(int x, int y);

    /// <summary>
    /// Centers the window on the current monitor. No-op when maximized/fullscreen.
    /// </summary>
    void Center();

    /// <summary>
    /// Sets the desktop window icon from RGBA pixel data.
    /// </summary>
    void SetIcon(WindowIcon icon);

    /// <summary>
    /// Android-specific chrome options. Mutations update configuration and are
    /// applied by the Android host when registered.
    /// </summary>
    IAndroidApplicationWindow Android { get; }

    /// <summary>
    /// iOS-specific chrome options. Mutations update configuration and are
    /// applied by the iOS host when registered.
    /// </summary>
    IiOSApplicationWindow iOS { get; }

    /// <summary>
    /// Current system safe-area insets in logical pixels (status bar, notch, cutout, nav bars).
    /// Empty (<c>0</c>) on desktop or when no mobile host is registered.
    /// Use <c>SafeArea.Top</c> for the top inset.
    /// </summary>
    SafeAreaInsets SafeArea { get; }
}
