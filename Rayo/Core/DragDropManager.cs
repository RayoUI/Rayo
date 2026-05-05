using Rayo.Core.Interfaces;
using System.Linq;

namespace Rayo.Core;

/// <summary>
/// Gestor central del sistema de drag & drop universal.
/// Coordina las operaciones de arrastre y suelta entre elementos draggables y drop targets.
/// </summary>
public class DragDropManager
{
    private readonly UITree _tree;
    private IDraggable? _currentDraggable;
    private IDropTarget? _currentDropTarget;
    private DragData? _currentDragData;
    private bool _isDragging = false;
    private float _dragStartX;
    private float _dragStartY;
    private float _lastMouseX;
    private float _lastMouseY;

    // Offset del mouse relativo al elemento cuando se inició el drag
    private float _mouseOffsetX;

    private float _mouseOffsetY;

    /// <summary>
    /// Indica si actualmente hay una operaci�n de drag en progreso.
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// Los datos del drag actual (null si no hay drag en progreso).
    /// </summary>
    public DragData? CurrentDragData => _currentDragData;

    /// <summary>
    /// El elemento draggable actual (null si no hay drag en progreso).
    /// </summary>
    public IDraggable? CurrentDraggable => _currentDraggable;

    /// <summary>
    /// El drop target actualmente bajo el cursor (null si no hay ninguno).
    /// </summary>
    public IDropTarget? CurrentDropTarget => _currentDropTarget;

    public DragDropManager(UITree tree)
    {
        _tree = tree;
    }

    /// <summary>
    /// Intenta iniciar una operaci�n de drag desde el elemento en las coordenadas dadas.
    /// </summary>
    public bool TryStartDrag(float mouseX, float mouseY)
    {
        if (_isDragging) return false;

        // Buscar el elemento draggable m�s profundo en la posici�n del mouse
        var element = FindDraggableAt(mouseX, mouseY, _tree.Root);
        if (element == null) return false;

        _dragStartX = mouseX;
        _dragStartY = mouseY;
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
        _currentDraggable = element;

        // No iniciamos el drag inmediatamente, esperamos el threshold
        return true;
    }

    /// <summary>
    /// Procesa el movimiento del mouse durante un posible drag.
    /// </summary>
    public void ProcessMouseMove(float mouseX, float mouseY, bool isCtrlPressed = false, bool isShiftPressed = false, bool isAltPressed = false)
    {
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;

        // Si tenemos un candidato a draggable pero aún no hemos iniciado el drag
        if (_currentDraggable != null && !_isDragging)
        {
            float dx = mouseX - _dragStartX;
            float dy = mouseY - _dragStartY;
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            // Verificar si hemos superado el threshold
            if (distance >= _currentDraggable.DragThreshold)
            {
                // Iniciar el drag
                _currentDragData = _currentDraggable.OnDragStart(_dragStartX, _dragStartY);

                if (_currentDragData != null)
                {
                    _currentDragData.StartX = _dragStartX;
                    _currentDragData.StartY = _dragStartY;
                    _isDragging = true;
                    _currentDraggable.IsDragging = true;

                    // Calcular el offset del mouse relativo al elemento
                    if (_currentDragData.SourceElement != null)
                    {
                        _mouseOffsetX = _dragStartX - _currentDragData.SourceElement.ComputedX;
                        _mouseOffsetY = _dragStartY - _currentDragData.SourceElement.ComputedY;
                    }

                    // Establecer efecto inicial basado en teclas modificadoras
                    _currentDragData.CurrentEffect = _currentDragData.AllowedEffects.GetEffectFromModifiers(
                        isCtrlPressed, isShiftPressed, isAltPressed);

                    // Marcar para re-renderizar
                    _tree.MarkNeedsRender();
                }
                else
                {
                    // El draggable canceló el drag
                    _currentDraggable = null;
                }
            }
        }

        // Si estamos en medio de un drag
        if (_isDragging && _currentDraggable != null && _currentDragData != null)
        {
            // Actualizar efecto basado en teclas modificadoras
            _currentDragData.CurrentEffect = _currentDragData.AllowedEffects.GetEffectFromModifiers(
                isCtrlPressed, isShiftPressed, isAltPressed);

            // Notificar al draggable
            _currentDraggable.OnDragging(mouseX, mouseY);

            // Buscar drop target bajo el cursor
            var dropTarget = FindDropTargetAt(mouseX, mouseY, _tree.Root);

            // Verificar si cambi� el drop target
            if (dropTarget != _currentDropTarget)
            {
                // Salir del drop target anterior
                if (_currentDropTarget != null)
                {
                    _currentDropTarget.OnDragLeave(_currentDragData);
                    _currentDropTarget.IsDropTargetActive = false;
                }

                _currentDropTarget = dropTarget;

                // Entrar al nuevo drop target
                if (_currentDropTarget != null)
                {
                    bool canAccept = ValidateDropTarget(_currentDropTarget, _currentDragData);
                    if (canAccept)
                    {
                        canAccept = _currentDropTarget.OnDragEnter(_currentDragData);
                    }

                    _currentDropTarget.IsDropTargetActive = canAccept;

                    if (!canAccept)
                    {
                        // El drop target rechaz� el drag
                        _currentDropTarget = null;
                    }
                }
            }

            // Notificar al drop target actual sobre el movimiento
            if (_currentDropTarget != null)
            {
                _currentDropTarget.OnDragOver(_currentDragData, mouseX, mouseY);
            }

            _tree.MarkNeedsRender();
        }
    }

