namespace Rayo.Core.Input;

using Silk.NET.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using System.Numerics;

/// <summary>
/// Modern unified pointer event manager that handles mouse, touch, and pen input.
/// Replaces the old mouse-only EventManager with a cross-platform approach.
/// 
/// Architecture:
/// - Receives platform-specific input (Silk.NET mouse/touch events)
/// - Converts to unified PointerEventArgs
/// - Routes to IPointerHandler elements via hit-testing
/// - Processes gesture recognizers (tap, long-press, swipe, pan, pinch)
/// 
/// Similar to:
/// - Avalonia's Pointer system
/// - MAUI's GestureRecognizer framework
/// - Web's Pointer Events API
/// </summary>
public class PointerEventManager
{
    private readonly UITree _tree;
    private UIApplication? _application;
    
    // Input sources
    private IMouse? _mouse;
    // Touch devices would be declared here when Silk.NET touch API is available
    // private var _touchDevices;

    // Active pointer tracking (important for multi-touch)
    private readonly Dictionary<int, PointerState> _activePointers = new();
    
    // Hover tracking (only for mouse, not touch)
    private readonly HashSet<VisualElement> _hoveredElements = new();
    
    // Hit-testing
    private readonly HitTestEngine _hitTestEngine;

    // Focus system
    private VisualElement? _focusedElement;

    public PointerEventManager(UITree tree)
    {
        _tree = tree;
        _hitTestEngine = new HitTestEngine(tree);
    }

    /// <summary>
    /// Sets the application reference for overlay access.
    /// </summary>
    public void SetApplication(UIApplication application)
    {
        _application = application;
    }

    /// <summary>
    /// Access to the advanced Hit-Testing engine.
    /// </summary>
    public HitTestEngine HitTest => _hitTestEngine;

    /// <summary>
    /// Currently focused element.
    /// </summary>
    public VisualElement? FocusedElement => _focusedElement;

    /// <summary>
    /// Attach Silk.NET input context.
    /// </summary>
    public void AttachInput(IInputContext input)
    {
        // Mouse input
        _mouse = input.Mice.FirstOrDefault();
        if (_mouse != null)
        {
            _mouse.MouseMove += OnMouseMove;
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
            _mouse.Scroll += OnMouseScroll;
        }

        // Touch input (for mobile/tablet)
        // NOTE: Touch API may vary by Silk.NET version and platform
        // Uncomment and adapt when touch support is needed:
        /*
        try
        {
            var touchDevices = input.Touches;
            if (touchDevices?.Count > 0)
            {
                foreach (var touch in touchDevices)
                {
                    touch.TouchDown += OnTouchDown;
                    touch.TouchMove += OnTouchMove;
                    touch.TouchUp += OnTouchUp;
                }
            }
        }
        catch (NotSupportedException)
        {
            // Touch not supported on this platform
        }
        */
    }

    // =========================================================================
    // MOUSE EVENT HANDLERS (convert to pointer events)
    // =========================================================================

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        const int MousePointerId = 0;

        var pointerArgs = PointerEventArgs.FromMouse(position, 0, mouse.IsButtonPressed(MouseButton.Left));
        
        // Update delta from last position
        if (_activePointers.TryGetValue(MousePointerId, out var state))
        {
            pointerArgs.Delta = position - state.LastPosition;
            state.LastPosition = position;
        }
        else
        {
            _activePointers[MousePointerId] = new PointerState
            {
                PointerId = MousePointerId,
                PointerType = PointerType.Mouse,
                LastPosition = position
            };
        }

        // Hit-test and route event
        RoutePointerMoved(pointerArgs);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        const int MousePointerId = 0;
        
        var position = mouse.Position;
        var pointerArgs = PointerEventArgs.FromMouse(position, (int)button, true);

