using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages.SpriteEditor;

public sealed class SpriteToolPicker(SpriteCanvas canvas) : Component
{
    public override VisualElement Build()
    {
        return new Frame()
            .Id("Toolbar")
            .Background(new Color(230, 235, 242))
            .Padding(new Thickness(8))
            .Content(new HStack()
                .Spacing(4)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children(
                    ToolButton(Icons.BrushTool, SpriteTool.Pencil),
                    ToolButton(Icons.Eraser, SpriteTool.Eraser),
                    ToolButton(Icons.FillBucket, SpriteTool.Fill),
                    ToolButton(Icons.Picker, SpriteTool.Picker),
                    ToolButton(Icons.LineTool, SpriteTool.Line),
                    ToolButton(Icons.RectangleTool, SpriteTool.Rectangle),
                    ToolButton(Icons.EllipseTool, SpriteTool.Ellipse),
                    new ButtonIcon(Icons.Delete).Size(44).Variant(ButtonVariant.Danger).OnTapped(canvas.Clear)));
    }

    private ButtonIcon ToolButton(IconData icon, SpriteTool tool) => new ButtonIcon(icon)
        .Size(44)
        .Variant(ButtonVariant.Secondary)
        .OnTapped(() => canvas.Tool = tool);
}
