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

    /// <summary>
    /// When true, only digits, an optional leading minus sign, and (if IsDecimalAllowed) a
    /// single decimal point are accepted as text input.
    /// </summary>
    public bool IsNumericOnly { get; set; } = false;

    /// <summary>
    /// Controls whether a decimal separator ('.') is accepted when IsNumericOnly is true.
    /// Defaults to true so floating-point values can be entered.
    /// </summary>
    public bool IsDecimalAllowed { get; set; } = true;

    /// <summary>
    /// Controls whether a leading minus sign is accepted when IsNumericOnly is true.
    /// Defaults to true for backward compatibility.
    /// </summary>
    public bool IsNegativeAllowed { get; set; } = true;

    /// <summary>Fluent helper to enable numeric-only input.</summary>
    public Entry NumericOnly(bool isDecimalAllowed = true, bool isNegativeAllowed = true)
    {
        IsNumericOnly = true;
        IsDecimalAllowed = isDecimalAllowed;
        IsNegativeAllowed = isNegativeAllowed;
        return this;
    }

    //// Fluent API for MaxLength (base property) - hide inherited version
    //public new Entry MaxLength(int maxLength)
    //{
    //    MaxLength = maxLength;
    //    return this;
    //}

    //// Fluent API for IsReadOnly (base property) - hide inherited version
    //public new Entry IsReadOnly(bool isReadOnly)
    //{
    //    IsReadOnly = isReadOnly;
    //    return this;
    //}

    // Override CanHandleInput to respect IsReadOnly
    public override bool CanHandleInput => !IsReadOnly;

    // Override HandleInput to enforce IsReadOnly and numeric filtering
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

        // Numeric-only filter: intercept text-input events before they reach InsertChar
        if (IsNumericOnly && args.EventType == InputEventType.TextInput && args.Character.HasValue)
        {
            char c = args.Character.Value;

            // Always allow digits
            if (char.IsDigit(c))
                return base.HandleInput(args);

            // Allow minus sign only at the very beginning and only once when enabled
            if (IsNegativeAllowed && c == '-' && _cursorPosition == 0 && !Text.Contains('-'))
                return base.HandleInput(args);

            // Allow one decimal separator when decimals are permitted
            if (IsDecimalAllowed && c == '.' && !Text.Contains('.'))
                return base.HandleInput(args);

            return false; // Reject all other characters
        }

        return base.HandleInput(args);
    }
}

