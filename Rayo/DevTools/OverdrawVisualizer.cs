using Rayo.Core;
using Rayo.Rendering;

namespace Rayo.DevTools;

/// <summary>
/// Visualises pixel overdraw by drawing a semi-transparent tinted rect over every
/// rendered element. Overlapping elements compound the colour, making hot spots
/// clearly visible (darker orange = more layers on top of each other).
/// Toggle via <see cref="Enabled"/> or the DevTool toolbar.
/// </summary>
public static class OverdrawVisualizer
{
    public static bool Enabled { get; set; } = false;

    // Colour used per draw layer — alpha compounds when rects overlap.
    private static readonly Color _tint = new Color(255, 120, 30, 38);

    // -----------------------------------------------------------------------
    // Hook — called from UITree.RenderElement() for every visible element
    // -----------------------------------------------------------------------
    internal static void RecordElement(VisualElement element)
    {
        // Intentionally left empty — we render live inside RenderOverlay(),
        // traversing the tree a second time so we always draw on top of everything.
    }

    // -----------------------------------------------------------------------
    // Render — called from UITree.Render() as the very last overlay pass
    // -----------------------------------------------------------------------
    public static void Render(IRenderer renderer, VisualElement root)
    {
        if (!Enabled) return;
        RenderElement(renderer, root);
    }

    private static void RenderElement(IRenderer renderer, VisualElement element)
    {
        if (!element.IsVisible) return;

        float w = element.ComputedWidth;
        float h = element.ComputedHeight;
        if (w > 0.5f && h > 0.5f &&
            !float.IsNaN(w) && !float.IsNaN(h) &&
            !float.IsInfinity(w) && !float.IsInfinity(h))
        {
            renderer.DrawRect(element.ComputedX, element.ComputedY, w, h, _tint);
        }

        foreach (var child in element.GetChildren())
            RenderElement(renderer, child);
    }
}
