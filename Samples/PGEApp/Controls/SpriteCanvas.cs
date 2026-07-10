using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace PGEApp.Controls;

public sealed class SpriteDocument
{
    public const int Columns = 16;
    public const int Rows = 16;

    private readonly Color?[,] _pixels = new Color?[Columns, Rows];

    public Color? SelectedColor { get; set; } = new Color(14, 165, 233);

    public Color? GetPixel(int column, int row) => _pixels[column, row];

    public void SetPixel(int column, int row, Color? color)
        => _pixels[column, row] = color;

    public void Clear() => Array.Clear(_pixels);
}

public sealed class SpriteCanvas : View<SpriteCanvas>, IPointerHandler
{
    private readonly SpriteDocument _document;
    private bool _isDrawing;

    public SpriteCanvas(SpriteDocument document)
    {
        _document = document;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public void Clear()
    {
        _document.Clear();
        MarkNeedsPaint();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth)
            ? availableWidth
            : 560;
        DesiredHeight = availableHeight > 0 && !float.IsInfinity(availableHeight)
            ? availableHeight
            : 480;
    }

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRect(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            new Color(9, 13, 21));

        var (origin, cellSize) = GetGridGeometry();
        var gridSize = cellSize * SpriteDocument.Columns;

        renderer.DrawRect(
            origin.X - 5,
            origin.Y - 5,
            gridSize + 10,
            gridSize + 10,
            new Color(30, 41, 59));

        for (var row = 0; row < SpriteDocument.Rows; row++)
        {
            for (var column = 0; column < SpriteDocument.Columns; column++)
            {
                var x = origin.X + column * cellSize;
                var y = origin.Y + row * cellSize;
                var pixel = _document.GetPixel(column, row);
                var checker = (column + row) % 2 == 0
                    ? new Color(203, 213, 225)
                    : new Color(148, 163, 184);

                renderer.DrawRect(x, y, cellSize, cellSize, pixel ?? checker);
                renderer.DrawRectOutline(
                    x,
                    y,
                    cellSize,
                    cellSize,
                    1,
                    new Color(15, 23, 42, 100));
            }
        }

        renderer.DrawRectOutline(
            origin.X,
            origin.Y,
            gridSize,
            gridSize,
            2,
            new Color(71, 85, 105));
    }

    void IPointerHandler.OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        _isDrawing = true;
        PaintAt(e.Position);
        e.Handled = true;
    }

    void IPointerHandler.OnPointerMoved(PointerEventArgs e)
    {
        if (_isDrawing)
        {
            PaintAt(e.Position);
            e.Handled = true;
        }
    }

    void IPointerHandler.OnPointerReleased(PointerEventArgs e)
    {
        _isDrawing = false;
        e.Handled = true;
    }

    private void PaintAt(Vector2 position)
    {
        var (origin, cellSize) = GetGridGeometry();
        var column = (int)((position.X - origin.X) / cellSize);
        var row = (int)((position.Y - origin.Y) / cellSize);

        if (position.X < origin.X ||
            position.Y < origin.Y ||
            column < 0 ||
            column >= SpriteDocument.Columns ||
            row < 0 ||
            row >= SpriteDocument.Rows)
        {
            return;
        }

        _document.SetPixel(column, row, _document.SelectedColor);
        MarkNeedsPaint();
    }

    private (Vector2 Origin, float CellSize) GetGridGeometry()
    {
        const float padding = 28;
        var availableSize = MathF.Max(
            16,
            MathF.Min(ComputedWidth, ComputedHeight) - padding * 2);
        var cellSize = MathF.Max(
            1,
            MathF.Floor(availableSize / SpriteDocument.Columns));
        var gridSize = cellSize * SpriteDocument.Columns;

        return (
            new Vector2(
                ComputedX + (ComputedWidth - gridSize) / 2,
                ComputedY + (ComputedHeight - gridSize) / 2),
            cellSize);
    }
}
