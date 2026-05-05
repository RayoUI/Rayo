using System.Runtime.InteropServices;

namespace Rayo.Core.Platform;

/// <summary>
/// Detects the current runtime platform.
/// </summary>
public static class PlatformDetector
{
    private static PlatformType? _cachedPlatform;

    /// <summary>
    /// Gets the current platform type.
    /// </summary>
    public static PlatformType CurrentPlatform
    {
        get
        {
            if (_cachedPlatform.HasValue)
                return _cachedPlatform.Value;

            _cachedPlatform = DetectPlatform();
            return _cachedPlatform.Value;
        }
    }

    /// <summary>
    /// Returns true if running on Windows.
    /// </summary>
    public static bool IsWindows => CurrentPlatform == PlatformType.Windows;

    /// <summary>
    /// Returns true if running on Linux.
    /// </summary>
    public static bool IsLinux => CurrentPlatform == PlatformType.Linux;

    /// <summary>
    /// Returns true if running on macOS.
    /// </summary>
    public static bool IsMacOS => CurrentPlatform == PlatformType.MacOS;

    /// <summary>
    /// Returns true if running on Android.
    /// </summary>
    public static bool IsAndroid => CurrentPlatform == PlatformType.Android;

    /// <summary>
    /// Returns true if running on iOS.
    /// </summary>
    public static bool IsiOS => CurrentPlatform == PlatformType.iOS;

    /// <summary>
    /// Returns true if running on WebAssembly.
    /// </summary>
    public static bool IsWebAssembly => CurrentPlatform == PlatformType.WebAssembly;

    /// <summary>
    /// Returns true if running on a desktop platform (Windows, Linux, macOS).
    /// </summary>
    public static bool IsDesktop => IsWindows || IsLinux || IsMacOS;

    /// <summary>
    /// Returns true if running on a mobile platform (Android, iOS).
    /// </summary>
    public static bool IsMobile => IsAndroid || IsiOS;

    private static PlatformType DetectPlatform()
    {
        // Check for WebAssembly first
        if (RuntimeInformation.OSDescription.Contains("Browser") ||
            RuntimeInformation.OSDescription.Contains("WebAssembly"))
        {
            return PlatformType.WebAssembly;
        }

        // Check for Android
        if (RuntimeInformation.OSDescription.Contains("Android") ||
            OperatingSystem.IsAndroid())
        {
            return PlatformType.Android;
        }

        // Check for iOS
        if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
        {
            return PlatformType.iOS;
        }

        // Desktop platforms
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return PlatformType.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return PlatformType.MacOS;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return PlatformType.Linux;
        }

        // Fallback to Windows
        return PlatformType.Windows;
    }

    /// <summary>
    /// Gets the platform-specific options that apply to the current platform.
    /// </summary>
    public static PlatformOptions? GetCurrentPlatformOptions(WindowConfiguration config)
    {
        return CurrentPlatform switch
        {
            PlatformType.Windows => config.Windows,
            PlatformType.Linux => config.Linux,
            PlatformType.MacOS => config.MacOS,
            PlatformType.Android => config.Android,
            PlatformType.iOS => config.iOS,
            _ => null
        };
    }
}
