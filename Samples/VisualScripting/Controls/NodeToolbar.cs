using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using VisualScripting.NodeTypes;

namespace VisualScripting.Controls;

/// <summary>
/// Vertical sidebar with a categorised node palette.
/// Each entry is a PaletteItem: click to spawn at a default position,
/// or drag directly onto the NodeEditorCanvas to place at the cursor.
/// </summary>
public class NodeToolbar : CompositeView<NodeToolbar>
{
    private readonly NodeEditorCanvas _editor;

    private static readonly Color ValueColor   = new Color(60, 100, 200);
    private static readonly Color MathColor    = new Color(190, 110, 30);
    private static readonly Color LogicColor   = new Color(50, 160, 80);
    private static readonly Color ControlColor = new Color(150, 60, 160);
    private static readonly Color IOColor      = new Color(30, 140, 160);

    private static Color CategoryColor(string cat) => cat switch
    {
        "Values"  => ValueColor,
        "Math"    => MathColor,
        "Logic"   => LogicColor,
        "Control" => ControlColor,
        "IO"      => IOColor,
        _         => new Color(80, 80, 90),
    };

    private const float ToolbarWidth = 162f;

    public NodeToolbar(NodeEditorCanvas editor)
    {
        _editor = editor;
        Width = ToolbarWidth;
        VerticalAlignment = VerticalAlignment.Stretch;

        BuildToolbar();
    }

    private void BuildToolbar()
    {
        var list = new VStack();
        list.Spacing = 3f;
        list.Padding = new Rayo.Thickness(8f);
        list.HorizontalAlignment = HorizontalAlignment.Stretch;

        // Palette title
        var title = new Label();
        title.Text       = "Node Palette";
        title.FontSize   = 13;
        title.Foreground = new Color(200, 200, 210);
        title.Height     = 30;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        list.AddChild(title);

        var accordion = new Accordion
        {
            Flush = true,
            SingleExpand = true,
            Spacing = 0f,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var categoryIndex = 0;
        foreach (var (category, items) in NodeFactory.Categories)
        {
            var categoryItems = new VStack();
            categoryItems.Spacing = 3f;
            categoryItems.HorizontalAlignment = HorizontalAlignment.Stretch;

            foreach (var (label, type) in items)
            {
                var captured = type;
                var item = new PaletteItem(label, captured, CategoryColor(category));
                item.OnTap = () => _editor.SpawnNode(captured);
                categoryItems.AddChild(item);
            }

            var categoryExpander = new Expander(category, categoryItems)
            {
                IsExpanded = categoryIndex == 0,
                TextColor = CategoryColor(category)
            };
            accordion.AddExpander(categoryExpander);
            categoryIndex++;
        }

        list.AddChild(accordion);

        var scroll = new ScrollView();
        scroll.VerticalAlignment   = VerticalAlignment.Stretch;
        scroll.HorizontalAlignment = HorizontalAlignment.Stretch;
        scroll.Content(list);

        AddChild(scroll);
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------

    protected override void Measure(float availableWidth, float availableHeight)
    {
        foreach (var child in Children)
            child.MeasureUpdate(ToolbarWidth, availableHeight);

        DesiredWidth  = ToolbarWidth;
        DesiredHeight = availableHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, ToolbarWidth, height);
        foreach (var child in Children)
            child.ArrangeUpdate(x, y, ToolbarWidth, height);
    }

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, new Color(26, 27, 33));
        renderer.DrawRect(ComputedX + ComputedWidth - 1f, ComputedY, 1f, ComputedHeight, new Color(55, 56, 65));
    }
}
