using System.Diagnostics;

namespace Rayo.Hosting.Android;

/// <summary>
/// Lightweight logging utility that minimizes overhead in release builds.
/// Debug logging is compiled out in Release configuration.
/// </summary>
internal static class RayoLog
{
    private const string Tag = "Rayo";

    /// <summary>
    /// Log info message (always included, use sparingly).
    /// </summary>
    [DebuggerStepThrough]
    public static void Info(string message)
    {
        global::Android.Util.Log.Info(Tag, message);
    }

    /// <summary>
    /// Log error message (always included).
    /// </summary>
    [DebuggerStepThrough]
    public static void Error(string message)
    {
        global::Android.Util.Log.Error(Tag, message);
    }

    /// <summary>
    /// Log error with exception (always included).
    /// </summary>
    [DebuggerStepThrough]
    public static void Error(string message, Exception ex)
    {
        global::Android.Util.Log.Error(Tag, $"{message}: {ex.Message}");
    }

    /// <summary>
    /// Debug logging - compiled out in Release builds.
    /// Use for high-frequency logging like touch events.
    /// </summary>
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Debug(string message)
    {
#if DEBUG
        // Only log if debugger is NOT attached to reduce VS synchronization overhead
        if (!Debugger.IsAttached)
        {
            global::Android.Util.Log.Debug(Tag, message);
        }
#endif
    }

    /// <summary>
    /// Verbose logging - only in DEBUG and when explicitly enabled.
    /// Use for very high-frequency events like touch move.
    /// </summary>
    [Conditional("DEBUG")]
    [DebuggerStepThrough]
    public static void Verbose(string message)
    {
#if DEBUG
        // Verbose is disabled by default even in debug
        // Uncomment below line to enable verbose logging
        // if (!Debugger.IsAttached) global::Android.Util.Log.Verbose(Tag, message);
#endif
    }
}
