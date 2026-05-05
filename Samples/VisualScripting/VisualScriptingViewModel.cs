#nullable enable

using Rayo.Core;
using Rayo.Reactivity;
using VisualScripting.Controls;
using VisualScripting.Lua;
using VisualScripting.Models;
using VisualScripting.NodeTypes;

namespace VisualScripting;

/// <summary>
/// ViewModel for the Visual Scripting editor.
/// Manages the node graph state, console output, and execution logic.
/// </summary>
public class VisualScriptingViewModel : ViewModelBase
{
    // Core state
    public NodeGraph Graph { get; } = new NodeGraph();
    
    // Reactive properties for console output
    public SignalList<ConsoleLineModel> ConsoleLines { get; } = new();
    public Signal<NodeModel?> SelectedNode { get; } = new(null);
    public Signal<bool> IsExecuting { get; } = new(false);

    protected override void OnInitialized()
    {
        // Initialize with demo graph
        SeedDemoGraph();
    }

    /// <summary>
    /// Executes the current node graph and updates console output.
    /// </summary>
    public void ExecuteGraph()
    {
        if (IsExecuting.Value) return;

        try
        {
            IsExecuting.Value = true;
            ClearConsole();

            // Generate Lua code
            string code = LuaCodeGenerator.Generate(Graph);

            AppendMeta("── Lua generado ──────────────────");
            foreach (var line in code.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                AppendLine(line, ConsoleLineType.Code);
            }

            AppendMeta("── Salida ────────────────────────");

            // Execute Lua
            var result = LuaRunner.Run(code);

            if (result.Output.Count == 0 && result.Error == null)
            {
                AppendLine("(sin salida)", ConsoleLineType.Output, new Rayo.Rendering.Color(80, 80, 90));
            }
            else
            {
                foreach (var line in result.Output)
                {
                    AppendLine(line, ConsoleLineType.Output);
                }
            }

            if (result.Error != null)
            {
                AppendLine($"Error: {result.Error}", ConsoleLineType.Error);
            }
        }
        finally
        {
            IsExecuting.Value = false;
        }
    }

    /// <summary>
    /// Clears all console output.
    /// </summary>
    public void ClearConsole()
    {
        ConsoleLines.Clear();
    }

    /// <summary>
    /// Appends a line to the console output.
    /// </summary>
    private void AppendLine(string text, ConsoleLineType type, Rayo.Rendering.Color? color = null)
    {
        ConsoleLines.Add(new ConsoleLineModel(text, type, color));
    }

    /// <summary>
    /// Appends a metadata line (section header) to the console.
    /// </summary>
    private void AppendMeta(string text)
    {
        AppendLine(text, ConsoleLineType.Meta);
    }

    /// <summary>
    /// Sets the currently selected node.
    /// </summary>
    public void SelectNode(NodeModel? node)
    {
        SelectedNode.Value = node;
    }

    /// <summary>
    /// Seeds the graph with demo nodes for initial testing.
    /// </summary>
    private void SeedDemoGraph()
    {
        var n1 = NodeFactory.Create(NodeTypes.NodeTypeId.NumberValue, 60, 80);
        n1.Value = "3";
        Graph.AddNode(n1);

        var n2 = NodeFactory.Create(NodeTypes.NodeTypeId.NumberValue, 60, 180);
        n2.Value = "4";
        Graph.AddNode(n2);

        var addNode = NodeFactory.Create(NodeTypes.NodeTypeId.Add, 260, 120);
        Graph.AddNode(addNode);

        var printNode = NodeFactory.Create(NodeTypes.NodeTypeId.Print, 460, 120);
        Graph.AddNode(printNode);

        // Connect: n1.out → add.a, n2.out → add.b, add.result → print.value
        Graph.AddConnection(n1.OutputPorts.First(), addNode.InputPorts.First());
        Graph.AddConnection(n2.OutputPorts.First(), addNode.InputPorts.Last());
        Graph.AddConnection(addNode.OutputPorts.First(), printNode.InputPorts.First());
    }
}

/// <summary>
/// Represents a single line in the console output.
/// </summary>
public record ConsoleLineModel(string Text, ConsoleLineType Type, Rayo.Rendering.Color? CustomColor = null);

/// <summary>
/// Type of console line for styling purposes.
/// </summary>
public enum ConsoleLineType
{
    Code,
    Output,
    Error,
    Meta
}