    /// <summary>
    /// Finaliza la operaci�n de drag actual.
    /// </summary>
    public void EndDrag()
    {
        if (_currentDraggable == null) return;

        bool wasDropped = false;

        // Si est�bamos dragging y hay un drop target v�lido
        if (_isDragging && _currentDropTarget != null && _currentDragData != null)
        {
            // Intentar hacer el drop
            wasDropped = _currentDropTarget.OnDrop(_currentDragData, _lastMouseX, _lastMouseY);
            _currentDropTarget.IsDropTargetActive = false;
        }
        else if (_currentDropTarget != null && _currentDragData != null)
        {
            // Limpiar el drop target si no completamos el drag
            _currentDropTarget.OnDragLeave(_currentDragData);
            _currentDropTarget.IsDropTargetActive = false;
        }

        // Notificar al draggable que termin�
        if (_isDragging)
        {
            _currentDraggable.IsDragging = false;
            _currentDraggable.OnDragEnd(wasDropped);
        }

        // Limpiar estado
        _currentDraggable = null;
        _currentDropTarget = null;
        _currentDragData = null;
        _isDragging = false;

        _tree.MarkNeedsRender();
    }

    /// <summary>
    /// Cancela la operaci�n de drag actual sin hacer drop.
    /// </summary>
    public void CancelDrag()
    {
        if (_currentDraggable == null) return;

        if (_currentDropTarget != null && _currentDragData != null)
        {
            _currentDropTarget.OnDragLeave(_currentDragData);
            _currentDropTarget.IsDropTargetActive = false;
        }

        if (_isDragging)
        {
            _currentDraggable.IsDragging = false;
            _currentDraggable.OnDragEnd(false);
        }

        _currentDraggable = null;
        _currentDropTarget = null;
        _currentDragData = null;
        _isDragging = false;

        _tree.MarkNeedsRender();
    }

    /// <summary>
    /// Busca el elemento draggable m�s profundo en las coordenadas dadas.
    /// </summary>
    private IDraggable? FindDraggableAt(float x, float y, VisualElement? element)
    {
        if (element == null || !element.IsVisible) return null;

        // Search children in reverse ZIndex order so a higher-ZIndex interactive
        // non-draggable element (e.g. a scroll button overlay) blocks the search
        // from reaching draggables hidden visually behind it.
        var children = element.GetChildrenByZIndex().ToArray();
        for (int i = children.Length - 1; i >= 0; i--)
        {
            var child = children[i];
            var found = FindDraggableAt(x, y, child);
            if (found != null) return found;

            // If an interactive non-draggable child is in bounds, it blocks any
            // draggables behind it — stop here so we don't start a drag on them.
            if (IsPointInside(x, y, child) &&
                child is Core.Interfaces.IInputHandler { CanHandleInput: true } &&
                child is not IDraggable)
            {
                return null;
            }
        }

        // Check this element itself
        if (element is IDraggable draggable && IsPointInside(x, y, element))
        {
            return draggable;
        }

        return null;
    }

