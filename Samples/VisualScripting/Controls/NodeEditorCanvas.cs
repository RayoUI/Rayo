using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using VisualScripting.Models;
using VisualScripting.NodeTypes;
using Rayo.Core.Input;

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
public class NodeEditorCanvas : CompositeView<NodeEditorCanvas>, IDropTarget, IInputHandler
{
    public NodeGraph Graph { get; }

    private readonly Absolute _canvas;
    private readonly ConnectionOverlay _overlay;
    private readonly List<ScriptNode> _nodeControls = new();

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

        _overlay = new ConnectionOverlay(Graph);

        _canvas = new Absolute();
        _canvas.Background   = new Color(22, 22, 28);
        _canvas.ClipToBounds = true;

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
        // Convert world position to canvas-local coordinates
        float localX = worldX - _canvas.ComputedX;
        float localY = worldY - _canvas.ComputedY;

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

        float mx = args.Position.X;
        float my = args.Position.Y;
        var   now = args.Timestamp;

        double elapsedMs = (now - _lastTapTime).TotalMilliseconds;
        float  dist      = MathF.Sqrt((mx - _lastTapX) * (mx - _lastTapX) +
                                       (my - _lastTapY) * (my - _lastTapY));

        bool isDoubleTap = elapsedMs <= DoubleTapMaxMs && dist <= DoubleTapMaxDist;

        _lastTapTime = now;
        _lastTapX    = mx;
        _lastTapY    = my;

        if (isDoubleTap)
        {
            var conn = FindConnectionAt(mx, my);
            if (conn != null)
            {
                Graph.RemoveConnection(conn);
                args.Handled = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Moves every node (model + visual position) by (dx, dy) to pan the viewport.
    /// </summary>
    private void ApplyPan(float dx, float dy)
    {
        foreach (var node in _nodeControls)
        {
            node.Model.X += dx;
            node.Model.Y += dy;
            node.X        = node.Model.X;
            node.Y        = node.Model.Y;

            // Update port positions immediately to prevent connection lag
            node.UpdatePortPositions();
        }
        _canvas.MarkNeedsLayout();
        _overlay.MarkNeedsPaint();
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
                if (d <= ConnectionHitTolerance)
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
        node.OnSelected        = n => OnNodeSelected?.Invoke(n);
        node.OnDeleteRequested = () => DeleteNode(node);

        _nodeControls.Add(node);

        // Overlay stays first; nodes are always added after it (render on top of wires)
        _canvas.AddChild(node);

        // Initialize port positions immediately after adding to canvas
        node.Arrange(node.X, node.Y, node.Width, node.Height);
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
            var port = node.HitTestPort(wx, wy);
            if (port != null)
                return port;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Layout delegation
    // -------------------------------------------------------------------------

    public override void Measure(float availableWidth, float availableHeight)
    {
        _canvas.Measure(availableWidth, availableHeight);
        DesiredWidth  = _canvas.DesiredWidth;
        DesiredHeight = _canvas.DesiredHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _canvas.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
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
        var dotColor = new Color(50, 50, 60, 180);
        float x = ComputedX, y = ComputedY, w = ComputedWidth, h = ComputedHeight;

        for (float gx = x + spacing; gx < x + w; gx += spacing)
            for (float gy = y + spacing; gy < y + h; gy += spacing)
                renderer.DrawCircle(gx, gy, 1f, dotColor);
    }
}
