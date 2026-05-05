using System.Numerics;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Gestures.Core;
using Rayo.Gestures.Events;
using Rayo.Gestures.Recognizers;
using Rayo.Rendering;

namespace Rayo.Gestures.Components;

/// <summary>
/// Flutter-style gesture detector component that wraps a child element
/// and recognizes various gestures (tap, double-tap, long-press, swipe, pan).
/// Uses lazy initialization - recognizers are only created when callbacks are registered.
/// </summary>
public class GestureDetector : CompositeView<GestureDetector>, IInputHandler, IPointerHandler
{
    private VisualElement? _child;
    private readonly GestureArena _arena = new();

    // Recognizers (lazy-initialized)
    private TapGestureRecognizer? _tapRecognizer;
    private DoubleTapGestureRecognizer? _doubleTapRecognizer;
    private LongPressGestureRecognizer? _longPressRecognizer;
    private SwipeGestureRecognizer? _swipeRecognizer;
    private PanGestureRecognizer? _panRecognizer;

    // Track active touch to prevent duplicate events
    private int _activeTouchId = -1;

    /// <summary>
    /// Creates a new GestureDetector that wraps the given child element.
    /// </summary>
    public GestureDetector(VisualElement child)
    {
        _child = child;
        if (_child != null)
        {
            AddChild(_child);
        }
    }

    // ========== IInputHandler Implementation ==========

    public bool CanHandleInput => true;

    public bool HandleInput(InputEventArgs args)
    {
        var position = args.Position;
        var touchId = args.TouchId;

        switch (args.EventType)
        {
            case InputEventType.MouseDown:
            case InputEventType.TouchStart:
                OnPointerDown(position, touchId);
                return true;

            case InputEventType.MouseMove:
            case InputEventType.MouseDrag:
            case InputEventType.TouchMove:
                if (_activeTouchId == touchId || _activeTouchId == -1)
                {
                    OnPointerMove(position, touchId);
                }
                return true;

            case InputEventType.MouseUp:
            case InputEventType.TouchEnd:
                if (_activeTouchId == touchId || _activeTouchId == -1)
                {
                    OnPointerUp(position, touchId);
                }
                return true;
        }

        return false;
    }

    // ========== Pointer Event Handling ==========

    private void OnPointerDown(Vector2 position, int touchId)
    {
        _activeTouchId = touchId;

        // Reset recognizers that were in terminal states before starting new touch
        ResetTerminalRecognizers();

        // Feed event to all active recognizers
        _tapRecognizer?.OnPointerDown(position, touchId);
        _doubleTapRecognizer?.OnPointerDown(position, touchId);
        _longPressRecognizer?.OnPointerDown(position, touchId);
        _swipeRecognizer?.OnPointerDown(position, touchId);
        _panRecognizer?.OnPointerDown(position, touchId);

        // Add active recognizers to arena
        AddActiveRecognizersToArena();
    }

    private void ResetTerminalRecognizers()
    {
        if (_tapRecognizer != null && !_tapRecognizer.IsActive) _tapRecognizer.Reset();
        if (_doubleTapRecognizer != null && !_doubleTapRecognizer.IsActive) _doubleTapRecognizer.Reset();
        if (_longPressRecognizer != null && !_longPressRecognizer.IsActive) _longPressRecognizer.Reset();
        if (_swipeRecognizer != null && !_swipeRecognizer.IsActive) _swipeRecognizer.Reset();
        if (_panRecognizer != null && !_panRecognizer.IsActive) _panRecognizer.Reset();
    }

    private void OnPointerMove(Vector2 position, int touchId)
    {
        _tapRecognizer?.OnPointerMove(position, touchId);
        _doubleTapRecognizer?.OnPointerMove(position, touchId);
        _longPressRecognizer?.OnPointerMove(position, touchId);
        _swipeRecognizer?.OnPointerMove(position, touchId);
        _panRecognizer?.OnPointerMove(position, touchId);

        // Resolve conflicts after each move
        _arena.Resolve();
    }

