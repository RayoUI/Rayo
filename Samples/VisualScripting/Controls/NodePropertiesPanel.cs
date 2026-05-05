using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;
using VisualScripting.Models;
using VisualScripting.NodeTypes;

namespace VisualScripting.Controls;

/// <summary>
/// Right-side panel that shows and edits the properties of the currently
/// selected node.  Supports:
///   - Number / String nodes  → single-line Entry (text input)
///   - Boolean nodes           → toggle Button (True / False)
///   - All other nodes         → port list (read-only)
/// </summary>
public class NodePropertiesPanel : CompositeView<NodePropertiesPanel>
{
    private const float PanelWidth = 190f;
    private ScriptNode _selectedNode;

    public NodePropertiesPanel()
    {
        Width = PanelWidth;
        VerticalAlignment   = VerticalAlignment.Stretch;
        ShowEmpty();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void Node(ScriptNode node)
    {
        _selectedNode = node;
        Rebuild();
    }

    // -------------------------------------------------------------------------
    // Layout overrides — fixed width, full height
    // -------------------------------------------------------------------------

    public override void Measure(float availableWidth, float availableHeight)
    {
        foreach (var child in Children)
            child.Measure(PanelWidth, availableHeight);
        DesiredWidth  = PanelWidth;
        DesiredHeight = availableHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, PanelWidth, height);
        foreach (var child in Children)
            child.Arrange(x, y, PanelWidth, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Background
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight,
            new Color(26, 27, 33));
        // Left border divider
        renderer.DrawRect(ComputedX, ComputedY, 1f, ComputedHeight,
            new Color(55, 56, 65));
    }

    // -------------------------------------------------------------------------
    // Build helpers
    // -------------------------------------------------------------------------

    private void ShowEmpty()
    {
        ClearChildren();

        var label = new Label();
        label.Text       = "No node selected";
        label.FontSize   = 12;
        label.Foreground = new Color(100, 100, 110);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment   = VerticalAlignment.Center;

        AddChild(label);
    }

    private void Rebuild()
    {
        ClearChildren();

        if (_selectedNode == null)
        {
            ShowEmpty();
            return;
        }

        var model = _selectedNode.Model;

        var list = new VStack();
        list.Spacing = 6f;
        list.Padding = new Rayo.Thickness(10f);
        list.HorizontalAlignment = HorizontalAlignment.Stretch;

        // ---- Header: node title ----
        var titleLabel = new Label();
        titleLabel.Text       = model.Title;
        titleLabel.FontSize   = 14;
        titleLabel.Foreground = Color.White;
        titleLabel.Height     = 34;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Stretch;
        list.AddChild(titleLabel);

        // Thin separator
        var sep = new Frame();
        sep.Height = 1;
        sep.Background = new Color(55, 56, 65);
        sep.HorizontalAlignment = HorizontalAlignment.Stretch;
        list.AddChild(sep);

        // ---- Value editor (for constant-value nodes) ----
        bool isValueNode = model.TypeId is NodeTypeId.NumberValue
                                       or NodeTypeId.BooleanValue
                                       or NodeTypeId.StringValue;

        if (isValueNode)
        {
            var valueLabel = new Label();
            valueLabel.Text       = "Value";
            valueLabel.FontSize   = 11;
            valueLabel.Foreground = new Color(160, 160, 180);
            valueLabel.Height     = 20;
            list.AddChild(valueLabel);

            if (model.TypeId == NodeTypeId.BooleanValue)
            {
                AddBooleanToggle(list, model);
            }
            else
            {
                AddTextEntry(list, model);
            }

            // Separator after value editor
            var sep2 = new Frame();
            sep2.Height = 1;
            sep2.Background = new Color(55, 56, 65);
            sep2.HorizontalAlignment = HorizontalAlignment.Stretch;
            list.AddChild(sep2);
        }

        // ---- Port list ----
        var portsLabel = new Label();
        portsLabel.Text       = "Ports";
        portsLabel.FontSize   = 11;
        portsLabel.Foreground = new Color(160, 160, 180);
        portsLabel.Height     = 20;
        list.AddChild(portsLabel);

        foreach (var port in model.Ports)
        {
            var row = BuildPortRow(port);
            list.AddChild(row);
        }

        AddChild(list);
    }

    private void AddTextEntry(VStack list, NodeModel model)
    {
        var entry = new Entry(model.Value ?? "");
        entry.HorizontalAlignment = HorizontalAlignment.Stretch;
        entry.Height = 28;
        entry.NumericOnly(model.TypeId == NodeTypeId.NumberValue);
        entry.TextChanged += newText =>
        {
            model.Value = newText;
            _selectedNode?.MarkNeedsPaint();
        };
        list.AddChild(entry);
    }

    private void AddBooleanToggle(VStack list, NodeModel model)
    {
        bool current = string.Equals(model.Value, "true", StringComparison.OrdinalIgnoreCase);

        var btn = new Button();
        btn.Text = current ? "True" : "False";
        btn.Background = current
            ? new Color(50, 160, 80)
            : new Color(160, 60, 60);
        btn.HorizontalAlignment = HorizontalAlignment.Stretch;
        btn.Height = 28;

        btn.Tapped += _ =>
        {
            current = !current;
            model.Value = current ? "true" : "false";
            btn.Text = current ? "True" : "False";
            btn.Background = current
                ? new Color(50, 160, 80)
                : new Color(160, 60, 60);
            _selectedNode?.MarkNeedsPaint();
        };

        list.AddChild(btn);
    }

    private static VisualElement BuildPortRow(PortModel port)
    {
        Color typeColor = port.Type switch
        {
            PortType.Number  => new Color(255, 198, 64),
            PortType.Boolean => new Color(72, 214, 100),
            PortType.String  => new Color(196, 112, 255),
            PortType.Flow    => new Color(210, 210, 210),
            _                => new Color(170, 170, 170),
        };

        string dirMark = port.Direction == PortDirection.Input ? "→" : "←";
        string typeName = port.Type == PortType.Flow ? "Exec" : port.Type.ToString();

        var row = new HStack();
        row.Spacing = 4f;
        row.Height  = 20f;
        row.HorizontalAlignment = HorizontalAlignment.Stretch;

        // Direction arrow
        var arrow = new Label();
        arrow.Text       = dirMark;
        arrow.FontSize   = 10;
        arrow.Foreground = typeColor;
        arrow.Width      = 12;

        // Port name
        var name = new Label();
        name.Text     = string.IsNullOrEmpty(port.Name) ? "(exec)" : port.Name;
        name.FontSize = 10;
        name.Foreground = new Color(200, 200, 210);

        // Type badge
        var type = new Label();
        type.Text     = typeName;
        type.FontSize = 9;
        type.Foreground = typeColor;
        type.HorizontalAlignment = HorizontalAlignment.Right;

        row.AddChild(arrow);
        row.AddChild(name);
        row.AddChild(type);

        return row;
    }

}
