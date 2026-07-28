namespace Rayo.Core.Platform;

/// <summary>
/// Host-provided source of live safe-area insets for the current platform.
/// </summary>
public interface ISafeAreaProvider
{
    /// <summary>
    /// Reads the current safe-area insets in logical pixels.
    /// </summary>
    SafeAreaInsets GetSafeAreaInsets();
}

/// <summary>
/// Registration and notification hub for <see cref="ISafeAreaProvider"/>.
/// </summary>
public static class SafeArea
{
    private static ISafeAreaProvider? _provider;

    /// <summary>
    /// Raised when the host reports that safe-area insets may have changed
    /// (orientation, immersive mode, system bars, etc.).
    /// </summary>
    public static event Action? Changed;

    public static void SetProvider(ISafeAreaProvider? provider) => _provider = provider;

    public static void ClearProvider(ISafeAreaProvider provider)
    {
        if (ReferenceEquals(_provider, provider))
        {
            _provider = null;
        }
    }

    /// <summary>
    /// Current safe-area insets, or <see cref="SafeAreaInsets.Empty"/> when no
    /// mobile host has registered a provider (e.g. desktop).
    /// </summary>
    public static SafeAreaInsets Current =>
        _provider?.GetSafeAreaInsets() ?? SafeAreaInsets.Empty;

    /// <summary>
    /// Convenience accessor for the top inset (status bar / notch).
    /// </summary>
    public static float Top => Current.Top;

    /// <summary>
    /// Invoked by platform hosts when insets may have changed.
    /// </summary>
    public static void NotifyChanged() => Changed?.Invoke();
}
