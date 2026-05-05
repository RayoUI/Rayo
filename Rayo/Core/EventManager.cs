namespace Rayo.Core;

using Silk.NET.Input;
using Rayo.Controls;
using Rayo.Core.Interfaces;
using System.Numerics;
using System.Linq;

/// <summary>
/// Modern and optimal event manager.
/// Supports mouse, touch, keyboard, and focus system.
/// 
/// Note: Hover tracking uses IPointerHandler pattern for modern pointer events.
/// </summary>
public class EventManager
{
    private UITree _tree;
    private IMouse? _mouse;
    private IKeyboard? _keyboard;
    private DateTime _lastMouseMoveProcess = DateTime.MinValue;
    private const int MouseMoveThrottleMs = 4; // ~250fps for mouse move - maximum responsiveness

    private VisualElement? _focusedElement;
    private VisualElement? _draggedElement;
    private Vector2 _lastMousePos = Vector2.Zero;
    private Vector2 _dragStartPos = Vector2.Zero;

    // Right-mouse-button pan state
    private IInputHandler? _rightDragTarget;
    private Vector2 _lastRightPos;

    /// <summary>
    /// Indicates if any element is currently being dragged.
    /// Useful for disabling idle mode optimizations during drag.
    /// </summary>
    public bool IsAnythingBeingDragged => _draggedElement != null;

    // TODO: Refactor hover tracking to use IPointerHandler pattern
    // Currently maintains backward compatibility with legacy hover system
    private readonly HashSet<VisualElement> _hoveredElements = new();

    // Key repeat system - Only uses IsKeyPressed() each frame
    private DateTime _lastKeyRepeatTime = DateTime.MinValue;
    private DateTime _lastKeyPressStartTime = DateTime.MinValue;
    private Key? _currentlyTrackedKey = null;
    private InputKey? _currentlyTrackedInputKey = null;
    private bool _hasStartedRepeat = false;
    private const int KeyRepeatDelayMs = 200;     // Initial delay (200ms)
    private const int KeyRepeatIntervalMs = 50;   // Repeat interval (50ms = ~20 reps/sec)

    // Universal Drag & Drop system
    private DragDropManager _dragDropManager;

    // Advanced Hit-Testing System
    private HitTestEngine _hitTestEngine;
    
    // Reference to Application for Overlays
    private readonly UIApplication? _app;

    // Global Pointer Handlers (Open/Closed Principle)
    private readonly List<IGlobalPointerHandler> _globalPointerHandlers = new();

    public EventManager(UITree tree, UIApplication? app = null)
    {
        _tree = tree;
        _app = app;
        _dragDropManager = new DragDropManager(tree);
        _hitTestEngine = new HitTestEngine(tree);

        // Connect DragDropManager with UITree to render the ghost
        _tree.SetDragDropManager(_dragDropManager);
    }

    /// <summary>
    /// Access to the Drag & Drop manager.
    /// </summary>
    public DragDropManager DragDrop => _dragDropManager;

    /// <summary>
    /// Access to the advanced Hit-Testing engine.
    /// </summary>
    public HitTestEngine HitTest => _hitTestEngine;

    /// <summary>
    /// Registers a global pointer handler.
    /// The handler will be notified of all pointer events and can check if they are outside its area.
    /// </summary>
    public void RegisterGlobalPointerHandler(IGlobalPointerHandler handler)
    {
        if (!_globalPointerHandlers.Contains(handler))
        {
            _globalPointerHandlers.Add(handler);
        }
    }

    /// <summary>
    /// Unregisters a global pointer handler.
    /// </summary>
    public void UnregisterGlobalPointerHandler(IGlobalPointerHandler handler)
    {
        _globalPointerHandlers.Remove(handler);
    }

    public void AttachInput(IInputContext input)
    {
        _mouse = input.Mice.FirstOrDefault();
        _keyboard = input.Keyboards.FirstOrDefault();

        if (_mouse != null)
        {
            _mouse.MouseMove += OnMouseMove;
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
            _mouse.Scroll += OnMouseWheel;
        }

        if (_keyboard != null)
        {
            _keyboard.KeyDown += OnKeyDown;
            _keyboard.KeyUp += OnKeyUp;
            _keyboard.KeyChar += OnKeyChar;
        }
    }

    public VisualElement? FocusedElement => _focusedElement;

    /// <summary>
    /// Processes automatic key repeat and returns true if a key is being tracked.
    /// </summary>
    /// <returns>True if a key is being tracked (to avoid idle mode)</returns>
    public bool ProcessKeyRepeat()
    {
        // Update hit-testing cache each frame
        _hitTestEngine.OnFrame();

        // Process time-based gestures (long-press, double-tap timeout)
        ProcessGestureDetectors();

        if (_keyboard == null || _focusedElement == null) return false;

        if (!_focusedElement.IsEffectivelyEnabled())
        {
            SetFocus(null);
            return false;
        }

        if (_focusedElement is not IInputHandler handler || !handler.CanHandleInput)
            return false;

        var now = DateTime.UtcNow;

        // Detect which repeat key is being pressed NOW
        Key? currentKey = null;
        if (_keyboard.IsKeyPressed(Key.Backspace)) currentKey = Key.Backspace;
        else if (_keyboard.IsKeyPressed(Key.Delete)) currentKey = Key.Delete;
        else if (_keyboard.IsKeyPressed(Key.Left)) currentKey = Key.Left;
        else if (_keyboard.IsKeyPressed(Key.Right)) currentKey = Key.Right;
        else if (_keyboard.IsKeyPressed(Key.Up)) currentKey = Key.Up;
        else if (_keyboard.IsKeyPressed(Key.Down)) currentKey = Key.Down;
        else if (_keyboard.IsKeyPressed(Key.Home)) currentKey = Key.Home;
        else if (_keyboard.IsKeyPressed(Key.End)) currentKey = Key.End;
        else if (_keyboard.IsKeyPressed(Key.PageUp)) currentKey = Key.PageUp;
        else if (_keyboard.IsKeyPressed(Key.PageDown)) currentKey = Key.PageDown;
        else if (_keyboard.IsKeyPressed(Key.Enter)) currentKey = Key.Enter;

        // If no key pressed, reset everything
        if (currentKey == null)
        {
            _currentlyTrackedKey = null;
            _currentlyTrackedInputKey = null;
            _lastKeyPressStartTime = DateTime.MinValue;
            _lastKeyRepeatTime = DateTime.MinValue;
            _hasStartedRepeat = false;
            return false;
        }

        // If it's a NEW key, reset state
        if (currentKey != _currentlyTrackedKey)
        {
            _currentlyTrackedKey = currentKey;
            _currentlyTrackedInputKey = MapKey(currentKey.Value);
            _lastKeyPressStartTime = now;
            _lastKeyRepeatTime = DateTime.MinValue;
            _hasStartedRepeat = false;
            // Console.WriteLine($"[ProcessKeyRepeat) 🔹 Key pressed: {currentKey}");
            return true;
        }

        // The SAME key is still pressed
        var timeSincePress = (now - _lastKeyPressStartTime).TotalMilliseconds;
        var timeSinceLastRepeat = (now - _lastKeyRepeatTime).TotalMilliseconds;

        // Wait for initial delay
        if (!_hasStartedRepeat && timeSincePress < KeyRepeatDelayMs)
        {
            // Still in initial wait period
            return true;
        }

        // Start repeat after delay
        if (!_hasStartedRepeat)
        {
            _hasStartedRepeat = true;
            _lastKeyRepeatTime = now;
            // Console.WriteLine($"[ProcessKeyRepeat) ✅ Starting repeat of {_currentlyTrackedKey}");
            // Will be processed in the next line
        }

        // Check if it's time to send the next repeat
        if (timeSinceLastRepeat >= KeyRepeatIntervalMs && _currentlyTrackedInputKey.HasValue)
        {
            bool isShift = _keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight);
            var args = new InputEventArgs
            {
                EventType = InputEventType.KeyRepeat,
                KeyCode = _currentlyTrackedInputKey.Value,
                IsShiftPressed = isShift,
                IsControlPressed = _keyboard.IsKeyPressed(Key.ControlLeft) || _keyboard.IsKeyPressed(Key.ControlRight),
                IsAltPressed = _keyboard.IsKeyPressed(Key.AltLeft) || _keyboard.IsKeyPressed(Key.AltRight),
                Timestamp = now
            };

            if (handler.HandleInput(args))
            {
                _lastKeyRepeatTime = now;
                _tree.MarkNeedsRender();
            }
        }

