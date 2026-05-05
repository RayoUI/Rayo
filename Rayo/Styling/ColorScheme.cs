namespace Rayo.Styling;

/// <summary>
/// OS-level color-scheme preference, equivalent to the CSS
/// <c>prefers-color-scheme</c> media feature.
/// </summary>
public enum ColorScheme
{
    /// <summary>The OS is using a light / default theme.</summary>
    Light,
    /// <summary>The OS is using a dark theme.</summary>
    Dark,
}

/// <summary>
/// Reads and caches the OS color-scheme preference and fires
/// <see cref="ColorSchemeChanged"/> when it changes.
/// </summary>
internal static class ColorSchemeHelper
{
    private static ColorScheme? _last;

    /// <summary>Fired when the OS color-scheme preference changes.</summary>
    internal static event Action<ColorScheme>? ColorSchemeChanged;

    /// <summary>Current OS color-scheme preference (cached).</summary>
    public static ColorScheme Current => _last ??= Detect();

    /// <summary>
    /// Compares the live OS preference with the cached value and fires
    /// <see cref="ColorSchemeChanged"/> if they differ.
    /// Called periodically from <c>UIApplication.OnUpdate</c>.
    /// </summary>
    internal static void NotifyIfChanged()
    {
        var current = Detect();
        if (_last.HasValue && _last.Value == current) return;
        _last = current;
        ColorSchemeChanged?.Invoke(current);
    }

    private static ColorScheme Detect()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416
                using var key = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int i)
                    return i == 0 ? ColorScheme.Dark : ColorScheme.Light;
#pragma warning restore CA1416
            }
        }
        catch { /* registry unavailable — fall through to default */ }

        // Android / iOS: could use platform-specific APIs in the future
        return ColorScheme.Light;
    }
}
