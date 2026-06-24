using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace DiagramApp.Controls;

public class DiagramToolboxItem : View<DiagramToolboxItem>, IDraggable
{
    public const string DragDataType = "diagram-shape";

    private readonly DiagramShapeKind _kind;
    private readonly string _label;

    public bool IsDragging { get; set; }
    public float DragThreshold => 3f;
    public bool ShouldRenderWhileDragging => false;

    public DiagramToolboxItem(DiagramShapeKind kind, string label)
    {
        _kind = kind;
        _label = label;
        Height = 76f;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public DragData? OnDragStart(float mouseX, float mouseY)
    {
        IsDragging = true;
        MarkNeedsPaint();
        return new DragData(DragDataType, _kind, this)
            .WithAllowedEffects(DragDropEffect.Copy);
    }

    public void OnDragging(float mouseX, float mouseY)
    {
        MarkNeedsPaint();
    }

    public void OnDragEnd(bool wasDropped)
    {
        IsDragging = false;
        MarkNeedsPaint();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth) ? availableWidth : 190f;
        DesiredHeight = Height;
    }

    public override void Render(IRenderer renderer)
    {
        var bg = IsDragging ? new Color(44, 50, 60, 170) : new Color(42, 48, 58);
        var border = IsDragging ? new Color(246, 196, 92) : new Color(78, 90, 108);
        renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, 8f, bg);
        renderer.DrawRoundedRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, 8f, 1.5f, border);

        float shapeX = ComputedX + 16f;
        float shapeY = ComputedY + 14f;
        float shapeW = 62f;
        float shapeH = 42f;
        var fill = _kind switch
        {
            DiagramShapeKind.Rectangle => new Color(62, 126, 214),
            DiagramShapeKind.Ellipse => new Color(54, 172, 130),
            DiagramShapeKind.Diamond => new Color(222, 162, 70),
            _ => new Color(110, 130, 160)
        };

        DrawShape(renderer, _kind, shapeX, shapeY, shapeW, shapeH, fill, new Color(218, 228, 240));

        var textSize = renderer.MeasureText(_label, 15f);
        renderer.DrawText(_label, ComputedX + 94f, ComputedY + (ComputedHeight - textSize.Y) / 2f, Color.White, 15f);
    }

    private static void DrawShape(IRenderer renderer, DiagramShapeKind kind, float x, float y, float w, float h, Color fill, Color stroke)
    {
        switch (kind)
        {
            case DiagramShapeKind.Ellipse:
                renderer.DrawPathFillAndStroke(
                    Rayo.Rendering.Graphics.VectorGraphics.VectorPath.Ellipse(x + w / 2f, y + h / 2f, w / 2f, h / 2f),
                    fill,
                    stroke,
                    2f);
                break;
            case DiagramShapeKind.Diamond:
                var points = new List<(float x, float y)>
                {
                    (x + w / 2f, y),
                    (x + w, y + h / 2f),
                    (x + w / 2f, y + h),
                    (x, y + h / 2f)
                };
                renderer.DrawPolygon(points, fill);
                for (int i = 0; i < points.Count; i++)
                {
                    var from = points[i];
                    var to = points[(i + 1) % points.Count];
                    renderer.DrawLine(from.x, from.y, to.x, to.y, 2f, stroke);
                }
                break;
            default:
                renderer.DrawRoundedRect(x, y, w, h, 8f, fill);
                renderer.DrawRoundedRectOutline(x, y, w, h, 8f, 2f, stroke);
                break;
        }
    }
}
