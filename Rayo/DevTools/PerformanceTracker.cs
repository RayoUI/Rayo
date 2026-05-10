using System.Runtime.CompilerServices;
using Rayo.Core;

namespace Rayo.DevTools;

/// <summary>
/// Central aggregator for per-frame performance data.
/// Collects dirty counts, element counts, and phase timings.
/// Enabled automatically when the DevTool performance panel is opened, or
/// when the render stats overlay is shown. Safe to call from any thread.
/// </summary>
public static class PerformanceTracker
{
    public const int MaxFrames = 120;   // 2 s history at 60 FPS
    public const int MaxDirtyLog = 400;

    // -----------------------------------------------------------------------
    // Frame snapshot
    // -----------------------------------------------------------------------
    public struct FrameSnapshot
    {
        public float FpsSnapshot;
        public float FrameTimeMs;
        public float MeasureTimeMs;
        public float ArrangeTimeMs;
        public float RenderTimeMs;
        public float EventTimeMs;
        public int ElementsMeasured;
        public int ElementsMeasureSkipped;
        public int MeasureCacheHits;
        public int MeasureCacheMisses;
        public int ElementsArranged;
        public int ElementsArrangeSkipped;
        public int ElementsRendered;
        public int RelayoutRoots;
        public int VirtualizedCreated;
        public int VirtualizedReused;
        public int VirtualizedRebound;
        public int VirtualizedRecycled;
        public int MeasureDirtyCount;
        public int ArrangeDirtyCount;
        public int LayoutDirtyCount;
        public int PaintDirtyCount;

        public readonly float MeasureCacheHitRate =>
            (MeasureCacheHits + MeasureCacheMisses) > 0
                ? (float)MeasureCacheHits / (MeasureCacheHits + MeasureCacheMisses)
                : 0;
    }

    // -----------------------------------------------------------------------
    // Aggregated summary
    // -----------------------------------------------------------------------
    public readonly record struct PerformanceSummary(
        int FrameCount,
        float AvgFps,
        float MinFps,
        float MaxFps,
        float AvgFrameTimeMs,
        float P95FrameTimeMs,
        float AvgMeasureTimeMs,
        float AvgArrangeTimeMs,
        float AvgRenderTimeMs,
        float AvgEventTimeMs,
        float AvgElementsMeasured,
        float AvgElementsMeasureSkipped,
        float AvgMeasureCacheHits,
        float AvgMeasureCacheMisses,
        float AvgMeasureCacheHitRate,
        float AvgElementsArranged,
        float AvgElementsArrangeSkipped,
        float AvgElementsRendered,
        float AvgRelayoutRoots,
        float AvgVirtualizedCreated,
        float AvgVirtualizedReused,
        float AvgVirtualizedRebound,
        float AvgVirtualizedRecycled,
        float AvgMeasureDirty,
        float AvgArrangeDirty,
        float AvgLayoutDirty,
        float AvgPaintDirty);

    // -----------------------------------------------------------------------
    // Dirty log entry
    // -----------------------------------------------------------------------
    public struct DirtyEntry
    {
        public long FrameNumber;
        public string ElementType;
        public string? ElementId;
        public string Classes;
        public bool IsLayout;
        public string Phase;
        public string Timestamp;
    }

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    public static bool IsEnabled { get; set; } = false;

    private static readonly FrameSnapshot[] _frames = new FrameSnapshot[MaxFrames];
    private static int _frameHead = 0;
    private static long _frameNumber = 0;
    private static readonly object _lock = new();

    // Per-frame accumulators — reset by CommitFrame
    private static int _curMeasured;
    private static int _curMeasureSkipped;
    private static int _curMeasureCacheHits;
    private static int _curMeasureCacheMisses;
    private static int _curArranged;
    private static int _curArrangeSkipped;
    private static int _curRendered;
    private static int _curRelayoutRoots;
    private static int _curVirtualizedCreated;
    private static int _curVirtualizedReused;
    private static int _curVirtualizedRebound;
    private static int _curVirtualizedRecycled;
    private static int _curMeasureDirty;
    private static int _curArrangeDirty;
    private static int _curLayoutDirty;
    private static int _curPaintDirty;

