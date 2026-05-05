using Rayo.Core;
using Rayo.Rendering;

namespace Rayo.DevTools;

/// <summary>
/// Extension methods to enable DevTools in Rayo applications
/// </summary>
public static class DevToolExtensions
{
    private static DevToolAgent? _agent;

    /// <summary>
    /// Enables DevTools for the application, allowing external inspection
    /// </summary>
    /// <param name="uiTree">The UI tree to inspect</param>
    /// <param name="renderer">The renderer for highlight overlays</param>
    /// <param name="port">The port to listen on (default: 9999)</param>
    /// <returns>The DevToolAgent instance</returns>
    public static DevToolAgent EnableDevTools(UITree uiTree, IRenderer renderer, int port = 9999)
    {
        _agent?.Dispose();
        _agent = new DevToolAgent(uiTree, renderer, port);
        _agent.Start();
        PerformanceTracker.IsEnabled = true; // always track when DevTool is running
        return _agent;
    }

    /// <summary>
    /// Gets the current DevTool agent if enabled
    /// </summary>
    public static DevToolAgent? GetDevToolAgent() => _agent;

    /// <summary>
    /// Disables DevTools
    /// </summary>
    public static void DisableDevTools()
    {
        _agent?.Dispose();
        _agent = null;
    }

    /// <summary>
    /// Renders the DevTool highlight overlay (call during render)
    /// </summary>
    public static void RenderDevToolOverlay(IRenderer renderer)
    {
        _agent?.RenderHighlight(renderer);
    }

    /// <summary>Enables or disables the extended stats overlay.</summary>
    public static void SetExtendedStats(bool enabled)
    {
        FpsOverlay.ShowExtendedStats = enabled;
        ActivatePerformanceTracker();
        UIApplication.Current?.Tree.MarkNeedsRender();
    }

    /// <summary>Enables or disables the dirty-element heatmap overlay.</summary>
    public static void SetDirtyHeatmap(bool enabled)
    {
        DirtyHeatmap.Enabled = enabled;
        ActivatePerformanceTracker();
        UIApplication.Current?.Tree.MarkNeedsRender();
    }

    /// <summary>Enables or disables the overdraw visualiser overlay.</summary>
    public static void SetOverdrawVisualizer(bool enabled)
    {
        OverdrawVisualizer.Enabled = enabled;
        UIApplication.Current?.Tree.MarkNeedsRender();
    }

    /// <summary>
    /// Enables <see cref="PerformanceTracker"/> if any debug feature that needs it is active.
    /// </summary>
    public static void ActivatePerformanceTracker()
    {
        PerformanceTracker.IsEnabled =
            FpsOverlay.ShowExtendedStats ||
            DirtyHeatmap.Enabled ||
            _agent != null; // always on when DevTool is connected
    }
}
