using Pokemon.Services.Interfaces;
using Rayo.DevTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pokemon.Services;

internal class LoggerService : ILoggerService
{
     //+ $"[LOG] [{DateTime.Now:HH:mm:ss.fff}] {message}
    public void Log(string message)
    {
        Console.WriteLine($"[LOG] {message}");
        DevToolLogger.Log(message);
    }

    public void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
        DevToolLogger.LogInfo(message);
    }

    public void LogWarn(string message)
    {
        Console.WriteLine($"[WARNING] {message}");
        DevToolLogger.LogWarning(message);
    }

    public void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
        DevToolLogger.LogError(message);
    }

    
}
