using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace Nano.Pages.SpriteEditor;

public sealed class SpriteFramePreview : View<SpriteFramePreview>, IPointerHandler
{
    private readonly SpriteFrame _frame;
    private readonly int _index;
    private readonly bool _isSelected;

    public Action? Selected { get; init; }

    public SpriteFramePreview(SpriteFrame frame, int index, bool isSelected)
    {
        _frame = frame;
        _index = index;
        _isSelected = isSelected;
    }

    public void Refresh() => MarkNeedsPaint();

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = 76;
        DesiredHeight = 74;
    }

    public override void Render(IRenderer renderer)
    {
        var border = _isSelected ? new Color(62, 126, 214) : new Color(150, 160, 175);
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, new Color(242, 245, 249));
        renderer.DrawRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, _isSelected ? 3f : 1f, border);

        const float tileSize = 6f;
        var originX = ComputedX + (ComputedWidth - 8 * tileSize) / 2f;
        var originY = ComputedY + 6f;
        for (var row = 0; row < 8; row++)
        {
            for (var column = 0; column < 8; column++)
            {
                renderer.DrawRect(originX + column * tileSize, originY + row * tileSize, tileSize, tileSize, _frame.Pixels[row, column]);
            }
        }

        renderer.DrawText($"{_index}", ComputedX + 6f, ComputedY + 57f, new Color(50, 60, 75), 12f);
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        Selected?.Invoke();
        e.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs e) { }
    public void OnPointerReleased(PointerEventArgs e) { }
    public void OnPointerEntered(PointerEventArgs e) { }
    public void OnPointerExited(PointerEventArgs e) { }
}
