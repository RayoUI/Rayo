using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Rayo.DevTool.Shared.Protocol;

// ============================================================================
// Performance tool — request / response messages
// ============================================================================

public class GetPerformanceStatsRequest : DevToolMessage
{
    public override string Type => "get_perf_stats";
}

public class SetDirtyHeatmapRequest : DevToolMessage
{
    public override string Type => "set_dirty_heatmap";
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
}

public class SetOverdrawVisualizerRequest : DevToolMessage
{
    public override string Type => "set_overdraw";
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
}

public class SetExtendedStatsRequest : DevToolMessage
{
    public override string Type => "set_extended_stats";
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
}

public class ClearDirtyLogRequest : DevToolMessage
{
    public override string Type => "clear_dirty_log";
}

// ---------------------------------------------------------------------------
// Response
// ---------------------------------------------------------------------------

public class PerformanceStatsResponse : DevToolMessage
{
    public override string Type => "perf_stats";

    // Latest frame snapshot
    [JsonPropertyName("fps")]            public float Fps            { get; set; }
    [JsonPropertyName("frameTimeMs")]    public float FrameTimeMs    { get; set; }
    [JsonPropertyName("measureTimeMs")]  public float MeasureTimeMs  { get; set; }
    [JsonPropertyName("renderTimeMs")]   public float RenderTimeMs   { get; set; }
    [JsonPropertyName("eventTimeMs")]    public float EventTimeMs    { get; set; }
    [JsonPropertyName("elemRendered")]   public int   ElemRendered   { get; set; }
    [JsonPropertyName("elemMeasured")]   public int   ElemMeasured   { get; set; }
    [JsonPropertyName("layoutDirty")]    public int   LayoutDirty    { get; set; }
    [JsonPropertyName("paintDirty")]     public int   PaintDirty     { get; set; }

    // Frame history (up to 120 frames) for the timeline chart
    [JsonPropertyName("frames")]
    public List<FrameStatsDto> Frames { get; set; } = new();

    // Dirty log (last N entries)
    [JsonPropertyName("dirtyLog")]
    public List<DirtyEntryDto> DirtyLog { get; set; } = new();

    // Current overlay states
    [JsonPropertyName("heatmapEnabled")]   public bool HeatmapEnabled   { get; set; }
    [JsonPropertyName("overdrawEnabled")]  public bool OverdrawEnabled  { get; set; }
    [JsonPropertyName("extStatsEnabled")]  public bool ExtStatsEnabled  { get; set; }
}

public class FrameStatsDto
{
    [JsonPropertyName("fps")]      public float Fps      { get; set; }
    [JsonPropertyName("frameMs")] public float FrameMs  { get; set; }
    [JsonPropertyName("measMs")]  public float MeasMs   { get; set; }
    [JsonPropertyName("rendMs")]  public float RendMs   { get; set; }
    [JsonPropertyName("evtMs")]   public float EvtMs    { get; set; }
    [JsonPropertyName("lDirty")]  public int   LDirty   { get; set; }
    [JsonPropertyName("pDirty")]  public int   PDirty   { get; set; }
    [JsonPropertyName("elems")]   public int   Elems    { get; set; }
}

public class DirtyEntryDto
{
    [JsonPropertyName("type")]      public string ElementType { get; set; } = "";
    [JsonPropertyName("id")]        public string? ElementId  { get; set; }
    [JsonPropertyName("classes")]   public string Classes     { get; set; } = "";
    [JsonPropertyName("isLayout")]  public bool   IsLayout    { get; set; }
    [JsonPropertyName("ts")]        public string Timestamp   { get; set; } = "";
}
