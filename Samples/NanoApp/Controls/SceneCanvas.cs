using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace NanoApp.Controls;

public enum SceneEntityKind
{
    Rectangle,
    Circle,
    Triangle
}

internal static class SceneColors
{
    public static Color Fill(SceneEntityKind kind) => kind switch
    {
        SceneEntityKind.Circle => new Color(168, 85, 247),
        SceneEntityKind.Triangle => new Color(249, 115, 22),
        _ => new Color(14, 165, 233)
    };
}

internal sealed class SceneEntity
{
    public required SceneEntityKind Kind { get; init; }
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; init; } = 112;
    public float Height { get; init; } = 80;
}

public sealed class SceneCanvas : View<SceneCanvas>, IPointerHandler, IDropTarget
{
    private readonly List<SceneEntity> _entities = [];
    private int _nextEntityNumber = 1;
    private Vector2 _viewportOffset;
    private Vector2 _lastPanPosition;
    private Vector2 _entityDragOffset;
    private Vector2 _dropPreviewPoint;
    private SceneEntity? _draggedEntity;
    private SceneEntity? _selectedEntity;
    private SceneEntityKind? _dropPreviewKind;
    private bool _isPanning;

    public SceneCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public bool IsDropTargetActive { get; set; }
    public DropConstraints? Constraints => null;
    public DragDropEffect? AllowedEffects => DragDropEffect.Copy;
    internal event Action<SceneEntity?>? SelectionChanged;

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth)
            ? availableWidth
            : 800;
        DesiredHeight = availableHeight > 0 && !float.IsInfinity(availableHeight)
            ? availableHeight
            : 480;
    }

    public override void Render(IRenderer renderer)
    {
        renderer.PushScissor(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        renderer.DrawRect(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            new Color(9, 13, 21));

        RenderInfiniteGrid(renderer);

        foreach (var entity in _entities)
        {
            DrawEntity(renderer, entity);
        }

        if (_dropPreviewKind is { } previewKind)
        {
            DrawEntity(
                renderer,
                previewKind,
                _dropPreviewPoint.X - 56,
                _dropPreviewPoint.Y - 40,
                ghost: true,
                selected: false);
        }

        if (IsDropTargetActive)
        {
            renderer.DrawRectOutline(
                ComputedX + 3,
                ComputedY + 3,
                ComputedWidth - 6,
                ComputedHeight - 6,
                2,
                new Color(56, 189, 248));
        }

        renderer.PopScissor();
    }

    void IPointerHandler.OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        var hit = HitTest(e.Position);
        SelectEntity(hit);

        if (hit is not null)
        {
            _draggedEntity = hit;
            var scenePoint = ToScene(e.Position);
            _entityDragOffset = new Vector2(
                scenePoint.X - hit.X,
                scenePoint.Y - hit.Y);

            _entities.Remove(hit);
            _entities.Add(hit);
        }
        else
        {
            _isPanning = true;
            _lastPanPosition = e.Position;
        }

        MarkNeedsPaint();
        e.Handled = true;
    }

    void IPointerHandler.OnPointerMoved(PointerEventArgs e)
    {
        if (_draggedEntity is not null)
        {
            var scenePoint = ToScene(e.Position);
            _draggedEntity.X = scenePoint.X - _entityDragOffset.X;
            _draggedEntity.Y = scenePoint.Y - _entityDragOffset.Y;
            MarkNeedsPaint();
            return;
        }

        if (!_isPanning)
        {
            return;
        }

        var delta = e.Position - _lastPanPosition;
        _lastPanPosition = e.Position;
        _viewportOffset -= delta;
        MarkNeedsPaint();
    }

    void IPointerHandler.OnPointerReleased(PointerEventArgs e)
    {
        _draggedEntity = null;
        _isPanning = false;
        MarkNeedsPaint();
    }

    public bool CanAcceptDataType(string dataType)
        => dataType == EntityPaletteItem.DragDataType;

    public bool OnDragEnter(DragData dragData)
    {
        IsDropTargetActive = true;
        _dropPreviewKind = dragData.Data as SceneEntityKind?;
        MarkNeedsPaint();
        return dragData.Data is SceneEntityKind;
    }

    public void OnDragOver(DragData dragData, float mouseX, float mouseY)
    {
        if (dragData.Data is not SceneEntityKind kind)
        {
            return;
        }

        _dropPreviewKind = kind;
        _dropPreviewPoint = ToScene(new Vector2(mouseX, mouseY));
        MarkNeedsPaint();
    }

    public void OnDragLeave(DragData dragData)
    {
        IsDropTargetActive = false;
        ClearDropPreview();
        MarkNeedsPaint();
    }

    public bool OnDrop(DragData dragData, float mouseX, float mouseY)
    {
        IsDropTargetActive = false;

        if (dragData.Data is not SceneEntityKind kind)
        {
            ClearDropPreview();
            MarkNeedsPaint();
            return false;
        }

        var point = ToScene(new Vector2(mouseX, mouseY));
        var entity = new SceneEntity
        {
            Kind = kind,
            Name = $"{kind} {_nextEntityNumber++}",
            X = point.X - 56,
            Y = point.Y - 40
        };

        _entities.Add(entity);
        SelectEntity(entity);
        ClearDropPreview();
        MarkNeedsPaint();
        return true;
    }

    private SceneEntity? HitTest(Vector2 point)
    {
        var scenePoint = ToScene(point);

        for (var index = _entities.Count - 1; index >= 0; index--)
        {
            var entity = _entities[index];
            if (scenePoint.X >= entity.X &&
                scenePoint.X <= entity.X + entity.Width &&
                scenePoint.Y >= entity.Y &&
                scenePoint.Y <= entity.Y + entity.Height)
            {
                return entity;
            }
        }

        return null;
    }

    private void SelectEntity(SceneEntity? entity)
    {
        if (ReferenceEquals(_selectedEntity, entity))
        {
            return;
        }

        _selectedEntity = entity;
        SelectionChanged?.Invoke(entity);
    }

    private Vector2 ToScene(Vector2 screenPoint)
        => new(
            screenPoint.X - ComputedX + _viewportOffset.X,
            screenPoint.Y - ComputedY + _viewportOffset.Y);

    private Vector2 ToScreen(Vector2 scenePoint)
        => new(
            ComputedX + scenePoint.X - _viewportOffset.X,
            ComputedY + scenePoint.Y - _viewportOffset.Y);

    private void DrawEntity(IRenderer renderer, SceneEntity entity)
    {
        DrawEntity(
            renderer,
            entity.Kind,
            entity.X,
            entity.Y,
            ghost: false,
            selected: entity == _selectedEntity);
    }

    private void DrawEntity(
        IRenderer renderer,
        SceneEntityKind kind,
        float sceneX,
        float sceneY,
        bool ghost,
        bool selected)
    {
        var origin = ToScreen(new Vector2(sceneX, sceneY));
        var fill = SceneColors.Fill(kind);

        if (ghost)
        {
            fill = new Color(fill.R, fill.G, fill.B, 130);
        }

        EntityPaletteItem.DrawShape(
            renderer,
            kind,
            origin.X,
            origin.Y,
            112,
            80,
            fill,
            selected
                ? new Color(250, 204, 21)
                : new Color(186, 230, 253, ghost ? 180 : 255));
    }

    private void RenderInfiniteGrid(IRenderer renderer)
    {
        const float spacing = 32;
        const int majorEvery = 4;

        var firstSceneX = MathF.Floor(_viewportOffset.X / spacing) * spacing;
        var firstSceneY = MathF.Floor(_viewportOffset.Y / spacing) * spacing;

        for (var sceneX = firstSceneX;
             sceneX <= _viewportOffset.X + ComputedWidth + spacing;
             sceneX += spacing)
        {
            var screenX = ToScreen(new Vector2(sceneX, 0)).X;
            var index = (int)MathF.Round(sceneX / spacing);
            var color = index % majorEvery == 0
                ? new Color(51, 65, 85, 180)
                : new Color(30, 41, 59, 150);
            renderer.DrawLine(
                screenX,
                ComputedY,
                screenX,
                ComputedY + ComputedHeight,
                1,
                color);
        }

        for (var sceneY = firstSceneY;
             sceneY <= _viewportOffset.Y + ComputedHeight + spacing;
             sceneY += spacing)
        {
            var screenY = ToScreen(new Vector2(0, sceneY)).Y;
            var index = (int)MathF.Round(sceneY / spacing);
            var color = index % majorEvery == 0
                ? new Color(51, 65, 85, 180)
                : new Color(30, 41, 59, 150);
            renderer.DrawLine(
                ComputedX,
                screenY,
                ComputedX + ComputedWidth,
                screenY,
                1,
                color);
        }
    }

    private void ClearDropPreview()
    {
        _dropPreviewKind = null;
        _dropPreviewPoint = default;
    }
}
