using System.Numerics;
using Nano.Views.SpriteEditor;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace Nano.Views.SpriteEditor.Components;

public enum SpriteTool
{
    Pencil,
    Eraser,
    Fill,
    Picker,
    Line,
    Rectangle,
    Ellipse
}

public sealed class SpriteFrame
{
    public int Width { get; }
    public int Height { get; }
    public Color[,] Pixels { get; }

    public SpriteFrame(int width = 16, int height = 16)
    {
        SpriteAssetDocument.ValidateDimensions(width, height);
        Width = width;
        Height = height;
        Pixels = new Color[height, width];
        for (var row = 0; row < height; row++)
        {
            for (var column = 0; column < width; column++)
            {
                Pixels[row, column] = new Color(244, 247, 250);
            }
        }
    }

    public SpriteFrame Clone()
    {
        var clone = new SpriteFrame(Width, Height);
        Array.Copy(Pixels, clone.Pixels, Pixels.Length);
        return clone;
    }
}

public sealed class SpriteCanvas : View<SpriteCanvas>, IPointerHandler, IExclusiveTouchHandler
{
    private const float BaseTileSize = 32f;
    private const float CanvasMargin = 24f;
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
    private float _initialFitScale = 1f;
    private bool _hasInitializedViewport;
    private (int Row, int Column)? _shapeStart;
    private (int Row, int Column)? _shapePreviewEnd;
    private int? _shapePointerId;
    private int? _paintPointerId;
    private bool _paintedDuringDrag;

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
    public event Action? EditCommitted;
    public event Action<Color>? ColorPicked;

    public SpriteTool Tool { get; set; } = SpriteTool.Pencil;

    public SpriteCanvas()
    {
        _pixels = _frame.Pixels;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsInfinity(availableWidth) ? 320f : availableWidth;
        DesiredHeight = float.IsInfinity(availableHeight) ? 360f : availableHeight;
    }

    public override void Render(IRenderer renderer)
    {
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, new Color(38, 48, 64));

        var tileSize = GetTileSize();
        var origin = GetSpriteOrigin(_frame.Width * tileSize, _frame.Height * tileSize);

        for (var row = 0; row < _frame.Height; row++)
        {
            for (var column = 0; column < _frame.Width; column++)
            {
                var x = origin.X + column * tileSize;
                var y = origin.Y + row * tileSize;
                renderer.DrawRect(x, y, tileSize, tileSize, _pixels[row, column]);
                renderer.DrawRectOutline(x, y, tileSize, tileSize, 0.01f, new Color(85, 99, 120));
            }
        }

        if (_shapeStart is { } start && _shapePreviewEnd is { } end)
        {
            RenderShapePreview(renderer, start, end, origin, tileSize);
        }

