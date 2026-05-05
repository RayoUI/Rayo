using Rayo.Rendering;
using VisualScripting.Models;

namespace VisualScripting.NodeTypes;

public enum NodeTypeId
{
    // Values
    NumberValue,
    BooleanValue,
    StringValue,

    // Math
    Add,
    Subtract,
    Multiply,
    Divide,

    // Logic
    And,
    Or,
    Not,
    Compare,

    // Control flow
    IfThen,
    ForLoop,
    Print,

    // Entry / Exit
    Start,
    Output
}

/// <summary>
/// Factory that creates NodeModel instances for each node type.
///
/// Blueprint-style port conventions:
///   - Execution ports (PortType.Flow) use white arrow/triangle visuals and
///     sit ABOVE data ports on each side.
///   - Data ports use colored circle visuals.
///   - Exec-in is always id "exec_in", exec-out ids are "exec_out", "exec_true", "exec_false".
/// </summary>
public static class NodeFactory
{
    // Category colors
    private static readonly Color ValueColor   = new Color(60, 100, 200);
    private static readonly Color MathColor    = new Color(190, 110, 30);
    private static readonly Color LogicColor   = new Color(50, 160, 80);
    private static readonly Color ControlColor = new Color(150, 60, 160);
    private static readonly Color IOColor      = new Color(30, 140, 160);

    public static NodeModel Create(NodeTypeId type, float x, float y) => type switch
    {
        NodeTypeId.NumberValue  => CreateNumberValue(x, y),
        NodeTypeId.BooleanValue => CreateBooleanValue(x, y),
        NodeTypeId.StringValue  => CreateStringValue(x, y),
        NodeTypeId.Add          => CreateAdd(x, y),
        NodeTypeId.Subtract     => CreateSubtract(x, y),
        NodeTypeId.Multiply     => CreateMultiply(x, y),
        NodeTypeId.Divide       => CreateDivide(x, y),
        NodeTypeId.And          => CreateAnd(x, y),
        NodeTypeId.Or           => CreateOr(x, y),
        NodeTypeId.Not          => CreateNot(x, y),
        NodeTypeId.Compare      => CreateCompare(x, y),
        NodeTypeId.IfThen       => CreateIfThen(x, y),
        NodeTypeId.ForLoop      => CreateForLoop(x, y),
        NodeTypeId.Print        => CreatePrint(x, y),
        NodeTypeId.Start        => CreateStart(x, y),
        NodeTypeId.Output       => CreateOutput(x, y),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // -------------------------------------------------------------------------
    // Value nodes — pure data, no exec ports
    // -------------------------------------------------------------------------

    private static NodeModel CreateNumberValue(float x, float y) =>
        new NodeModel("Number", NodeTypeId.NumberValue, x, y, ValueColor)
            .AddPort("out", "Value", PortDirection.Output, PortType.Number);

    private static NodeModel CreateBooleanValue(float x, float y) =>
        new NodeModel("Boolean", NodeTypeId.BooleanValue, x, y, ValueColor)
            .AddPort("out", "Value", PortDirection.Output, PortType.Boolean);

    private static NodeModel CreateStringValue(float x, float y) =>
        new NodeModel("String", NodeTypeId.StringValue, x, y, ValueColor)
            .AddPort("out", "Value", PortDirection.Output, PortType.String);

    // -------------------------------------------------------------------------
    // Math nodes — pure data, no exec ports
    // -------------------------------------------------------------------------

    private static NodeModel CreateAdd(float x, float y) =>
        new NodeModel("Add", NodeTypeId.Add, x, y, MathColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Number)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Number)
            .AddPort("out", "Result", PortDirection.Output, PortType.Number);

