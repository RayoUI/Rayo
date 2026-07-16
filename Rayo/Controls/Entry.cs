namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// Single-line text input control (MAUI-compatible Entry).
/// This is a specialized version of TextBox locked to single-line mode.
/// </summary>
public class Entry : TextBox<Entry>
{
    public Entry()
    {
        // Lock to single-line mode
        base.IsMultiline = false;
        DoubleTapSelectionUnit = TextSelectionUnit.WordThenLine;
    }

    public Entry(string text) : this()
    {
        Text = text;
    }

    // Hide IsMultiline to prevent accidental usage
    [Obsolete("Entry is always single-line. Use Editor for multi-line input.", true)]
    public new bool IsMultiline
    {
        get => false;
        set { /* Ignore - always single-line */ }
    }

    // MAUI-compatible event name (maps to OnEnter internally)
    public Entry OnCompletedHandler(System.Action handler)
    {
        Enter += handler;
        return this;
    }

    // Override CanHandleInput to respect IsReadOnly
    public override bool CanHandleInput => !IsReadOnly;

    // Override HandleInput to enforce IsReadOnly
    public override bool HandleInput(InputEventArgs args)
    {
        // In read-only mode, allow navigation but not editing
        if (IsReadOnly)
        {
            // Allow mouse events for focus and selection
            if (args.EventType == InputEventType.MouseDown ||
                args.EventType == InputEventType.MouseDrag ||
                args.EventType == InputEventType.MouseUp)
            {
                return base.HandleInput(args);
            }

            // Allow arrow keys and selection keys
            if (args.EventType == InputEventType.KeyDown || args.EventType == InputEventType.KeyRepeat)
            {
                if (args.KeyCode.HasValue)
                {
                    var key = args.KeyCode.Value;
                    // Allow navigation and selection
                    if (key == InputKey.Left || key == InputKey.Right ||
                        key == InputKey.Home || key == InputKey.End ||
                        (args.IsControlPressed && key == InputKey.A) ||
                        (args.IsControlPressed && key == InputKey.C))
                    {
                        return base.HandleInput(args);
                    }
                }
            }

            return false; // Block all other input
        }

        return base.HandleInput(args);
    }
}

