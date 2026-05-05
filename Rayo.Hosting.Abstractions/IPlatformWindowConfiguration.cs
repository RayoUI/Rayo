namespace Rayo.Hosting.Abstractions;

/// <summary>
/// Platform-agnostic window configuration.
/// Platform-specific implementations will map these to their native equivalents.
/// </summary>
public interface IPlatformWindowConfiguration
{
    string Title { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    bool CanResize { get; set; }
    bool VSync { get; set; }
    int Samples { get; set; }
}
