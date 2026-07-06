using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using VisualScripting.Controls;
using VisualScripting.Models;
using VisualScripting.NodeTypes;

namespace Rayo.Tests;

public sealed class NodeEditorTouchTests
{
    [Fact]
    public void Touch_can_move_a_node()
    {
        var (tree, editor, graph) = CreateEditor();
        editor.SpawnNode(NodeTypeId.NumberValue);
        tree.Update(800, 500);

        var node = graph.Nodes.Single();
        var start = new Vector2(
            editor.ComputedX + node.X + 90,
            editor.ComputedY + node.Y + 14);
        var end = start + new Vector2(80, 50);

        Assert.IsType<ScriptNode>(Hit(tree, start));

        tree.EventManager!.ProcessTouchDown(PointerEventArgs.FromTouch(1, start));
        tree.EventManager.ProcessTouchMove(PointerEventArgs.FromTouch(1, end));
        tree.EventManager.ProcessTouchUp(PointerEventArgs.FromTouch(1, end));

        Assert.Equal(140, node.X);
        Assert.Equal(110, node.Y);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Touch_can_connect_value_and_math_ports_in_both_directions(
        bool startFromMathInput)
    {
        var graph = new NodeGraph();
        graph.AddNode(NodeFactory.Create(NodeTypeId.NumberValue, 60, 80));
        graph.AddNode(NodeFactory.Create(NodeTypeId.Add, 400, 100));
        var (tree, _, _) = CreateEditor(graph);
        tree.Update(800, 500);

        var output = graph.Nodes[0].OutputPorts.Single();
        var input = graph.Nodes[1].InputPorts.Last();
        var outputPoint = new Vector2(output.WorldX, output.WorldY);
        var inputPoint = new Vector2(input.WorldX, input.WorldY);
        var start = startFromMathInput ? inputPoint : outputPoint;
        var end = startFromMathInput ? outputPoint : inputPoint;

        Assert.IsType<ScriptNode>(Hit(tree, start));
        Assert.IsType<ScriptNode>(Hit(tree, end));

        var sourceNode = Assert.IsType<ScriptNode>(Hit(tree, start));
        Assert.Same(
            startFromMathInput ? input : output,
            sourceNode.HitTestPort(start.X, start.Y));

        tree.EventManager!.ProcessTouchDown(PointerEventArgs.FromTouch(1, start));
        tree.EventManager.ProcessTouchMove(PointerEventArgs.FromTouch(1, end));
        tree.EventManager.ProcessTouchUp(PointerEventArgs.FromTouch(1, end));

        var connection = Assert.Single(graph.Connections);
        Assert.Same(output, connection.OutputPort);
        Assert.Same(input, connection.InputPort);
    }

    [Fact]
    public void Two_finger_pan_and_zoom_update_node_layout_bounds()
    {
        var (tree, editor, graph) = CreateEditor();
        editor.SpawnNode(NodeTypeId.NumberValue);
        tree.Update(800, 500);

        var model = graph.Nodes.Single();
        var nodeCenter = new Vector2(
            editor.ComputedX + model.X + 90,
            editor.ComputedY + model.Y + 35);
        var node = Assert.IsType<ScriptNode>(Hit(tree, nodeCenter));
        var originalX = node.ComputedX;
        var originalWidth = node.ComputedWidth;
        var first = new Vector2(600, 400);
        var second = new Vector2(700, 400);

        tree.EventManager!.ProcessTouchDown(PointerEventArgs.FromTouch(1, first));
        tree.EventManager.ProcessTouchDown(PointerEventArgs.FromTouch(2, second));
        tree.EventManager.ProcessTouchMove(
            PointerEventArgs.FromTouch(1, first + new Vector2(60, 0)));
        tree.EventManager.ProcessTouchMove(
            PointerEventArgs.FromTouch(2, second + new Vector2(60, 0)));

        Assert.InRange(node.ComputedX - originalX, 59f, 61f);

        tree.EventManager.ProcessTouchMove(
            PointerEventArgs.FromTouch(1, first + new Vector2(35, 0)));
        tree.EventManager.ProcessTouchMove(
            PointerEventArgs.FromTouch(2, second + new Vector2(85, 0)));

        Assert.True(node.ComputedWidth > originalWidth * 1.4f);
        var transformedCenter = new Vector2(
            node.ComputedX + node.ComputedWidth / 2f,
            node.ComputedY + node.ComputedHeight / 2f);
        Assert.Same(node, Hit(tree, transformedCenter));

        tree.EventManager.ProcessTouchUp(
            PointerEventArgs.FromTouch(1, first + new Vector2(35, 0)));
        tree.EventManager.ProcessTouchUp(
            PointerEventArgs.FromTouch(2, second + new Vector2(85, 0)));
    }

    [Fact]
    public void Touch_double_tap_deletes_an_edge()
    {
        var graph = new NodeGraph();
        var source = NodeFactory.Create(NodeTypeId.NumberValue, 60, 80);
        var target = NodeFactory.Create(NodeTypeId.Add, 400, 100);
        graph.AddNode(source);
        graph.AddNode(target);
        graph.AddConnection(
            source.OutputPorts.Single(),
            target.InputPorts.First());
        var (tree, _, _) = CreateEditor(graph);
        tree.Update(800, 500);

        var connection = graph.Connections.Single();
        var point = BezierMidpoint(
            new Vector2(
                connection.OutputPort.WorldX,
                connection.OutputPort.WorldY),
            new Vector2(
                connection.InputPort.WorldX,
                connection.InputPort.WorldY));
        var firstTap = DateTime.UtcNow;
        var secondTap = firstTap.AddMilliseconds(180);

        Tap(tree, point, firstTap);
        Tap(tree, point, secondTap);

        Assert.Empty(graph.Connections);
    }

    private static (UITree Tree, NodeEditorCanvas Editor, NodeGraph Graph) CreateEditor(
        NodeGraph? existingGraph = null)
    {
        var graph = existingGraph ?? new NodeGraph();
        var editor = new NodeEditorCanvas(graph);
        var tree = new UITree();
        tree.SetRoot(editor);
        tree.InitializeEventManager(null);
        tree.Update(800, 500);
        return (tree, editor, graph);
    }

    private static VisualElement? Hit(UITree tree, Vector2 point)
        => tree.EventManager!.HitTest.HitTest(point, new HitTestOptions
        {
            Mode = HitTestMode.InteractiveOnly,
            CheckClipping = true,
            RespectInputTransparency = true
        })?.Element;

    private static void Tap(UITree tree, Vector2 point, DateTime timestamp)
    {
        var down = PointerEventArgs.FromTouch(1, point);
        down.Timestamp = timestamp;
        tree.EventManager!.ProcessTouchDown(down);

        var up = PointerEventArgs.FromTouch(1, point);
        up.Timestamp = timestamp.AddMilliseconds(40);
        tree.EventManager.ProcessTouchUp(up);
    }

    private static Vector2 BezierMidpoint(Vector2 start, Vector2 end)
    {
        var distance = Vector2.Distance(start, end);
        var dx = Math.Clamp(distance * 0.4f, 20f, 220f);
        const float t = 0.5f;
        const float u = 1f - t;

        return new Vector2(
            u * u * u * start.X +
            3 * u * u * t * (start.X + dx) +
            3 * u * t * t * (end.X - dx) +
            t * t * t * end.X,
            u * u * u * start.Y +
            3 * u * u * t * start.Y +
            3 * u * t * t * end.Y +
            t * t * t * end.Y);
    }

}