    // Dirty log circular buffer
    private static readonly DirtyEntry[] _dirtyLog = new DirtyEntry[MaxDirtyLog];
    private static int _dirtyLogHead = 0;
    private static int _dirtyLogCount = 0;

    // -----------------------------------------------------------------------
    // Hooks — called from VisualElement and UITree
    // -----------------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordMeasureDirty(VisualElement element)
    {
        if (!IsEnabled) return;
        Interlocked.Increment(ref _curMeasureDirty);
        Interlocked.Increment(ref _curLayoutDirty);
        AppendDirtyEntry(element, isLayout: true, phase: "measure");
        DirtyHeatmap.Increment(element, isLayout: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordArrangeDirty(VisualElement element)
    {
        if (!IsEnabled) return;
        Interlocked.Increment(ref _curArrangeDirty);
        Interlocked.Increment(ref _curLayoutDirty);
        AppendDirtyEntry(element, isLayout: true, phase: "arrange");
        DirtyHeatmap.Increment(element, isLayout: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordPaintDirty(VisualElement element)
    {
        if (!IsEnabled) return;
        Interlocked.Increment(ref _curPaintDirty);
        AppendDirtyEntry(element, isLayout: false, phase: "paint");
        DirtyHeatmap.Increment(element, isLayout: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordMeasured() { if (IsEnabled) Interlocked.Increment(ref _curMeasured); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordMeasureSkipped() { if (IsEnabled) Interlocked.Increment(ref _curMeasureSkipped); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordMeasureCacheHit() { if (IsEnabled) Interlocked.Increment(ref _curMeasureCacheHits); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordMeasureCacheMiss() { if (IsEnabled) Interlocked.Increment(ref _curMeasureCacheMisses); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordArranged() { if (IsEnabled) Interlocked.Increment(ref _curArranged); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordArrangeSkipped() { if (IsEnabled) Interlocked.Increment(ref _curArrangeSkipped); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordRendered() { if (IsEnabled) Interlocked.Increment(ref _curRendered); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordRelayoutRoot() { if (IsEnabled) Interlocked.Increment(ref _curRelayoutRoots); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordVirtualizedCreated() { if (IsEnabled) Interlocked.Increment(ref _curVirtualizedCreated); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordVirtualizedReused() { if (IsEnabled) Interlocked.Increment(ref _curVirtualizedReused); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordVirtualizedRebound() { if (IsEnabled) Interlocked.Increment(ref _curVirtualizedRebound); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordVirtualizedRecycled() { if (IsEnabled) Interlocked.Increment(ref _curVirtualizedRecycled); }

    private static void AppendDirtyEntry(VisualElement element, bool isLayout, string phase)
    {
        lock (_lock)
        {
            _dirtyLog[_dirtyLogHead] = new DirtyEntry
            {
                FrameNumber = _frameNumber,
                ElementType = element.GetType().Name,
                ElementId = element.Id,
                Classes = element.Classes ?? "",
                IsLayout = isLayout,
                Phase = phase,
                Timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
            };
            _dirtyLogHead = (_dirtyLogHead + 1) % MaxDirtyLog;
            if (_dirtyLogCount < MaxDirtyLog) _dirtyLogCount++;
        }
    }

    // -----------------------------------------------------------------------
    // Frame commit — called once per rendered frame from UIApplication
    // -----------------------------------------------------------------------
    internal static void CommitFrame(
        float fps, float frameTimeMs,
        float measureMs, float arrangeMs, float renderMs, float eventMs)
    {
        lock (_lock)
        {
            _frames[_frameHead] = new FrameSnapshot
            {
                FpsSnapshot    = fps,
                FrameTimeMs    = frameTimeMs,
                MeasureTimeMs  = measureMs,
                ArrangeTimeMs  = arrangeMs,
                RenderTimeMs   = renderMs,
                EventTimeMs    = eventMs,
                ElementsMeasured  = _curMeasured,
                ElementsMeasureSkipped = _curMeasureSkipped,
                MeasureCacheHits = _curMeasureCacheHits,
                MeasureCacheMisses = _curMeasureCacheMisses,
                ElementsArranged  = _curArranged,
                ElementsArrangeSkipped = _curArrangeSkipped,
                ElementsRendered  = _curRendered,
                RelayoutRoots     = _curRelayoutRoots,
                VirtualizedCreated = _curVirtualizedCreated,
                VirtualizedReused = _curVirtualizedReused,
                VirtualizedRebound = _curVirtualizedRebound,
                VirtualizedRecycled = _curVirtualizedRecycled,
                MeasureDirtyCount = _curMeasureDirty,
                ArrangeDirtyCount = _curArrangeDirty,
                LayoutDirtyCount  = _curLayoutDirty,
                PaintDirtyCount   = _curPaintDirty,
            };
            _frameHead = (_frameHead + 1) % MaxFrames;
            _frameNumber++;

            _curMeasured = _curMeasureSkipped = _curMeasureCacheHits = _curMeasureCacheMisses =
            _curArranged = _curArrangeSkipped = _curRendered = _curRelayoutRoots =
            _curVirtualizedCreated = _curVirtualizedReused = _curVirtualizedRebound = _curVirtualizedRecycled =
            _curMeasureDirty = _curArrangeDirty = _curLayoutDirty = _curPaintDirty = 0;
        }
    }

    // -----------------------------------------------------------------------
    // Queries — called from DevToolAgent or RenderStatsOverlay
    // -----------------------------------------------------------------------

    /// <summary>Returns up to MaxFrames snapshots in chronological order (oldest first).</summary>
    public static FrameSnapshot[] GetFrameHistory()
    {
        lock (_lock)
        {
            var result = new FrameSnapshot[MaxFrames];
            for (int i = 0; i < MaxFrames; i++)
                result[i] = _frames[(_frameHead + i) % MaxFrames];
            return result;
        }
    }

    /// <summary>Returns the last <paramref name="maxEntries"/> dirty log entries, newest last.</summary>
    public static DirtyEntry[] GetDirtyLog(int maxEntries = 200)
    {
        lock (_lock)
        {
            int count = Math.Min(maxEntries, _dirtyLogCount);
            var result = new DirtyEntry[count];
            for (int i = 0; i < count; i++)
            {
                int idx = (_dirtyLogHead - count + i + MaxDirtyLog) % MaxDirtyLog;
                result[i] = _dirtyLog[idx];
            }
            return result;
        }
    }

    public static void ClearDirtyLog()
    {
        lock (_lock) { _dirtyLogCount = 0; _dirtyLogHead = 0; }
    }

    public static void ClearFrameHistory()
    {
        lock (_lock)
        {
            Array.Clear(_frames, 0, _frames.Length);
            _frameHead = 0;
            _frameNumber = 0;
            _curMeasured = _curMeasureSkipped = _curMeasureCacheHits = _curMeasureCacheMisses =
            _curArranged = _curArrangeSkipped = _curRendered = _curRelayoutRoots = 0;
            _curVirtualizedCreated = _curVirtualizedReused = _curVirtualizedRebound = _curVirtualizedRecycled = 0;
            _curMeasureDirty = _curArrangeDirty = _curLayoutDirty = _curPaintDirty = 0;
        }
    }

    /// <summary>
    /// Returns an aggregate summary for the most recent non-empty frames.
    /// </summary>
    public static PerformanceSummary GetSummary(int maxFrames = 60)
    {
        var history = GetFrameHistory()
            .Where(f => f.FrameTimeMs > 0 || f.FpsSnapshot > 0)
            .TakeLast(Math.Max(1, maxFrames))
            .ToArray();

        if (history.Length == 0)
        {
            return new PerformanceSummary(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var orderedFrameTimes = history
            .Select(f => f.FrameTimeMs)
            .OrderBy(v => v)
            .ToArray();

        int p95Index = Math.Clamp((int)Math.Ceiling(orderedFrameTimes.Length * 0.95) - 1, 0, orderedFrameTimes.Length - 1);

        return new PerformanceSummary(
            FrameCount: history.Length,
            AvgFps: history.Average(f => f.FpsSnapshot),
            MinFps: history.Min(f => f.FpsSnapshot),
            MaxFps: history.Max(f => f.FpsSnapshot),
            AvgFrameTimeMs: history.Average(f => f.FrameTimeMs),
            P95FrameTimeMs: orderedFrameTimes[p95Index],
            AvgMeasureTimeMs: history.Average(f => f.MeasureTimeMs),
            AvgArrangeTimeMs: history.Average(f => f.ArrangeTimeMs),
            AvgRenderTimeMs: history.Average(f => f.RenderTimeMs),
            AvgEventTimeMs: history.Average(f => f.EventTimeMs),
            AvgElementsMeasured: (float)history.Average(f => f.ElementsMeasured),
            AvgElementsMeasureSkipped: (float)history.Average(f => f.ElementsMeasureSkipped),
            AvgMeasureCacheHits: (float)history.Average(f => f.MeasureCacheHits),
            AvgMeasureCacheMisses: (float)history.Average(f => f.MeasureCacheMisses),
            AvgMeasureCacheHitRate: (float)history.Average(f => f.MeasureCacheHitRate),
            AvgElementsArranged: (float)history.Average(f => f.ElementsArranged),
            AvgElementsArrangeSkipped: (float)history.Average(f => f.ElementsArrangeSkipped),
            AvgElementsRendered: (float)history.Average(f => f.ElementsRendered),
            AvgRelayoutRoots: (float)history.Average(f => f.RelayoutRoots),
            AvgVirtualizedCreated: (float)history.Average(f => f.VirtualizedCreated),
            AvgVirtualizedReused: (float)history.Average(f => f.VirtualizedReused),
            AvgVirtualizedRebound: (float)history.Average(f => f.VirtualizedRebound),
            AvgVirtualizedRecycled: (float)history.Average(f => f.VirtualizedRecycled),
            AvgMeasureDirty: (float)history.Average(f => f.MeasureDirtyCount),
            AvgArrangeDirty: (float)history.Average(f => f.ArrangeDirtyCount),
            AvgLayoutDirty: (float)history.Average(f => f.LayoutDirtyCount),
            AvgPaintDirty: (float)history.Average(f => f.PaintDirtyCount));
    }

    public static string FormatSummary(string label, int maxFrames = 60)
    {
        var summary = GetSummary(maxFrames);
        return $"""
            Performance Summary: {label}
            Frames: {summary.FrameCount}
            Avg FPS: {summary.AvgFps:F2}
            Min/Max FPS: {summary.MinFps:F2} / {summary.MaxFps:F2}
            Avg Frame Time: {summary.AvgFrameTimeMs:F2} ms
            P95 Frame Time: {summary.P95FrameTimeMs:F2} ms
            Avg Measure: {summary.AvgMeasureTimeMs:F2} ms
            Avg Arrange: {summary.AvgArrangeTimeMs:F2} ms
            Avg Render: {summary.AvgRenderTimeMs:F2} ms
            Avg Event: {summary.AvgEventTimeMs:F2} ms
            Avg Elements Measured: {summary.AvgElementsMeasured:F2}
            Avg Measure Skipped: {summary.AvgElementsMeasureSkipped:F2}
            Avg Measure Cache Hits: {summary.AvgMeasureCacheHits:F2}
            Avg Measure Cache Misses: {summary.AvgMeasureCacheMisses:F2}
            Avg Measure Cache Hit Rate: {summary.AvgMeasureCacheHitRate:P1}
            Avg Elements Arranged: {summary.AvgElementsArranged:F2}
            Avg Arrange Skipped: {summary.AvgElementsArrangeSkipped:F2}
            Avg Elements Rendered: {summary.AvgElementsRendered:F2}
            Avg Relayout Roots: {summary.AvgRelayoutRoots:F2}
            Avg Virtualized Created: {summary.AvgVirtualizedCreated:F2}
            Avg Virtualized Reused: {summary.AvgVirtualizedReused:F2}
            Avg Virtualized Rebound: {summary.AvgVirtualizedRebound:F2}
            Avg Virtualized Recycled: {summary.AvgVirtualizedRecycled:F2}
            Avg Measure Dirty: {summary.AvgMeasureDirty:F2}
            Avg Arrange Dirty: {summary.AvgArrangeDirty:F2}
            Avg Layout Dirty: {summary.AvgLayoutDirty:F2}
            Avg Paint Dirty: {summary.AvgPaintDirty:F2}
            """;
    }

    public static FrameSnapshot LatestFrame
    {
        get
        {
            lock (_lock)
            {
                int prev = (_frameHead - 1 + MaxFrames) % MaxFrames;
                return _frames[prev];
            }
        }
    }
}
