using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace Nano.Pages.SpriteEditor;

public sealed class SpriteFramePreview : View<SpriteFramePreview>, IPointerHandler, IGestureRecognizerHost
{
    private readonly TapRecognizer _tapRecognizer;
    private readonly SpriteFrame _frame;
    private readonly int _index;
    private bool _isSelected;

    public Action? Selected { get; init; }
    public Action? OptionsRequested { get; set; }

    public List<IGestureRecognizer> GestureRecognizers { get; } = [];

    public SpriteFramePreview(SpriteFrame frame, int index, bool isSelected)
    {
        _frame = frame;
        _index = index;
        _isSelected = isSelected;
        _tapRecognizer = new TapRecognizer(
            maxMovementThreshold: 5f,
            maxPressDurationMs: 500,
            doubleTapWindowMs: 300);
        _tapRecognizer.TapDetected += e =>
        {
            if (e.TapCount >= 2)
            {
                OptionsRequested?.Invoke();
            }
            else
            {
                Selected?.Invoke();
            }
        };
        GestureRecognizers.Add(_tapRecognizer);
    }

    public void Refresh() => MarkNeedsPaint();

    public void SetSelected(bool isSelected)
    {
        if (_isSelected == isSelected)
        {
            return;
        }

        _isSelected = isSelected;
        MarkNeedsPaint();
    }

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

    public void OnPointerPressed(PointerEventArgs e) { }

    public void OnPointerMoved(PointerEventArgs e) { }
    public void OnPointerReleased(PointerEventArgs e) { }
    public void OnPointerEntered(PointerEventArgs e) { }
    public void OnPointerExited(PointerEventArgs e) { }
}