        // Track pointer state
        if (!_activePointers.TryGetValue(MousePointerId, out var state))
        {
            state = new PointerState
            {
                PointerId = MousePointerId,
                PointerType = PointerType.Mouse,
                LastPosition = position
            };
            _activePointers[MousePointerId] = state;
        }

        state.IsPressed = true;
        state.PressStartTime = DateTime.UtcNow;
        state.PressStartPosition = position;

        // Hit-test and route event
        var hitResult = _hitTestEngine.HitTest(position);
        if (hitResult?.Element != null)
        {
            var hitElement = hitResult.Element;
            state.CapturedElement = hitElement;
            
            // Set local position
            var localPos = hitElement.GetLocalPosition(position);
            pointerArgs.LocalPosition = localPos;

            // Route to element
            if (hitElement is IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerPressed(pointerArgs);
            }

            // Process gesture recognizers
            ProcessGestureRecognizers(hitElement, pointerArgs);

            // Set focus
            SetFocus(hitElement);

            // Mark tree as needing repaint
            _tree.Root?.MarkNeedsPaint();
        }
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        const int MousePointerId = 0;
        
        var position = mouse.Position;
        var pointerArgs = PointerEventArgs.FromMouse(position, (int)button, false);

        if (_activePointers.TryGetValue(MousePointerId, out var state))
        {
            state.IsPressed = false;

            // Route to captured element (if any)
            if (state.CapturedElement != null)
            {
                var localPos = state.CapturedElement.GetLocalPosition(position);
                pointerArgs.LocalPosition = localPos;

                if (state.CapturedElement is IPointerHandler pointerHandler)
                {
                    pointerHandler.OnPointerReleased(pointerArgs);
                }

                // Process gesture recognizers
                ProcessGestureRecognizers(state.CapturedElement, pointerArgs);

                state.CapturedElement = null;
                
                // Mark tree as needing repaint
                _tree.Root?.MarkNeedsPaint();
            }
        }
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        // Route scroll delta to the nearest scrollable element under the pointer
        var position = mouse.Position;
        
        // First, check overlays (they're on top)
        VisualElement? hitElement = null;
        
        if (_application != null && _application.Overlays.Count > 0)
        {
            // Check overlays in reverse order (last added = topmost)
            for (int i = _application.Overlays.Count - 1; i >= 0; i--)
            {
                var overlay = _application.Overlays[i];
                var overlayResult = _hitTestEngine.HitTestRoot(overlay, position);
                if (overlayResult?.Element != null)
                {
                    hitElement = overlayResult.Element;
                    break;
                }
            }
        }
        
        // If no overlay hit, check main tree
        if (hitElement == null)
        {
            var hitResult = _hitTestEngine.HitTest(position);
            hitElement = hitResult?.Element;
        }

        if (hitElement == null)
        {
            return;
        }

        const float ScrollSpeed = 60f;
        float deltaY = -scrollWheel.Y * ScrollSpeed;

