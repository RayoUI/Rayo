using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace Nano.Views.SpriteEditor.Components;

public sealed class SpriteFramePreview : View<SpriteFramePreview>, IPointerHandler, IGestureRecognizerHost
{
    private readonly TapRecognizer _tapRecognizer;
    private readonly SpriteFrame _frame;
    private readonly int _index;
    private bool _isSelected;
    private ITexture? _texture;
    private bool _textureDirty = true;

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

    public void Refresh()
    {
        _textureDirty = true;
        MarkNeedsPaint();
    }

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
        DesiredHeight = 76;
    }

    public override void Render(IRenderer renderer)
    {
        var rowCount = _frame.Pixels.GetLength(0);
        var columnCount = _frame.Pixels.GetLength(1);
        var tileSize = MathF.Min(ComputedWidth / columnCount, ComputedHeight / rowCount);
        var previewWidth = columnCount * tileSize;
        var previewHeight = rowCount * tileSize;
        var previewX = ComputedX + (ComputedWidth - previewWidth) / 2f;
        var previewY = ComputedY + (ComputedHeight - previewHeight) / 2f;

        const float checkerSize = 6f;
        var light = new Color(220, 225, 232);
        var dark = new Color(174, 183, 196);
        renderer.DrawRect(previewX, previewY, previewWidth, previewHeight, light);
        var checkerRows = (int)MathF.Ceiling(previewHeight / checkerSize);
        var checkerColumns = (int)MathF.Ceiling(previewWidth / checkerSize);
        for (var row = 0; row < checkerRows; row++)
        {
            for (var column = row & 1; column < checkerColumns; column += 2)
            {
                var x = previewX + column * checkerSize;
                var y = previewY + row * checkerSize;
                renderer.DrawRect(
                    x,
                    y,
                    MathF.Min(checkerSize, previewX + previewWidth - x),
                    MathF.Min(checkerSize, previewY + previewHeight - y),
                    dark);
            }
        }

        if (_textureDirty || _texture is null)
        {
            _texture?.Dispose();
            _texture = renderer.CreateTextureFromPixels(
                SpriteCanvas.CreateRgbaPixels(_frame),
                columnCount,
                rowCount,
                TextureSamplingMode.Nearest);
            _textureDirty = false;
        }
        renderer.DrawTexture(_texture, previewX, previewY, previewWidth, previewHeight);

        var border = _isSelected ? new Color(62, 126, 214) : new Color(150, 160, 175);
        renderer.DrawRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, _isSelected ? 3f : 1f, border);

        const float indexSize = 18f;
        var indexY = ComputedY + ComputedHeight - indexSize;
        var indexText = _index.ToString();
        var textSize = renderer.MeasureText(indexText, 12f);
        renderer.DrawRect(ComputedX, indexY, indexSize, indexSize, new Color(50, 60, 75));
        renderer.DrawText(
            indexText,
            ComputedX + (indexSize - textSize.X) / 2f,
            indexY + (indexSize - textSize.Y) / 2f,
            new Color(255, 255, 255),
            12f);
    }

    public void OnPointerPressed(PointerEventArgs e) { }

    public void OnPointerMoved(PointerEventArgs e) { }
    public void OnPointerReleased(PointerEventArgs e) { }
    public void OnPointerEntered(PointerEventArgs e) { }
    public void OnPointerExited(PointerEventArgs e) { }

    protected override void OnUnmounted()
    {
        _texture?.Dispose();
        _texture = null;
        base.OnUnmounted();
    }
}
