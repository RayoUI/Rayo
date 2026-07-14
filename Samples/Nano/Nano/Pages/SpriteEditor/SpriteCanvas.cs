using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace Nano.Pages.SpriteEditor;

public enum SpriteTool
{
    Pencil,
    Eraser,
    Fill,
    Line,
    Rectangle,
    Ellipse
}

public sealed class SpriteFrame
{
    public Color[,] Pixels { get; } = new Color[8, 8];

    public SpriteFrame()
    {
        for (var row = 0; row < 8; row++)
        {
            for (var column = 0; column < 8; column++)
            {
                Pixels[row, column] = new Color(244, 247, 250);
            }
        }
    }

    public SpriteFrame Clone()
    {
        var clone = new SpriteFrame();
        Array.Copy(Pixels, clone.Pixels, Pixels.Length);
        return clone;
    }
}

public sealed class SpriteCanvas : View<SpriteCanvas>, IPointerHandler
{
    private const int SpriteSize = 8;
    private const float BaseTileSize = 32f;
    private SpriteFrame _frame = new();
    private Color[,] _pixels;
    private readonly Dictionary<int, Vector2> _touches = [];
    private Color _selectedColor = new(62, 126, 214);
    private Vector2 _pan;
    private Vector2 _pinchStartMidpoint;
    private Vector2 _pinchStartPan;
    private float _pinchStartDistance;
    private float _pinchStartZoom = 1f;
    private float _zoom = 1f;
    private (int Row, int Column)? _shapeStart;
    private int? _shapePointerId;

    public Color SelectedColor
    {
        get => _selectedColor;
        set => _selectedColor = value;
    }

    public SpriteFrame Frame
    {
        get => _frame;
        set
        {
            _frame = value;
            _pixels = value.Pixels;
            MarkNeedsPaint();
            FrameChanged?.Invoke();
        }
    }

    public event Action? FrameChanged;

    public SpriteTool Tool { get; set; } = SpriteTool.Pencil;

    public SpriteCanvas()
    {
        _pixels = _frame.Pixels;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsInfinity(availableWidth) ? 320f : availableWidth;
        DesiredHeight = 360f;
    }

    public override void Render(IRenderer renderer)
    {
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, new Color(38, 48, 64));

        var tileSize = BaseTileSize * _zoom;
        var spriteExtent = SpriteSize * tileSize;
        var origin = GetSpriteOrigin(spriteExtent);

        for (var row = 0; row < SpriteSize; row++)
        {
            for (var column = 0; column < SpriteSize; column++)
            {
                var x = origin.X + column * tileSize;
                var y = origin.Y + row * tileSize;
                renderer.DrawRect(x, y, tileSize, tileSize, _pixels[row, column]);
                renderer.DrawRectOutline(x, y, tileSize, tileSize, 1f, new Color(85, 99, 120));
            }
        }

