namespace Rayo.Core.Platform;

public interface IVirtualKeyboardService
{
    void Show(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys);
    void Hide();
    void SetAccessoryKeys(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys);
    void NotifyAppPaused() { }
    void RestoreAfterResume(IVirtualKeyboardOptions? options) { }
}

public static class VirtualKeyboardManager
{
    private static IVirtualKeyboardService? _service;
    private static IReadOnlyList<VirtualKeyboardAccessoryKey> _accessoryKeys = [];

    public static void SetService(IVirtualKeyboardService service)
    {
        if (ReferenceEquals(_service, service))
        {
            return;
        }

        var previousService = _service;
        _service = service;
        (previousService as IDisposable)?.Dispose();
        _service.SetAccessoryKeys(_accessoryKeys);
    }

    public static void ClearService(IVirtualKeyboardService service)
    {
        if (!ReferenceEquals(_service, service))
        {
            return;
        }

        _service = null;
        (service as IDisposable)?.Dispose();
    }

    /// <summary>Sets a persistent accessory bar for platforms that support it.</summary>
    public static void SetAccessoryKeys(IReadOnlyList<VirtualKeyboardAccessoryKey>? accessoryKeys)
    {
        _accessoryKeys = accessoryKeys ?? [];
        _service?.SetAccessoryKeys(_accessoryKeys);
    }

    public static void Show(IReadOnlyList<VirtualKeyboardAccessoryKey>? accessoryKeys = null)
    {
        var keys = accessoryKeys is { Count: > 0 } ? accessoryKeys : _accessoryKeys;
        _service?.Show(keys);
    }

    public static void Hide()
    {
        _service?.Hide();
    }

    public static void NotifyAppPaused() => _service?.NotifyAppPaused();

    public static void RestoreAfterResume(IVirtualKeyboardOptions? options) =>
        _service?.RestoreAfterResume(options);
}
