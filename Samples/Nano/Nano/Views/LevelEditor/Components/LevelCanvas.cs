using System.Numerics;
using Nano.ViewModels;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Rendering;

namespace Nano.Views.LevelEditor.Components;

internal sealed class LevelCanvas : View<LevelCanvas>, IPointerHandler, IExclusiveTouchHandler
{
    private const float BaseTileSize = 32f;
    private const float MinimumZoom = 0.15f;
    private const float MaximumZoom = 6f;
    private readonly LevelEditorViewModel _viewModel;
    private readonly Dictionary<int, Vector2> _touches = [];
    private Vector2 _pan;
    private Vector2 _pinchWorldAnchor;
    private float _zoom = 1f;
    private float _pinchStartDistance;
    private float _pinchStartZoom;
    private int? _paintPointerId;
    private bool _paintedDuringDrag;
    private bool _gestureWasPinch;
    private (int Column, int Row)? _lastPaintedCell;

    public LevelCanvas(LevelEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsInfinity(availableWidth) ? 640 : availableWidth;
        DesiredHeight = float.IsInfinity(availableHeight) ? 360 : availableHeight;
    }

    public override void Render(IRenderer renderer)
    {
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        renderer.DrawRect(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            new Color(15, 23, 36));

        var tileSize = GetTileSize();
        var origin = GetWorldOrigin();
        var visible = GetVisibleCells(origin, tileSize);

        RenderPlacedTiles(renderer, origin, tileSize, visible);
        RenderAdaptiveGrid(renderer, origin, tileSize, visible);
        RenderObjects(renderer, origin, tileSize, visible);
        renderer.PopScissor();
    }

    public void OnPointerPressed(PointerEventArgs args)
    {
        _touches[args.PointerId] = args.Position;
        if (_touches.Count == 1)
        {
            _paintPointerId = args.PointerId;
            _paintedDuringDrag = false;
            _gestureWasPinch = false;
        }
        else if (_touches.Count == 2)
        {
            _paintPointerId = null;
            _lastPaintedCell = null;
            _gestureWasPinch = true;
            BeginPinch();
        }

        args.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs args)
    {
        if (!_touches.ContainsKey(args.PointerId))
            return;

        _touches[args.PointerId] = args.Position;
        if (_touches.Count >= 2)
        {
            UpdatePinch();
        }
        else if (!_gestureWasPinch &&
                 _paintPointerId == args.PointerId &&
                 _viewModel.SelectedCategory.Value == LevelAssetCategory.Tiles)
        {
            PlaceAt(args.Position);
            _paintedDuringDrag = true;
        }

        args.Handled = true;
    }

    public void OnPointerReleased(PointerEventArgs args)
    {
        if (_touches.Count == 1 &&
            !_gestureWasPinch &&
            _paintPointerId == args.PointerId &&
            !_paintedDuringDrag)
        {
            PlaceAt(args.Position);
        }

        _touches.Remove(args.PointerId);
        _lastPaintedCell = null;
        _paintPointerId = null;
        _paintedDuringDrag = false;
        if (_touches.Count == 0)
            _gestureWasPinch = false;
        args.Handled = true;
    }

    public void OnPointerCanceled(PointerEventArgs args)
    {
        _touches.Remove(args.PointerId);
        _lastPaintedCell = null;
        _paintPointerId = null;
        _paintedDuringDrag = false;
        if (_touches.Count == 0)
            _gestureWasPinch = false;
    }

    private void BeginPinch()
    {
        var points = _touches.Values.Take(2).ToArray();
        var midpoint = (points[0] + points[1]) / 2f;
        _pinchStartDistance = MathF.Max(1f, Vector2.Distance(points[0], points[1]));
        _pinchStartZoom = _zoom;
        _pinchWorldAnchor = ScreenToWorld(midpoint);
    }

    private void UpdatePinch()
    {
        var points = _touches.Values.Take(2).ToArray();
        var midpoint = (points[0] + points[1]) / 2f;
        var distance = Vector2.Distance(points[0], points[1]);
        _zoom = Math.Clamp(
            _pinchStartZoom * distance / _pinchStartDistance,
            MinimumZoom,
            MaximumZoom);

        var center = GetViewportCenter();
        _pan = midpoint - center - _pinchWorldAnchor * GetTileSize();
        MarkNeedsPaint();
    }

    private void PlaceAt(Vector2 position)
    {
        var world = ScreenToWorld(position);
        var column = (int)MathF.Floor(world.X);
        var row = (int)MathF.Floor(world.Y);
        if (_lastPaintedCell == (column, row))
            return;

        _lastPaintedCell = (column, row);
        _viewModel.PlaceAt(column, row);
        MarkNeedsPaint();
    }

