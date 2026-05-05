using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.DevTool.Shared.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace Rayo.DevTool.Controls;

/// <summary>
/// Custom-drawn control that renders the frame timeline chart in a single pass.
/// All bars (Events / Layout / Render phases) are drawn directly via IRenderer
/// without constructing any child element tree.
/// </summary>
public class FrameTimelineChart : View<FrameTimelineChart>
{
    private static readonly Color ColorEvents = new(200,  60,  60);
    private static readonly Color ColorLayout = new( 59, 130, 246);
    private static readonly Color ColorRender = new( 34, 197,  94);
    private static readonly Color ColorEmpty  = new( 80,  80, 100);

    #region Frames
    /// <summary>
    /// Frame history to visualise. Triggers a repaint on change.
    /// </summary>
    [PaintProperty]
    public List<FrameStatsDto>? Frames
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth  = Width  > 0 ? Width  : availableWidth;
        DesiredHeight = Height > 0 ? Height : availableHeight;
    }

    public override void Render(IRenderer renderer)
    {
        float x      = ComputedX;
        float y      = ComputedY;
        float width  = ComputedWidth;
        float height = ComputedHeight;

        // Background (respects BorderRadius set on the element)
        if (Background.PrimaryColor.A > 0)
        {
            float radius = BorderRadius.TopLeft;
            if (radius > 0)
                renderer.DrawRoundedRect(x, y, width, height, radius, Background);
            else
                renderer.DrawRect(x, y, width, height, Background);
        }

        var frames = Frames;
        if (frames == null || frames.Count == 0)
        {
            const string placeholder = "Waiting for data…";
            var textSize = renderer.MeasureText(placeholder, 11);
            renderer.DrawText(
                placeholder,
                x + (width  - textSize.X) / 2f,
                y + (height - textSize.Y) / 2f,
                ColorEmpty, 11);
            return;
        }

        // Skip leading zeros from circular buffer, keep last 60 frames
        var visible = frames.Where(f => f.FrameMs > 0).TakeLast(60).ToList();
        if (visible.Count == 0) return;

        // All three phases share the same vertical scale so heights are comparable
        float maxBarH   = height - 10f;
        float commonMax = Math.Max(
            visible.Max(f => Math.Max(f.EvtMs, Math.Max(f.MeasMs, f.RendMs))),
            1f);

        // Each group: 3 bars × 2 px = 6 px. Distribute leftover width as gaps (SpaceBetween)
        const float barW      = 2f;
        const float groupSpan = barW * 3;
        int   count           = visible.Count;
        float gap             = count > 1
            ? Math.Max((width - count * groupSpan) / (count - 1), 1f)
            : 0f;

        float bottom = y + height;

        for (int i = 0; i < count; i++)
        {
            var   f      = visible[i];
            float groupX = x + i * (groupSpan + gap);

            float evtH  = f.EvtMs  > 0 ? Math.Max(f.EvtMs  / commonMax * maxBarH, 1f) : 0f;
            float measH = f.MeasMs > 0 ? Math.Max(f.MeasMs / commonMax * maxBarH, 1f) : 0f;
            float rendH = f.RendMs > 0 ? Math.Max(f.RendMs / commonMax * maxBarH, 1f) : 0f;

            if (evtH  > 0) renderer.DrawRect(groupX,          bottom - evtH,  barW, evtH,  ColorEvents);
            if (measH > 0) renderer.DrawRect(groupX + barW,   bottom - measH, barW, measH, ColorLayout);
            if (rendH > 0) renderer.DrawRect(groupX + barW*2, bottom - rendH, barW, rendH, ColorRender);
        }
    }
}
