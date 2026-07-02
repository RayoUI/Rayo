namespace Rayo.Styling;

/// <summary>
/// OS-level color-scheme preference, equivalent to the CSS
/// <c>prefers-color-scheme</c> media feature.
/// </summary>
public enum PreferredColorScheme
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
internal static class PreferredColorSchemeHelper
{
    private static PreferredColorScheme? _last;
    private static HostThemePreferences? _lastHostPreferences;

    /// <summary>Fired when the OS color-scheme preference changes.</summary>
    internal static event Action<PreferredColorScheme>? ColorSchemeChanged;
    internal static event Action<HostThemePreferences>? HostPreferencesChanged;

    /// <summary>Current OS color-scheme preference (cached).</summary>
    public static PreferredColorScheme Current => _last ??= Detect();
    public static HostThemePreferences CurrentHostPreferences =>
        _lastHostPreferences ??= DetectHostPreferences();

    /// <summary>
    /// Compares the live OS preference with the cached value and fires
    /// <see cref="ColorSchemeChanged"/> if they differ.
    /// Called periodically from <c>UIApplication.OnUpdate</c>.
    /// </summary>
    internal static void NotifyIfChanged()
    {
        var current = Detect();
        if (!_last.HasValue || _last.Value != current)
        {
            _last = current;
            ColorSchemeChanged?.Invoke(current);
        }

        var hostPreferences = DetectHostPreferences();
        if (_lastHostPreferences != hostPreferences)
        {
            _lastHostPreferences = hostPreferences;
            HostPreferencesChanged?.Invoke(hostPreferences);
        }
    }

    private static HostThemePreferences DetectHostPreferences()
    {
        var preferences = HostThemePreferences.Default with
        {
            PrefersDark = Detect() == PreferredColorScheme.Dark,
        };

        if (!OperatingSystem.IsWindows())
            return preferences;

        try
        {
#pragma warning disable CA1416
            using var highContrastKey = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Control Panel\Accessibility\HighContrast");
            var highContrast = int.TryParse(
                highContrastKey?.GetValue("Flags")?.ToString(),
                out var highContrastFlags) &&
                highContrastFlags != 0;

            using var animationKey = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
            var reduceMotion = animationKey?.GetValue("MinAnimate")?.ToString() == "0";

            using var textScaleKey = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Accessibility");
            var textScale = textScaleKey?.GetValue("TextScaleFactor") is int percentage
                ? Math.Clamp(percentage / 100f, 0.5f, 3f)
                : 1f;
#pragma warning restore CA1416

            return preferences with
            {
                HighContrast = highContrast,
                ReduceMotion = reduceMotion,
                TextScale = textScale,
            };
        }
        catch
        {
            return preferences;
        }
    }

    private static PreferredColorScheme Detect()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
#pragma warning disable CA1416
                using var key = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int i)
                    return i == 0 ? PreferredColorScheme.Dark : PreferredColorScheme.Light;
#pragma warning restore CA1416
            }
        }
        catch { /* registry unavailable — fall through to default */ }

        // Android / iOS: could use platform-specific APIs in the future
        return PreferredColorScheme.Light;
    }
}
