using Rayo.Hosting.Abstractions;
using System.Runtime.InteropServices;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Desktop platform capabilities.
/// </summary>
internal class DesktopPlatformCapabilities : IPlatformCapabilities
{
    public string PlatformName { get; }
    
    public bool SupportsWindowing => true;
    
    public bool SupportsHotReload => true;
    
    public bool IsMobile => false;
    
    public float DpiScale { get; set; } = 1.0f;

    public DesktopPlatformCapabilities()
    {
        PlatformName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
            : "Unknown";
    }
}
