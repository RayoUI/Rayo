namespace Rayo.Controls;

using System;
using System.Globalization;
using System.Numerics;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Core.Platform;
using Rayo.Reactivity;

/// <summary>
/// Single-line numeric input control based on Entry.
/// </summary>
public class EntryNumber : Entry, IInputHandler
{
    private const float DragStartThreshold = 4f;

    private bool _isSynchronizingText;
    private bool _isPointerDown;
    private bool _isDraggingValue;
    private Vector2 _dragStartPosition;
    private double _dragStartValue;
    private double _value;

    public event Action<double>? ValueChanged;

    public EntryNumber()
    {
        KeyboardType = VirtualKeyboardType.Numeric;
        TextChanged += HandleTextChanged;
        UpdateTextFromValue();
    }

    public EntryNumber(double value) : this()
    {
        Value = value;
    }

    [PaintProperty]
    public double Value
    {
        get => _value;
        set => SetValue(value, updateText: true, notify: true);
    }

    public double Minimum
    {
        get => field;
        set
        {
            field = value;
            if (Maximum < value)
            {
                Maximum = value;
            }

            SetValue(_value, updateText: true, notify: true);
        }
    } = double.NegativeInfinity;

    public double Maximum
    {
        get => field;
        set
        {
            field = value;
            if (Minimum > value)
            {
                Minimum = value;
            }

            SetValue(_value, updateText: true, notify: true);
        }
    } = double.PositiveInfinity;

    public bool AllowDecimal
    {
        get => field;
        set
        {
            field = value;
            if (!value)
            {
                Text = SanitizeText(Text);
            }
        }
    } = true;

    public bool AllowNegative
    {
        get => field;
        set
        {
            field = value;
            if (!value)
            {
                Text = SanitizeText(Text);
            }
        }
    } = true;

    public string ValueFormat
    {
        get => field;
        set
        {
            field = value;
            UpdateTextFromValue();
        }
    } = "0.##";

    public bool HasValidNumber => TryParseCompleteNumber(Text, out _);

    public bool IsPointerValueDragEnabled { get; set; } = true;

    public double DragIncrement
    {
        get => field;
        set => field = Math.Max(double.Epsilon, value);
    } = 1d;

    public float DragPixelsPerStep
    {
        get => field;
        set => field = Math.Max(1f, value);
    } = 8f;

    public bool IsDraggingValue => _isDraggingValue;

    public bool WantsMouseCapture => _isPointerDown || _isDraggingValue;

    public override bool HandleInput(InputEventArgs args)
    {
        if (IsReadOnly)
        {
            return base.HandleInput(args);
        }

        switch (args.EventType)
        {
            case InputEventType.MouseDown:
                _isPointerDown = true;
                _isDraggingValue = false;
                _dragStartPosition = args.Position;
                _dragStartValue = Value;
                return base.HandleInput(args);

            case InputEventType.MouseDrag:
                if (IsPointerValueDragEnabled && (_isPointerDown || _isDraggingValue))
                {
                    var delta = args.Position - _dragStartPosition;
                    if (_isDraggingValue ||
                        (MathF.Abs(delta.Y) >= DragStartThreshold && MathF.Abs(delta.Y) >= MathF.Abs(delta.X)))
                    {
                        _isDraggingValue = true;
                        var steps = -delta.Y / DragPixelsPerStep;
                        SetValue(_dragStartValue + steps * DragIncrement, updateText: true, notify: true);
                        return true;
                    }
                }
                break;

            case InputEventType.MouseUp:
                if (_isPointerDown || _isDraggingValue)
                {
                    bool wasDraggingValue = _isDraggingValue;
                    _isPointerDown = false;
                    _isDraggingValue = false;
                    if (wasDraggingValue)
                    {
                        CommitText();
                        return true;
                    }
                }
                break;

            case InputEventType.TextInput:
                if (args.Character.HasValue && !IsAcceptedCharacter(args.Character.Value))
                {
                    return false;
                }
                break;
        }

        return base.HandleInput(args);
    }

