using System;

namespace Rayo.DevTools;

/// <summary>
/// Logger helper for sending messages to the connected DevTool
/// </summary>
public static class DevToolLogger
{
    public static event Action<string, string>? OnLog;

    public static void Log(string message) => Log("Info", message);
    public static void LogInfo(string message) => Log("Info", message);
    public static void LogWarning(string message) => Log("Warning", message);
    public static void LogError(string message) => Log("Error", message);

    private static void Log(string level, string message)
    {
        OnLog?.Invoke(level, message);
    }
}