        renderer.PopScissor();
    }

    public void Clear()
    {
        SetAllPixels(new Color(244, 247, 250));
        EditCommitted?.Invoke();
    }

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
                _shapePreviewEnd = _shapeStart;
            }
            else
            {
                // Defer a tap until release so a second finger can turn the
                // interaction into a pan/zoom gesture without painting.
                _paintPointerId = e.PointerId;
                _paintedDuringDrag = false;
            }
        }
        else if (_touches.Count == 2)
        {
            _shapeStart = null;
            _shapePreviewEnd = null;
            _shapePointerId = null;
            _paintPointerId = null;
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
        else if (_paintPointerId == e.PointerId && !IsShapeTool() &&
                 (Tool is SpriteTool.Pencil or SpriteTool.Eraser))
        {
            PaintAt(e.Position);
            _paintedDuringDrag = true;
        }
        else if (_shapePointerId == e.PointerId && IsShapeTool())
        {
            _shapePreviewEnd = GetCellAt(e.Position);
            MarkNeedsPaint();
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
        else if (_touches.Count == 1 && _paintPointerId == e.PointerId && !_paintedDuringDrag)
        {
            PaintAt(e.Position);
        }

        if ((_shapePointerId == e.PointerId || _paintPointerId == e.PointerId) && Tool != SpriteTool.Picker)
        {
            EditCommitted?.Invoke();
        }

        _touches.Remove(e.PointerId);
        _shapeStart = null;
        _shapePreviewEnd = null;
        _shapePointerId = null;
        _paintPointerId = null;
        _paintedDuringDrag = false;
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
            FloodFill(cell.Row, cell.Column);
            return;
        }

        if (Tool == SpriteTool.Picker)
        {
            ColorPicked?.Invoke(_pixels[cell.Row, cell.Column]);
            return;
        }

        _pixels[cell.Row, cell.Column] = Tool == SpriteTool.Eraser
            ? new Color(244, 247, 250)
            : _selectedColor;
        NotifyFrameChanged();
    }

    private (int Row, int Column)? GetCellAt(Vector2 point)
    {
        var tileSize = GetTileSize();
        var origin = GetSpriteOrigin(_frame.Width * tileSize, _frame.Height * tileSize);
        var column = (int)((point.X - origin.X) / tileSize);
        var row = (int)((point.Y - origin.Y) / tileSize);
        return row >= 0 && row < _frame.Height && column >= 0 && column < _frame.Width
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
        => DrawLine(start, end, (row, column) => _pixels[row, column] = _selectedColor);

    private static void DrawLine((int Row, int Column) start, (int Row, int Column) end, Action<int, int> setPixel)
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
            setPixel(y0, x0);
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
        => DrawRectangle(start, end, (row, column) => _pixels[row, column] = _selectedColor);

    private static void DrawRectangle((int Row, int Column) start, (int Row, int Column) end, Action<int, int> setPixel)
    {
        var minRow = Math.Min(start.Row, end.Row);
        var maxRow = Math.Max(start.Row, end.Row);
        var minColumn = Math.Min(start.Column, end.Column);
        var maxColumn = Math.Max(start.Column, end.Column);

        for (var column = minColumn; column <= maxColumn; column++)
        {
            setPixel(minRow, column);
            setPixel(maxRow, column);
        }
        for (var row = minRow; row <= maxRow; row++)
        {
            setPixel(row, minColumn);
            setPixel(row, maxColumn);
        }
    }

    private void DrawEllipse((int Row, int Column) start, (int Row, int Column) end)
        => DrawEllipse(start, end, (row, column) => _pixels[row, column] = _selectedColor);

    private static void DrawEllipse((int Row, int Column) start, (int Row, int Column) end, Action<int, int> setPixel)
    {
        var minRow = Math.Min(start.Row, end.Row);
        var maxRow = Math.Max(start.Row, end.Row);
        var minColumn = Math.Min(start.Column, end.Column);
        var maxColumn = Math.Max(start.Column, end.Column);
        var radiusX = (maxColumn - minColumn) / 2f;
        var radiusY = (maxRow - minRow) / 2f;

        if (radiusX == 0 || radiusY == 0)
        {
            DrawLine(start, end, setPixel);
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
                    setPixel(row, column);
                }
            }
        }
    }

    private void RenderShapePreview(
        IRenderer renderer,
        (int Row, int Column) start,
        (int Row, int Column) end,
        Vector2 origin,
        float tileSize)
    {
        void DrawPreviewCell(int row, int column)
        {
            renderer.DrawRectOutline(
                origin.X + column * tileSize,
                origin.Y + row * tileSize,
                tileSize,
                tileSize,
                3f,
                _selectedColor);
        }

        switch (Tool)
        {
            case SpriteTool.Line:
                DrawLine(start, end, DrawPreviewCell);
                break;
            case SpriteTool.Rectangle:
                DrawRectangle(start, end, DrawPreviewCell);
                break;
            case SpriteTool.Ellipse:
                DrawEllipse(start, end, DrawPreviewCell);
                break;
        }
    }

    private Vector2 GetSpriteOrigin(float spriteWidth, float spriteHeight) => new(
        ComputedX + (ComputedWidth - spriteWidth) / 2f + _pan.X,
        ComputedY + (ComputedHeight - spriteHeight) / 2f + _pan.Y);

    private float GetTileSize()
    {
        if (!_hasInitializedViewport && ComputedWidth > 0 && ComputedHeight > 0)
        {
            var availableExtent = MathF.Max(1f, MathF.Min(ComputedWidth, ComputedHeight) - CanvasMargin * 2f);
            var longestSide = Math.Max(_frame.Width, _frame.Height);
            _initialFitScale = MathF.Min(1f, availableExtent / (longestSide * BaseTileSize));
            _hasInitializedViewport = true;
        }

        return BaseTileSize * _initialFitScale * _zoom;
    }

    private void SetAllPixels(Color color)
    {
        for (var row = 0; row < _frame.Height; row++)
        {
            for (var column = 0; column < _frame.Width; column++)
            {
                _pixels[row, column] = color;
            }
        }

        NotifyFrameChanged();
    }

    private void FloodFill(int startRow, int startColumn)
    {
        var sourceColor = _pixels[startRow, startColumn];
        if (sourceColor == _selectedColor)
        {
            return;
        }

        var pending = new Queue<(int Row, int Column)>();
        pending.Enqueue((startRow, startColumn));

        while (pending.Count > 0)
        {
            var (row, column) = pending.Dequeue();
            if (row < 0 || row >= _frame.Height || column < 0 || column >= _frame.Width ||
                _pixels[row, column] != sourceColor)
            {
                continue;
            }

            _pixels[row, column] = _selectedColor;
            pending.Enqueue((row - 1, column));
            pending.Enqueue((row + 1, column));
            pending.Enqueue((row, column - 1));
            pending.Enqueue((row, column + 1));
        }

        NotifyFrameChanged();
    }

    private void NotifyFrameChanged()
    {
        MarkNeedsPaint();
        FrameChanged?.Invoke();
    }
}
