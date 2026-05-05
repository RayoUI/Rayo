using Rayo.Core;
using Rayo.Rendering;
using VisualScripting.Models;

namespace VisualScripting.Controls;

/// <summary>
/// Transparent overlay rendered on top of all nodes.
/// Draws established connections as Bezier curves and the in-progress
/// connection preview wire while the user drags from a port.
///
/// Always input-transparent: hit testing passes through it so nodes below
/// can be captured by the EventManager for drag and click events.
/// </summary>
public class ConnectionOverlay : View<ConnectionOverlay>
{
    private readonly NodeGraph _graph;

    // In-progress connection state - updated by ScriptNode during drag
    public PortModel PendingSource { get; private set; }
    public float PendingMouseX { get; set; }
    public float PendingMouseY { get; set; }

    public bool HasPendingConnection => PendingSource != null;

    public ConnectionOverlay(NodeGraph graph)
    {
        _graph = graph;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment   = VerticalAlignment.Stretch;

        // Always transparent to input - ScriptNode handles all user interaction
        IsInputTransparent = true;
    }

    public void StartPendingConnection(PortModel source, float mouseX, float mouseY)
    {
        PendingSource  = source;
        PendingMouseX  = mouseX;
        PendingMouseY  = mouseY;
        MarkNeedsPaint();
    }

    public void UpdatePreview(float mouseX, float mouseY)
    {
        PendingMouseX = mouseX;
        PendingMouseY = mouseY;
        MarkNeedsPaint();
    }

    public void CancelPendingConnection()
    {
        PendingSource = null;
        MarkNeedsPaint();
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth  = availableWidth;
        DesiredHeight = availableHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    private static Color GetPortColor(PortType type) => type switch
    {
        PortType.Number  => new Color(255, 198, 64),
        PortType.Boolean => new Color(72, 214, 100),
        PortType.String  => new Color(196, 112, 255),
        PortType.Flow    => new Color(220, 220, 220),
        PortType.Any     => new Color(170, 170, 170),
        _                => new Color(170, 170, 170),
    };

    public override void Render(IRenderer renderer)
    {
        // Draw all established connections
        foreach (var conn in _graph.Connections)
        {
            DrawWire(renderer,
                conn.OutputPort.WorldX, conn.OutputPort.WorldY,
                conn.InputPort.WorldX,  conn.InputPort.WorldY,
                GetPortColor(conn.OutputPort.Type),
                2.5f);
        }

        // Draw in-progress connection preview wire
        if (PendingSource != null)
        {
            float sx = PendingSource.WorldX;
            float sy = PendingSource.WorldY;
            float ex = PendingMouseX;
            float ey = PendingMouseY;

            // Flip direction so wire always flows left→right visually
            if (PendingSource.Direction == PortDirection.Input)
            {
                (sx, ex) = (ex, sx);
                (sy, ey) = (ey, sy);
            }

            Color wireColor = GetPortColor(PendingSource.Type);
            DrawWire(renderer, sx, sy, ex, ey,
                new Color(wireColor.R, wireColor.G, wireColor.B, 190),
                2f);

            // Dot at cursor
            renderer.DrawCircle(PendingMouseX, PendingMouseY, 5f,
                new Color(wireColor.R, wireColor.G, wireColor.B, 180));
        }
    }

    private static void DrawWire(IRenderer renderer,
        float sx, float sy, float ex, float ey,
        Color color, float thickness)
    {
        float dist = MathF.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
        float dx   = Math.Clamp(dist * 0.4f, 20f, 220f);

        renderer.DrawCubicBezier(
            sx, sy,
            sx + dx, sy,
            ex - dx, ey,
            ex, ey,
            color,
            thickness
        );
    }
}