    private void RenderPlacedTiles(
        IRenderer renderer,
        Vector2 origin,
        float tileSize,
        VisibleCells visible)
    {
        foreach (var (position, tile) in _viewModel.TileMap)
        {
            if (!visible.Contains(position.Column, position.Row))
                continue;

            renderer.DrawRect(
                origin.X + position.Column * tileSize,
                origin.Y + position.Row * tileSize,
                tileSize,
                tileSize,
                tile.Color);
        }
    }

    private void RenderObjects(
        IRenderer renderer,
        Vector2 origin,
        float tileSize,
        VisibleCells visible)
    {
        foreach (var instance in _viewModel.ObjectInstances)
        {
            if (!visible.Contains(instance.Column, instance.Row))
                continue;

            var centerX = origin.X + (instance.Column + 0.5f) * tileSize;
            var centerY = origin.Y + (instance.Row + 0.5f) * tileSize;
            var radius = MathF.Max(2, tileSize * 0.32f);
            renderer.DrawCircle(centerX, centerY, radius, instance.Color);
            if (tileSize >= 7)
                renderer.DrawCircleOutline(centerX, centerY, radius, 1.5f, Color.White);
        }
    }

    private void RenderAdaptiveGrid(
        IRenderer renderer,
        Vector2 origin,
        float tileSize,
        VisibleCells visible)
    {
        var majorStep = GetMajorGridStep(tileSize);
        var minorStep = tileSize >= 8f ? 1 : tileSize * 4f >= 8f ? 4 : majorStep;

        RenderGridLines(
            renderer,
            origin,
            tileSize,
            visible,
            minorStep,
            new Color(55, 67, 84, 135),
            1f,
            skipMultiplesOf: majorStep);
        RenderGridLines(
            renderer,
            origin,
            tileSize,
            visible,
            majorStep,
            new Color(100, 116, 139, 210),
            tileSize >= 10 ? 2f : 1.5f);

        var axisColor = new Color(62, 126, 214, 235);
        if (visible.MinColumn <= 0 && visible.MaxColumn >= 0)
        {
            var x = origin.X;
            renderer.DrawLine(x, ComputedY, x, ComputedY + ComputedHeight, 2.5f, axisColor);
        }
        if (visible.MinRow <= 0 && visible.MaxRow >= 0)
        {
            var y = origin.Y;
            renderer.DrawLine(ComputedX, y, ComputedX + ComputedWidth, y, 2.5f, axisColor);
        }
    }

    private void RenderGridLines(
        IRenderer renderer,
        Vector2 origin,
        float tileSize,
        VisibleCells visible,
        int step,
        Color color,
        float thickness,
        int skipMultiplesOf = 0)
    {
        var firstColumn = FloorToMultiple(visible.MinColumn, step);
        for (var column = firstColumn; column <= visible.MaxColumn + 1; column += step)
        {
            if (skipMultiplesOf > 0 && Mod(column, skipMultiplesOf) == 0)
                continue;
            var x = origin.X + column * tileSize;
            renderer.DrawLine(x, ComputedY, x, ComputedY + ComputedHeight, thickness, color);
        }

        var firstRow = FloorToMultiple(visible.MinRow, step);
        for (var row = firstRow; row <= visible.MaxRow + 1; row += step)
        {
            if (skipMultiplesOf > 0 && Mod(row, skipMultiplesOf) == 0)
                continue;
            var y = origin.Y + row * tileSize;
            renderer.DrawLine(ComputedX, y, ComputedX + ComputedWidth, y, thickness, color);
        }
    }

    private VisibleCells GetVisibleCells(Vector2 origin, float tileSize) => new(
        (int)MathF.Floor((ComputedX - origin.X) / tileSize) - 1,
        (int)MathF.Ceiling((ComputedX + ComputedWidth - origin.X) / tileSize) + 1,
        (int)MathF.Floor((ComputedY - origin.Y) / tileSize) - 1,
        (int)MathF.Ceiling((ComputedY + ComputedHeight - origin.Y) / tileSize) + 1);

    private Vector2 ScreenToWorld(Vector2 screen) =>
        (screen - GetWorldOrigin()) / GetTileSize();

    private Vector2 GetWorldOrigin() => GetViewportCenter() + _pan;

    private Vector2 GetViewportCenter() => new(
        ComputedX + ComputedWidth / 2f,
        ComputedY + ComputedHeight / 2f);

    private float GetTileSize() => BaseTileSize * _zoom;

    private static int GetMajorGridStep(float tileSize)
    {
        var step = 8;
        while (tileSize * step < 40f)
            step *= 4;
        return step;
    }

    private static int FloorToMultiple(int value, int step) =>
        (int)MathF.Floor(value / (float)step) * step;

    private static int Mod(int value, int modulus) =>
        (value % modulus + modulus) % modulus;

    private readonly record struct VisibleCells(
        int MinColumn,
        int MaxColumn,
        int MinRow,
        int MaxRow)
    {
        public bool Contains(int column, int row) =>
            column >= MinColumn &&
            column <= MaxColumn &&
            row >= MinRow &&
            row <= MaxRow;
    }
}
