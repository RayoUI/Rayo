using Rayo.Rendering;
using VisualScripting.NodeTypes;

namespace VisualScripting.Models;

/// <summary>
/// Data model for a single node in the graph.
/// </summary>
public class NodeModel
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Title { get; }
    public NodeTypeId TypeId { get; }

    // Position on the canvas
    public float X { get; set; }
    public float Y { get; set; }

    // Node dimensions (computed by ScriptNode control)
    public float Width { get; set; } = 160f;
    public float Height { get; set; } = 80f;

    public List<PortModel> Ports { get; } = new();
    public Color HeaderColor { get; }

    // Optional editable value (for constant nodes like Number, Boolean, String)
    public string Value { get; set; } = "";

    public IEnumerable<PortModel> InputPorts => Ports.Where(p => p.Direction == PortDirection.Input);
    public IEnumerable<PortModel> OutputPorts => Ports.Where(p => p.Direction == PortDirection.Output);

    public NodeModel(string title, NodeTypeId typeId, float x, float y, Color headerColor)
    {
        Title = title;
        TypeId = typeId;
        X = x;
        Y = y;
        HeaderColor = headerColor;
    }

    public NodeModel AddPort(string id, string name, PortDirection direction, PortType type)
    {
        var port = new PortModel(id, name, direction, type);
        port.Owner = this;
        Ports.Add(port);
        return this;
    }
}
