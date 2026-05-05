using Rayo.Hosting.Abstractions;
using Rayo.Core.Platform;

namespace Rayo.Hosting.Android;

/// <summary>
/// Android window configuration adapter.
/// On Android, "window" refers to the full-screen surface.
/// </summary>
public class AndroidWindowConfiguration : IPlatformWindowConfiguration
{
    private readonly WindowConfiguration _config;

    public AndroidWindowConfiguration(WindowConfiguration config)
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
    /// Gets the underlying WindowConfiguration for Android-specific usage.
    /// </summary>
    public WindowConfiguration NativeConfiguration => _config;
}
