using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages.SpriteEditor;

public sealed class SpriteToolPicker(SpriteCanvas canvas) : Component
{
    private readonly Dictionary<SpriteTool, ButtonIcon> _toolButtons = [];

    public override VisualElement Build()
    {
        _toolButtons.Clear();
        return new Frame()
            .Id("Toolbar")
            .Background(new Color(230, 235, 242))
            .Padding(new Thickness(8))
            .Content(new HStack()
                .Spacing(4)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children(
                    ToolButton(Icons.Brush, SpriteTool.Pencil),
                    ToolButton(Icons.Eraser, SpriteTool.Eraser),
                    ToolButton(Icons.FillBucket, SpriteTool.Fill),
                    ToolButton(Icons.Picker, SpriteTool.Picker),
                    ToolButton(Icons.Line, SpriteTool.Line),
                    ToolButton(Icons.Rectangle, SpriteTool.Rectangle),
                    ToolButton(Icons.Ellipse, SpriteTool.Ellipse),
                    new ButtonIcon(Icons.Delete).Size(44).Variant(ButtonVariant.Danger).OnTapped(ConfirmClear)));
    }

    private ButtonIcon ToolButton(IconData icon, SpriteTool tool)
    {
        var button = new ButtonIcon(icon)
            .Size(44)
            .Variant(tool == canvas.Tool ? ButtonVariant.Primary : ButtonVariant.Secondary)
            .OnTapped(() =>
            {
                canvas.Tool = tool;
                RefreshSelection();
            });

        _toolButtons.Add(tool, button);
        return button;
    }

    private void RefreshSelection()
    {
        foreach (var (tool, button) in _toolButtons)
        {
            button.Variant(tool == canvas.Tool ? ButtonVariant.Primary : ButtonVariant.Secondary);
        }
    }

    private void ConfirmClear()
    {
        var message = new VStack()
            .Spacing(8)
            .Padding(new Thickness(8, 4))
            .Children(
                new Label("¿Quieres limpiar el canvas?")
                    .FontSize(14)
                    .HorizontalAlignment(HorizontalAlignment.Left),
                new Label("Esta acción no se puede deshacer.")
                    .FontSize(14)
                    .HorizontalAlignment(HorizontalAlignment.Left));

        Dialog.Show("Limpiar canvas", message, true, canvas.Clear, okText: "Limpiar", cancelText: "Cancelar");
    }
}
