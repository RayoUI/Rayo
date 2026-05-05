using System.Runtime.CompilerServices;
using Rayo.Core;
using Rayo.Rendering;

namespace Rayo.DevTools;

/// <summary>
/// Renders a colour overlay on each element proportional to how frequently it marks
/// itself dirty. Green = rarely dirty, yellow = occasionally, red = every frame.
/// Scores decay each frame so the map reflects recent activity, not history.
/// </summary>
public static class DirtyHeatmap
{
    public static bool Enabled { get; set; } = false;

    // Score added per dirty event (layout costs more than paint).
    private const int LayoutScore = 4;
    private const int PaintScore  = 2;
    private const int MaxScore    = 120;

    // Per-frame exponential decay: score *= DecayFactor each render frame.
    private const float DecayFactor = 0.96f;

    // ConditionalWeakTable keeps element scores without preventing GC.
    private static readonly ConditionalWeakTable<VisualElement, HeatScore> _scores = new();

    private sealed class HeatScore { public float Score; }

    // -----------------------------------------------------------------------
    // Hooks — called from PerformanceTracker
    // -----------------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Increment(VisualElement element, bool isLayout)
    {
        if (!Enabled) return;
        var s = _scores.GetOrCreateValue(element);
        s.Score = Math.Min(s.Score + (isLayout ? LayoutScore : PaintScore), MaxScore);
    }

    // -----------------------------------------------------------------------
    // Render — called from UITree.Render() after the normal render pass
    // -----------------------------------------------------------------------
    public static void Render(IRenderer renderer, VisualElement root)
    {
        if (!Enabled) return;
        RenderElement(renderer, root);
    }

    private static void RenderElement(IRenderer renderer, VisualElement element)
    {
        if (!element.IsVisible) return;

        if (_scores.TryGetValue(element, out var score) && score.Score >= 1f)
        {
            // Decay this frame
            score.Score *= DecayFactor;

            float w = element.ComputedWidth;
            float h = element.ComputedHeight;
            if (w > 0 && h > 0 &&
                !float.IsNaN(w) && !float.IsNaN(h) &&
                !float.IsInfinity(w) && !float.IsInfinity(h))
            {
                renderer.DrawRoundedRect(
                    element.ComputedX, element.ComputedY, w, h,
                    4f, ScoreToColor(score.Score));
            }
        }

        foreach (var child in element.GetChildren())
            RenderElement(renderer, child);
    }

    private static Color ScoreToColor(float score)
    {
        // 0-40: green, 41-80: yellow, 81-120: red
        byte alpha = (byte)Math.Clamp(30 + score * 1.2f, 30, 160);
        if (score < 41f) return new Color((byte)0,   (byte)220, (byte)0,   alpha);
        if (score < 81f) return new Color((byte)220, (byte)200, (byte)0,   alpha);
        return               new Color((byte)220, (byte)30,  (byte)30,  alpha);
    }
}
