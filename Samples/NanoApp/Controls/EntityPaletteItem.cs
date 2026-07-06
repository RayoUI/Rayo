using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using Rayo.Rendering.Graphics.VectorGraphics;
using IRenderer = Rayo.Rendering.IRenderer;

namespace NanoApp.Controls;

public sealed class EntityPaletteItem : View<EntityPaletteItem>, IDraggable
{
    public const string DragDataType = "nano-scene-entity";

    private readonly SceneEntityKind _kind;
    private readonly string _label;
    private readonly Func<SceneEntityKind, float, float, bool>? _fallbackDrop;
    private float _lastDragX;
    private float _lastDragY;

    public EntityPaletteItem(
        SceneEntityKind kind,
        string label,
        Func<SceneEntityKind, float, float, bool>? fallbackDrop = null)
    {
        _kind = kind;
        _label = label;
        _fallbackDrop = fallbackDrop;
        Height = 70;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public bool IsDragging { get; set; }
    public float DragThreshold => 3f;
    public bool ShouldRenderWhileDragging => false;

    public DragData? OnDragStart(float mouseX, float mouseY)
    {
        IsDragging = true;
        _lastDragX = mouseX;
        _lastDragY = mouseY;
        MarkNeedsPaint();

        return new DragData(DragDataType, _kind, this)
            .WithAllowedEffects(DragDropEffect.Copy);
    }

    public void OnDragging(float mouseX, float mouseY)
    {
        _lastDragX = mouseX;
        _lastDragY = mouseY;
        MarkNeedsPaint();
    }

    public void OnDragEnd(bool wasDropped)
    {
        IsDragging = false;
        MarkNeedsPaint();

        if (!wasDropped)
        {
            _fallbackDrop?.Invoke(_kind, _lastDragX, _lastDragY);
        }
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth)
            ? availableWidth
            : 200;
        DesiredHeight = Height;
    }

    public override void Render(IRenderer renderer)
    {
        var background = IsDragging
            ? new Color(30, 41, 59, 150)
            : new Color(30, 41, 59);
        var border = IsDragging
            ? new Color(56, 189, 248)
            : new Color(71, 85, 105);

        renderer.DrawRoundedRect(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            8,
            background);
        renderer.DrawRoundedRectOutline(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            8,
            1.5f,
            border);

        DrawShape(
            renderer,
            _kind,
            ComputedX + 14,
            ComputedY + 14,
            54,
            42,
            SceneColors.Fill(_kind),
            new Color(224, 242, 254));

        var textSize = renderer.MeasureText(_label, 14);
        renderer.DrawText(
            _label,
            ComputedX + 82,
            ComputedY + (ComputedHeight - textSize.Y) / 2,
            new Color(226, 232, 240),
            14);
    }

    internal static void DrawShape(
        IRenderer renderer,
        SceneEntityKind kind,
        float x,
        float y,
        float width,
        float height,
        Color fill,
        Color stroke)
    {
        switch (kind)
        {
            case SceneEntityKind.Circle:
                renderer.DrawPathFillAndStroke(
                    VectorPath.Ellipse(
                        x + width / 2,
                        y + height / 2,
                        Math.Min(width, height) / 2,
                        Math.Min(width, height) / 2),
                    fill,
                    stroke,
                    2);
                break;

            case SceneEntityKind.Triangle:
                var points = new List<(float x, float y)>
                {
                    (x + width / 2, y),
                    (x + width, y + height),
                    (x, y + height)
                };
                renderer.DrawPolygon(points, fill);
                DrawPolygonOutline(renderer, points, stroke, 2);
                break;

            default:
                renderer.DrawRoundedRect(x, y, width, height, 6, fill);
                renderer.DrawRoundedRectOutline(x, y, width, height, 6, 2, stroke);
                break;
        }
    }

    private static void DrawPolygonOutline(
        IRenderer renderer,
        IReadOnlyList<(float x, float y)> points,
        Color color,
        float thickness)
    {
        for (var index = 0; index < points.Count; index++)
        {
            var from = points[index];
            var to = points[(index + 1) % points.Count];
            renderer.DrawLine(from.x, from.y, to.x, to.y, thickness, color);
        }
    }
}
