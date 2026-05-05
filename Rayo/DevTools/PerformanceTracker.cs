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
        public int ElementsArranged;
        public int ElementsRendered;
        public int LayoutDirtyCount;
        public int PaintDirtyCount;
    }

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
    private static int _curArranged;
    private static int _curRendered;
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
    internal static void RecordLayoutDirty(VisualElement element)
    {
        if (!IsEnabled) return;
        Interlocked.Increment(ref _curLayoutDirty);
        AppendDirtyEntry(element, isLayout: true);
        DirtyHeatmap.Increment(element, isLayout: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordPaintDirty(VisualElement element)
    {
        if (!IsEnabled) return;
        Interlocked.Increment(ref _curPaintDirty);
        AppendDirtyEntry(element, isLayout: false);
        DirtyHeatmap.Increment(element, isLayout: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordMeasured() { if (IsEnabled) Interlocked.Increment(ref _curMeasured); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordArranged() { if (IsEnabled) Interlocked.Increment(ref _curArranged); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordRendered() { if (IsEnabled) Interlocked.Increment(ref _curRendered); }

    private static void AppendDirtyEntry(VisualElement element, bool isLayout)
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
                ElementsArranged  = _curArranged,
                ElementsRendered  = _curRendered,
                LayoutDirtyCount  = _curLayoutDirty,
                PaintDirtyCount   = _curPaintDirty,
            };
            _frameHead = (_frameHead + 1) % MaxFrames;
            _frameNumber++;

            _curMeasured = _curArranged = _curRendered =
            _curLayoutDirty = _curPaintDirty = 0;
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
