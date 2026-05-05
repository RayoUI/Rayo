namespace VisualScripting.Models;

/// <summary>
/// Represents a connection (wire) between an output port and an input port.
/// </summary>
public class ConnectionModel
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public PortModel OutputPort { get; }
    public PortModel InputPort { get; }

    public ConnectionModel(PortModel outputPort, PortModel inputPort)
    {
        OutputPort = outputPort;
        InputPort = inputPort;
    }
}
