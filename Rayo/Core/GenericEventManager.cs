namespace Rayo.Core;

using Silk.NET.Input;
using Rayo.Core.Interfaces;
using System.Numerics;
using System.Linq;

/// <summary>
/// EventManager genérico que sigue el Principio Open/Closed.
/// NO conoce tipos específicos de componentes - solo interfaces.
///
/// EJEMPLO DE ARQUITECTURA MEJORADA - No se usa actualmente.
/// Ver ARCHITECTURE_PROPOSAL.md para más detalles.
/// </summary>
public class GenericEventManager
{
    private UITree _tree;
    private IMouse? _mouse;
    private IKeyboard? _keyboard;

    private VisualElement? _focusedElement;
    private VisualElement? _draggedElement;
    private Vector2 _lastMousePos = Vector2.Zero;
    private bool _eventConsumed;

    public GenericEventManager(UITree tree)
    {
        _tree = tree;
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

    public void SetFocus(VisualElement? element)
    {
        if (_focusedElement == element) return;

        // Notificar con interfaz genérica
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

        _tree.MarkNeedsRender();
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (_tree.Root == null || button != MouseButton.Left) return;

        var position = mouse.Position;
        _eventConsumed = false;
        ProcessMouseDown(_tree.Root, position);
    }

    private void ProcessMouseDown(VisualElement element, Vector2 position)
    {
        if (!element.IsVisible || _eventConsumed) return;

        // Procesar children primero (z-order)
        var children = element.GetChildren().ToList();
        for (int i = children.Count - 1; i >= 0; i--)
        {
            ProcessMouseDown(children[i], position);
            if (_eventConsumed) return;
        }

        if (!IsPointInside(element, position)) return;

        // ✅ GENÉRICO: Solo usa interfaces, no tipos concretos
        if (element is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseDown,
                Timestamp = DateTime.UtcNow
            };

            bool handled = handler.HandleInput(args);
            if (handled)
            {
                _eventConsumed = true;

                // Si es draggable, registrarlo
                if (element is IDraggable draggable)
                {
                    _draggedElement = element;
                    draggable.OnDragStart(position.X, position.Y);
                }

                // Si es focusable, darle foco
                if (element is IFocusable)
                {
                    SetFocus(element);
                }

                _tree.MarkNeedsRender();
            }
        }
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (_tree.Root == null) return;

        // Procesar drag si hay elemento arrastrado
        if (_draggedElement is IDraggable draggable)
        {
            draggable.OnDragging(position.X, position.Y);
            _tree.MarkNeedsRender();
        }
        else
        {
            // Procesar hover
            ProcessMouseMove(_tree.Root, position);
        }

        _lastMousePos = position;
    }

    private void ProcessMouseMove(VisualElement element, Vector2 position)
    {
        if (!element.IsVisible) return;

        bool isInside = IsPointInside(element, position);

        // Procesar children
        foreach (var child in element.GetChildren().ToArray())
        {
            ProcessMouseMove(child, position);
        }

        // NOTE: Hover state is now managed by IPointerHandler in modern EventManager
        // GenericEventManager is legacy and hover tracking has been removed
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (_tree.Root == null || button != MouseButton.Left) return;

        var position = mouse.Position;

        // Terminar drag si hay
        if (_draggedElement is IDraggable draggable)
        {
            draggable.OnDragEnd(false); // No hay drop target, así que false
            _draggedElement = null;
            _tree.MarkNeedsRender();
        }

        _eventConsumed = false;
        ProcessMouseUp(_tree.Root, position);
    }

    private void ProcessMouseUp(VisualElement element, Vector2 position)
    {
        if (!element.IsVisible || _eventConsumed) return;

        // Procesar children
        var children = element.GetChildren().ToList();
        for (int i = children.Count - 1; i >= 0; i--)
        {
            ProcessMouseUp(children[i], position);
            if (_eventConsumed) return;
        }

        if (!IsPointInside(element, position)) return;

        // ✅ GENÉRICO: Despachar evento genérico
        if (element is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseUp,
                Timestamp = DateTime.UtcNow
            };

            if (handler.HandleInput(args))
            {
                _eventConsumed = true;
                _tree.MarkNeedsRender();
            }
        }
    }

    private void OnMouseWheel(IMouse mouse, ScrollWheel wheel)
    {
        if (_tree.Root == null) return;

        var position = mouse.Position;

        // Buscar elemento que pueda manejar scroll
        var element = FindElementAt(_tree.Root, position, e => e is IInputHandler);

        if (element is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                Position = position,
                EventType = InputEventType.MouseWheel,
                ScrollDelta = new Vector2(wheel.X, wheel.Y),
                Timestamp = DateTime.UtcNow
            };

            if (handler.HandleInput(args))
            {
                _tree.MarkNeedsRender();
            }
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                EventType = InputEventType.KeyDown,
                KeyCode = MapKey(key),
                IsShiftPressed = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight),
                IsControlPressed = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight),
                IsAltPressed = keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight),
                Timestamp = DateTime.UtcNow
            };

            handler.HandleInput(args);
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                EventType = InputEventType.KeyUp,
                KeyCode = MapKey(key),
                Timestamp = DateTime.UtcNow
            };

            handler.HandleInput(args);
        }
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        if (_focusedElement is IInputHandler handler && handler.CanHandleInput)
        {
            var args = new InputEventArgs
            {
                EventType = InputEventType.TextInput,
                Character = character,
                Timestamp = DateTime.UtcNow
            };

            handler.HandleInput(args);
        }
    }

    // Helper methods
    private bool IsPointInside(VisualElement element, Vector2 position)
    {
        return element.ContainsWindowPoint(position);
    }

    private VisualElement? FindElementAt(VisualElement root, Vector2 position, Func<VisualElement, bool> predicate)
    {
        if (!root.IsVisible) return null;

        // Buscar en children primero
        for (int i = root.GetChildren().Count() - 1; i >= 0; i--)
        {
            var result = FindElementAt(root.GetChildren().ElementAt(i), position, predicate);
            if (result != null) return result;
        }

        // Verificar este elemento
        if (IsPointInside(root, position) && predicate(root))
        {
            return root;
        }

        return null;
    }

    private InputKey MapKey(Key key)
    {
        return key switch
        {
            Key.Enter => InputKey.Return,
            Key.Escape => InputKey.Escape,
            Key.Tab => InputKey.Tab,
            Key.Space => InputKey.Space,
            Key.Delete => InputKey.Delete,
            Key.Backspace => InputKey.Backspace,
            Key.Home => InputKey.Home,
            Key.End => InputKey.End,
            Key.Left => InputKey.Left,
            Key.Right => InputKey.Right,
            Key.Up => InputKey.Up,
            Key.Down => InputKey.Down,
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
}