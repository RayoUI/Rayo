namespace Rayo.Core.Platform;

public interface IVirtualKeyboardService
{
    void Show(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys);
    void Hide();
    void NotifyAppPaused() { }
    void RestoreAfterResume(IVirtualKeyboardOptions? options) { }
}

public static class VirtualKeyboardManager
{
    private static IVirtualKeyboardService? _service;

    public static void SetService(IVirtualKeyboardService service)
    {
        if (ReferenceEquals(_service, service))
        {
            return;
        }

        var previousService = _service;
        _service = service;
        (previousService as IDisposable)?.Dispose();
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

    public static void Show(IReadOnlyList<VirtualKeyboardAccessoryKey>? accessoryKeys = null)
    {
        _service?.Show(accessoryKeys ?? []);
    }

    public static void Hide()
    {
        _service?.Hide();
    }

    public static void NotifyAppPaused() => _service?.NotifyAppPaused();

    public static void RestoreAfterResume(IVirtualKeyboardOptions? options) =>
        _service?.RestoreAfterResume(options);
}
