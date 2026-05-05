using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using VisualScripting.Models;

namespace VisualScripting.Controls;

/// <summary>
/// Visual representation of a node in the graph.
///
/// Implements IFocusable so clicking the node body sets focus, which:
///   • Highlights the node with a selection glow
///   • Enables keyboard events (Delete key removes the node)
///   • Notifies the properties panel via OnSelected callback
/// </summary>
public class ScriptNode : View<ScriptNode>, IInputHandler, IFocusable
{
    private const float HeaderHeight   = 28f;
    private const float PortRowHeight  = 22f;
    private const float ExecPortSize   = 10f;
    private const float DataPortRadius = 6f;
    private const float NodeWidth      = 180f;
    private const float BodyPaddingY   = 6f;
    private const float ExecDataGap    = 4f;

    // Extends computed bounds on each side so port circles fall within hit area.
    private const float PortHitZone = DataPortRadius + 6f;

    public NodeModel Model { get; }

    // IFocusable
    public bool IsFocused { get; set; }

    // ---- Node drag state ----
    private bool  _isDragging;
    private float _dragOffsetX;
    private float _dragOffsetY;

    // ---- Connection drag state ----
    private bool      _isConnecting;
    private PortModel _connectingPort;

    // ---- Callbacks injected by NodeEditorCanvas ----
    public Action<PortModel, float, float> OnConnectionStarted   { get; set; }
    public Action<float, float>            OnConnectionDragging  { get; set; }
    public Action<PortModel, float, float> OnConnectionReleased  { get; set; }
    public Action                          OnConnectionCancelled { get; set; }
    public Func<bool>                      IsPendingConnectionActive { get; set; } = () => false;

    /// <summary>Called when this node gains focus (user clicked its body).</summary>
    public Action<ScriptNode> OnSelected   { get; set; }

    /// <summary>Called when Delete is pressed while this node is focused.</summary>
    public Action             OnDeleteRequested { get; set; }

    public ScriptNode(NodeModel model)
    {
        Model  = model;
        X      = model.X;
        Y      = model.Y;
        Width  = NodeWidth;
        Height = ComputeHeight();
    }

    /// <summary>
    /// Computes the total vertical space reserved for the exec (flow) port section,
    /// including the gap before the data rows. When there are multiple exec output
    /// ports they are stacked at <c>ExecPortSize*2+2</c> px intervals, so the
    /// section height grows accordingly.
    /// </summary>
    private float ComputeExecSectionHeight()
    {
        int execOut = Model.OutputPorts.Count(p => p.Type == PortType.Flow);
        bool hasExec = Model.InputPorts.Any(p => p.Type == PortType.Flow) || execOut > 0;
        if (!hasExec) return 0f;

        // First exec output sits at execRowY (= bodyStart + PortRowHeight/2).
        // Each additional exec output adds one ExecPortSize*2+2 step downward.
        // The data section must start below the last exec output plus a margin.
        int extraExecRows = Math.Max(execOut - 1, 0);
        return PortRowHeight + extraExecRows * (ExecPortSize * 2f + 2f) + ExecDataGap;
    }

    private float ComputeHeight()
    {
        var inputs  = Model.InputPorts.ToArray();
        var outputs = Model.OutputPorts.ToArray();

        int dataIn  = inputs.Count(p  => p.Type != PortType.Flow);
        int dataOut = outputs.Count(p => p.Type != PortType.Flow);
        int dataRows = Math.Max(dataIn, dataOut);

        float execHeight = ComputeExecSectionHeight();
        float dataHeight = dataRows * PortRowHeight;

        return HeaderHeight + BodyPaddingY * 2 + execHeight + Math.Max(dataHeight, PortRowHeight);
    }

    // -------------------------------------------------------------------------
    // IFocusable callbacks
    // -------------------------------------------------------------------------

    public void OnFocusGained()
    {
        IsFocused = true;
        ZIndex    = 1;          // Bring to front over all other nodes (ZIndex 0)
        OnSelected?.Invoke(this);
        MarkNeedsPaint();
    }

