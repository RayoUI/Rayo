using Rayo.Core.Platform;
using System.Runtime.InteropServices;

namespace Rayo.Styling;

/// <summary>
/// Resolves the current display <see cref="ScreenDensity"/> tier.
/// Detection is best-effort and platform-dependent; callers can call
/// <see cref="Override"/> to pin a density when automatic detection is unavailable.
/// </summary>
internal static class ScreenDensityHelper
{
    private static ScreenDensity? _override;

    /// <summary>
    /// Current display density tier.
    /// Returns the manually overridden value if one has been set via <see cref="Override"/>;
    /// otherwise attempts platform-specific detection, defaulting to <see cref="ScreenDensity.Normal"/>.
    /// </summary>
    public static ScreenDensity Current => _override ?? Detect();

    /// <summary>
    /// Pins the density to a specific value, bypassing automatic detection.
    /// Useful when the platform host can supply an authoritative DPI value.
    /// Pass <c>null</c> to restore automatic detection.
    /// </summary>
    public static void Override(ScreenDensity? density) => _override = density;

    private static ScreenDensity Detect()
    {
        float dpi = GetSystemDpi();
        return dpi switch
        {
            < 96f  => ScreenDensity.Low,
            < 144f => ScreenDensity.Normal,
            < 192f => ScreenDensity.High,
            _      => ScreenDensity.ExtraHigh,
        };
    }

    private static float GetSystemDpi()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416
                // Get DPI for the primary monitor via Win32
                var hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int dpi = GetDeviceCaps(hdc, LOGPIXELSX);
                    ReleaseDC(IntPtr.Zero, hdc);
                    if (dpi > 0) return dpi;
                }
#pragma warning restore CA1416
            }
        }
        catch { }

        return 96f; // standard 1× fallback
    }

    private const int LOGPIXELSX = 88;

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
}
