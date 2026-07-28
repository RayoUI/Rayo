namespace Rayo.Core.Platform;

/// <summary>
/// Host-provided bridge that applies <see cref="AndroidPlatformOptions"/> to the
/// live Android Activity / window and reports safe-area insets.
/// </summary>
public interface IAndroidWindowController : ISafeAreaProvider
{
    void Apply(AndroidPlatformOptions options);
}

/// <summary>
/// Host-provided bridge that applies <see cref="iOSPlatformOptions"/> to the
/// live iOS view controller / window.
/// </summary>
public interface IiOSWindowController
{
    void Apply(iOSPlatformOptions options);
}

/// <summary>
/// Registration point for platform window controllers (same pattern as
/// <see cref="VirtualKeyboardManager"/>).
/// </summary>
public static class PlatformWindowControllers
{
    private static IAndroidWindowController? _android;
    private static IiOSWindowController? _ios;

    public static void SetAndroid(IAndroidWindowController? controller) => _android = controller;

    public static void SetiOS(IiOSWindowController? controller) => _ios = controller;

    public static void ClearAndroid(IAndroidWindowController controller)
    {
        if (ReferenceEquals(_android, controller))
        {
            _android = null;
        }
    }

    public static void CleariOS(IiOSWindowController controller)
    {
        if (ReferenceEquals(_ios, controller))
        {
            _ios = null;
        }
    }

    internal static void ApplyAndroid(AndroidPlatformOptions options) => _android?.Apply(options);

    internal static void ApplyiOS(iOSPlatformOptions options) => _ios?.Apply(options);
}
