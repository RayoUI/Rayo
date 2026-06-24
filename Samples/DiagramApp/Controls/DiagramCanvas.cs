using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace DiagramApp.Controls;

public enum DiagramTool
{
    Select,
    Rectangle,
    Ellipse,
    Diamond,
    Connect
}

public enum DiagramShapeKind
{
    Rectangle,
    Ellipse,
    Diamond
}

internal sealed class DiagramNode
{
    public int Id { get; init; }
    public DiagramShapeKind Kind { get; init; }
    public string Text { get; init; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; init; } = 136f;
    public float Height { get; init; } = 76f;
    public Color Fill { get; init; }

    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);
}

internal sealed record DiagramEdge(int FromId, int ToId);

public class DiagramCanvas : View<DiagramCanvas>, IPointerHandler, IDropTarget
{
    private readonly List<DiagramNode> _nodes = new();
    private readonly List<DiagramEdge> _edges = new();

    private int _nextId = 1;
    private DiagramNode? _draggedNode;
    private DiagramNode? _connectSource;
    private Vector2 _viewportOffset;
    private Vector2 _dragOffset;
    private Vector2 _previewPoint;
    private bool _isConnecting;
    private bool _isPanning;
    private Vector2 _panLastPosition;
    private DiagramShapeKind? _dropPreviewKind;
    private Vector2 _dropPreviewPoint;
    private DateTime _lastClickTime = DateTime.MinValue;
    private Vector2 _lastClickPosition;

    public bool IsDropTargetActive { get; set; }
    public DropConstraints? Constraints => null;
    public DragDropEffect? AllowedEffects => DragDropEffect.Copy;

    public DiagramTool ActiveTool { get; set; } = DiagramTool.Select;

