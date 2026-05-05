namespace Rayo.Core;

using System.Numerics;

/// <summary>
/// Información sobre eventos de entrada (mouse, touch, teclado)
/// </summary>
public class InputEventArgs
{
    public Vector2 Position { get; set; }
    public InputEventType EventType { get; set; }
    public InputKey? KeyCode { get; set; }
    public char? Character { get; set; }
    public bool IsShiftPressed { get; set; }
    public bool IsControlPressed { get; set; }
    public bool IsAltPressed { get; set; }
    public Vector2 ScrollDelta { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Unique identifier for touch point (0 for mouse, 0+ for touch).
    /// </summary>
    public int TouchId { get; set; } = 0;

    /// <summary>
    /// Indica si el evento fue manejado por algún control
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Mouse button that triggered this event. Defaults to Left for backward compatibility.
    /// </summary>
    public InputMouseButton Button { get; set; } = InputMouseButton.Left;
}

/// <summary>Mouse button identifier — avoids collision with Silk.NET.Input.MouseButton.</summary>
public enum InputMouseButton
{
    Left,
    Middle,
    Right
}

/// <summary>
/// Tipos de eventos de entrada
/// </summary>
public enum InputEventType
{
    MouseMove,
    MouseDown,
    MouseUp,
    MouseDrag,
    MouseWheel,
    TouchStart,
    TouchMove,
    TouchEnd,
    KeyDown,
    KeyUp,
    TextInput,
    KeyRepeat // Para repetición automática de teclas
}

/// <summary>
/// Códigos de tecla
/// </summary>
public enum InputKey
{
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Number0, Number1, Number2, Number3, Number4, Number5, Number6, Number7, Number8, Number9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Return, Escape, Backspace, Tab, Space,
    Delete, Home, End, PageUp, PageDown,
    Up, Down, Left, Right,
    Shift, Control, Alt,
    Other
}