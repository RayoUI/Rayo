namespace Rayo.Core.Platform;

/// <summary>
/// Screen pixel-density tier, equivalent to the CSS
/// <c>resolution</c> / <c>-webkit-device-pixel-ratio</c> media feature.
/// </summary>
public enum ScreenDensity
{
    /// <summary>Below 96 DPI — low-density display.</summary>
    Low,
    /// <summary>96–143 DPI — standard 1× display.</summary>
    Normal,
    /// <summary>144–191 DPI — high-density / 1.5× display.</summary>
    High,
    /// <summary>192 DPI and above — extra-high / Retina display.</summary>
    ExtraHigh,
}