        // Always return true while a key is being pressed
        return true;
    }

    /// <summary>
    /// Sets the focused element.
    /// </summary>
    public void SetFocus(VisualElement? element)
    {
        if (element != null && !element.IsEffectivelyEnabled())
            element = null;

        if (_focusedElement == element) return;

        // Console.WriteLine($"[SetFocus) Changing focus from {_focusedElement?.GetType().Name ?? "null"} to {element?.GetType().Name ?? "null"}");

        // REFACTORED: Use IInputHandler and/or IFocusable generically
        if (_focusedElement is IInputHandler oldHandler)
        {
            oldHandler.OnFocusLost();
        }
        if (_focusedElement is IFocusable oldFocusable)
        {
            oldFocusable.IsFocused = false;
        }

        _focusedElement = element;

        if (_focusedElement is IInputHandler newHandler)
        {
            newHandler.OnFocusGained();
        }
        if (_focusedElement is IFocusable newFocusable)
        {
            newFocusable.IsFocused = true;
        }

        // The component is responsible for marking paint if necessary
        // We don't call MarkNeedsRender here to allow batching
    }

    /// <summary>
    /// Handles right mouse button down by bubbling a MouseDown(Right) event
    /// up the ancestor chain until an IInputHandler accepts it.
    /// </summary>
    private void HandleRightMouseDown(Vector2 position)
    {
        _rightDragTarget = null;
        _lastRightPos    = position;

        // Hit-test to find the element under the cursor
        var root = _tree.Root;
        if (root == null)
            return;

        var hitResult = _hitTestEngine.HitTestRoot(root, position, new HitTestOptions
        {
            Mode                    = HitTestMode.FirstMatch,
            IncludeInvisible        = false,
            RespectInputTransparency = true,
        });

        var args = new InputEventArgs
        {
            Position  = position,
            EventType = InputEventType.MouseDown,
            Button    = InputMouseButton.Right,
            Timestamp = DateTime.UtcNow
        };

        // Bubble up the parent chain looking for the first IInputHandler that accepts it
        VisualElement? el = hitResult?.Element;
        while (el != null)
        {
            if (el.IsEffectivelyEnabled() && el is IInputHandler handler && handler.CanHandleInput)
            {
                bool handled = handler.HandleInput(args);
                if (handled || args.Handled)
                {
                    _rightDragTarget = handler;
                    _needsRenderThisFrame = true;
                    return;
                }
            }
            el = el.Parent;
        }
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (_tree.Root == null) return;

        var now = DateTime.UtcNow;

        // Process universal drag & drop if active
        if (_dragDropManager.IsDragging || _dragDropManager.CurrentDraggable != null)
        {
            _dragDropManager.ProcessMouseMove(position.X, position.Y);
            _lastMousePos = position;
            return;
        }

        // If dragging, use more aggressive throttling (no throttling = 0ms)
        int effectiveThrottle = _draggedElement != null ? 0 : MouseMoveThrottleMs;

        if ((now - _lastMouseMoveProcess).TotalMilliseconds < effectiveThrottle)
        {
            _lastMousePos = position;
            return;
        }
        _lastMouseMoveProcess = now;

        // Right-button drag — dispatch to the captured handler
        if (_rightDragTarget != null && mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Right))
        {
            _rightDragTarget.HandleInput(new InputEventArgs
            {
                Position  = position,
                EventType = InputEventType.MouseDrag,
                Button    = InputMouseButton.Right,
                Timestamp = now
            });
            _lastMousePos = position;
            _tree.MarkNeedsRender();
        }

        // If dragging, only update the dragged element
        if (_draggedElement != null)
        {

            // CRITICAL: Force continuous rendering during drag
            _tree.MarkNeedsRender();

            // ✨ NEW: Notify gesture recognizers during drag move
            // This allows TapRecognizer to detect movement and cancel tap BEFORE they release the finger
            if (_draggedElement is Rayo.Core.Input.IGestureRecognizerHost gestureHost)
            {
                var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(position, 0, true);
                pointerArgs.LocalPosition = _draggedElement.GetLocalPosition(position);
                foreach (var recognizer in gestureHost.GestureRecognizers)
                {
                    recognizer.ProcessPointerEvent(pointerArgs);
                }
            }

            // MODIFIED: Only search for IDragScrollable ancestor if current element is NOT an interactive control
            // Interactive controls (Slider, TextBox, ScrollView, etc.) have PRIORITY over ancestor scroll
            bool isInteractiveControl = _draggedElement is Slider || _draggedElement is ScrollView ||
                (_draggedElement is IInputHandler handler && handler.CanHandleInput && _draggedElement is not IDragScrollable);

            if (!isInteractiveControl)
            {
                // REFACTORED: Find IDragScrollable ancestor
                var dragScrollable = FindDragScrollableAncestor(_draggedElement);
                if (dragScrollable is VisualElement scrollElement && scrollElement is IInputHandler scrollHandler && scrollHandler.CanHandleInput)
                {
                    var args = new InputEventArgs
                    {
                        Position = position,
                        EventType = InputEventType.MouseDrag,
                        Timestamp = now
                    };

                    // If scrollable captures drag, cancel current element
                    if (scrollHandler.HandleInput(args))
                    {
                        // Clear state using interfaces
                        if (_draggedElement is Slider slider)
                        {
                            slider.IsDragging = false;
                            slider.MarkNeedsPaint();
                        }

                        // NEW: Reset gestures on the previous element to avoid "ghost clicks" after scrolling
                        if (_draggedElement is Rayo.Core.Input.IGestureRecognizerHost oldGestureHost)
                        {
                            foreach (var recognizer in oldGestureHost.GestureRecognizers)
                            {
                                recognizer.Reset();
                            }
                        }

                        // Transfer drag to scrollable
                        _draggedElement = scrollElement;
                        _tree.MarkNeedsRender();
                        _lastMousePos = position;
                        return;
                    }
                }
            }

            // REFACTORED: Update via IInputHandler generically
            if (_draggedElement.IsEffectivelyEnabled() && _draggedElement is IInputHandler dragHandler)
            {
                var args = new InputEventArgs
                {
                    Position = position,
                    EventType = InputEventType.MouseDrag,
                    Timestamp = now
                };
                dragHandler.HandleInput(args);
                _draggedElement.MarkNeedsPaint();
            }

            _tree.MarkNeedsRender();
        }
        else
        {

            // Reset batching flag
            _needsRenderThisFrame = false;

            // Process hover with optimization
            // Check overlays first if available
            bool hitInOverlay = false;
            if (_app != null && _app.Overlays.Count > 0)
            {
                // Iterate overlays in reverse order (top-most first)
                for (int i = _app.Overlays.Count - 1; i >= 0; i--)
                {
                    var overlay = _app.Overlays[i];
                    bool inBounds = overlay.ContainsWindowPoint(position);
                    if (inBounds)
                    {
                        ProcessMouseMoveOptimized(overlay, position);
                        hitInOverlay = true;
                        break; // Stop processing overlays once we find a hit
                    }
                }
            }

            // Only process main tree if no overlay was hit
            if (!hitInOverlay)
            {
                ProcessMouseMoveOptimized(_tree.Root, position);
            }
            else
            {
                // Clear hover states on main-tree elements so they don't stay highlighted
                ClearHoverStates();
            }

            // Notify ONCE if there were changes in this frame
            if (_needsRenderThisFrame)
            {
                _tree.MarkNeedsRender();
            }
        }

        _lastMousePos = position;
    }

    private bool _needsRenderThisFrame = false;

    /// <summary>
    /// Optimized version of ProcessMouseMove using advanced hit-testing.
    /// OPTIMIZATION: Only updates elements that actually changed state.
    /// Returns true if any element with pointer handling was found at the position.
    /// </summary>
    private bool ProcessMouseMoveOptimized(VisualElement element, Vector2 position)
    {
        // OPTIMIZED: Use hit-testing to find all elements with pointer handlers
        var options = new HitTestOptions
        {
            Mode = HitTestMode.AllMatches,
            IncludeInvisible = false,
            RespectInputTransparency = true,
            CheckClipping = true,
            ElementFilter = e => e is Rayo.Core.Input.IPointerHandler
        };

        // Use HitTestRoot if element is not tree root (e.g. overlay)
        var result = _hitTestEngine.HitTestRoot(element, position, options);

        // OPTIMIZATION: Create set of new hovered elements
        var newHoveredElements = new HashSet<VisualElement>();
        
        if (result?.Metadata.ContainsKey("AllMatches") == true)
        {
            var matches = (List<HitTestResult>)result.Metadata["AllMatches"];
            foreach (var match in matches)
            {
                if (match.Element is Rayo.Core.Input.IPointerHandler && match.Element is VisualElement elem)
                {
                    newHoveredElements.Add(elem);
                }
            }
        }

        // OPTIMIZATION: Only update elements that CHANGED state
        // 1. Deactivate hover for elements NO LONGER hovered
        foreach (var oldHovered in _hoveredElements)
        {
            if (!newHoveredElements.Contains(oldHovered))
            {
                // Call modern pointer exit event
                if (oldHovered is Rayo.Core.Input.IPointerHandler pointerHandler)
                {
                    var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(
                        position, 0, false
                    );
                    pointerArgs.LocalPosition = oldHovered.GetLocalPosition(position);

                    pointerHandler.OnPointerExited(pointerArgs);
                }

                oldHovered.IsHovered = false;
                oldHovered.MarkNeedsPaint();
                _needsRenderThisFrame = true;
            }
        }

        // 2. Activate hover for NEW hovered elements
        foreach (var newHovered in newHoveredElements)
        {
            if (!_hoveredElements.Contains(newHovered))
            {
                // Call modern pointer enter event
                if (newHovered is Rayo.Core.Input.IPointerHandler pointerHandler)
                {
                    var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(
                        position, 0, false
                    );
                    pointerArgs.LocalPosition = newHovered.GetLocalPosition(position);

                    pointerHandler.OnPointerEntered(pointerArgs);
                }

                newHovered.IsHovered = true;
                newHovered.MarkNeedsPaint();
                _needsRenderThisFrame = true;
            }
            else
            {
                // ✨ NEW: Call modern pointer MOVE event for elements STAYING hovered
                // This is critical for controls that need to know position without clicking (like Splitter for hover markers)
                if (newHovered is Rayo.Core.Input.IPointerHandler pointerHandler)
                {
                    var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(
                        position, 0, false
                    );
                    pointerArgs.LocalPosition = newHovered.GetLocalPosition(position);
                        
                    pointerHandler.OnPointerMoved(pointerArgs);
                }
            }
        }


        // Update hovered elements set
        _hoveredElements.Clear();
        foreach (var elem in newHoveredElements)
        {
            _hoveredElements.Add(elem);
        }

        // Return true if any hoverable element was found
        return newHoveredElements.Count > 0;
    }

    /// <summary>
    /// Clears hover state from all currently hovered elements.
    /// Called when an overlay blocks input to the main tree.
    /// </summary>
    private void ClearHoverStates()
    {
        foreach (var hoverable in _hoveredElements)
        {
            if (hoverable is Rayo.Core.Input.IPointerHandler pointerHandler)
            {
                var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(
                    new Vector2(0, 0), 0, false
                );
                pointerHandler.OnPointerExited(pointerArgs);
            }

            hoverable.IsHovered = false;
            hoverable.MarkNeedsPaint();
            _needsRenderThisFrame = true;
        }
        _hoveredElements.Clear();
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (_tree.Root == null) return;

        if (button == Silk.NET.Input.MouseButton.Right)
        {
            HandleRightMouseDown(mouse.Position);
            return;
        }

        if (button != Silk.NET.Input.MouseButton.Left) return;

        var position = mouse.Position;
        _needsRenderThisFrame = false;

        // Try to start universal drag & drop (will wait for threshold)
        _dragDropManager.TryStartDrag(position.X, position.Y);

        // REFACTORED: DO NOT attempt drag immediately
        // Only save position for threshold check in OnMouseMove
        // Drag will activate if user moves mouse > threshold

        // Clear all IDragScrollable
        CleanupAllDragScrollables(_tree.Root);

        // OPTIMIZED: Use advanced hit-testing to find appropriate element
        // Check overlays first
        HitTestResult? hitResult = null;

        bool mouseDownInOverlay = false;
        if (_app != null && _app.Overlays.Count > 0)
        {
            // Iterate overlays in reverse order (top-most first)
            for (int i = _app.Overlays.Count - 1; i >= 0; i--)
            {
                var overlay = _app.Overlays[i];
                hitResult = _hitTestEngine.HitTestRoot(overlay, position, new HitTestOptions
                {
                    Mode = HitTestMode.InteractiveOnly,
                    IncludeInvisible = false,
                    RespectInputTransparency = true,
                    CheckClipping = true
                });

                if (hitResult?.Element != null)
                {
                    mouseDownInOverlay = true;
                    break; // Found hit in overlay
                }

                // Even if no interactive element found, block if pointer is within overlay bounds
                bool inBounds = overlay.ContainsWindowPoint(position);
                if (inBounds)
                {
                    mouseDownInOverlay = true;
                    break;
                }
            }
        }

        // If no hit in overlays, check main tree
        if (!mouseDownInOverlay && hitResult?.Element == null)
        {
            hitResult = _hitTestEngine.HitTest(position, new HitTestOptions
            {
                Mode = HitTestMode.InteractiveOnly,
                IncludeInvisible = false,
                RespectInputTransparency = true,
                CheckClipping = true
            });
        }

        ComboBox.HandleGlobalPointer(position, hitResult?.Element);
        DatePicker.HandleGlobalPointer(position, hitResult?.Element);

        // Notify all registered global pointer handlers (Open/Closed Principle)
        foreach (var handler in _globalPointerHandlers.ToList())  // ToList to avoid modification during iteration
        {
            if (handler.HandleGlobalPointer(position, hitResult?.Element))
            {
                break;  // Handler consumed the event
            }
        }

        if (hitResult?.Element != null)
        {
            // Process the mouse down event to set IsPressed and handle focus
            ProcessMouseDownForElement(hitResult.Element, position);

            // Note: _draggedElement is already set in ProcessMouseDownForElement
            (hitResult.Element as IDragScrollable)?.StartDragPending();

            // Also notify IDragScrollable ancestors so they can prepare for potential scroll
            // This is critical for ListView/ScrollView to work when clicking on child items
            var ancestorScrollable = FindDragScrollableAncestor(hitResult.Element);
            if (ancestorScrollable != null)
            {
                ancestorScrollable.StartDragPending();

                // Also send MouseDown to the scrollable so it can initialize its drag state
                if (ancestorScrollable is VisualElement scrollElement &&
                    ancestorScrollable is IInputHandler scrollHandler &&
                    scrollHandler.CanHandleInput)
                {
                    var args = new InputEventArgs
                    {
                        Position = position,
                        EventType = InputEventType.MouseDown,
                        Timestamp = DateTime.UtcNow
                    };
                    scrollHandler.HandleInput(args);
                }
            }
        }
        else
        {
            // Clicked on empty area - clear focus
            SetFocus(null);
        }

        // If an interactive element captured the mouse, cancel universal drag
        if (_draggedElement != null)
        {
            _dragDropManager.CancelDrag();
        }
    }

    /// <summary>
    /// Processes MouseDown for a specific element found by hit-testing.
    /// </summary>
    private void ProcessMouseDownForElement(VisualElement element, Vector2 position)
    {
        element.IsPressed = true;

        // Notify IDragScrollable if inside
        if (element is IDragScrollable dragScrollable && element is IInputHandler scrollHandler && scrollHandler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseDown,
                Timestamp = DateTime.UtcNow
            };
            scrollHandler.HandleInput(args);
        }

        // NEW: Check if there is an interactive control (Slider, Button, etc.) that wants the drag
        // These controls have PRIORITY over IDragScrollable
        bool interactiveElementCapturedDrag = false;

        // ✨ NEW: Call modern pointer event handlers (BUBBLING support)
        // This ensures ancestors like GestureDetector receive events even if children are clicked
        var current = element;
        while (current != null)
        {
            if (current is Rayo.Core.Input.IPointerHandler pointerHandler)
            {
                var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(
                    position, 
                    0, // Left button
                    true  // isPressed
                );
                pointerArgs.LocalPosition = current.GetLocalPosition(position);
                pointerHandler.OnPointerPressed(pointerArgs);
                _needsRenderThisFrame = true;
            }

            if (current is Rayo.Core.Input.IGestureRecognizerHost gestureHost)
            {
                var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(position, 0, true);
                pointerArgs.LocalPosition = current.GetLocalPosition(position);
                foreach (var recognizer in gestureHost.GestureRecognizers)
                {
                    recognizer.ProcessPointerEvent(pointerArgs);
                }
                _needsRenderThisFrame = true;
            }

            // Special handling for legacy GestureDetector component (bubbling)
            if (current is Rayo.Gestures.Components.GestureDetector detector)
            {
                var args = new InputEventArgs
                {
                    Position = position,
                    EventType = InputEventType.MouseDown,
                    Timestamp = DateTime.UtcNow
                };
                detector.HandleInput(args);
                
                // If nothing has captured the drag yet, GestureDetector can be a candidate
                if (_draggedElement == null)
                {
                    _draggedElement = current;
                }
            }

            current = current.Parent;
        }

        // CRITICAL: Process elements with focus (IFocusable) FIRST before generic IInputHandler
        // This ensures TextBox and other focusable elements receive focus when clicked
        if (element is IFocusable focusable && element is IInputHandler focusHandler && focusHandler.CanHandleInput)
        {
            SetFocus(element);

            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseDown,
                Timestamp = DateTime.UtcNow,
                IsShiftPressed = false,
                IsControlPressed = false,
                IsAltPressed = false
            };

            if (focusHandler.HandleInput(args))
            {
                _draggedElement = element;
                interactiveElementCapturedDrag = true;
                element.MarkNeedsPaint();
                _needsRenderThisFrame = true;
                return;
            }

            element.MarkNeedsPaint();
            _needsRenderThisFrame = true;
            return;
        }

        // Generic components implementing IInputHandler (Slider, ScrollView, etc.)
        // but NOT IFocusable - clear focus from any previously focused element
        if (element is IInputHandler handler && handler.CanHandleInput)
        {
            // Clear focus when clicking on non-focusable interactive elements
            SetFocus(null);

            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseDown,
                Timestamp = DateTime.UtcNow,
                IsShiftPressed = false,
                IsControlPressed = false,
                IsAltPressed = false
            };
            if (handler.HandleInput(args))
            {
                // FIX: Capture ANY IInputHandler returning true, including ScrollView
                _draggedElement = element;
                interactiveElementCapturedDrag = true;
                element.MarkNeedsPaint();
                _needsRenderThisFrame = true;
                return; // IMPORTANT: Exit here to avoid further processing
            }
        }

        // MODIFIED: Only allow IDragScrollable to capture if no interactive control did
        if (!interactiveElementCapturedDrag && element is IDragScrollable dragScrollable2 && dragScrollable2.IsDragPending)
        {
            _draggedElement = element;
        }
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (_tree.Root == null) return;

        if (button == Silk.NET.Input.MouseButton.Right)
        {
            if (_rightDragTarget != null)
            {
                if (_rightDragTarget is not VisualElement rightDragElement || rightDragElement.IsEffectivelyEnabled())
                {
                    _rightDragTarget.HandleInput(new InputEventArgs
                    {
                        Position  = mouse.Position,
                        EventType = InputEventType.MouseUp,
                        Button    = InputMouseButton.Right,
                        Timestamp = DateTime.UtcNow
                    });
                    _needsRenderThisFrame = true;
                }

                _rightDragTarget = null;
            }
            return;
        }

        if (button != Silk.NET.Input.MouseButton.Left) return;

        var position = mouse.Position;
        _needsRenderThisFrame = false;

        // REFACTORED: Only finish drag if actually dragging
        // If we only had a candidate but drag didn't activate, allow click
        bool wasDragging = _dragDropManager.IsDragging;

        if (_dragDropManager.CurrentDraggable != null || wasDragging)
        {
            _dragDropManager.EndDrag();

            // If actually dragging, DO NOT process as click
            if (wasDragging)
            {
                _draggedElement = null;
                return;
            }
        }

        // Clear IDragScrollable state if any
        if (_draggedElement != null)
        {
            var dragScrollable = FindDragScrollableAncestor(_draggedElement);
            if (dragScrollable != null && _draggedElement.IsEffectivelyEnabled() && _draggedElement is IInputHandler scrollHandler && scrollHandler.CanHandleInput)
            {
                var args = new InputEventArgs
                {
                    Position = position,
                    EventType = InputEventType.MouseUp,
                    Timestamp = DateTime.UtcNow
                };
                scrollHandler.HandleInput(args);
            }
        }

        // Stop drag
        if (_draggedElement is VisualElement draggedElement && draggedElement.IsEffectivelyEnabled() && _draggedElement is IInputHandler dragHandler)
        {
            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseUp,
                Timestamp = DateTime.UtcNow
            };
            dragHandler.HandleInput(args);
            _draggedElement?.MarkNeedsPaint();
            _needsRenderThisFrame = true;
        }

        // Clear draggedElement AFTER processing
        var wasElementDragging = _draggedElement != null;
        _draggedElement = null;

        // OPTIMIZED: Use hit-testing to find element under cursor
        HitTestResult? hitResult = null;
        bool hitWasInOverlay = false;

        if (_app != null && _app.Overlays.Count > 0)
        {
            for (int i = _app.Overlays.Count - 1; i >= 0; i--)
            {
                var overlay = _app.Overlays[i];
                hitResult = _hitTestEngine.HitTestRoot(overlay, position, new HitTestOptions
                {
                    Mode = HitTestMode.InteractiveOnly,
                    IncludeInvisible = false,
                    RespectInputTransparency = true,
                    CheckClipping = true
                });

                if (hitResult?.Element != null)
                {
                    hitWasInOverlay = true;
                    break;
                }

                // Even if no interactive element found, block if pointer is within overlay bounds
                bool inBounds = overlay.ContainsWindowPoint(position);
                if (inBounds)
                {
                    hitWasInOverlay = true;
                    break;
                }
            }

            // If click was outside overlay, close any open menus
            if (!hitWasInOverlay)
            {
                Controls.Menu.CloseCurrentMenu();
            }
        }

        if (!hitWasInOverlay && hitResult?.Element == null)
        {
            hitResult = _hitTestEngine.HitTest(position, new HitTestOptions
            {
                Mode = HitTestMode.InteractiveOnly,
                IncludeInvisible = false,
                RespectInputTransparency = true,
                CheckClipping = true
            });
        }

        if (hitResult?.Element != null)
        {
            ProcessMouseUpForElement(hitResult.Element, position, hitResult.IsInBounds);
        }

        // Notify ONCE per frame
        if (_needsRenderThisFrame || wasElementDragging)
        {
            _tree.MarkNeedsRender();
        }
    }

    /// <summary>
    /// Processes MouseUp for a specific element found by hit-testing.
    /// Mirrors the bubbling behaviour of ProcessMouseDownForElement: both
    /// IPointerHandler.OnPointerReleased and IGestureRecognizerHost recognizers
    /// are dispatched to the hit element AND every ancestor in the parent chain.
    ///
    /// When a gesture recognizer consumes the event (returns true) the shared
    /// PointerEventArgs.Handled flag is set to true so that ancestor
    /// IPointerHandler implementations can skip their own toggle logic and avoid
    /// double-firing (e.g. TimePicker.OnPointerReleased must not also call
    /// TogglePicker when the inner IconButton's TapRecognizer already did so).
    /// </summary>
    private void ProcessMouseUpForElement(VisualElement element, Vector2 position, bool isInside)
    {
        element.IsPressed = false;

        // Single shared args object so that Handled propagates through the whole bubble.
        var pointerArgs = Rayo.Core.Input.PointerEventArgs.FromMouse(position, 0, false);

        var current = element;
        while (current != null)
        {
            pointerArgs.LocalPosition = current.GetLocalPosition(position);

            // Dispatch IPointerHandler.OnPointerReleased (ancestors can check Handled).
            if (current is Rayo.Core.Input.IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerReleased(pointerArgs);
                _needsRenderThisFrame = true;
            }

            // Dispatch gesture recognizers; mark event handled when a recognizer fires.
            if (current is Rayo.Core.Input.IGestureRecognizerHost gestureHost)
            {
                foreach (var recognizer in gestureHost.GestureRecognizers)
                {
                    if (recognizer.ProcessPointerEvent(pointerArgs))
                    {
                        pointerArgs.Handled = true; // Gesture consumed — stop further toggle logic.
                    }
                }
                _needsRenderThisFrame = true;
            }

            current = current.Parent;
        }
    }

    private void OnMouseWheel(IMouse mouse, ScrollWheel wheel)
    {
        if (_tree.Root == null) return;

        var position = mouse.Position;
        float deltaY = wheel.Y * 20;
        float deltaX = wheel.X * 20;
        bool isShift = _keyboard != null &&
                       (_keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight));
        // Shift+vertical wheel redirects to horizontal scroll
        if (isShift && deltaX == 0) { deltaX = -deltaY; deltaY = 0; }

        // First, check overlays (they're on top)
        IScrollable? scrollable = null;
        bool isInOverlay = false;
        
        if (_app != null && _app.Overlays.Count > 0)
        {
            // Check overlays in reverse order (last added = topmost)
            for (int i = _app.Overlays.Count - 1; i >= 0; i--)
            {
                var overlay = _app.Overlays[i];
                
                // Check if the pointer is within the overlay bounds
                // Use simple bounds check since overlays typically fill the screen
                if (overlay.ContainsWindowPoint(position))
                {
                    isInOverlay = true;
                    
                    // Now search for scrollable within this overlay
                    scrollable = FindScrollableAtPosition(overlay, position);
                    if (scrollable != null)
                    {
                        break;
                    }
                    
                    // Even if no scrollable found, stop here if we're inside the overlay
                    // This prevents scrolling elements behind the overlay
                    break;
                }
            }
        }
        
        // If no overlay hit, check main tree
        if (!isInOverlay && scrollable == null)
        {
            scrollable = FindScrollableAtPosition(_tree.Root, position);
        }

        if (scrollable != null)
        {
            if (deltaY != 0) scrollable.Scroll(-deltaY);
            if (deltaX != 0) scrollable.ScrollHorizontal(deltaX);
            _tree.MarkNeedsRender();
            return;
        }

        // If no scrollable element, try with focused element
        if (_focusedElement != null && _focusedElement is IInputHandler handler)
        {
            var args = new InputEventArgs
            {
                Position = _lastMousePos,
                EventType = InputEventType.MouseWheel,
                ScrollDelta = new System.Numerics.Vector2(wheel.X, wheel.Y),
                Timestamp = DateTime.UtcNow
            };
            handler.HandleInput(args);
        }
    }

    /// <summary>
    /// Finds an IScrollable instead of specific ScrollView.
    /// </summary>
    private IScrollable? FindScrollableAtPosition(VisualElement element, Vector2 position)
    {
        if (!element.IsVisible) return null;

        // Search in children first (top to bottom in z-order)
        for (int i = element.GetChildren().Count() - 1; i >= 0; i--)
        {
            var result = FindScrollableAtPosition(element.GetChildren().ElementAt(i), position);
            if (result != null) return result;
        }

        // Check if this element is scrollable and point is inside
        // Use simple bounds check instead of complex hit-testing for better overlay compatibility
        if (element is IScrollable scrollable)
        {
            bool isInside = element.ContainsWindowPoint(position);
            
            if (isInside)
            {
                return scrollable;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an IDragScrollable in the ancestor hierarchy.
    /// </summary>
    private IDragScrollable? FindDragScrollableAncestor(VisualElement element)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current is IDragScrollable dragScrollable)
            {
                return dragScrollable;
            }
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Clears state of all IDragScrollable in the tree.
    /// </summary>
    private void CleanupAllDragScrollables(VisualElement element)
    {
        if (!element.IsVisible) return;

        if (element is IDragScrollable dragScrollable)
        {
            dragScrollable.CancelDragPending();
        }

        foreach (var child in element.GetChildren().ToArray())
        {
            CleanupAllDragScrollables(child);
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        // NEW: Handle Tab navigation
        if (key == Key.Tab)
        {
            bool isShiftPressed = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);
            NavigateFocus(isShiftPressed ? FocusDirection.Previous : FocusDirection.Next);
            _tree.MarkNeedsRender();
            return; // Consume Tab key
        }

        if (_focusedElement == null) return;

        if (!_focusedElement.IsEffectivelyEnabled())
        {
            SetFocus(null);
            return;
        }

        _needsRenderThisFrame = false;

        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var mappedKey = MapKey(key);

            var args = new InputEventArgs
            {
                Position = _lastMousePos,
                EventType = InputEventType.KeyDown,
                KeyCode = mappedKey,
                Timestamp = DateTime.UtcNow,
                IsShiftPressed = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight),
                IsControlPressed = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight),
                IsAltPressed = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight)
            };

            if (handler.HandleInput(args))
            {
                _needsRenderThisFrame = true;
            }

            if (_needsRenderThisFrame)
            {
                _tree.MarkNeedsRender();
            }
        }
    }

    public void ProcessTextInput(char character)
    {
        if (_focusedElement == null) return;

        if (!_focusedElement.IsEffectivelyEnabled())
        {
            SetFocus(null);
            return;
        }

        _needsRenderThisFrame = false;

        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = _lastMousePos,
                EventType = InputEventType.TextInput,
                Character = character,
                Timestamp = DateTime.UtcNow
            };

            if (handler.HandleInput(args))
            {
                _needsRenderThisFrame = true;
            }

            if (_needsRenderThisFrame)
            {
                _tree.MarkNeedsRender();
            }
        }
    }

    public void ProcessKeyDown(InputKey key)
    {
        if (_focusedElement == null) return;

        if (!_focusedElement.IsEffectivelyEnabled())
        {
            SetFocus(null);
            return;
        }

        _needsRenderThisFrame = false;

        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = _lastMousePos,
                EventType = InputEventType.KeyDown,
                KeyCode = key,
                Timestamp = DateTime.UtcNow
            };

            if (handler.HandleInput(args))
            {
                _needsRenderThisFrame = true;
            }

            if (_needsRenderThisFrame)
            {
                _tree.MarkNeedsRender();
            }
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (_focusedElement == null) return;

        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var mappedKey = MapKey(key);
            var args = new InputEventArgs
            {
                Position = _lastMousePos,
                EventType = InputEventType.KeyUp,
                KeyCode = mappedKey,
                Timestamp = DateTime.UtcNow
            };
            handler.HandleInput(args);
        }
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        if (_focusedElement == null) return;

        _needsRenderThisFrame = false;

        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = _lastMousePos,
                EventType = InputEventType.TextInput,
                Character = character,
                Timestamp = DateTime.UtcNow
            };

            if (handler.HandleInput(args))
            {
                _needsRenderThisFrame = true;
            }

            if (_needsRenderThisFrame)
            {
                _tree.MarkNeedsRender();
            }
        }
    }

    /// <summary>
    /// Checks if a point is inside an element using advanced hit-testing.
    /// </summary>
    private bool IsPointInside(VisualElement element, Vector2 position)
    {
        var options = new HitTestOptions
        {
            Mode = HitTestMode.FirstMatch,
            IncludeInvisible = false,
            RespectInputTransparency = true,
            CheckClipping = true,
            ElementFilter = e => e == element
        };

        var result = _hitTestEngine.HitTest(position, options);
        return result?.Element == element && result.IsInBounds && result.IsInClipRegion;
    }

    /// <summary>
    /// Finds the most appropriate element under a point using advanced hit-testing.
    /// </summary>
    private VisualElement? FindElementAt(Vector2 position, bool interactiveOnly = true)
    {
        var options = new HitTestOptions
        {
            Mode = interactiveOnly ? HitTestMode.InteractiveOnly : HitTestMode.FirstMatch,
            IncludeInvisible = false,
            RespectInputTransparency = true,
            CheckClipping = true
        };

        var result = _hitTestEngine.HitTest(position, options);
        return result?.Element;
    }

    private InputKey MapKey(Key key)
    {
        return key switch
        {
            Key.A => InputKey.A,
            Key.B => InputKey.B,
            Key.C => InputKey.C,
            Key.D => InputKey.D,
            Key.E => InputKey.E,
            Key.F => InputKey.F,
            Key.G => InputKey.G,
            Key.H => InputKey.H,
            Key.I => InputKey.I,
            Key.J => InputKey.J,
            Key.K => InputKey.K,
            Key.L => InputKey.L,
            Key.M => InputKey.M,
            Key.N => InputKey.N,
            Key.O => InputKey.O,
            Key.P => InputKey.P,
            Key.Q => InputKey.Q,
            Key.R => InputKey.R,
            Key.S => InputKey.S,
            Key.T => InputKey.T,
            Key.U => InputKey.U,
            Key.V => InputKey.V,
            Key.W => InputKey.W,
            Key.X => InputKey.X,
            Key.Y => InputKey.Y,
            Key.Z => InputKey.Z,
            Key.Number0 => InputKey.Number0,
            Key.Number1 => InputKey.Number1,
            Key.Number2 => InputKey.Number2,
            Key.Number3 => InputKey.Number3,
            Key.Number4 => InputKey.Number4,
            Key.Number5 => InputKey.Number5,
            Key.Number6 => InputKey.Number6,
            Key.Number7 => InputKey.Number7,
            Key.Number8 => InputKey.Number8,
            Key.Number9 => InputKey.Number9,
            Key.F1 => InputKey.F1,
            Key.F2 => InputKey.F2,
            Key.F3 => InputKey.F3,
            Key.F4 => InputKey.F4,
            Key.F5 => InputKey.F5,
            Key.F6 => InputKey.F6,
            Key.F7 => InputKey.F7,
            Key.F8 => InputKey.F8,
            Key.F9 => InputKey.F9,
            Key.F10 => InputKey.F10,
            Key.F11 => InputKey.F11,
            Key.F12 => InputKey.F12,
            Key.Enter => InputKey.Return,
            Key.Escape => InputKey.Escape,
            Key.Backspace => InputKey.Backspace,
            Key.Tab => InputKey.Tab,
            Key.Space => InputKey.Space,
            Key.Delete => InputKey.Delete,
            Key.Home => InputKey.Home,
            Key.End => InputKey.End,
            Key.PageUp => InputKey.PageUp,
            Key.PageDown => InputKey.PageDown,
            Key.Up => InputKey.Up,
            Key.Down => InputKey.Down,
            Key.Left => InputKey.Left,
            Key.Right => InputKey.Right,
            Key.ShiftLeft or Key.ShiftRight => InputKey.Shift,
            Key.ControlLeft or Key.ControlRight => InputKey.Control,
            Key.AltLeft or Key.AltRight => InputKey.Alt,
            _ => InputKey.Other
        };
    }

    public void Detach()
    {
        if (_mouse != null)
        {
            _mouse.MouseMove -= OnMouseMove;
            _mouse.MouseDown -= OnMouseDown;
            _mouse.MouseUp -= OnMouseUp;
            _mouse.Scroll -= OnMouseWheel;
        }

        if (_keyboard != null)
        {
            _keyboard.KeyDown -= OnKeyDown;
            _keyboard.KeyUp -= OnKeyUp;
            _keyboard.KeyChar -= OnKeyChar;
        }

        SetFocus(null);
        _draggedElement = null;
    }

    // ============= Focus Navigation =============

    private enum FocusDirection
    {
        Next,
        Previous
    }

    private void NavigateFocus(FocusDirection direction)
    {
        if (_tree.Root == null) return;

        // 1. Find all focusable elements in the tree (in visual/logical order)
        var focusableElements = new List<VisualElement>();
        CollectFocusableElements(_tree.Root, focusableElements);

        if (focusableElements.Count == 0) return;

        // 2. Find current focus index
        int currentIndex = -1;
        if (_focusedElement != null)
        {
            currentIndex = focusableElements.IndexOf(_focusedElement);
        }

        // 3. Calculate next index
        int nextIndex;
        if (direction == FocusDirection.Next)
        {
            if (currentIndex == -1)
            {
                nextIndex = 0; // Start from beginning
            }
            else
            {
                nextIndex = (currentIndex + 1) % focusableElements.Count;
            }
        }
        else // Previous
        {
            if (currentIndex == -1)
            {
                nextIndex = focusableElements.Count - 1; // Start from end
            }
            else
            {
                nextIndex = (currentIndex - 1 + focusableElements.Count) % focusableElements.Count;
            }
        }

        // 4. Set focus
        SetFocus(focusableElements[nextIndex]);
    }

    private void CollectFocusableElements(VisualElement element, List<VisualElement> list)
    {
        if (!element.IsVisible) return;

        // Check if current element is focusable
        // Must implement IFocusable AND be enabled/interactive
        if (element.IsEffectivelyEnabled() && element is IFocusable focusable && element is IInputHandler handler && handler.CanHandleInput)
        {
            list.Add(element);
        }

        // Recursively check children
        // Note: This simple traversal follows the tree structure (depth-first)
        // which usually matches the visual layout for standard containers
        foreach (var child in element.GetChildren().ToArray())
        {
            CollectFocusableElements(child, list);
        }
    }

    /// <summary>
    /// Processes time-based gestures for all GestureDetectors in the tree.
    /// Called every frame to handle long-press, double-tap timeout, etc.
    /// </summary>
    private void ProcessGestureDetectors()
    {
        if (_tree.Root != null)
        {
            ProcessGestureDetectorsRecursive(_tree.Root);
        }
    }

    /// <summary>
    /// Recursively processes GestureDetectors in the UI tree.
    /// </summary>
    private void ProcessGestureDetectorsRecursive(VisualElement? element)
    {
        if (element == null || !element.IsVisible) return;

        // Process GestureDetector frame updates
        if (element is Rayo.Gestures.Components.GestureDetector detector)
        {
            detector.ProcessFrame();
        }

        // Recursively process children
        foreach (var child in element.GetChildren().ToArray())
        {
            ProcessGestureDetectorsRecursive(child);
        }
    }

    // =========================================================================
    // TOUCH EVENT HANDLERS (Android/iOS support)
    // =========================================================================

    // Track active touch pointers
    private readonly Dictionary<int, TouchPointerState> _activeTouchPointers = new();

    private class TouchPointerState
    {
        public int PointerId { get; set; }
        public Vector2 LastPosition { get; set; }
        public DateTime StartTime { get; set; }
        public Vector2 StartPosition { get; set; }
        public VisualElement? CapturedElement { get; set; }
        public VisualElement? ScrollCaptureTarget { get; set; }
        public bool IsScrollCaptured { get; set; }
    }

    private const float TouchScrollThreshold = 10f;

    private static bool ShouldDispatchTouchInputHandler(VisualElement element)
    {
        return element is Rayo.Controls.Slider ||
               element is Rayo.Controls.Splitter ||
               element is not Rayo.Core.Input.IPointerHandler;
    }

    private void DispatchTouchPointerPressed(VisualElement element, Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        var current = element;
        while (current != null)
        {
            pointerArgs.LocalPosition = current.GetLocalPosition(pointerArgs.Position);

            if (current.IsEffectivelyEnabled() && current is Rayo.Core.Input.IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerPressed(pointerArgs);
            }

            if (current.IsEffectivelyEnabled() && current is Rayo.Core.Input.IGestureRecognizerHost gestureHost)
            {
                foreach (var recognizer in gestureHost.GestureRecognizers)
                {
                    recognizer.ProcessPointerEvent(pointerArgs);
                }
            }

            current = current.Parent;
        }
    }

    private void DispatchTouchPointerMoved(VisualElement element, Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        var current = element;
        while (current != null)
        {
            pointerArgs.LocalPosition = current.GetLocalPosition(pointerArgs.Position);

            if (current.IsEffectivelyEnabled() && current is Rayo.Core.Input.IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerMoved(pointerArgs);
            }

            if (current.IsEffectivelyEnabled() && current is Rayo.Core.Input.IGestureRecognizerHost gestureHost)
            {
                foreach (var recognizer in gestureHost.GestureRecognizers)
                {
                    recognizer.ProcessPointerEvent(pointerArgs);
                }
            }

            current = current.Parent;
        }
    }

    private void DispatchTouchPointerReleased(VisualElement element, Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        var current = element;
        while (current != null)
        {
            pointerArgs.LocalPosition = current.GetLocalPosition(pointerArgs.Position);

            if (current.IsEffectivelyEnabled() && current is Rayo.Core.Input.IPointerHandler pointerHandler)
            {
                pointerHandler.OnPointerReleased(pointerArgs);
            }

            if (current.IsEffectivelyEnabled() && current is Rayo.Core.Input.IGestureRecognizerHost gestureHost)
            {
                foreach (var recognizer in gestureHost.GestureRecognizers)
                {
                    recognizer.ProcessPointerEvent(pointerArgs);
                }
            }

            current = current.Parent;
        }
    }


    /// <summary>
    /// Process touch down event (Android/iOS).
    /// Called from platform-specific code when a finger touches the screen.
    /// </summary>
    public void ProcessTouchDown(Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        if (_tree.Root == null) return;

        _needsRenderThisFrame = false;

        // Track touch pointer
        var state = new TouchPointerState
        {
            PointerId = pointerArgs.PointerId,
            LastPosition = pointerArgs.Position,
            StartTime = DateTime.UtcNow,
            StartPosition = pointerArgs.Position
        };
        _activeTouchPointers[pointerArgs.PointerId] = state;

        // Hit-test to find element - check overlays first (from UITree for Android/iOS)
        HitTestResult? hitResult = null;

        // Check UITree overlays (for Android/iOS platforms without UIApplication)
        if (_tree.Overlays.Count > 0)
        {
            for (int i = _tree.Overlays.Count - 1; i >= 0; i--)
            {
                hitResult = _hitTestEngine.HitTestRoot(_tree.Overlays[i], pointerArgs.Position, new HitTestOptions
                {
                    Mode = HitTestMode.InteractiveOnly,
                    IncludeInvisible = false,
                    RespectInputTransparency = true,
                    CheckClipping = true
                });

                if (hitResult?.Element != null)
                {
                    break;
                }
            }
        }

        // If no hit in overlays, check main tree
        if (hitResult?.Element == null)
        {
            hitResult = _hitTestEngine.HitTest(pointerArgs.Position, new HitTestOptions
            {
                Mode = HitTestMode.InteractiveOnly,
                IncludeInvisible = false,
                RespectInputTransparency = true,
                CheckClipping = true
            });
        }

        ComboBox.HandleGlobalPointer(pointerArgs.Position, hitResult?.Element);
        DatePicker.HandleGlobalPointer(pointerArgs.Position, hitResult?.Element);

        foreach (var handler in _globalPointerHandlers.ToList())
        {
            if (handler.HandleGlobalPointer(pointerArgs.Position, hitResult?.Element))
            {
                break;
            }
        }

        if (hitResult?.Element != null)
        {
            state.CapturedElement = hitResult.Element;
            // Elements that handle their own drag exclusively (Slider, Splitter)
            // should never have scroll capture transferred away from them.
            if (hitResult.Element is Rayo.Controls.Slider ||
                hitResult.Element is Rayo.Controls.Splitter)
            {
                state.ScrollCaptureTarget = null;
            }
            else
            {
                state.ScrollCaptureTarget = hitResult.Element is IDragScrollable
                    ? hitResult.Element
                    : FindDragScrollableAncestor(hitResult.Element) as VisualElement;
            }
            ProcessTouchDownForElement(hitResult.Element, pointerArgs);

            // Also initialize IDragScrollable ancestors so they're ready for potential scroll
            // This is critical for ListView/ScrollView to work when touching child items
            if (state.ScrollCaptureTarget != null && state.ScrollCaptureTarget != hitResult.Element)
            {
                if (state.ScrollCaptureTarget is IDragScrollable ancestorScrollable)
                {
                    ancestorScrollable.StartDragPending();
                }

                // Send MouseDown to the scrollable so it initializes its drag state
                if (state.ScrollCaptureTarget is VisualElement scrollCaptureTarget && scrollCaptureTarget.IsEffectivelyEnabled()
                    && state.ScrollCaptureTarget is IInputHandler scrollHandler && scrollHandler.CanHandleInput)
                {
                    var args = new InputEventArgs
                    {
                        Position = pointerArgs.Position,
                        EventType = InputEventType.MouseDown,
                        Timestamp = DateTime.UtcNow
                    };
                    scrollHandler.HandleInput(args);
                }
            }
        }

        if (_needsRenderThisFrame)
        {
            _tree.MarkNeedsRender();
        }
    }

    /// <summary>
    /// Process touch move event (Android/iOS).
    /// Called when a finger moves on the screen.
    /// </summary>
    public void ProcessTouchMove(Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        if (!_activeTouchPointers.TryGetValue(pointerArgs.PointerId, out var state))
            return;

        // Calculate delta
        pointerArgs.Delta = pointerArgs.Position - state.LastPosition;
        state.LastPosition = pointerArgs.Position;

        if (!state.IsScrollCaptured && state.ScrollCaptureTarget != null)
        {
            var deltaFromStart = pointerArgs.Position - state.StartPosition;
            float distance = MathF.Sqrt(deltaFromStart.X * deltaFromStart.X + deltaFromStart.Y * deltaFromStart.Y);

            if (distance >= TouchScrollThreshold)
            {
                state.IsScrollCaptured = true;

                if (state.CapturedElement != state.ScrollCaptureTarget)
                {
                    var previousElement = state.CapturedElement;

                    // Call OnPointerReleased for previous element
                    if (previousElement != null)
                    {
                        var cancelArgs = Rayo.Core.Input.PointerEventArgs.FromTouch(pointerArgs.PointerId, pointerArgs.Position, pointerArgs.Pressure);
                        cancelArgs.IsInContact = false;
                        DispatchTouchPointerReleased(previousElement, cancelArgs);
                    }

                    if (previousElement is Rayo.Core.Input.IGestureRecognizerHost previousGestureHost)
                    {
                        foreach (var recognizer in previousGestureHost.GestureRecognizers)
                        {
                            recognizer.Reset();
                        }
                    }

                    state.CapturedElement = state.ScrollCaptureTarget;

                    if (state.CapturedElement is IDragScrollable dragScrollable)
                    {
                        dragScrollable.StartDragPending();
                    }

                    if (state.CapturedElement is VisualElement capturedElement && capturedElement.IsEffectivelyEnabled()
                        && state.CapturedElement is IInputHandler inputHandler && inputHandler.CanHandleInput)
                    {
                        var downArgs = new InputEventArgs
                        {
                            Position = state.StartPosition,
                            EventType = InputEventType.MouseDown,
                            Timestamp = DateTime.UtcNow
                        };
                        inputHandler.HandleInput(downArgs);
                    }
                }
            }
        }

        if (state.CapturedElement != null)
        {
            DispatchTouchPointerMoved(state.CapturedElement, pointerArgs);

            // Call IInputHandler for components like ScrollView that need MouseDrag events.
            if (state.CapturedElement is VisualElement capturedElement && capturedElement.IsEffectivelyEnabled()
                && state.CapturedElement is IInputHandler inputHandler && inputHandler.CanHandleInput
                && ShouldDispatchTouchInputHandler(state.CapturedElement))
            {
                var args = new InputEventArgs
                {
                    Position = pointerArgs.Position,
                    EventType = InputEventType.MouseDrag,
                    Timestamp = DateTime.UtcNow
                };
                inputHandler.HandleInput(args);
            }

            _tree.MarkNeedsRender();
        }
    }

    /// <summary>
    /// Checks if a point is inside an element's bounds.
    /// </summary>
    private bool IsPointInsideElement(VisualElement element, Vector2 position)
    {
        return element.ContainsWindowPoint(position);
    }

    /// <summary>
    /// Process touch up event (Android/iOS).
    /// Called when a finger is lifted from the screen.
    /// </summary>
    public void ProcessTouchUp(Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        if (!_activeTouchPointers.TryGetValue(pointerArgs.PointerId, out var state))
            return;

        if (state.CapturedElement != null)
        {
            ProcessTouchUpForElement(state.CapturedElement, pointerArgs);
        }
        else
        {
            pointerArgs.IsInContact = false;
        }

        // Remove touch state
        _activeTouchPointers.Remove(pointerArgs.PointerId);

        _tree.MarkNeedsRender();
    }

    /// <summary>
    /// Process touch cancel event (Android/iOS).
    /// Called when touch is interrupted (e.g., phone call).
    /// </summary>
    public void ProcessTouchCancel(Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        if (!_activeTouchPointers.TryGetValue(pointerArgs.PointerId, out var state))
            return;

        // Reset state for captured element
        if (state.CapturedElement != null)
        {
            DispatchTouchPointerReleased(state.CapturedElement, pointerArgs);
        }

        // Remove touch state
        _activeTouchPointers.Remove(pointerArgs.PointerId);

        _tree.MarkNeedsRender();
    }

    /// <summary>
    /// Process touch down for a specific element.
    /// </summary>
    private void ProcessTouchDownForElement(VisualElement element, Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        DispatchTouchPointerPressed(element, pointerArgs);

        // Call IInputHandler for components like ScrollView that need MouseDown events.
        if (element.IsEffectivelyEnabled() && element is IInputHandler inputHandler && inputHandler.CanHandleInput
            && ShouldDispatchTouchInputHandler(element))
        {
            var args = new InputEventArgs
            {
                Position = pointerArgs.Position,
                EventType = InputEventType.MouseDown,
                Timestamp = DateTime.UtcNow
            };
            inputHandler.HandleInput(args);
        }

        // Set focus on touch
        SetFocus(element);
    }

    /// <summary>
    /// Process touch up for a specific element.
    /// </summary>
    private void ProcessTouchUpForElement(VisualElement element, Rayo.Core.Input.PointerEventArgs pointerArgs)
    {
        DispatchTouchPointerReleased(element, pointerArgs);

        // Call IInputHandler for components like ScrollView that need MouseUp events.
        if (element.IsEffectivelyEnabled() && element is IInputHandler inputHandler && inputHandler.CanHandleInput
            && ShouldDispatchTouchInputHandler(element))
        {
            var args = new InputEventArgs
            {
                Position = pointerArgs.Position,
                EventType = InputEventType.MouseUp,
                Timestamp = DateTime.UtcNow
            };
            inputHandler.HandleInput(args);
        }
    }
}