    public void OnFocusLost()
    {
        IsFocused = false;
        ZIndex    = 0;
        MarkNeedsPaint();
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth  = NodeWidth;
        DesiredHeight = ComputeHeight();
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x - PortHitZone, y, NodeWidth + 2f * PortHitZone, ComputeHeight());

        // Update port positions after arrange to ensure they're in sync with layout
        UpdatePortPositions();
    }

    // -------------------------------------------------------------------------
    // Input handling
    // -------------------------------------------------------------------------

    public bool CanHandleInput => true;

    public bool HandleInput(InputEventArgs args)
    {
        switch (args.EventType)
        {
            case InputEventType.MouseDown:
            {
                // Only handle left-button interactions (connections, node drag)
                if (args.Button != InputMouseButton.Left) return false;

                var port = HitTestPort(args.Position.X, args.Position.Y);

                if (port != null)
                {
                    _isConnecting   = true;
                    _connectingPort = port;
                    OnConnectionStarted?.Invoke(port, args.Position.X, args.Position.Y);
                    args.Handled = true;
                    return true;
                }

                if (!IsPendingConnectionActive())
                {
                    _isDragging  = true;
                    _dragOffsetX = args.Position.X - Model.X;
                    _dragOffsetY = args.Position.Y - Model.Y;
                    args.Handled = true;
                    return true;
                }

                return false;
            }

            case InputEventType.MouseDrag:
            {
                if (_isDragging)
                {
                    Model.X = args.Position.X - _dragOffsetX;
                    Model.Y = args.Position.Y - _dragOffsetY;
                    X = Model.X;
                    Y = Model.Y;

                    // MarkNeedsLayout triggers Arrange which calls UpdatePortPositions
                    MarkNeedsLayout();
                    return true;
                }

                if (_isConnecting)
                {
                    OnConnectionDragging?.Invoke(args.Position.X, args.Position.Y);
                    return false;
                }

                return false;
            }

            case InputEventType.MouseUp:
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    MarkNeedsPaint();
                    return true;
                }

                if (_isConnecting)
                {
                    _isConnecting = false;
                    var src = _connectingPort;
                    _connectingPort = null;
                    OnConnectionReleased?.Invoke(src, args.Position.X, args.Position.Y);
                    return true;
                }

                return false;
            }

            case InputEventType.KeyDown:
            case InputEventType.KeyRepeat:
            {
                if (args.KeyCode == InputKey.Delete)
                {
                    OnDeleteRequested?.Invoke();
                    args.Handled = true;
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Port hit testing
    // -------------------------------------------------------------------------

    public PortModel HitTestPort(float wx, float wy)
    {
        foreach (var port in Model.Ports)
        {
            float tolerance = port.Type == PortType.Flow
                ? ExecPortSize + 4f
                : DataPortRadius + 5f;

            float dx = wx - port.WorldX;
            float dy = wy - port.WorldY;
            if (dx * dx + dy * dy <= tolerance * tolerance)
                return port;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Port position updates
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the world positions of all ports without rendering.
    /// Called after pan operations or during drag to prevent lag in connection drawing.
    /// </summary>
    public void UpdatePortPositions()
    {
        // Use ComputedX/ComputedY after Arrange, or X/Y during drag (they should be in sync)
        float x = ComputedX + PortHitZone;
        float y = ComputedY;
        float w = NodeWidth;

        var execInputs  = Model.InputPorts.Where(p  => p.Type == PortType.Flow).ToArray();
        var execOutputs = Model.OutputPorts.Where(p => p.Type == PortType.Flow).ToArray();
        var dataInputs  = Model.InputPorts.Where(p  => p.Type != PortType.Flow).ToArray();
        var dataOutputs = Model.OutputPorts.Where(p => p.Type != PortType.Flow).ToArray();

        bool hasExec    = execInputs.Length > 0 || execOutputs.Length > 0;
        float bodyStart = y + HeaderHeight + BodyPaddingY;

        // -- Exec row --
        if (hasExec)
        {
            float execRowY = bodyStart + PortRowHeight / 2f;

            if (execInputs.Length > 0)
            {
                execInputs[0].WorldX = x;
                execInputs[0].WorldY = execRowY;
            }

            for (int i = 0; i < execOutputs.Length; i++)
            {
                float py = execRowY + i * (ExecPortSize * 2f + 2f);
                execOutputs[i].WorldX = x + w;
                execOutputs[i].WorldY = py;
            }
        }

        // -- Data rows --
        float dataStart = bodyStart + ComputeExecSectionHeight();

        for (int i = 0; i < dataInputs.Length; i++)
        {
            float py = dataStart + i * PortRowHeight + PortRowHeight / 2f;
            dataInputs[i].WorldX = x;
            dataInputs[i].WorldY = py;
        }

        for (int i = 0; i < dataOutputs.Length; i++)
        {
            float py = dataStart + i * PortRowHeight + PortRowHeight / 2f;
            dataOutputs[i].WorldX = x + w;
            dataOutputs[i].WorldY = py;
        }
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    private static Color GetDataPortColor(PortType type) => type switch
    {
        PortType.Number  => new Color(255, 198, 64),
        PortType.Boolean => new Color(72, 214, 100),
        PortType.String  => new Color(196, 112, 255),
        PortType.Any     => new Color(170, 170, 170),
        _                => new Color(170, 170, 170),
    };

    public override void Render(IRenderer renderer)
    {
        float x = ComputedX + PortHitZone; // visual body (compensate for hit zone expansion)
        float y = ComputedY;
        float w = NodeWidth;
        float h = ComputedHeight;

        // Selection glow (drawn behind node)
        if (IsFocused)
        {
            renderer.DrawRoundedRect(x - 3, y - 3, w + 6, h + 6, 10,
                new Color(80, 160, 255, 60));
            renderer.DrawRectOutline(x - 2, y - 2, w + 4, h + 4, 2f,
                new Color(80, 160, 255, 200));
        }

        // Drop shadow
        renderer.DrawRoundedRect(x + 4, y + 6, w, h, 7,
            new Color(0, 0, 0, _isDragging ? 90 : 55));

        // Node body
        renderer.DrawRoundedRect(x, y, w, h, 7, new Color(30, 30, 36));

        // Header
        renderer.DrawRoundedRect(x, y, w, HeaderHeight, 7, Model.HeaderColor);
        renderer.DrawRect(x, y + HeaderHeight - 7, w, 7, Model.HeaderColor);

        // Title
        var titleSize = renderer.MeasureText(Model.Title, 12);
        renderer.DrawText(Model.Title,
            x + (w - titleSize.X) / 2f,
            y + (HeaderHeight - titleSize.Y) / 2f,
            Color.White, 12);

        // Border (drag highlight or normal)
        var borderColor = _isDragging
            ? new Color(120, 180, 255, 220)
            : new Color(65, 65, 75, 200);
        renderer.DrawRectOutline(x, y, w, h, _isDragging ? 2f : 1f, borderColor);

        // ---- Ports ----
        var execInputs  = Model.InputPorts.Where(p  => p.Type == PortType.Flow).ToArray();
        var execOutputs = Model.OutputPorts.Where(p => p.Type == PortType.Flow).ToArray();
        var dataInputs  = Model.InputPorts.Where(p  => p.Type != PortType.Flow).ToArray();
        var dataOutputs = Model.OutputPorts.Where(p => p.Type != PortType.Flow).ToArray();

        bool hasExec    = execInputs.Length > 0 || execOutputs.Length > 0;
        float bodyStart = y + HeaderHeight + BodyPaddingY;

        // -- Exec row --
        if (hasExec)
        {
            float execRowY = bodyStart + PortRowHeight / 2f;

            if (execInputs.Length > 0)
            {
                float px = x;
                float py = execRowY;
                // Port positions already updated by UpdatePortPositions()
                DrawExecPort(renderer, px, py, PortDirection.Input);
            }

            for (int i = 0; i < execOutputs.Length; i++)
            {
                float py = execRowY + i * (ExecPortSize * 2f + 2f);
                float px = x + w;
                // Port positions already updated by UpdatePortPositions()

                string label = execOutputs[i].Name;
                if (!string.IsNullOrEmpty(label))
                {
                    var sz = renderer.MeasureText(label, 10f);
                    renderer.DrawText(label,
                        px - ExecPortSize - 4f - sz.X, py - 5f,
                        new Color(200, 200, 200), 10f);
                }

                DrawExecPort(renderer, px, py, PortDirection.Output);
            }
        }

        // -- Data rows --
        float dataStart = bodyStart + ComputeExecSectionHeight();
        const float fontSize = 10f;

        for (int i = 0; i < dataInputs.Length; i++)
        {
            float py = dataStart + i * PortRowHeight + PortRowHeight / 2f;
            float px = x;
            // Port positions already updated by UpdatePortPositions()

            Color pc = GetDataPortColor(dataInputs[i].Type);
            renderer.DrawCircle(px, py, DataPortRadius, pc);
            renderer.DrawCircle(px, py, DataPortRadius - 2f, new Color(30, 30, 36));
            renderer.DrawText(dataInputs[i].Name,
                px + DataPortRadius + 4f, py - fontSize / 2f - 1f,
                new Color(200, 200, 200), fontSize);
        }

        for (int i = 0; i < dataOutputs.Length; i++)
        {
            float py = dataStart + i * PortRowHeight + PortRowHeight / 2f;
            float px = x + w;
            // Port positions already updated by UpdatePortPositions()

            Color pc = GetDataPortColor(dataOutputs[i].Type);
            renderer.DrawCircle(px, py, DataPortRadius, pc);
            renderer.DrawCircle(px, py, DataPortRadius - 2f, new Color(30, 30, 36));
            var nameSize = renderer.MeasureText(dataOutputs[i].Name, fontSize);
            renderer.DrawText(dataOutputs[i].Name,
                px - DataPortRadius - 4f - nameSize.X,
                py - fontSize / 2f - 1f,
                new Color(200, 200, 200), fontSize);
        }

        // -- Inline value label (centered in the body area to the left of the output port) --
        if (!string.IsNullOrEmpty(Model.Value))
        {
            const float valFontSize = 11f;
            var valSize = renderer.MeasureText(Model.Value, valFontSize);

            float portRowCount = Math.Max(dataInputs.Length, dataOutputs.Length);

            if (dataInputs.Length == 0 && portRowCount == 1)
            {
                // Value node: output port on right, value text centered on left half
                float py = dataStart + 0 * PortRowHeight + PortRowHeight / 2f;
                float valX = x + (w / 2f - valSize.X) / 2f; // center in left half
                float valY = py - valSize.Y / 2f;
                renderer.DrawText(Model.Value, valX, valY,
                    new Color(210, 210, 160), valFontSize);
            }
            else
            {
                // General case: below port rows, centered
                float valY = dataStart + portRowCount * PortRowHeight +
                             (h - (dataStart - y) - portRowCount * PortRowHeight - valSize.Y) / 2f;
                valY = Math.Max(valY, dataStart + portRowCount * PortRowHeight + 3f);
                renderer.DrawText(Model.Value, x + (w - valSize.X) / 2f, valY,
                    new Color(210, 210, 160), valFontSize);
            }
        }
    }

    private static void DrawExecPort(IRenderer renderer, float cx, float cy,
        PortDirection direction)
    {
        float s = ExecPortSize;

        List<(float x, float y)> pts = direction == PortDirection.Output
            ? new List<(float, float)>
              {
                  (cx,     cy - s * 0.7f),
                  (cx + s, cy),
                  (cx,     cy + s * 0.7f),
              }
            : new List<(float, float)>
              {
                  (cx - s, cy - s * 0.7f),
                  (cx,     cy),
                  (cx - s, cy + s * 0.7f),
              };

        var border = new List<(float, float)>
        {
            (pts[0].x - 1, pts[0].y - 1),
            (pts[1].x + 1, pts[1].y),
            (pts[2].x - 1, pts[2].y + 1),
        };
        renderer.DrawPolygon(border, new Color(0, 0, 0, 150));
        renderer.DrawPolygon(pts, new Color(210, 210, 210));
    }
}
