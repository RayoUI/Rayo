using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Adapter that wraps Rayo's WindowConfiguration to implement the platform-agnostic interface.
/// </summary>
internal class DesktopWindowConfiguration : IPlatformWindowConfiguration
{
    private readonly WindowConfiguration _config;

    public DesktopWindowConfiguration(WindowConfiguration config)
    {
        _config = config;
    }

    public string Title
    {
        get => _config.Title;
        set => _config.Title = value;
    }

    public int Width
    {
        get => _config.Width;
        set => _config.Width = value;
    }

    public int Height
    {
        get => _config.Height;
        set => _config.Height = value;
    }

    public bool CanResize
    {
        get => _config.CanResize;
        set => _config.CanResize = value;
    }

    public bool VSync
    {
        get => _config.VSync;
        set => _config.VSync = value;
    }

    public int Samples
    {
        get => _config.Samples;
        set => _config.Samples = value;
    }

    /// <summary>
    /// Gets the underlying WindowConfiguration for desktop-specific usage.
    /// </summary>
    public WindowConfiguration NativeConfiguration => _config;
}
