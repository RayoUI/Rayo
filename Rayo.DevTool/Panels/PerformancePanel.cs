using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.DevTool.Controls;
using Rayo.DevTool.Shared.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace Rayo.DevTool.Frames;

public class PerformanceFrame : Component
{
    private readonly DevToolState _state;

    public PerformanceFrame(DevToolState state) { _state = state; }

    public override VisualElement Build()
    {
        return new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        BuildOverlayToggles(),
                        BuildCurrentStats(),
                        BuildTimeline(),
                        BuildDirtyLog()
                    )
            );
    }

    // -------------------------------------------------------------------------
    // Current frame stats
    // -------------------------------------------------------------------------
    private VisualElement BuildCurrentStats() =>
        new Frame()
            .Background(DevToolTheme.Colors.Surface)
            .Padding(new Thickness(10, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(4)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new HStack()
                            .JustifyContent(JustifyContent.SpaceBetween)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Alignment(Alignment.Center)
                            .Children(
                                new Label("Current Frame")
                                    .FontSize(11)
                                    .Foreground(DevToolTheme.Colors.OnDisabled),
                                ClearButton(() => _state.PerformanceStats.Value = null)
                            ),
                        // Row 1 — FPS + frame time
                        new HStack()
                            .Spacing(16)
                            .Children(
                                StatCell("FPS",    _state.PerformanceStats.Map(s => $"{s?.Fps:F0}")),
                                StatCell("Frame",  _state.PerformanceStats.Map(s => $"{s?.FrameTimeMs:F1} ms")),
                                StatCell("Layout", _state.PerformanceStats.Map(s => $"{s?.MeasureTimeMs:F2} ms")),
                                StatCell("Render", _state.PerformanceStats.Map(s => $"{s?.RenderTimeMs:F2} ms"))
                            ),
                        // Row 2 — element / dirty counts
                        new HStack()
                            .Spacing(16)
                            .Children(
                                StatCell("Rendered", _state.PerformanceStats.Map(s => $"{s?.ElemRendered}")),
                                StatCell("Measured", _state.PerformanceStats.Map(s => $"{s?.ElemMeasured}")),
                                StatCell("L-Dirty",  _state.PerformanceStats.Map(s => $"{s?.LayoutDirty}"),
                                    _state.PerformanceStats.Map(s => (s?.LayoutDirty ?? 0) > 0
                                        ? DevToolTheme.Colors.Danger : DevToolTheme.Colors.Success)),
                                StatCell("P-Dirty",  _state.PerformanceStats.Map(s => $"{s?.PaintDirty}"),
                                    _state.PerformanceStats.Map(s => (s?.PaintDirty ?? 0) > 0
                                        ? DevToolTheme.Colors.Warning : DevToolTheme.Colors.Success))
                            )
                    )
            );

    private static VisualElement StatCell(string label,
        IReadableSignal<string> value,
        IReadableSignal<Color>? color = null)
    {
        var valLabel = new Label()
            .Text(value)
            .FontSize(13)
            .Foreground(color ?? new Signal<Color>(DevToolTheme.Colors.OnSurface));

        return new VStack()
            .Spacing(2)
            .Children(
                new Label(label).FontSize(9).Foreground(DevToolTheme.Colors.OnDisabled),
                valLabel
            );
    }

    // -------------------------------------------------------------------------
    // Overlay toggles
    // -------------------------------------------------------------------------
    private VisualElement BuildOverlayToggles() =>
        new Frame()
            .Background(DevToolTheme.Colors.SurfaceHover)
            .Padding(new Thickness(10, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new VStack()
                    .Spacing(6)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new Label("In-App Overlays")
                            .FontSize(11)
                            .Foreground(DevToolTheme.Colors.OnDisabled),
                        new HStack()
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .JustifyContent(JustifyContent.SpaceBetween)
                            .Alignment(Alignment.Center)
                            .Children(
                                new HStack()
                                    .Spacing(8)
                                    .Children(
                                        ToggleButton("Record",   _state.IsPerformancePanelOpen),
                                        ToggleButton("Heatmap",  _state.IsDirtyHeatmapEnabled),
                                        ToggleButton("Overdraw", _state.IsOverdrawEnabled)
                                    ),
                                new Button()
                                    .Text("Clear All")
                                    .Variant(ButtonVariant.Danger)
                                    .Height(26)
                                    .FontSize(11)
                                    .OnTapped(async () =>
                                    {
                                        _state.PerformanceStats.Value = null;
                                        if (_state.Client.IsConnected)
                                            await _state.Client.ClearDirtyLogAsync();
                                        _state.DirtyLog.Value = new List<DirtyEntryDto>();
                                    })
                            )
                    )
            );

    private static VisualElement ClearButton(Action onTap) =>
        new Button()
            .Text("Clear")
            .Variant(ButtonVariant.Danger)
            .Height(20)
            .FontSize(10)
            .Width(50)
            .OnTapped(onTap);

    private static VisualElement ToggleButton(string label,
        IWritableSignal<bool> state)
    {
        return new Button()
            .Text(state.Map(on => $"{label}: {(on ? "ON" : "OFF")}"))
            .Variant(state.Map(on => on ? ButtonVariant.Primary : ButtonVariant.Secondary))
            .Height(26)
            .FontSize(11)
            .OnTapped(() => { state.Value = !state.Value; });
    }

    // -------------------------------------------------------------------------
    // Frame timeline chart
    // -------------------------------------------------------------------------
    private VisualElement BuildTimeline() =>
        new Frame()
            .Background(DevToolTheme.Colors.Surface)
            .Padding(new Thickness(10, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(6)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new Label("Frame Timeline  (last 120 frames)")
                            .FontSize(11)
                            .Foreground(DevToolTheme.Colors.OnDisabled),
                        new FrameTimelineChart()
                            .Height(80)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Background(DevToolTheme.Colors.Background)
                            .BorderRadius(4f)
                            .Frames(_state.PerformanceStats.Map(s => s?.Frames ?? new List<FrameStatsDto>())),
                        BuildTimelineLegend()
                    )
            );

    private static VisualElement BuildTimelineLegend() =>
        new HStack()
            .Spacing(14)
            .Children(
                LegendDot(DevToolTheme.Colors.Success, "Render"),
                LegendDot(DevToolTheme.Colors.Primary, "Layout"),
                LegendDot(DevToolTheme.Colors.Danger, "Events")
            );

    private static VisualElement LegendDot(Color color, string label) =>
        new HStack()
            .Spacing(4)
            .Alignment(Alignment.Center)
            .Children(
                new Frame().Width(8).Height(8).Background(color).BorderRadius(2f),
                new Label(label).FontSize(10).Foreground(DevToolTheme.Colors.OnDisabled)
            );

    // -------------------------------------------------------------------------
    // Dirty log
    // -------------------------------------------------------------------------
    private VisualElement BuildDirtyLog() =>
        new Frame()
            .Background(DevToolTheme.Colors.Surface)
            .Padding(new Thickness(10, 8))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(4)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new HStack()
                            .JustifyContent(JustifyContent.SpaceBetween)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Children(
                                new Label("Dirty Log  (last 150 entries)")
                                    .FontSize(11)
                                    .Foreground(DevToolTheme.Colors.OnDisabled),
                                new Button()
                                    .Text("Clear")
                                    .Variant(ButtonVariant.Danger)
                                    .Height(20)
                                    .FontSize(10)
                                    .Width(50)
                                    .OnTapped(async () =>
                                    {
                                        if (_state.Client.IsConnected)
                                            await _state.Client.ClearDirtyLogAsync();
                                        _state.DirtyLog.Value = new List<DirtyEntryDto>();
                                    })
                            ),
                        new Frame()
                            .Background(DevToolTheme.Colors.Background)
                            .BorderRadius(4f)
                            .Padding(new Thickness(6, 4))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Content(
                                _state.DirtyLog.Map(log => (VisualElement?)BuildDirtyLogRows(log))
                            )
                    )
            );

    private static VisualElement BuildDirtyLogRows(List<DirtyEntryDto> log)
    {
        if (log.Count == 0)
        {
            return new Label("No dirty events recorded.")
                .FontSize(11)
                .Foreground(DevToolTheme.Colors.OnDisabled);
        }

        var rows = new List<VisualElement>();
        // Show newest entries at top
        foreach (var entry in ((IEnumerable<DirtyEntryDto>)log).Reverse().Take(80))
        {
            var typeColor  = entry.IsLayout ? DevToolTheme.Colors.Danger : DevToolTheme.Colors.Warning;
            var kindLabel  = entry.IsLayout ? "Layout" : "Paint";
            var nameStr    = string.IsNullOrEmpty(entry.ElementId)
                ? entry.ElementType
                : $"{entry.ElementType}#{entry.ElementId}";
            if (!string.IsNullOrEmpty(entry.Classes))
                nameStr += $".{entry.Classes.Replace(" ", ".")}";

            rows.Add(new HStack()
                .Spacing(6)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Children(
                    new Label(entry.Timestamp)
                        .FontSize(9)
                        .Foreground(DevToolTheme.Colors.OnDisabled)
                        .Width(72),
                    new Label(kindLabel)
                        .FontSize(10)
                        .Foreground(typeColor)
                        .Width(42),
                    new Label(nameStr)
                        .FontSize(10)
                        .Foreground(DevToolTheme.Colors.OnSurface)
                ));
        }

        return new VStack()
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(rows.ToArray());
    }
}
