using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using VisualScripting.Models;
using VisualScripting.NodeTypes;
using Rayo.Core.Input;
using System.Numerics;

#nullable disable

namespace VisualScripting.Controls;

/// <summary>
/// Main node editor area built on a Rayo Absolute layout.
///
/// The Absolute provides absolute positioning for ScriptNode controls.
/// The ConnectionOverlay (always input-transparent) is kept as the last
/// Absolute child so it renders on top of all nodes.
///
/// Connection drag lifecycle (handled inside ScriptNode via IInputHandler):
///   MouseDown on port  → OnConnectionStarted  → overlay shows preview wire
///   MouseDrag          → OnConnectionDragging  → overlay updates wire endpoint
///   MouseUp on port    → OnConnectionReleased  → graph.AddConnection
///   MouseUp elsewhere  → OnConnectionCancelled → overlay clears preview
///
/// Drag-and-drop from palette:
///   IDraggable PaletteItem is dragged → DragDropManager finds this IDropTarget
///   → OnDrop converts world coordinates to canvas-local and spawns the node.
/// </summary>
public class NodeEditorCanvas : CompositeView<NodeEditorCanvas>,
    IDropTarget,
    IInputHandler,
    IPointerHandler,
    IScrollable
{
    public NodeGraph Graph { get; }

    private readonly Absolute _canvas;
    private readonly ConnectionOverlay _overlay;
    private readonly List<ScriptNode> _nodeControls = new();
    private readonly Dictionary<int, Vector2> _touchPointers = new();

    private float _zoom = 1f;
    private Vector2 _panOffset;
    private float _lastPinchDistance;
    private Vector2 _lastPinchCenter;
    private const float MinZoom = 0.35f;
    private const float MaxZoom = 2.5f;

    // Stagger spawn position for toolbar-added nodes
    private float _nextNodeX = 60f;
    private float _nextNodeY = 60f;

    // IDropTarget state
    public bool IsDropTargetActive { get; set; }
    public DropConstraints Constraints => null;
    public DragDropEffect? AllowedEffects => null; // Accept any drag effect

    /// <summary>Fired when the user clicks (focuses) a node body.</summary>
    public Action<ScriptNode> OnNodeSelected { get; set; }

    // Right-button pan state
    private bool  _isPanning;
    private float _panLastX;
    private float _panLastY;

    // Double-tap state for edge deletion
    private DateTime _lastTapTime = DateTime.MinValue;
    private float    _lastTapX;
    private float    _lastTapY;
    private const float DoubleTapMaxMs   = 350f;
    private const float DoubleTapMaxDist = 20f;

    // Hit tolerance in pixels for clicking on a Bezier wire
    private const float ConnectionHitTolerance = 12f;

    public NodeEditorCanvas(NodeGraph graph)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment   = VerticalAlignment.Stretch;
        ClipToBounds = true;

        _overlay = new ConnectionOverlay(Graph);
        _overlay.ToScreenPosition = ToScreenPosition;

        _canvas = new Absolute();
        _canvas.Background   = Color.Transparent;

        _canvas.AddChild(_overlay); // Overlay is always the first child (renders behind nodes)

        AddChild(_canvas);

        Graph.Changed += () => _overlay.MarkNeedsPaint();

        // Register existing nodes if graph already has nodes (MVVM scenario)
        foreach (var node in Graph.Nodes)
        {
            RegisterNodeControl(node);
        }
    }

    // -------------------------------------------------------------------------
    // Public API (called by NodeToolbar on click, or IDropTarget on drop)
    // -------------------------------------------------------------------------

    public void SpawnNode(NodeTypeId type)
    {
        var model = NodeFactory.Create(type, _nextNodeX, _nextNodeY);
        _nextNodeX = (_nextNodeX + 30f) % 480f + 20f;
        _nextNodeY = (_nextNodeY + 25f) % 380f + 20f;

        Graph.Nodes.Add(model);
        RegisterNodeControl(model);
        _overlay.MarkNeedsPaint();
    }

    public void SpawnNodeAt(NodeTypeId type, float worldX, float worldY)
    {
        var canvasPoint = ToCanvasPosition(new Vector2(worldX, worldY));
        float localX = canvasPoint.X - _canvas.ComputedX;
        float localY = canvasPoint.Y - _canvas.ComputedY;

        var model = NodeFactory.Create(type, localX, localY);
        Graph.Nodes.Add(model);
        RegisterNodeControl(model);
        _overlay.MarkNeedsPaint();
    }

    // -------------------------------------------------------------------------
    // IDropTarget — accepts palette-node drags
    // -------------------------------------------------------------------------

    public bool CanAcceptDataType(string dataType) => dataType == "palette-node";

    public bool OnDragEnter(DragData dragData)
    {
        IsDropTargetActive = true;
        MarkNeedsPaint();
        return true;
    }

    public void OnDragOver(DragData dragData, float mouseX, float mouseY) { }

    public void OnDragLeave(DragData dragData)
    {
        IsDropTargetActive = false;
        MarkNeedsPaint();
    }

    public bool OnDrop(DragData dragData, float mouseX, float mouseY)
    {
        IsDropTargetActive = false;

        if (dragData.Data is NodeTypeId nodeType)
        {
            SpawnNodeAt(nodeType, mouseX, mouseY);
            return true;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // IInputHandler — double-tap on an edge to delete it
    // -------------------------------------------------------------------------

    public bool CanHandleInput => true;

    public bool HandleInput(InputEventArgs args)
    {
        // ---- Right-button pan ----
        if (args.Button == InputMouseButton.Right)
        {
            switch (args.EventType)
            {
                case InputEventType.MouseDown:
                    _isPanning = true;
                    _panLastX  = args.Position.X;
                    _panLastY  = args.Position.Y;
                    args.Handled = true;
                    return true;

                case InputEventType.MouseDrag:
                    if (_isPanning)
                    {
                        float dx = args.Position.X - _panLastX;
                        float dy = args.Position.Y - _panLastY;
                        _panLastX = args.Position.X;
                        _panLastY = args.Position.Y;
                        ApplyPan(dx, dy);
                    }
                    return true;

                case InputEventType.MouseUp:
                    _isPanning = false;
                    return true;
            }
            return false;
        }

        // ---- Left-button: double-tap to delete edge ----
        if (args.EventType != InputEventType.MouseDown)
            return false;

        args.Handled = TryDeleteEdgeOnDoubleTap(args.Position, args.Timestamp);
        return args.Handled;
    }

    private void ApplyPan(float dx, float dy)
    {
        _panOffset += new Vector2(dx, dy);
        UpdateNodeTransforms();
        MarkNeedsPaint();
        _overlay.MarkNeedsPaint();
    }

    public float ContentWidth => float.PositiveInfinity;
    public float ContentHeight => float.PositiveInfinity;

    public void Scroll(float deltaY)
    {
        var focus = new Vector2(
            ComputedX + ComputedWidth / 2f,
            ComputedY + ComputedHeight / 2f);
        SetZoom(_zoom * MathF.Exp(-deltaY * 0.005f), focus);
    }

    public void ScrollHorizontal(float deltaX)
    {
        ApplyPan(-deltaX, 0);
    }

    void IPointerHandler.OnPointerPressed(PointerEventArgs e)
    {
        if (e.PointerType != PointerType.Touch)
            return;

        _touchPointers[e.PointerId] = e.Position;
        if (_touchPointers.Count == 2)
        {
            _lastTapTime = DateTime.MinValue;
            foreach (var node in _nodeControls)
            {
                node.CancelPointerInteraction();
            }
        }
        else if (_touchPointers.Count == 1 && !IsPointOverNode(e.Position))
        {
            e.Handled |= TryDeleteEdgeOnDoubleTap(e.Position, e.Timestamp);
        }

        ResetPinchReference();
    }

    void IPointerHandler.OnPointerMoved(PointerEventArgs e)
    {
        if (e.PointerType != PointerType.Touch ||
            !_touchPointers.ContainsKey(e.PointerId))
        {
            return;
        }

        _touchPointers[e.PointerId] = e.Position;

        if (_touchPointers.Count >= 2)
        {
            var points = _touchPointers.Values.Take(2).ToArray();
            var center = (points[0] + points[1]) / 2f;
            var distance = Vector2.Distance(points[0], points[1]);

            if (_lastPinchDistance > 0)
            {
                ApplyPan(
                    center.X - _lastPinchCenter.X,
                    center.Y - _lastPinchCenter.Y);
                SetZoom(
                    _zoom * distance / _lastPinchDistance,
                    center);
            }

            _lastPinchCenter = center;
            _lastPinchDistance = distance;
            e.Handled = true;
            return;
        }

        // One finger is reserved exclusively for node and port interaction.
    }

    void IPointerHandler.OnPointerReleased(PointerEventArgs e)
    {
        if (e.PointerType != PointerType.Touch)
            return;

        _touchPointers.Remove(e.PointerId);
        ResetPinchReference();
    }

    private void ResetPinchReference()
    {
        _lastPinchDistance = 0;
        if (_touchPointers.Count < 2)
            return;

        var points = _touchPointers.Values.Take(2).ToArray();
        _lastPinchCenter = (points[0] + points[1]) / 2f;
        _lastPinchDistance = Vector2.Distance(points[0], points[1]);
    }

    private bool TryDeleteEdgeOnDoubleTap(
        Vector2 screenPosition,
        DateTime timestamp)
    {
        var canvasPoint = ToCanvasPosition(screenPosition);
        var elapsedMs = (timestamp - _lastTapTime).TotalMilliseconds;
        var distance = Vector2.Distance(
            canvasPoint,
            new Vector2(_lastTapX, _lastTapY));
        var isDoubleTap =
            elapsedMs >= 0 &&
            elapsedMs <= DoubleTapMaxMs &&
            distance <= DoubleTapMaxDist / _zoom;

        _lastTapTime = timestamp;
        _lastTapX = canvasPoint.X;
        _lastTapY = canvasPoint.Y;

        if (!isDoubleTap)
            return false;

        var connection = FindConnectionAt(canvasPoint.X, canvasPoint.Y);
        if (connection is null)
            return false;

        Graph.RemoveConnection(connection);
        MarkNeedsPaint();
        _overlay.MarkNeedsPaint();
        return true;
    }

    private bool IsPointOverNode(Vector2 screenPosition)
        => _nodeControls.Any(node =>
            node.ContainsWindowPoint(screenPosition, 8f));

    private void SetZoom(float zoom, Vector2 screenFocus)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (MathF.Abs(zoom - _zoom) < 0.0001f)
            return;

        var center = new Vector2(
            _canvas.ComputedX + _canvas.ComputedWidth / 2f,
            _canvas.ComputedY + _canvas.ComputedHeight / 2f);
        var logicalFocus = center +
            (screenFocus - center - _panOffset) / _zoom;
        var newPanOffset = screenFocus - center -
            (logicalFocus - center) * zoom;

        _zoom = zoom;
        _panOffset = newPanOffset;
        UpdateNodeTransforms();
        MarkNeedsPaint();
        _overlay.MarkNeedsPaint();
    }

    private Vector2 ToCanvasPosition(Vector2 screenPosition)
    {
        var center = new Vector2(
            _canvas.ComputedX + _canvas.ComputedWidth / 2f,
            _canvas.ComputedY + _canvas.ComputedHeight / 2f);
        return center + (screenPosition - center - _panOffset) / _zoom;
    }

    /// <summary>
    /// Returns the first connection whose Bezier curve passes within
    /// <see cref="ConnectionHitTolerance"/> pixels of (mx, my), or null.
    /// The curve is sampled at 40 evenly-spaced t values for accuracy.
    /// </summary>
    private ConnectionModel FindConnectionAt(float mx, float my)
    {
        const int samples = 40;

        foreach (var conn in Graph.Connections)
        {
            float sx = conn.OutputPort.WorldX;
            float sy = conn.OutputPort.WorldY;
            float ex = conn.InputPort.WorldX;
            float ey = conn.InputPort.WorldY;

            // Same control-point formula used by ConnectionOverlay.DrawWire
            float dist = MathF.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
            float dx   = Math.Clamp(dist * 0.4f, 20f, 220f);

            // P0 = (sx,sy)  P1 = (sx+dx,sy)  P2 = (ex-dx,ey)  P3 = (ex,ey)
            for (int i = 0; i <= samples; i++)
            {
                float t  = i / (float)samples;
                float u  = 1f - t;
                float bx = u*u*u * sx
                         + 3*u*u*t * (sx + dx)
                         + 3*u*t*t * (ex - dx)
                         + t*t*t   * ex;
                float by = u*u*u * sy
                         + 3*u*u*t * sy
                         + 3*u*t*t * ey
                         + t*t*t   * ey;

                float d = MathF.Sqrt((bx - mx) * (bx - mx) + (by - my) * (by - my));
                if (d <= ConnectionHitTolerance / _zoom)
                    return conn;
            }
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Node control management
    // -------------------------------------------------------------------------

    private void RegisterNodeControl(NodeModel model)
    {
        var node = new ScriptNode(model);

        // Wire up connection drag callbacks
        node.OnConnectionStarted   = OnConnectionStarted;
        node.OnConnectionDragging  = OnConnectionDragging;
        node.OnConnectionReleased  = OnConnectionReleased;
        node.OnConnectionCancelled = OnConnectionCancelled;
        node.IsPendingConnectionActive = () => _overlay.HasPendingConnection;
        node.ToCanvasPosition = ToCanvasPosition;
        node.CanvasScale = () => _zoom;
        node.CanvasOrigin = () => new Vector2(
            _canvas.ComputedX,
            _canvas.ComputedY);
        node.PositionChanged = () =>
        {
            // Repaint the whole viewport on every drag step. Marking only the
            // node would dirty its new bounds but leave the previous pixels in
            // retained/partial rendering until touch-up.
            MarkNeedsPaint();
            UpdateNodeTransform(node);
            node.MarkNeedsPaint();
            _overlay.MarkNeedsPaint();
        };
        node.OnSelected        = n => OnNodeSelected?.Invoke(n);
        node.OnDeleteRequested = () => DeleteNode(node);

        _nodeControls.Add(node);

        // Overlay stays first; nodes are always added after it (render on top of wires)
        _canvas.AddChild(node);

        // Initialize port positions immediately after adding to canvas
        node.ArrangeUpdate(node.X, node.Y, node.Width, node.Height);
        node.UpdatePortPositions();
    }

    /// <summary>Removes a node from the graph, its control from the canvas, and notifies the properties panel.</summary>
    private void DeleteNode(ScriptNode node)
    {
        Graph.RemoveNode(node.Model);
        _nodeControls.Remove(node);
        _canvas.RemoveChild(node);
        OnNodeSelected?.Invoke(null);
        _overlay.MarkNeedsPaint();
    }

    // -------------------------------------------------------------------------
    // Connection drag callbacks
    // -------------------------------------------------------------------------

    private void OnConnectionStarted(PortModel port, float mx, float my)
    {
        _overlay.StartPendingConnection(port, mx, my);
    }

    private void OnConnectionDragging(float mx, float my)
    {
        _overlay.UpdatePreview(mx, my);
    }

    private void OnConnectionReleased(PortModel sourcePort, float mx, float my)
    {
        // Find a compatible port at the release position
        var targetPort = FindPortAt(mx, my);

        if (targetPort != null && targetPort != sourcePort && sourcePort.CanConnectTo(targetPort))
        {
            PortModel output = sourcePort.Direction == PortDirection.Output ? sourcePort : targetPort;
            PortModel input  = sourcePort.Direction == PortDirection.Input  ? sourcePort : targetPort;
            _overlay.CancelPendingConnection();
            Graph.AddConnection(output, input);
        }
        else
        {
            _overlay.CancelPendingConnection();
        }
    }

    private void OnConnectionCancelled()
    {
        _overlay.CancelPendingConnection();
    }

    /// <summary>Searches all node controls for a port within hit-test range of (wx, wy).</summary>
    private PortModel FindPortAt(float wx, float wy)
    {
        foreach (var node in _nodeControls)
        {
            var port = node.HitTestPort(wx, wy, 14f / _zoom);
            if (port != null)
                return port;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Layout delegation
    // -------------------------------------------------------------------------

    protected override void Measure(float availableWidth, float availableHeight)
    {
        _canvas.MeasureUpdate(availableWidth, availableHeight);
        DesiredWidth  = _canvas.DesiredWidth;
        DesiredHeight = _canvas.DesiredHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _canvas.ArrangeUpdate(x, y, width, height);
        UpdateNodeTransforms();
    }

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRect(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            new Color(22, 22, 28));
        RenderGrid(renderer);

        // Drop target highlight
        if (IsDropTargetActive)
        {
            renderer.DrawRectOutline(
                ComputedX + 2, ComputedY + 2,
                ComputedWidth - 4, ComputedHeight - 4,
                2f, new Color(80, 140, 255, 120));
        }
    }

    private void RenderGrid(IRenderer renderer)
    {
        const float spacing = 40f;
        const int majorEvery = 4;

        var logicalTopLeft = ToCanvasPosition(
            new Vector2(ComputedX, ComputedY));
        var logicalBottomRight = ToCanvasPosition(
            new Vector2(
                ComputedX + ComputedWidth,
                ComputedY + ComputedHeight));

        var minX = MathF.Min(logicalTopLeft.X, logicalBottomRight.X);
        var maxX = MathF.Max(logicalTopLeft.X, logicalBottomRight.X);
        var minY = MathF.Min(logicalTopLeft.Y, logicalBottomRight.Y);
        var maxY = MathF.Max(logicalTopLeft.Y, logicalBottomRight.Y);

        var firstX = MathF.Floor(minX / spacing) * spacing;
        var firstY = MathF.Floor(minY / spacing) * spacing;

        for (var logicalX = firstX; logicalX <= maxX + spacing; logicalX += spacing)
        {
            var screenX = ToScreenPosition(new Vector2(logicalX, 0)).X;
            var index = (int)MathF.Round(logicalX / spacing);
            var color = index % majorEvery == 0
                ? new Color(64, 68, 82, 190)
                : new Color(43, 46, 57, 170);
            renderer.DrawLine(
                screenX,
                ComputedY,
                screenX,
                ComputedY + ComputedHeight,
                1,
                color);
        }

        for (var logicalY = firstY; logicalY <= maxY + spacing; logicalY += spacing)
        {
            var screenY = ToScreenPosition(new Vector2(0, logicalY)).Y;
            var index = (int)MathF.Round(logicalY / spacing);
            var color = index % majorEvery == 0
                ? new Color(64, 68, 82, 190)
                : new Color(43, 46, 57, 170);
            renderer.DrawLine(
                ComputedX,
                screenY,
                ComputedX + ComputedWidth,
                screenY,
                1,
                color);
        }
    }

    private Vector2 ToScreenPosition(Vector2 canvasPosition)
    {
        var center = new Vector2(
            _canvas.ComputedX + _canvas.ComputedWidth / 2f,
            _canvas.ComputedY + _canvas.ComputedHeight / 2f);
        return center +
            (canvasPosition - center) * _zoom +
            _panOffset;
    }

    private void UpdateNodeTransforms()
    {
        foreach (var node in _nodeControls)
        {
            UpdateNodeTransform(node);
        }
    }

    private void UpdateNodeTransform(ScriptNode node)
    {
        var logicalBodyPosition = new Vector2(
            _canvas.ComputedX + node.Model.X,
            _canvas.ComputedY + node.Model.Y);
        var screenBodyPosition = ToScreenPosition(logicalBodyPosition);
        node.ApplyViewportLayout(
            screenBodyPosition.X,
            screenBodyPosition.Y,
            _zoom);
    }
}