        renderer.PopScissor();
    }

    public void Clear() => SetAllPixels(new Color(244, 247, 250));

    public void Fill() => SetAllPixels(_selectedColor);

    public void OnPointerPressed(PointerEventArgs e)
    {
        _touches[e.PointerId] = e.Position;
        if (_touches.Count == 1)
        {
            if (IsShapeTool())
            {
                _shapePointerId = e.PointerId;
                _shapeStart = GetCellAt(e.Position);
            }
            else
            {
                PaintAt(e.Position);
            }
        }
        else if (_touches.Count == 2)
        {
            _shapeStart = null;
            _shapePointerId = null;
            BeginPinch();
        }

        e.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        if (!_touches.ContainsKey(e.PointerId))
        {
            return;
        }

        _touches[e.PointerId] = e.Position;
        if (_touches.Count >= 2)
        {
            UpdatePinch();
        }

        e.Handled = true;
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (_touches.Count == 1 && _shapePointerId == e.PointerId &&
            _shapeStart is { } start && GetCellAt(e.Position) is { } end)
        {
            DrawShape(start, end);
        }

        _touches.Remove(e.PointerId);
        _shapeStart = null;
        _shapePointerId = null;
        e.Handled = true;
    }

    public void OnPointerEntered(PointerEventArgs e) { }
    public void OnPointerExited(PointerEventArgs e) { }

    private void BeginPinch()
    {
        var points = _touches.Values.Take(2).ToArray();
        _pinchStartMidpoint = (points[0] + points[1]) / 2f;
        _pinchStartDistance = Vector2.Distance(points[0], points[1]);
        _pinchStartPan = _pan;
        _pinchStartZoom = _zoom;
    }

    private void UpdatePinch()
    {
        var points = _touches.Values.Take(2).ToArray();
        var midpoint = (points[0] + points[1]) / 2f;
        var distance = Vector2.Distance(points[0], points[1]);
        _zoom = Math.Clamp(_pinchStartZoom * distance / Math.Max(_pinchStartDistance, 1f), 0.5f, 5f);
        _pan = _pinchStartPan + midpoint - _pinchStartMidpoint;
        MarkNeedsPaint();
    }

    private void PaintAt(Vector2 point)
    {
        if (GetCellAt(point) is not { } cell)
        {
            return;
        }

        if (Tool == SpriteTool.Fill)
        {
            Fill();
            return;
        }

        _pixels[cell.Row, cell.Column] = Tool == SpriteTool.Eraser
            ? new Color(244, 247, 250)
            : _selectedColor;
        NotifyFrameChanged();
    }

    private (int Row, int Column)? GetCellAt(Vector2 point)
    {
        var tileSize = BaseTileSize * _zoom;
        var origin = GetSpriteOrigin(SpriteSize * tileSize);
        var column = (int)((point.X - origin.X) / tileSize);
        var row = (int)((point.Y - origin.Y) / tileSize);
        return row is >= 0 and < SpriteSize && column is >= 0 and < SpriteSize
            ? (row, column)
            : null;
    }

    private bool IsShapeTool() => Tool is SpriteTool.Line or SpriteTool.Rectangle or SpriteTool.Ellipse;

    private void DrawShape((int Row, int Column) start, (int Row, int Column) end)
    {
        switch (Tool)
        {
            case SpriteTool.Line:
                DrawLine(start, end);
                break;
            case SpriteTool.Rectangle:
                DrawRectangle(start, end);
                break;
            case SpriteTool.Ellipse:
                DrawEllipse(start, end);
                break;
        }

        NotifyFrameChanged();
    }

    private void DrawLine((int Row, int Column) start, (int Row, int Column) end)
    {
        var x0 = start.Column;
        var y0 = start.Row;
        var x1 = end.Column;
        var y1 = end.Row;
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            _pixels[y0, x0] = _selectedColor;
            if (x0 == x1 && y0 == y1)
            {
                return;
            }

            var twiceError = 2 * error;
            if (twiceError >= dy)
            {
                error += dy;
                x0 += sx;
            }
            if (twiceError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void DrawRectangle((int Row, int Column) start, (int Row, int Column) end)
    {
        var minRow = Math.Min(start.Row, end.Row);
        var maxRow = Math.Max(start.Row, end.Row);
        var minColumn = Math.Min(start.Column, end.Column);
        var maxColumn = Math.Max(start.Column, end.Column);

        for (var column = minColumn; column <= maxColumn; column++)
        {
            _pixels[minRow, column] = _selectedColor;
            _pixels[maxRow, column] = _selectedColor;
        }
        for (var row = minRow; row <= maxRow; row++)
        {
            _pixels[row, minColumn] = _selectedColor;
            _pixels[row, maxColumn] = _selectedColor;
        }
    }

    private void DrawEllipse((int Row, int Column) start, (int Row, int Column) end)
    {
        var minRow = Math.Min(start.Row, end.Row);
        var maxRow = Math.Max(start.Row, end.Row);
        var minColumn = Math.Min(start.Column, end.Column);
        var maxColumn = Math.Max(start.Column, end.Column);
        var radiusX = (maxColumn - minColumn) / 2f;
        var radiusY = (maxRow - minRow) / 2f;

        if (radiusX == 0 || radiusY == 0)
        {
            DrawLine(start, end);
            return;
        }

        var centerX = (minColumn + maxColumn) / 2f;
        var centerY = (minRow + maxRow) / 2f;
        for (var row = minRow; row <= maxRow; row++)
        {
            for (var column = minColumn; column <= maxColumn; column++)
            {
                var normalizedDistance = MathF.Pow((column - centerX) / radiusX, 2) +
                                         MathF.Pow((row - centerY) / radiusY, 2);
                if (MathF.Abs(normalizedDistance - 1f) <= 0.4f)
                {
                    _pixels[row, column] = _selectedColor;
                }
            }
        }
    }

    private Vector2 GetSpriteOrigin(float spriteExtent) => new(
        ComputedX + (ComputedWidth - spriteExtent) / 2f + _pan.X,
        ComputedY + (ComputedHeight - spriteExtent) / 2f + _pan.Y);

    private void SetAllPixels(Color color)
    {
        for (var row = 0; row < SpriteSize; row++)
        {
            for (var column = 0; column < SpriteSize; column++)
            {
                _pixels[row, column] = color;
            }
        }

        NotifyFrameChanged();
    }

    private void NotifyFrameChanged()
    {
        MarkNeedsPaint();
        FrameChanged?.Invoke();
    }
}
