using Rayo.Hosting.Abstractions;

namespace Rayo.Hosting.Android;

/// <summary>
/// Android platform capabilities.
/// </summary>
internal class AndroidPlatformCapabilities : IPlatformCapabilities
{
    public string PlatformName => "Android";
    
    public bool SupportsWindowing => false;
    
    public bool SupportsHotReload => false;
    
    public bool IsMobile => true;
    
    public float DpiScale { get; set; } = 1.0f;
}