    public DiagramCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        AddNode(DiagramShapeKind.Rectangle, 120, 120);
        AddNode(DiagramShapeKind.Diamond, 400, 130);
        AddNode(DiagramShapeKind.Ellipse, 680, 320);
        _edges.Add(new DiagramEdge(1, 2));
        _edges.Add(new DiagramEdge(2, 3));
    }

    public void Clear()
    {
        _nodes.Clear();
        _edges.Clear();
        _nextId = 1;
        _draggedNode = null;
        _connectSource = null;
        _isConnecting = false;
        _isPanning = false;
        _viewportOffset = Vector2.Zero;
        MarkNeedsPaint();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth) ? availableWidth : 800f;
        DesiredHeight = availableHeight > 0 && !float.IsInfinity(availableHeight) ? availableHeight : 600f;
    }

    public override void Render(IRenderer renderer)
    {
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, new Color(236, 240, 245));
        RenderGrid(renderer);

        if (IsDropTargetActive)
        {
            renderer.DrawRectOutline(ComputedX + 3f, ComputedY + 3f, ComputedWidth - 6f, ComputedHeight - 6f, 2f, new Color(246, 196, 92));
        }

        foreach (var edge in _edges)
        {
            var from = FindNode(edge.FromId);
            var to = FindNode(edge.ToId);
            if (from is not null && to is not null)
            {
                DrawEdge(renderer, ToWorld(from.Center), ToWorld(to.Center), new Color(135, 168, 210), 2.4f);
            }
        }

        if (_isConnecting && _connectSource is not null)
        {
            DrawConnectionPreview(renderer, ToWorld(_connectSource.Center), _previewPoint, new Color(246, 196, 92), 2f);
            renderer.DrawCircle(_previewPoint.X, _previewPoint.Y, 5f, new Color(246, 196, 92));
        }

        foreach (var node in _nodes)
        {
            DrawNode(renderer, node);
        }

        if (_dropPreviewKind is { } previewKind)
        {
            DrawNodeGhost(renderer, previewKind, _dropPreviewPoint.X - 68f, _dropPreviewPoint.Y - 38f);
        }

        renderer.PopScissor();
    }

    void IPointerHandler.OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        var point = e.Position;
        if (TryDeleteEdgeOnDoubleClick(point, e.Timestamp))
        {
            e.Handled = true;
            return;
        }

        var hit = HitTest(point);

        if (ActiveTool == DiagramTool.Connect)
        {
            if (hit is null)
            {
                _connectSource = null;
                _isConnecting = false;
                MarkNeedsPaint();
                return;
            }

            _connectSource = hit;
            _previewPoint = point;
            _isConnecting = true;
            MarkNeedsPaint();
            e.Handled = true;
            return;
        }

        if (ActiveTool == DiagramTool.Select)
        {
            if (hit is not null)
            {
                _draggedNode = hit;
                var canvasPoint = ToCanvas(point);
                _dragOffset = new Vector2(canvasPoint.X - hit.X, canvasPoint.Y - hit.Y);
                e.Handled = true;
            }
            else
            {
                _isPanning = true;
                _panLastPosition = point;
                e.Handled = true;
            }
            return;
        }

        var newNodePoint = ToCanvas(point);
        AddNode(ToShapeKind(ActiveTool), newNodePoint.X - 68f, newNodePoint.Y - 38f);
        ActiveTool = DiagramTool.Select;
        MarkNeedsPaint();
        e.Handled = true;
    }

    void IPointerHandler.OnPointerMoved(PointerEventArgs e)
    {
        if (_isConnecting)
        {
            _previewPoint = e.Position;
            MarkNeedsPaint();
            return;
        }

        if (_isPanning)
        {
            var delta = e.Position - _panLastPosition;
            _panLastPosition = e.Position;
            _viewportOffset -= delta;
            MarkNeedsPaint();
            return;
        }

        if (_draggedNode is null)
        {
            return;
        }

        var canvasPoint = ToCanvas(e.Position);
        _draggedNode.X = canvasPoint.X - _dragOffset.X;
        _draggedNode.Y = canvasPoint.Y - _dragOffset.Y;
        MarkNeedsPaint();
    }

    void IPointerHandler.OnPointerReleased(PointerEventArgs e)
    {
        if (_isConnecting && _connectSource is not null)
        {
            var target = HitTest(e.Position);
            if (target is not null && target != _connectSource && !_edges.Any(edge => edge.FromId == _connectSource.Id && edge.ToId == target.Id))
            {
                _edges.Add(new DiagramEdge(_connectSource.Id, target.Id));
            }
        }

        _draggedNode = null;
        _connectSource = null;
        _isConnecting = false;
        _isPanning = false;
        MarkNeedsPaint();
    }

    public bool CanAcceptDataType(string dataType) => dataType == DiagramToolboxItem.DragDataType;

    public bool OnDragEnter(DragData dragData)
    {
        IsDropTargetActive = true;
        if (dragData.Data is DiagramShapeKind kind)
        {
            _dropPreviewKind = kind;
        }
        MarkNeedsPaint();
        return dragData.Data is DiagramShapeKind;
    }

    public void OnDragOver(DragData dragData, float mouseX, float mouseY)
    {
        if (dragData.Data is DiagramShapeKind kind)
        {
            _dropPreviewKind = kind;
            _dropPreviewPoint = ToCanvas(new Vector2(mouseX, mouseY));
            MarkNeedsPaint();
        }
    }

    public void OnDragLeave(DragData dragData)
    {
        IsDropTargetActive = false;
        ClearDropPreview();
        MarkNeedsPaint();
    }

    public bool OnDrop(DragData dragData, float mouseX, float mouseY)
    {
        IsDropTargetActive = false;

        if (dragData.Data is not DiagramShapeKind kind)
        {
            MarkNeedsPaint();
            return false;
        }

        var dropPoint = ToCanvas(new Vector2(mouseX, mouseY));
        AddNode(kind, dropPoint.X - 68f, dropPoint.Y - 38f);
        ClearDropPreview();
        MarkNeedsPaint();
        return true;
    }

    private void AddNode(DiagramShapeKind kind, float localX, float localY)
    {
        var node = new DiagramNode
        {
            Id = _nextId++,
            Kind = kind,
            Text = kind switch
            {
                DiagramShapeKind.Rectangle => "Process",
                DiagramShapeKind.Ellipse => "Start / End",
                DiagramShapeKind.Diamond => "Decision",
                _ => "Shape"
            },
            X = localX,
            Y = localY,
            Fill = kind switch
            {
                DiagramShapeKind.Rectangle => new Color(62, 126, 214),
                DiagramShapeKind.Ellipse => new Color(54, 172, 130),
                DiagramShapeKind.Diamond => new Color(222, 162, 70),
                _ => new Color(110, 130, 160)
            }
        };

        _nodes.Add(node);
    }

    private DiagramNode? HitTest(Vector2 point)
    {
        var canvas = ToCanvas(point);
        float localX = canvas.X;
        float localY = canvas.Y;

        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            var node = _nodes[i];
            if (localX >= node.X && localX <= node.X + node.Width &&
                localY >= node.Y && localY <= node.Y + node.Height)
            {
                return node;
            }
        }

        return null;
    }

    private DiagramNode? FindNode(int id) => _nodes.FirstOrDefault(node => node.Id == id);

    private Vector2 ToCanvas(Vector2 world)
        => new(world.X - ComputedX + _viewportOffset.X, world.Y - ComputedY + _viewportOffset.Y);

    private Vector2 ToWorld(Vector2 canvas)
        => new(ComputedX + canvas.X - _viewportOffset.X, ComputedY + canvas.Y - _viewportOffset.Y);

    private bool TryDeleteEdgeOnDoubleClick(Vector2 point, DateTime timestamp)
    {
        double elapsedMs = (timestamp - _lastClickTime).TotalMilliseconds;
        float distance = Vector2.Distance(point, _lastClickPosition);
        bool isDoubleClick = elapsedMs <= 350 && distance <= 18f;

        _lastClickTime = timestamp;
        _lastClickPosition = point;

        if (!isDoubleClick)
        {
            return false;
        }

        var edge = FindEdgeAt(point);
        if (edge is null)
        {
            return false;
        }

        _edges.Remove(edge);
        MarkNeedsPaint();
        return true;
    }

    private DiagramEdge? FindEdgeAt(Vector2 point)
    {
        foreach (var edge in _edges)
        {
            var from = FindNode(edge.FromId);
            var to = FindNode(edge.ToId);
            if (from is null || to is null)
            {
                continue;
            }

            if (DistanceToEdge(point, ToWorld(from.Center), ToWorld(to.Center)) <= 12f)
            {
                return edge;
            }
        }

        return null;
    }

    private static float DistanceToEdge(Vector2 point, Vector2 from, Vector2 to)
    {
        const int samples = 36;
        float distance = Vector2.Distance(from, to);
        float control = Math.Clamp(distance * 0.35f, 40f, 180f);
        var previous = from;
        float best = float.MaxValue;

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            var current = CubicBezier(
                from,
                new Vector2(from.X + control, from.Y),
                new Vector2(to.X - control, to.Y),
                to,
                t);

            best = Math.Min(best, DistanceToSegment(point, previous, current));
            previous = current;
        }

        return best;
    }

    private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lengthSquared = Vector2.Dot(ab, ab);
        if (lengthSquared <= 0.001f)
        {
            return Vector2.Distance(point, a);
        }

        float t = Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, a + ab * t);
    }

    private static DiagramShapeKind ToShapeKind(DiagramTool tool) => tool switch
    {
        DiagramTool.Ellipse => DiagramShapeKind.Ellipse,
        DiagramTool.Diamond => DiagramShapeKind.Diamond,
        _ => DiagramShapeKind.Rectangle
    };

    private void DrawNode(IRenderer renderer, DiagramNode node)
    {
        var origin = ToWorld(new Vector2(node.X, node.Y));
        float x = origin.X;
        float y = origin.Y;
        float w = node.Width;
        float h = node.Height;
        bool selectedForConnection = _connectSource == node;

        DrawNodeShadow(renderer, node.Kind, x + 5f, y + 6f, w, h);

        switch (node.Kind)
        {
            case DiagramShapeKind.Ellipse:
                renderer.DrawPathFillAndStroke(
                    Rayo.Rendering.Graphics.VectorGraphics.VectorPath.Ellipse(x + w / 2f, y + h / 2f, w / 2f, h / 2f),
                    node.Fill,
                    selectedForConnection ? new Color(246, 196, 92) : new Color(205, 218, 232),
                    selectedForConnection ? 3f : 2f);
                break;
            case DiagramShapeKind.Diamond:
                var points = new List<(float x, float y)>
                {
                    (x + w / 2f, y),
                    (x + w, y + h / 2f),
                    (x + w / 2f, y + h),
                    (x, y + h / 2f)
                };
                renderer.DrawPolygon(points, node.Fill);
                DrawPolygonOutline(renderer, points, selectedForConnection ? new Color(246, 196, 92) : new Color(205, 218, 232), selectedForConnection ? 3f : 2f);
                break;
            default:
                renderer.DrawRoundedRect(x, y, w, h, 8f, node.Fill);
                renderer.DrawRoundedRectOutline(x, y, w, h, 8f, selectedForConnection ? 3f : 2f, selectedForConnection ? new Color(246, 196, 92) : new Color(205, 218, 232));
                break;
        }

        var textSize = renderer.MeasureText(node.Text, 15f);
        renderer.DrawText(node.Text, x + (w - textSize.X) / 2f, y + (h - textSize.Y) / 2f, Color.White, 15f);
    }

    private static void DrawNodeShadow(IRenderer renderer, DiagramShapeKind kind, float x, float y, float w, float h)
    {
        var shadowColor = new Color(34, 42, 54, 82);

        switch (kind)
        {
            case DiagramShapeKind.Ellipse:
                renderer.DrawPath(
                    Rayo.Rendering.Graphics.VectorGraphics.VectorPath.Ellipse(x + w / 2f, y + h / 2f, w / 2f, h / 2f),
                    shadowColor);
                break;
            case DiagramShapeKind.Diamond:
                renderer.DrawPolygon(
                    [
                        (x + w / 2f, y),
                        (x + w, y + h / 2f),
                        (x + w / 2f, y + h),
                        (x, y + h / 2f)
                    ],
                    shadowColor);
                break;
            default:
                renderer.DrawRoundedRect(x, y, w, h, 8f, shadowColor);
                break;
        }
    }

    private void DrawNodeGhost(IRenderer renderer, DiagramShapeKind kind, float localX, float localY)
    {
        var origin = ToWorld(new Vector2(localX, localY));
        float x = origin.X;
        float y = origin.Y;
        const float w = 136f;
        const float h = 76f;
        var fill = kind switch
        {
            DiagramShapeKind.Rectangle => new Color(62, 126, 214, 135),
            DiagramShapeKind.Ellipse => new Color(54, 172, 130, 135),
            DiagramShapeKind.Diamond => new Color(222, 162, 70, 135),
            _ => new Color(110, 130, 160, 135)
        };

        switch (kind)
        {
            case DiagramShapeKind.Ellipse:
                renderer.DrawPathFillAndStroke(
                    Rayo.Rendering.Graphics.VectorGraphics.VectorPath.Ellipse(x + w / 2f, y + h / 2f, w / 2f, h / 2f),
                    fill,
                    new Color(246, 196, 92, 210),
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
                DrawPolygonOutline(renderer, points, new Color(246, 196, 92, 210), 2f);
                break;
            default:
                renderer.DrawRoundedRect(x, y, w, h, 8f, fill);
                renderer.DrawRoundedRectOutline(x, y, w, h, 8f, 2f, new Color(246, 196, 92, 210));
                break;
        }
    }

    private void ClearDropPreview()
    {
        _dropPreviewKind = null;
        _dropPreviewPoint = default;
    }

    private void RenderGrid(IRenderer renderer)
    {
        const float spacing = 32f;
        var dotColor = new Color(168, 178, 192, 155);

        float firstCanvasX = MathF.Floor(_viewportOffset.X / spacing) * spacing;
        float firstCanvasY = MathF.Floor(_viewportOffset.Y / spacing) * spacing;

        for (float canvasX = firstCanvasX; canvasX < _viewportOffset.X + ComputedWidth + spacing; canvasX += spacing)
        {
            for (float canvasY = firstCanvasY; canvasY < _viewportOffset.Y + ComputedHeight + spacing; canvasY += spacing)
            {
                var p = ToWorld(new Vector2(canvasX, canvasY));
                renderer.DrawCircle(p.X, p.Y, 1f, dotColor);
            }
        }
    }

    private static void DrawEdge(IRenderer renderer, Vector2 from, Vector2 to, Color color, float thickness)
    {
        float distance = Vector2.Distance(from, to);
        float control = Math.Clamp(distance * 0.35f, 40f, 180f);
        renderer.DrawCubicBezier(from.X, from.Y, from.X + control, from.Y, to.X - control, to.Y, to.X, to.Y, color, thickness);
    }

    private static void DrawConnectionPreview(IRenderer renderer, Vector2 from, Vector2 to, Color color, float thickness)
    {
        renderer.DrawLine(from.X, from.Y, to.X, to.Y, thickness, color);
    }

    private static void DrawPolygonOutline(IRenderer renderer, List<(float x, float y)> points, Color color, float thickness)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var from = points[i];
            var to = points[(i + 1) % points.Count];
            renderer.DrawLine(from.x, from.y, to.x, to.y, thickness, color);
        }
    }
}
