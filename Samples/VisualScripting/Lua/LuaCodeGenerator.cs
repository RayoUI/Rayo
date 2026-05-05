using System.Text;
using VisualScripting.Models;
using VisualScripting.NodeTypes;

namespace VisualScripting.Lua;

/// <summary>
/// Traverses a NodeGraph and emits Lua source code.
///
/// Execution flow:
///   - Starts from every Start node and follows Flow port connections.
///   - At each exec node (Print, Branch, Output) it recursively evaluates
///     data inputs by walking backward through data connections.
///
/// Fallback:
///   - If no Start node is present, emits a comment indicating this.
/// </summary>
public static class LuaCodeGenerator
{
    public static string Generate(NodeGraph graph)
    {
        var sb = new StringBuilder();

        var starts = graph.Nodes.Where(n => n.TypeId == NodeTypeId.Start).ToList();

        if (starts.Count == 0)
        {
            sb.AppendLine("-- No se encontró ningún nodo Start.");
            sb.AppendLine("-- Conecta un nodo Start para generar código ejecutable.");
            return sb.ToString();
        }

        foreach (var start in starts)
            EmitExecChain(graph, start, sb, 0);

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Execution chain traversal
    // -------------------------------------------------------------------------

    private static void EmitExecChain(NodeGraph graph, NodeModel node, StringBuilder sb, int depth)
    {
        string pad = new string(' ', depth * 2);

        switch (node.TypeId)
        {
            case NodeTypeId.Start:
                // Entry point — just follow exec_out
                FollowExec(graph, node, "exec_out", sb, depth);
                break;

            case NodeTypeId.Print:
            {
                string val = EvalDataInput(graph, node, "in");
                sb.AppendLine($"{pad}print(tostring({val}))");
                FollowExec(graph, node, "exec_out", sb, depth);
                break;
            }

            case NodeTypeId.IfThen:
            {
                string cond = EvalDataInput(graph, node, "cond");
                sb.AppendLine($"{pad}if {cond} then");
                FollowExec(graph, node, "exec_true", sb, depth + 1);
                sb.AppendLine($"{pad}else");
                FollowExec(graph, node, "exec_false", sb, depth + 1);
                sb.AppendLine($"{pad}end");
                break;
            }

            case NodeTypeId.ForLoop:
            {
                string loopVar = LoopVar(node);
                string first   = EvalDataInput(graph, node, "first");
                string last    = EvalDataInput(graph, node, "last");
                sb.AppendLine($"{pad}for {loopVar} = {first}, {last} do");
                FollowExec(graph, node, "exec_body", sb, depth + 1);
                sb.AppendLine($"{pad}end");
                FollowExec(graph, node, "exec_done", sb, depth);
                break;
            }

            case NodeTypeId.Output:
            {
                string val = EvalDataInput(graph, node, "in");
                sb.AppendLine($"{pad}print(\"Output: \" .. tostring({val}))");
                break;
            }
        }
    }

    /// <summary>Follows a Flow output port and emits code for the connected node.</summary>
    private static void FollowExec(NodeGraph graph, NodeModel node, string execPortId,
        StringBuilder sb, int depth)
    {
        var port = node.Ports.FirstOrDefault(p => p.Id == execPortId);
        if (port == null) return;

        var conn = graph.Connections.FirstOrDefault(c => c.OutputPort == port);
        if (conn == null) return;

        EmitExecChain(graph, conn.InputPort.Owner, sb, depth);
    }

    // -------------------------------------------------------------------------
    // Data expression evaluation (walks backward through data connections)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a Lua expression string for the value arriving at a data input port.
    /// If no connection exists, returns the port's default value.
    /// </summary>
    private static string EvalDataInput(NodeGraph graph, NodeModel node, string portId)
    {
        var port = node.Ports.FirstOrDefault(p => p.Id == portId && p.Direction == PortDirection.Input);
        if (port == null) return DefaultForType(PortType.Any);

        var conn = graph.Connections.FirstOrDefault(c => c.InputPort == port);
        if (conn == null) return DefaultForType(port.Type);

        return EvalOutputNode(graph, conn.OutputPort.Owner);
    }

    /// <summary>Returns the Lua expression that a node produces on its primary output.</summary>
    private static string EvalOutputNode(NodeGraph graph, NodeModel node)
    {
        return node.TypeId switch
        {
            NodeTypeId.NumberValue =>
                string.IsNullOrWhiteSpace(node.Value) ? "0" : node.Value,

            NodeTypeId.BooleanValue =>
                string.Equals(node.Value, "true", StringComparison.OrdinalIgnoreCase)
                    ? "true" : "false",

            NodeTypeId.StringValue =>
                $"\"{EscapeLuaString(node.Value ?? "")}\"",

            NodeTypeId.Add =>
                $"({EvalDataInput(graph, node, "a")} + {EvalDataInput(graph, node, "b")})",

            NodeTypeId.Subtract =>
                $"({EvalDataInput(graph, node, "a")} - {EvalDataInput(graph, node, "b")})",

            NodeTypeId.Multiply =>
                $"({EvalDataInput(graph, node, "a")} * {EvalDataInput(graph, node, "b")})",

            NodeTypeId.Divide =>
                $"({EvalDataInput(graph, node, "a")} / {EvalDataInput(graph, node, "b")})",

            NodeTypeId.And =>
                $"({EvalDataInput(graph, node, "a")} and {EvalDataInput(graph, node, "b")})",

            NodeTypeId.Or =>
                $"({EvalDataInput(graph, node, "a")} or {EvalDataInput(graph, node, "b")})",

            NodeTypeId.Not =>
                $"(not {EvalDataInput(graph, node, "in")})",

            NodeTypeId.Compare =>
                $"({EvalDataInput(graph, node, "a")} == {EvalDataInput(graph, node, "b")})",

            // ForLoop exposes its iteration variable as a data output
            NodeTypeId.ForLoop => LoopVar(node),

            _ => "nil"
        };
    }

    private static string DefaultForType(PortType type) => type switch
    {
        PortType.Number  => "0",
        PortType.Boolean => "false",
        PortType.String  => "\"\"",
        _                => "nil"
    };

    private static string EscapeLuaString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

    /// <summary>
    /// Returns a unique, stable Lua identifier for the for-loop variable of the given node.
    /// Uses the first 8 hex chars of the node GUID so it stays valid across re-runs.
    /// </summary>
    private static string LoopVar(NodeModel node) =>
        "_i_" + node.Id.Replace("-", "")[..8];
}