    private void OnPointerUp(Vector2 position, int touchId)
    {
        _tapRecognizer?.OnPointerUp(position, touchId);
        _doubleTapRecognizer?.OnPointerUp(position, touchId);
        _longPressRecognizer?.OnPointerUp(position, touchId);
        _swipeRecognizer?.OnPointerUp(position, touchId);
        _panRecognizer?.OnPointerUp(position, touchId);

        // Final conflict resolution
        _arena.Resolve();

        // DO NOT reset everything here as some recognizers (DoubleTap) 
        // need to persist state between multiple pointer interactions.
        // Terminal recognizers will be reset on next PointerDown.
        _activeTouchId = -1;
        _arena.Clear();
    }

    private void AddActiveRecognizersToArena()
    {
        if (_tapRecognizer != null) _arena.Add(_tapRecognizer);
        if (_doubleTapRecognizer != null) _arena.Add(_doubleTapRecognizer);
        if (_longPressRecognizer != null) _arena.Add(_longPressRecognizer);
        if (_swipeRecognizer != null) _arena.Add(_swipeRecognizer);
        if (_panRecognizer != null) _arena.Add(_panRecognizer);
    }

    private void ResetRecognizers()
    {
        _tapRecognizer?.Reset();
        _doubleTapRecognizer?.Reset();
        _longPressRecognizer?.Reset();
        _swipeRecognizer?.Reset();
        _panRecognizer?.Reset();
    }

    /// <summary>
    /// Called every frame for time-based gesture processing (long-press, double-tap timeout).
    /// </summary>
    public void ProcessFrame()
    {
        _longPressRecognizer?.ProcessFrame();
        _doubleTapRecognizer?.ProcessFrame();
    }

    // ========== Fluent API for Gesture Registration ==========

    /// <summary>
    /// Registers a callback for tap gestures.
    /// </summary>
    public GestureDetector OnTap(Action<Vector2> handler)
    {
        if (_tapRecognizer == null)
        {
            _tapRecognizer = new TapGestureRecognizer();
        }

        _tapRecognizer.OnTap += args => handler(args.Position);
        return this;
    }

    /// <summary>
    /// Registers a callback for tap gestures with full event args.
    /// </summary>
    public GestureDetector OnTap(Action<TapEventArgs> handler)
    {
        if (_tapRecognizer == null)
        {
            _tapRecognizer = new TapGestureRecognizer();
        }

        _tapRecognizer.OnTap += handler;
        return this;
    }

    /// <summary>
    /// Registers a callback for double-tap gestures.
    /// </summary>
    public GestureDetector OnDoubleTap(Action<Vector2> handler)
    {
        if (_doubleTapRecognizer == null)
        {
            _doubleTapRecognizer = new DoubleTapGestureRecognizer();
        }

        _doubleTapRecognizer.OnDoubleTap += args => handler(args.Position);
        return this;
    }

    /// <summary>
    /// Registers a callback for double-tap gestures with full event args.
    /// </summary>
    public GestureDetector OnDoubleTap(Action<TapEventArgs> handler)
    {
        if (_doubleTapRecognizer == null)
        {
            _doubleTapRecognizer = new DoubleTapGestureRecognizer();
        }

        _doubleTapRecognizer.OnDoubleTap += handler;
        return this;
    }

    /// <summary>
    /// Registers a callback for long-press gestures.
    /// </summary>
    public GestureDetector OnLongPress(Action<Vector2> handler)
    {
        if (_longPressRecognizer == null)
        {
            _longPressRecognizer = new LongPressGestureRecognizer();
        }

        _longPressRecognizer.OnLongPress += handler;
        return this;
    }

    /// <summary>
    /// Registers a callback for swipe gestures.
    /// </summary>
    public GestureDetector OnSwipe(Action<SwipeDirection, float> handler)
    {
        if (_swipeRecognizer == null)
        {
            _swipeRecognizer = new SwipeGestureRecognizer();
        }

        _swipeRecognizer.OnSwipe += args => handler(args.Direction, args.Velocity);
        return this;
    }

