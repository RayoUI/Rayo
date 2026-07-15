namespace Rayo.Core.Platform;

public interface IVirtualKeyboardService
{
    void Show(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys);
    void Hide();
    void SetAccessoryKeys(IReadOnlyList<VirtualKeyboardAccessoryKey> accessoryKeys);
}

public static class VirtualKeyboardManager
{
    private static IVirtualKeyboardService? _service;
    private static IReadOnlyList<VirtualKeyboardAccessoryKey> _accessoryKeys = [];

    public static void SetService(IVirtualKeyboardService service)
    {
        _service = service;
        _service.SetAccessoryKeys(_accessoryKeys);
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
}