    public EntryNumber IntegerOnly(bool allowNegative = true)
    {
        AllowDecimal = false;
        AllowNegative = allowNegative;
        Text = SanitizeText(Text);
        return this;
    }

    public EntryNumber PositiveOnly()
    {
        Minimum = Math.Max(0, Minimum);
        AllowNegative = false;
        Text = SanitizeText(Text);
        return this;
    }

    public EntryNumber Decimal(bool allowDecimal)
    {
        AllowDecimal = allowDecimal;
        return this;
    }

    public EntryNumber Negative(bool allowNegative)
    {
        AllowNegative = allowNegative;
        return this;
    }

    public EntryNumber CommitValue()
    {
        CommitText();
        return this;
    }

    void IInputHandler.OnFocusGained()
    {
        base.OnFocusGained();
    }

    void IInputHandler.OnFocusLost()
    {
        _isPointerDown = false;
        _isDraggingValue = false;
        CommitText();
        base.OnFocusLost();
    }

    private bool IsAcceptedCharacter(char c)
    {
        if (char.IsDigit(c))
        {
            return true;
        }

        if (AllowNegative && c == '-' && _cursorPosition == 0 && !Text.Contains('-'))
        {
            return true;
        }

        return AllowDecimal && c == '.' && !Text.Contains('.');
    }

    private void HandleTextChanged(string text)
    {
        if (_isSynchronizingText)
        {
            return;
        }

        var sanitized = SanitizeText(text);
        if (sanitized != text)
        {
            SetTextSilently(sanitized);
            text = sanitized;
        }

        if (TryParseCompleteNumber(text, out var parsedValue))
        {
            SetValue(parsedValue, updateText: false, notify: true);
        }
    }

    private void CommitText()
    {
        if (!TryParseCompleteNumber(Text, out var parsedValue))
        {
            UpdateTextFromValue();
            return;
        }

        SetValue(parsedValue, updateText: true, notify: true);
    }

    private void SetValue(double value, bool updateText, bool notify)
    {
        var clampedValue = Math.Clamp(value, Minimum, Maximum);
        if (Math.Abs(_value - clampedValue) < double.Epsilon)
        {
            if (updateText)
            {
                UpdateTextFromValue();
            }

            return;
        }

        _value = clampedValue;
        MarkNeedsPaint();

        if (updateText)
        {
            UpdateTextFromValue();
        }

        if (notify)
        {
            ValueChanged?.Invoke(_value);
        }
    }

    private void UpdateTextFromValue()
    {
        var formattedValue = string.IsNullOrWhiteSpace(ValueFormat)
            ? _value.ToString(CultureInfo.InvariantCulture)
            : _value.ToString(ValueFormat, CultureInfo.InvariantCulture);

        SetTextSilently(formattedValue);
    }

    private void SetTextSilently(string text)
    {
        _isSynchronizingText = true;
        try
        {
            Text = text;
        }
        finally
        {
            _isSynchronizingText = false;
        }
    }

    private string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        Span<char> buffer = text.Length <= 256 ? stackalloc char[text.Length] : new char[text.Length];
        int length = 0;
        bool hasDecimalSeparator = false;
        bool hasMinus = false;

        foreach (char c in text)
        {
            if (char.IsDigit(c))
            {
                buffer[length++] = c;
                continue;
            }

            if (AllowNegative && c == '-' && length == 0 && !hasMinus)
            {
                buffer[length++] = c;
                hasMinus = true;
                continue;
            }

            if (AllowDecimal && c == '.' && !hasDecimalSeparator)
            {
                buffer[length++] = c;
                hasDecimalSeparator = true;
            }
        }

        return new string(buffer[..length]);
    }

    private static bool TryParseCompleteNumber(string text, out double value)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            text == "-" ||
            text == "." ||
            text == "-.")
        {
            value = 0;
            return false;
        }

        return double.TryParse(
            text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }
}
