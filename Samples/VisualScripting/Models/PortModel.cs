namespace VisualScripting.Models;

public enum PortDirection
{
    Input,
    Output
}

public enum PortType
{
    Number,
    Boolean,
    String,
    Flow,
    Any
}

/// <summary>
/// Represents a port (connection point) on a node.
/// </summary>
public class PortModel
{
    public string Id { get; }
    public string Name { get; }
    public PortDirection Direction { get; }
    public PortType Type { get; }

    // World-space position cached during rendering for hit testing
    public float WorldX { get; set; }
    public float WorldY { get; set; }

    public NodeModel Owner { get; set; }

    public PortModel(string id, string name, PortDirection direction, PortType type)
    {
        Id = id;
        Name = name;
        Direction = direction;
        Type = type;
    }

    /// <summary>
    /// Returns true if this port can be connected to the other port.
    /// Requires opposite directions and compatible types.
    /// </summary>
    public bool CanConnectTo(PortModel other)
    {
        if (other == this) return false;
        if (Direction == other.Direction) return false;
        if (Owner == other.Owner) return false;
        // Flow (exec) ports can only connect to other Flow ports
        if (Type == PortType.Flow || other.Type == PortType.Flow)
            return Type == PortType.Flow && other.Type == PortType.Flow;
        if (Type == PortType.Any || other.Type == PortType.Any) return true;
        return Type == other.Type;
    }
}
