namespace Rayo.Core.Platform;

public interface IVirtualKeyboardService
{
    void Show();
    void Hide();
}

public static class VirtualKeyboardManager
{
    private static IVirtualKeyboardService? _service;

    public static void SetService(IVirtualKeyboardService service)
    {
        _service = service;
    }

    public static void Show()
    {
        _service?.Show();
    }

    public static void Hide()
    {
        _service?.Hide();
    }
}