        var element = hitElement;
        while (element != null)
        {
            if (element is IScrollable scrollable)
            {
                scrollable.Scroll(deltaY);
                break;
            }

            element = element.Parent;
        }
    }

    // =========================================================================
    // TOUCH EVENT HANDLERS (convert to pointer events)
    // NOTE: Touch events require platform-specific implementation
    // This is a placeholder for future Android/iOS touch support
    // =========================================================================

    // Touch handlers would go here when Silk.NET touch API is available
    // For now, mouse events work on both desktop and touchscreens (simulated)

    // =========================================================================
    // POINTER EVENT ROUTING
    // =========================================================================

    private void RoutePointerMoved(PointerEventArgs pointerArgs)
    {
        // Hit-test at current position
        var hitResult = _hitTestEngine.HitTest(pointerArgs.Position);
        var hitElement = hitResult?.Element;

        // Update hover state (only for mouse, not touch)
        if (pointerArgs.PointerType == PointerType.Mouse)
        {
            UpdateHoverState(hitElement, pointerArgs);
        }

        // Route to captured element (if dragging) or hit element
        const int MousePointerId = 0;
        if (_activePointers.TryGetValue(MousePointerId, out var state) && state.CapturedElement != null)
        {
            var localPos = state.CapturedElement.GetLocalPosition(pointerArgs.Position);
            pointerArgs.LocalPosition = localPos;

            if (state.CapturedElement is IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerMoved(pointerArgs);
            }

            // Process gesture recognizers
            ProcessGestureRecognizers(state.CapturedElement, pointerArgs);

            // Mark tree as needing repaint
            _tree.Root?.MarkNeedsPaint();
        }
        else if (hitElement != null)
        {
            var localPos = hitElement.GetLocalPosition(pointerArgs.Position);
            pointerArgs.LocalPosition = localPos;

            if (hitElement is IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerMoved(pointerArgs);
            }
        }
    }

    private void UpdateHoverState(VisualElement? currentHitElement, PointerEventArgs pointerArgs)
    {
        // Find all elements under pointer (for nested hover)
        var newHoveredElements = new HashSet<VisualElement>();
        
        var element = currentHitElement;
        while (element != null)
        {
            newHoveredElements.Add(element);
            element = element.Parent;
        }

        // Elements that are no longer hovered
        foreach (var oldElement in _hoveredElements)
        {
            if (!newHoveredElements.Contains(oldElement))
            {
                if (oldElement is IPointerHandler pointerHandler)
                {
                    pointerHandler.OnPointerExited(pointerArgs);
                }
            }
        }

        // Elements that are newly hovered
        foreach (var newElement in newHoveredElements)
        {
            if (!_hoveredElements.Contains(newElement))
            {
                if (newElement is IPointerHandler pointerHandler)
                {
                    pointerHandler.OnPointerEntered(pointerArgs);
                }
            }
        }

        _hoveredElements.Clear();
        foreach (var elem in newHoveredElements)
        {
            _hoveredElements.Add(elem);
        }
    }

    // =========================================================================
    // GESTURE RECOGNITION
    // =========================================================================

    private void ProcessGestureRecognizers(VisualElement element, PointerEventArgs pointerArgs)
    {
        // Check if element has gesture recognizers attached
        if (element is IGestureRecognizerHost host)
        {
            foreach (var recognizer in host.GestureRecognizers)
            {
                if (recognizer.ProcessPointerEvent(pointerArgs))
                {
                    // Gesture consumed the event
                    pointerArgs.Handled = true;
                    break;
                }
            }
        }
    }

    // =========================================================================
    // FOCUS MANAGEMENT
    // =========================================================================

    private void SetFocus(VisualElement? element)
    {
        if (_focusedElement == element)
            return;

        // Remove focus from old element
        if (_focusedElement is IInputHandler oldHandler)
        {
            oldHandler.OnFocusLost();
        }

        _focusedElement = element;

        // Set focus on new element
        if (_focusedElement is IInputHandler newHandler)
        {
            newHandler.OnFocusGained();
        }
    }

    /// <summary>
    /// Manually set focus to an element.
    /// </summary>
    public void Focus(VisualElement? element)
    {
        SetFocus(element);
    }

    // =========================================================================
    // INTERNAL STATE TRACKING
    // =========================================================================

    private class PointerState
    {
        public int PointerId { get; set; }
        public PointerType PointerType { get; set; }
        public Vector2 LastPosition { get; set; }
        public bool IsPressed { get; set; }
        public DateTime PressStartTime { get; set; }
        public Vector2 PressStartPosition { get; set; }
        public VisualElement? CapturedElement { get; set; }
    }
}

/// <summary>
/// Interface for elements that host gesture recognizers.
/// Elements can attach multiple gesture recognizers (tap, swipe, pan, etc.)
/// </summary>
public interface IGestureRecognizerHost
{
    /// <summary>
    /// List of gesture recognizers attached to this element.
    /// </summary>
    List<IGestureRecognizer> GestureRecognizers { get; }
}
