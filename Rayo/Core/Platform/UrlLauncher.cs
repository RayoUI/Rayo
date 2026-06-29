using System.Diagnostics;

namespace Rayo.Core.Platform;

public interface IUrlLauncherService
{
    bool Open(string url);
}

public static class UrlLauncher
{
    private static IUrlLauncherService? _service;

    public static void SetService(IUrlLauncherService service)
    {
        _service = service;
    }

    public static bool Open(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (_service?.Open(url) == true)
        {
            return true;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
            return process != null;
        }
        catch
        {
            return false;
        }
    }
}