    /// <summary>
    /// Busca el drop target más profundo en las coordenadas dadas.
    /// Excluye el elemento que se está arrastrando.
    /// </summary>
    private IDropTarget? FindDropTargetAt(float x, float y, VisualElement? element)
    {
        if (element == null || !element.IsVisible) return null;

        // No considerar el elemento que se está arrastrando como drop target
        if (_currentDragData?.SourceElement == element) return null;

        // Buscar en hijos primero (más profundos tienen prioridad)
        for (int i = element.GetChildren().Count() - 1; i >= 0; i--)
        {
            var found = FindDropTargetAt(x, y, element.GetChildren().ElementAt(i));
            if (found != null) return found;
        }

        // Verificar este elemento
        if (element is IDropTarget dropTarget &&
            IsPointInside(x, y, element) &&
            _currentDragData != null &&
            dropTarget.CanAcceptDataType(_currentDragData.DataType))
        {
            return dropTarget;
        }

        return null;
    }

    /// <summary>
    /// Valida si un drop target puede aceptar el drag actual según sus restricciones.
    /// </summary>
    private bool ValidateDropTarget(IDropTarget dropTarget, DragData dragData)
    {
        // Validar efectos permitidos por el drop target
        if (dropTarget.AllowedEffects.HasValue)
        {
            if (!dropTarget.AllowedEffects.Value.HasEffect(dragData.CurrentEffect))
            {
                return false;
            }
        }

        // Validar restricciones avanzadas si existen
        if (dropTarget.Constraints != null && dropTarget is VisualElement targetElement)
        {
            // Contar elementos actuales si es necesario para MaxItems
            int currentItemCount = targetElement.GetChildren().Count();

            if (!dropTarget.Constraints.Validate(dragData, targetElement, currentItemCount))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Verifica si un punto está dentro de los límites de un elemento.
    /// </summary>
    private bool IsPointInside(float x, float y, VisualElement element)
    {
        return x >= element.ComputedX &&
               x <= element.ComputedX + element.ComputedWidth &&
               y >= element.ComputedY &&
               y <= element.ComputedY + element.ComputedHeight;
    }

    /// <summary>
    /// Renders the "ghost" of the element being dragged.
    /// Must be called at the end of the render loop so it appears above everything.
    /// </summary>
    public void RenderDragGhost(Rendering.IRenderer renderer)
    {
        if (!_isDragging || _currentDragData?.SourceElement == null) return;
        if (_currentDraggable == null || !_currentDraggable.ShouldRenderWhileDragging) return;

        var source = _currentDragData.SourceElement;

        float ghostX = _lastMouseX - _mouseOffsetX;
        float ghostY = _lastMouseY - _mouseOffsetY;
        float dx = ghostX - source.ComputedX;
        float dy = ghostY - source.ComputedY;

        // Shift the entire subtree (root + all descendants) to the ghost position
        // so that content elements render at the correct offset inside the ghost.
        OffsetSubtreePositions(source, dx, dy);
        try
        {
            RenderElementWithChildren(source, renderer);
        }
        finally
        {
            // Always restore positions even if rendering throws, so the live layout
            // is never left in a corrupted state.
            OffsetSubtreePositions(source, -dx, -dy);
        }
    }

    /// <summary>
    /// Recursively offsets the ComputedX/Y of an element and all its descendants.
    /// </summary>
    private static void OffsetSubtreePositions(VisualElement element, float dx, float dy)
    {
        element.ComputedX += dx;
        element.ComputedY += dy;
        foreach (var child in element.GetChildren().ToArray())
            OffsetSubtreePositions(child, dx, dy);
    }

    /// <summary>
    /// Renders an element and all its children recursively, matching the full
    /// render pass: Render → children → OnAfterRender.
    /// </summary>
    private void RenderElementWithChildren(VisualElement element, Rendering.IRenderer renderer)
    {
        if (!element.IsVisible) return;

        element.Render(renderer);

        if (!element.RendersChildrenManually)
        {
            foreach (var child in element.GetChildren().ToArray())
            {
                RenderElementWithChildren(child, renderer);
            }
        }

        // Mirror the full UITree render pass so that OnAfterRender decorations
        // (accent bars, outlines, etc.) appear correctly in the ghost.
        element.InvokeOnAfterRender(renderer);
    }
}
