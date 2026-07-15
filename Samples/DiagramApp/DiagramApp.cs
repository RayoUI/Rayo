using DiagramApp.Controls;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace DiagramApp;

public class DiagramApp : Component
{
    private readonly DiagramCanvas _canvas = new();
    private ButtonIcon? _connectToolButton;
    private ButtonIcon? _moveToolButton;

    public override VisualElement Build()
    {
        return new Grid()
            .Columns(GridLength.Pixels(240), GridLength.Star)
            .Rows(GridLength.Star)
            .Background(new Color(18, 20, 24))
            .AddChild(CreateSidePanel(), 0, 0)
            .AddChild(_canvas, 0, 1);
    }

    private VisualElement CreateSidePanel()
    {
        return new Frame()
            .Background(new Color(30, 34, 40))
            .BorderBrush(new Color(48, 54, 64))
            .BorderThickness(1)
            .Padding(new Thickness(16))
            .Content(
                new VStack()
                    .Spacing(12)
                    .Height(20)
                    .Children(
                        new Label("DiagramApp")
                            .FontSize(24)
                            .Foreground(Color.White)
                            .TextHorizontalAlignment(HorizontalAlignment.Left),
                        new Label("Shapes")
                            .FontSize(13)
                            .Foreground(Color.LightGray),
                        new DiagramToolboxItem(DiagramShapeKind.Rectangle, "Rectangle"),
                        new DiagramToolboxItem(DiagramShapeKind.Ellipse, "Ellipse"),
                        new DiagramToolboxItem(DiagramShapeKind.Diamond, "Diamond"),
                        new Label("Tools")
                            .FontSize(13)
                            .Foreground(Color.LightGray),
                        new HStack()
                            .Height(46)
                            .Spacing(10)
                            .Alignment(Alignment.Center)
                            .VerticalAlignment(VerticalAlignment.Top)
                            .Children(
                                CreateToolButton(Icons.Connector, "Connect", DiagramTool.Connect),
                                CreateToolButton(Icons.Move, "Move", DiagramTool.Select),
                                CreateClearButton()
                            ),
                        new Label("Drag shapes into the editor.\nIn Connect mode, drag from one shape to another.\nDouble-click a connection to delete it.")
                            .FontSize(12)
                            .LineHeight(1.25f)
                            .Foreground(new Color(178, 186, 198))
                    ));
    }

    private VisualElement CreateToolButton(IconData icon, string tooltip, DiagramTool tool)
    {
        var button = new ButtonIcon(icon)
            .Size(46)
            .IconSize(24)
            .IconColor(Color.White)
            .OnTapped(() => SetActiveTool(tool));

        if (tool == DiagramTool.Connect)
        {
            _connectToolButton = button;
        }
        else if (tool == DiagramTool.Select)
        {
            _moveToolButton = button;
        }

        ApplyToolButtonState(button, _canvas.ActiveTool == tool);
        return button.WithTooltip(tooltip, TooltipPlacement.Bottom);
    }

    private VisualElement CreateClearButton()
    {
        var button = new ButtonIcon(Icons.Broom)
            .Size(46)
            .IconSize(24)
            .IconColor(Color.White)
            .Background(new Color(84, 92, 104))
            .HoverBackground(new Color(104, 114, 128))
            .PressedBackground(new Color(66, 72, 82))
            .BorderBrush(new Color(112, 122, 138))
            .BorderThickness(1.5f)
            .OnTapped(ConfirmClear);

        return button.WithTooltip("Clear", TooltipPlacement.Bottom);
    }

    private void ConfirmClear()
    {
        Dialog.Show(
            "Clear Diagram",
            "Do you want to delete all shapes and connections?",
            showCancelButton: true,
            onAccepted: _canvas.Clear,
            okText: "Clear",
            cancelText: "Cancel");
    }

    private void SetActiveTool(DiagramTool tool)
    {
        _canvas.ActiveTool = tool;
        UpdateToolButtons();
    }

    private void UpdateToolButtons()
    {
        if (_connectToolButton is not null)
        {
            ApplyToolButtonState(_connectToolButton, _canvas.ActiveTool == DiagramTool.Connect);
        }

        if (_moveToolButton is not null)
        {
            ApplyToolButtonState(_moveToolButton, _canvas.ActiveTool == DiagramTool.Select);
        }
    }

    private static void ApplyToolButtonState(ButtonIcon button, bool isActive)
    {
        button
            .Background(isActive ? new Color(62, 126, 214) : new Color(54, 64, 78))
            .HoverBackground(isActive ? new Color(76, 146, 238) : new Color(68, 82, 100))
            .PressedBackground(isActive ? new Color(46, 100, 178) : new Color(42, 50, 62))
            .BorderBrush(isActive ? new Color(246, 196, 92) : new Color(84, 104, 130))
            .BorderThickness(isActive ? 2.5f : 1.5f);
    }
}