    /// <summary>
    /// Registers a callback for swipe gestures with full event args.
    /// </summary>
    public GestureDetector OnSwipe(Action<SwipeEventArgs> handler)
    {
        if (_swipeRecognizer == null)
        {
            _swipeRecognizer = new SwipeGestureRecognizer();
        }

        _swipeRecognizer.OnSwipe += handler;
        return this;
    }

    /// <summary>
    /// Registers a callback for pan start events.
    /// </summary>
    public GestureDetector OnPanStart(Action<Vector2> handler)
    {
        if (_panRecognizer == null)
        {
            _panRecognizer = new PanGestureRecognizer();
        }

        _panRecognizer.OnPanStart += args => handler(args.Position);
        return this;
    }

    /// <summary>
    /// Registers a callback for pan start events with full event args.
    /// </summary>
    public GestureDetector OnPanStart(Action<PanEventArgs> handler)
    {
        if (_panRecognizer == null)
        {
            _panRecognizer = new PanGestureRecognizer();
        }

        _panRecognizer.OnPanStart += handler;
        return this;
    }

    /// <summary>
    /// Registers a callback for pan update events (continuous).
    /// </summary>
    public GestureDetector OnPanUpdate(Action<Vector2> delta)
    {
        if (_panRecognizer == null)
        {
            _panRecognizer = new PanGestureRecognizer();
        }

        _panRecognizer.OnPanUpdate += args => delta(args.Delta);
        return this;
    }

    /// <summary>
    /// Registers a callback for pan update events with full event args.
    /// </summary>
    public GestureDetector OnPanUpdate(Action<PanEventArgs> handler)
    {
        if (_panRecognizer == null)
        {
            _panRecognizer = new PanGestureRecognizer();
        }

        _panRecognizer.OnPanUpdate += handler;
        return this;
    }

    /// <summary>
    /// Registers a callback for pan end events.
    /// </summary>
    public GestureDetector OnPanEnd(Action<Vector2> velocity)
    {
        if (_panRecognizer == null)
        {
            _panRecognizer = new PanGestureRecognizer();
        }

        _panRecognizer.OnPanEnd += args => velocity(args.Velocity);
        return this;
    }

    /// <summary>
    /// Registers a callback for pan end events with full event args.
    /// </summary>
    public GestureDetector OnPanEnd(Action<PanEventArgs> handler)
    {
        if (_panRecognizer == null)
        {
            _panRecognizer = new PanGestureRecognizer();
        }

        _panRecognizer.OnPanEnd += handler;
        return this;
    }

    // ========== IPointerHandler Implementation (for Android/touch) ==========

    public void OnPointerEntered(PointerEventArgs e) { }
    public void OnPointerExited(PointerEventArgs e) { }

    public void OnPointerPressed(PointerEventArgs e)
    {
        OnPointerDown(e.Position, e.PointerId);
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        if (_activeTouchId == e.PointerId || _activeTouchId == -1)
        {
            OnPointerMove(e.Position, e.PointerId);
        }
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (_activeTouchId == e.PointerId || _activeTouchId == -1)
        {
            OnPointerUp(e.Position, e.PointerId);
        }
    }

    public void OnPointerCanceled(PointerEventArgs e)
    {
        // Treat cancel as a pointer up without firing tap
        _activeTouchId = -1;
        _arena.Clear();
        ResetRecognizers();
    }

    // ========== Layout and Rendering ==========

    public override void Measure(float availableWidth, float availableHeight)
    {
        if (_child != null)
        {
            _child.Measure(availableWidth, availableHeight);
            DesiredWidth = _child.DesiredWidth;
            DesiredHeight = _child.DesiredHeight;
        }
        else
        {
            DesiredWidth = 0;
            DesiredHeight = 0;
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_child != null)
        {
            _child.Arrange(x, y, width, height);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // GestureDetector is transparent - just render the child
        if (_child != null && _child.IsVisible)
        {
            _child.Render(renderer);
        }
    }
}
