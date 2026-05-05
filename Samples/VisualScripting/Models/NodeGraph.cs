namespace VisualScripting.Models;

/// <summary>
/// The complete state of the node graph: all nodes and their connections.
/// </summary>
public class NodeGraph
{
    public List<NodeModel> Nodes { get; } = new();
    public List<ConnectionModel> Connections { get; } = new();

    public event Action Changed;

    public void AddNode(NodeModel node)
    {
        Nodes.Add(node);
        Changed?.Invoke();
    }

    public void RemoveNode(NodeModel node)
    {
        Connections.RemoveAll(c => c.OutputPort.Owner == node || c.InputPort.Owner == node);
        Nodes.Remove(node);
        Changed?.Invoke();
    }

    /// <summary>
    /// Attempts to add a connection from an output port to an input port.
    /// Returns true if the connection was created.
    /// </summary>
    public bool AddConnection(PortModel output, PortModel input)
    {
        if (output == null || input == null) return false;
        if (!output.CanConnectTo(input)) return false;
        if (output.Direction != PortDirection.Output || input.Direction != PortDirection.Input) return false;

        // Exec output ports allow only one outgoing connection (Blueprint rule)
        if (output.Type == PortType.Flow)
            Connections.RemoveAll(c => c.OutputPort == output);

        // Data input ports allow only one incoming connection
        // Exec input ports allow multiple (e.g. several branches can reach the same node)
        if (input.Type != PortType.Flow)
            Connections.RemoveAll(c => c.InputPort == input);

        Connections.Add(new ConnectionModel(output, input));
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes all connections involving the given port.
    /// </summary>
    public void RemoveConnectionsFor(PortModel port)
    {
        int removed = Connections.RemoveAll(c => c.OutputPort == port || c.InputPort == port);
        if (removed > 0)
            Changed?.Invoke();
    }

    /// <summary>
    /// Removes a specific connection from the graph.
    /// </summary>
    public void RemoveConnection(ConnectionModel conn)
    {
        if (Connections.Remove(conn))
            Changed?.Invoke();
    }

    public bool IsPortConnected(PortModel port) =>
        Connections.Any(c => c.OutputPort == port || c.InputPort == port);
}