    private static NodeModel CreateSubtract(float x, float y) =>
        new NodeModel("Subtract", NodeTypeId.Subtract, x, y, MathColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Number)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Number)
            .AddPort("out", "Result", PortDirection.Output, PortType.Number);

    private static NodeModel CreateMultiply(float x, float y) =>
        new NodeModel("Multiply", NodeTypeId.Multiply, x, y, MathColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Number)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Number)
            .AddPort("out", "Result", PortDirection.Output, PortType.Number);

    private static NodeModel CreateDivide(float x, float y) =>
        new NodeModel("Divide", NodeTypeId.Divide, x, y, MathColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Number)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Number)
            .AddPort("out", "Result", PortDirection.Output, PortType.Number);

    // -------------------------------------------------------------------------
    // Logic nodes — pure data, no exec ports
    // -------------------------------------------------------------------------

    private static NodeModel CreateAnd(float x, float y) =>
        new NodeModel("AND", NodeTypeId.And, x, y, LogicColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Boolean)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Boolean)
            .AddPort("out", "Result", PortDirection.Output, PortType.Boolean);

    private static NodeModel CreateOr(float x, float y) =>
        new NodeModel("OR", NodeTypeId.Or, x, y, LogicColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Boolean)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Boolean)
            .AddPort("out", "Result", PortDirection.Output, PortType.Boolean);

    private static NodeModel CreateNot(float x, float y) =>
        new NodeModel("NOT", NodeTypeId.Not, x, y, LogicColor)
            .AddPort("in",  "Input",  PortDirection.Input,  PortType.Boolean)
            .AddPort("out", "Result", PortDirection.Output, PortType.Boolean);

    private static NodeModel CreateCompare(float x, float y) =>
        new NodeModel("Compare (==)", NodeTypeId.Compare, x, y, LogicColor)
            .AddPort("a",   "A",      PortDirection.Input,  PortType.Number)
            .AddPort("b",   "B",      PortDirection.Input,  PortType.Number)
            .AddPort("out", "Result", PortDirection.Output, PortType.Boolean);

    // -------------------------------------------------------------------------
    // Control flow nodes — have exec ports (Blueprint-style)
    // -------------------------------------------------------------------------

    private static NodeModel CreateIfThen(float x, float y) =>
        new NodeModel("Branch", NodeTypeId.IfThen, x, y, ControlColor)
            // Exec ports first (Flow type)
            .AddPort("exec_in",    "",      PortDirection.Input,  PortType.Flow)
            .AddPort("exec_true",  "True",  PortDirection.Output, PortType.Flow)
            .AddPort("exec_false", "False", PortDirection.Output, PortType.Flow)
            // Data input
            .AddPort("cond", "Condition", PortDirection.Input, PortType.Boolean);

    private static NodeModel CreateForLoop(float x, float y) =>
        new NodeModel("For Loop", NodeTypeId.ForLoop, x, y, ControlColor)
            // Exec ports
            .AddPort("exec_in",   "",          PortDirection.Input,  PortType.Flow)
            .AddPort("exec_body", "Loop Body", PortDirection.Output, PortType.Flow)
            .AddPort("exec_done", "Completed", PortDirection.Output, PortType.Flow)
            // Data inputs
            .AddPort("first", "First",  PortDirection.Input,  PortType.Number)
            .AddPort("last",  "Last",   PortDirection.Input,  PortType.Number)
            // Data output — current iteration index
            .AddPort("index", "Index",  PortDirection.Output, PortType.Number);

    private static NodeModel CreatePrint(float x, float y) =>
        new NodeModel("Print", NodeTypeId.Print, x, y, ControlColor)
            // Exec ports
            .AddPort("exec_in",  "",  PortDirection.Input,  PortType.Flow)
            .AddPort("exec_out", "",  PortDirection.Output, PortType.Flow)
            // Data input
            .AddPort("in", "Value", PortDirection.Input, PortType.Any);

    // -------------------------------------------------------------------------
    // IO nodes — entry/exit of execution
    // -------------------------------------------------------------------------

    private static NodeModel CreateStart(float x, float y) =>
        new NodeModel("Start", NodeTypeId.Start, x, y, IOColor)
            .AddPort("exec_out", "", PortDirection.Output, PortType.Flow);

    private static NodeModel CreateOutput(float x, float y) =>
        new NodeModel("Output", NodeTypeId.Output, x, y, IOColor)
            .AddPort("exec_in", "", PortDirection.Input, PortType.Flow)
            .AddPort("in", "Value", PortDirection.Input, PortType.Any);

    /// <summary>
    /// Category groups for the node palette toolbar.
    /// </summary>
    public static (string Category, (string Label, NodeTypeId Type)[] Items)[] Categories => new[]
    {
        ("Values", new[]
        {
            ("Number",  NodeTypeId.NumberValue),
            ("Boolean", NodeTypeId.BooleanValue),
            ("String",  NodeTypeId.StringValue),
        }),
        ("Math", new[]
        {
            ("Add",      NodeTypeId.Add),
            ("Subtract", NodeTypeId.Subtract),
            ("Multiply", NodeTypeId.Multiply),
            ("Divide",   NodeTypeId.Divide),
        }),
        ("Logic", new[]
        {
            ("AND",     NodeTypeId.And),
            ("OR",      NodeTypeId.Or),
            ("NOT",     NodeTypeId.Not),
            ("Compare", NodeTypeId.Compare),
        }),
        ("Control", new[]
        {
            ("Branch",   NodeTypeId.IfThen),
            ("For Loop", NodeTypeId.ForLoop),
            ("Print",    NodeTypeId.Print),
        }),
        ("IO", new[]
        {
            ("Start",  NodeTypeId.Start),
            ("Output", NodeTypeId.Output),
        }),
    };
}
