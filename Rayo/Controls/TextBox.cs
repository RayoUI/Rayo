namespace Rayo.Controls;

using System;
using Rayo.Animation;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Text input field with support for single-line and multi-line text editing.
/// </summary>
public abstract class TextBox<T> : Rayo.Core.View<T>, IInputHandler, IFocusable, IFrameAnimation, Rayo.Core.Platform.IVirtualKeyboardOptions where T : Rayo.Core.View<T>
{
    // =========================================================================
    // INTERFACE IMPLEMENTATIONS
    // =========================================================================

    // IInputHandler (virtual to allow override in derived classes like Editor)
    public virtual bool CanHandleInput => true;

    // IFocusable — set by EventManager; [NotFluent] suppresses builder generation,
    // [PaintProperty] documents that focus changes require a repaint.
    [NotFluent, PaintProperty]
    public bool IsFocused
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    // IVirtualKeyboardOptions — controls on-screen keyboard type on mobile platforms.
    // Behavioral only: no effect on layout or visuals.
    public Rayo.Core.Platform.VirtualKeyboardType KeyboardType { get; set; } =
        Rayo.Core.Platform.VirtualKeyboardType.Default;

    // =========================================================================
    // MANUAL PROPERTIES (Complex state with validation and side effects)
    // =========================================================================

    private bool _suppressCursorAutoMove;

    /// <summary>
    /// Gets or sets the text content.
    /// Manual implementation required for cursor/selection clamping logic.
    /// Does not use SetProperty — marks paint explicitly after side effects.
    /// </summary>
    [PaintProperty]
    public string Text
    {
        get => field ?? string.Empty;
        set
        {
            var newValue = value ?? string.Empty;
            if (field == newValue)
            {
                return;
            }

            field = newValue;

            if (_suppressCursorAutoMove)
            {
                ClampCursorAndSelection();
            }
            else
            {
                MoveCursorToEnd();
            }

            MarkNeedsPaint();
            TextChanged?.Invoke(field);
        }
    } = string.Empty;

    private void AssignTextPreservingCursor(string newValue)
    {
        bool previous = _suppressCursorAutoMove;
        _suppressCursorAutoMove = true;
        try
        {
            Text = newValue;
        }
        finally
        {
            _suppressCursorAutoMove = previous;
        }
    }

    private void MoveCursorToEnd()
    {
        int length = Text.Length;
        _cursorPosition = length;
        _selectionStart = length;
        _selectionEnd = length;
    }

    private void ClampCursorAndSelection()
    {
        int length = Text.Length;
        _cursorPosition = Math.Clamp(_cursorPosition, 0, length);
        _selectionStart = Math.Clamp(_selectionStart, 0, length);
        _selectionEnd = Math.Clamp(_selectionEnd, 0, length);
    }

    // =========================================================================
    // REACTIVE PROPERTIES
    // =========================================================================

    #region Placeholder
    [PaintProperty]
    public string Placeholder
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = string.Empty;
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(40, 40, 40);
    #endregion

    #region FocusBackground
    [PaintProperty]
    public Brush FocusBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(50, 50, 50);
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.White;
    #endregion

    #region PlaceholderColor
    [PaintProperty]
    public Brush PlaceholderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(120, 120, 120);
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(70, 70, 70);
    #endregion

    #region FocusBorderColor
    [PaintProperty]
    public Brush FocusBorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(59, 130, 246);
    #endregion

    #region BorderWidth
    [PaintProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 2;
    #endregion

    #region IsPassword
    [PaintProperty]
    public bool IsPassword
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = false;
    #endregion

    #region IsMultiline
    [LayoutProperty]
    public bool IsMultiline
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = false;
    #endregion

    #region FontSize
    [LayoutProperty]
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 14;
    #endregion

    #region SelectionBackground
    [PaintProperty]
    public Brush SelectionBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 120, 215);
    #endregion

    // =========================================================================
    // OPTIONAL PROPERTIES (Used by Entry/Editor for MAUI compatibility)
    // =========================================================================

    /// <summary>
    /// Optional: Maximum number of characters allowed. 0 = unlimited.
    /// Used by Entry/Editor controls for MAUI compatibility.
    /// </summary>
    public int MaxLength { get; set; } = 0;

    /// <summary>
    /// Optional: Read-only mode. When true, text cannot be edited.
    /// Used by Entry/Editor controls for MAUI compatibility.
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    protected int _cursorPosition = 0;
    protected float _scrollOffsetX = 0;  // Offset horizontal para scroll
    protected float _scrollOffsetY = 0;  // Offset vertical para scroll (multiline)

    // =========================================================================
    // INTERNAL STATE (Selection, scrolling, mouse tracking)
    // =========================================================================

    // Sistema de selección de texto
    protected int _selectionStart = 0;  // Inicio de la selección
    protected int _selectionEnd = 0;    // Fin de la selección

    // Estado para selección con mouse
    private bool _isMouseSelecting = false;

    private int _mouseSelectionStart = 0;
    protected IRenderer? _cachedRenderer = null;  // Para mediciones precisas

    // Estado para doble click
    private DateTime _lastClickTime = DateTime.MinValue;
    private const double DoubleClickThresholdMs = 500;  // 500ms para detectar doble click

    // Cursor blink state
    private DateTime _lastCursorActivityTime = DateTime.UtcNow;
    protected bool _cursorVisible = true;
    private const double CursorBlinkIntervalMs = 530;  // Standard Windows caret blink rate

    // =========================================================================
    // EVENTS
    // =========================================================================

    public event Action<string>? TextChanged;
    public event Action? Enter;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public TextBox()
    {
        // Remove hardcoded size - let it size dynamically
        Padding = new Thickness(10, 6, 10, 6);
        BorderRadius = new CornerRadius(4);
    }

    // =========================================================================
    // CURSOR BLINK
    // =========================================================================

    /// <summary>
    /// Resets the cursor blink timer and makes the cursor visible.
    /// Should be called when the cursor moves or text is edited.
    /// </summary>
    protected void ResetCursorBlink()
    {
        _lastCursorActivityTime = DateTime.UtcNow;
        _cursorVisible = true;
    }

    /// <summary>
    /// Returns true if cursor should be visible based on current blink state.
    /// </summary>
    private bool IsCursorVisible()
    {
        return _cursorVisible;
    }

    // =========================================================================
    // FLUENT API (Manual methods with custom logic)
    // =========================================================================
    // Note: Placeholder(), Background(), TextColor(), BorderWidth(),
    // IsPassword(), IsMultiline(), FontSize() are auto-generated

    public TextBox<T> Password(bool isPassword)
    {
        IsPassword = isPassword;
        return this;
    }

//public TextBox<T> KeyboardType(Rayo.Core.Platform.VirtualKeyboardType keyboardType)
    //{
    //    KeyboardType = keyboardType;
    //    return this;
    //}

    internal void InvokeEnter()
    {
        Enter?.Invoke();
    }

    public void InsertChar(char c)
    {
        // Check read-only mode
        if (IsReadOnly) return;

        // Check MaxLength before inserting
        if (MaxLength > 0 && Text.Length >= MaxLength && !HasSelection) return;

        // Si hay selección, eliminarla primero
        if (HasSelection)
        {
            DeleteSelection();
        }

        AssignTextPreservingCursor(Text.Insert(_cursorPosition, c.ToString()));
        _cursorPosition++;
        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();  // Text editing only requires repaint
    }

    public void DeleteChar()
    {
        // Check read-only mode
        if (IsReadOnly) return;

        // Si hay selección, eliminarla
        if (HasSelection)
        {
            DeleteSelection();
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();
            return;
        }

        // Si no hay selección, borrar carácter anterior
        if (_cursorPosition > 0)
        {
            // FIX: Move cursor back first to avoid Text setter clamping logic causing negative index
            _cursorPosition--;
            AssignTextPreservingCursor(Text.Remove(_cursorPosition, 1));
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();  // Text editing only requires repaint
        }
    }

    /// <summary>
    /// Elimina el carácter a la derecha del cursor (tecla Delete)
    /// </summary>
    public void DeleteCharForward()
    {
        // Check read-only mode
        if (IsReadOnly) return;

        // Si hay selección, eliminarla
        if (HasSelection)
        {
            DeleteSelection();
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();
            return;
        }

        // Si no hay selección, borrar carácter siguiente
        if (_cursorPosition < Text.Length)
        {
            AssignTextPreservingCursor(Text.Remove(_cursorPosition, 1));
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();  // Text editing only requires repaint
        }
    }

    public void MoveCursorLeft(bool shiftPressed = false)
    {
        if (shiftPressed)
        {
            // Si no hay selección, iniciar desde la posición actual
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = Math.Max(0, _cursorPosition - 1);
            _selectionEnd = _cursorPosition;
        }
        else
        {
            // Si hay selección, mover al inicio de la selección
            if (HasSelection)
            {
                _cursorPosition = Math.Min(_selectionStart, _selectionEnd);
                ClearSelection();
            }
            else
            {
                _cursorPosition = Math.Max(0, _cursorPosition - 1);
            }
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    public void MoveCursorRight(bool shiftPressed = false)
    {
        if (shiftPressed)
        {
            // Si no hay selección, iniciar desde la posición actual
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = Math.Min(Text.Length, _cursorPosition + 1);
            _selectionEnd = _cursorPosition;
        }
        else
        {
            // Si hay selección, mover al final de la selección
            if (HasSelection)
            {
                _cursorPosition = Math.Max(_selectionStart, _selectionEnd);
                ClearSelection();
            }
            else
            {
                _cursorPosition = Math.Min(Text.Length, _cursorPosition + 1);
            }
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    public void MoveCursorToStart(bool shiftPressed = false)
    {
        if (shiftPressed)
        {
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = 0;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = 0;
            ClearSelection();
        }

        ResetCursorBlink();
        _scrollOffsetX = 0;
        MarkNeedsPaint();
    }

    public void MoveCursorToEnd(bool shiftPressed = false)
    {
        if (shiftPressed)
        {
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = Text.Length;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = Text.Length;
            ClearSelection();
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    /// <summary>
    /// Mueve el cursor una línea hacia arriba (solo para multiline)
    /// </summary>
    public virtual void MoveCursorUp(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        // Encontrar la línea actual y la posición del cursor en esa línea
        int currentLineIndex = 0;
        int charIndexInLine = 0;
        int lineStartPos = 0;
        
        // Dividir texto en líneas
        string[] lines = Text.Split('\n');
        
        // Encontrar la línea actual y la posición del cursor
        int accumulatedPos = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int lineLength = lines[i].Length;
            if (_cursorPosition >= accumulatedPos && _cursorPosition <= accumulatedPos + lineLength)
            {
                currentLineIndex = i;
                charIndexInLine = _cursorPosition - accumulatedPos;
                lineStartPos = accumulatedPos;
                break;
            }
            accumulatedPos += lineLength + 1; // +1 por el '\n'
        }

        // Si estamos en la primera línea, no podemos subir más
        if (currentLineIndex == 0)
        {
            MoveCursorToStart(shiftPressed);
            return;
        }

        // Calcular nueva posición en la línea anterior
        int previousLineIndex = currentLineIndex - 1;
        string previousLine = lines[previousLineIndex];
        
        // Mover al mismo índice de carácter o al final de la línea anterior (lo que sea menor)
        int newCharIndex = Math.Min(charIndexInLine, previousLine.Length);
        
        // Calcular nueva posición absoluta
        int newCursorPos = 0;
        for (int i = 0; i < previousLineIndex; i++)
        {
            newCursorPos += lines[i].Length + 1; // +1 por el '\n'
        }
        newCursorPos += newCharIndex;

        // Manejar selección si Shift está presionado
        if (shiftPressed)
        {
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = newCursorPos;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = newCursorPos;
            ClearSelection();
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    /// <summary>
    /// Mueve el cursor una línea hacia abajo (solo para multiline)
    /// </summary>
    public virtual void MoveCursorDown(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        // Encontrar la línea actual y la posición del cursor en esa línea
        int currentLineIndex = 0;
        int charIndexInLine = 0;
        int lineStartPos = 0;
        
        // Dividir texto en líneas
        string[] lines = Text.Split('\n');
        
        // Encontrar la línea actual y la posición del cursor
        int accumulatedPos = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int lineLength = lines[i].Length;
            if (_cursorPosition >= accumulatedPos && _cursorPosition <= accumulatedPos + lineLength)
            {
                currentLineIndex = i;
                charIndexInLine = _cursorPosition - accumulatedPos;
                lineStartPos = accumulatedPos;
                break;
            }
            accumulatedPos += lineLength + 1; // +1 por el '\n'
        }

        // Si estamos en la última línea, no podemos bajar más
        if (currentLineIndex >= lines.Length - 1)
        {
            MoveCursorToEnd(shiftPressed);
            return;
        }

        // Calcular nueva posición en la línea siguiente
        int nextLineIndex = currentLineIndex + 1;
        string nextLine = lines[nextLineIndex];
        
        // Mover al mismo índice de carácter o al final de la línea siguiente (lo que sea menor)
        int newCharIndex = Math.Min(charIndexInLine, nextLine.Length);
        
        // Calcular nueva posición absoluta
        int newCursorPos = 0;
        for (int i = 0; i < nextLineIndex; i++)
        {
            newCursorPos += lines[i].Length + 1; // +1 por el '\n'
        }
        newCursorPos += newCharIndex;

        // Manejar selección si Shift está presionado
        if (shiftPressed)
        {
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = newCursorPos;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = newCursorPos;
            ClearSelection();
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    /// <summary>
    /// Limpia la selección actual
    /// </summary>
    protected void ClearSelection()
    {
        _selectionStart = 0;
        _selectionEnd = 0;
    }

    /// <summary>
    /// Verifica si hay texto seleccionado
    /// </summary>
    public bool HasSelection => _selectionStart != _selectionEnd;

    /// <summary>
    /// Obtiene el texto seleccionado
    /// </summary>
    public string GetSelectedText()
    {
        if (!HasSelection) return string.Empty;

        int start = Math.Min(_selectionStart, _selectionEnd);
        int length = Math.Abs(_selectionEnd - _selectionStart);
        return Text.Substring(start, length);
    }

    /// <summary>
    /// Elimina el texto seleccionado
    /// </summary>
    private void DeleteSelection()
    {
        if (!HasSelection) return;

        int start = Math.Min(_selectionStart, _selectionEnd);
        int length = Math.Abs(_selectionEnd - _selectionStart);

        AssignTextPreservingCursor(Text.Remove(start, length));
        _cursorPosition = start;
        ClearSelection();
    }

    /// <summary>
    /// Selecciona todo el texto
    /// </summary>
    public void SelectAll()
    {
        _selectionStart = 0;
        _selectionEnd = Text.Length;
        _cursorPosition = Text.Length;
        MarkNeedsPaint();
    }

    /// <summary>
    /// Copia el texto seleccionado al clipboard
    /// </summary>
    public void Copy()
    {
        if (!HasSelection) return;

        string selectedText = GetSelectedText();
        try
        {
            Rayo.Core.ClipboardService.SetText(selectedText);
        }
        catch
        {
            // Silently fail if clipboard is not available
        }
    }

    /// <summary>
    /// Corta el texto seleccionado al clipboard
    /// </summary>
    public void Cut()
    {
        // Check read-only mode
        if (IsReadOnly) return;
        if (!HasSelection) return;

        Copy();
        DeleteSelection();
        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    /// <summary>
    /// Pega texto desde el clipboard
    /// </summary>
    public void Paste()
    {
        // Check read-only mode
        if (IsReadOnly) return;
        
        try
        {
            string clipboardText = Rayo.Core.ClipboardService.GetText() ?? string.Empty;
            if (string.IsNullOrEmpty(clipboardText)) return;

            // Si hay selección, reemplazarla
            if (HasSelection)
            {
                DeleteSelection();
            }

            // Check MaxLength before pasting
            if (MaxLength > 0)
            {
                int availableSpace = MaxLength - Text.Length;
                if (availableSpace <= 0) return; // No space left
                
                if (clipboardText.Length > availableSpace)
                {
                    clipboardText = clipboardText.Substring(0, availableSpace);
                }
            }

            // Insertar el texto del clipboard
            AssignTextPreservingCursor(Text.Insert(_cursorPosition, clipboardText));
            _cursorPosition += clipboardText.Length;
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();
        }
        catch
        {
            // Silently fail if clipboard is not available
        }
    }

    /// <summary>
    /// Mueve el cursor una página hacia arriba (multiline - aprox. 10 líneas)
    /// </summary>
    public void MoveCursorPageUp(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        // Dividir texto en líneas
        string[] lines = Text.Split('\n');
        
        // Encontrar línea actual
        int currentLineIndex = 0;
        int accumulatedPos = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int lineLength = lines[i].Length;
            if (_cursorPosition >= accumulatedPos && _cursorPosition <= accumulatedPos + lineLength)
            {
                currentLineIndex = i;
                break;
            }
            accumulatedPos += lineLength + 1; // +1 por el '\n'
        }

        // Calcular línea de destino (10 líneas hacia arriba, o primera línea)
        int targetLineIndex = Math.Max(0, currentLineIndex - 10);
        
        // Calcular nueva posición absoluta
        int newCursorPos = 0;
        for (int i = 0; i < targetLineIndex; i++)
        {
            newCursorPos += lines[i].Length + 1; // +1 por el '\n'
        }

        // Manejar selección
        if (shiftPressed)
        {
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = newCursorPos;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = newCursorPos;
            ClearSelection();
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    /// <summary>
    /// Mueve el cursor una página hacia abajo (multiline - aprox. 10 líneas)
    /// </summary>
    public void MoveCursorPageDown(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        // Dividir texto en líneas
        string[] lines = Text.Split('\n');
        
        // Encontrar línea actual
        int currentLineIndex = 0;
        int accumulatedPos = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int lineLength = lines[i].Length;
            if (_cursorPosition >= accumulatedPos && _cursorPosition <= accumulatedPos + lineLength)
            {
                currentLineIndex = i;
                break;
            }
            accumulatedPos += lineLength + 1; // +1 por el '\n'
        }

        // Calcular línea de destino (10 líneas hacia abajo, o última línea)
        int targetLineIndex = Math.Min(lines.Length - 1, currentLineIndex + 10);
        
        // Calcular nueva posición absoluta (al final de la línea de destino)
        int newCursorPos = 0;
        for (int i = 0; i < targetLineIndex; i++)
        {
            newCursorPos += lines[i].Length + 1; // +1 por el '\n'
        }
        newCursorPos += lines[targetLineIndex].Length; // Al final de la línea

        // Manejar selección
        if (shiftPressed)
        {
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = newCursorPos;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = newCursorPos;
            ClearSelection();
        }

        ResetCursorBlink();
        UpdateScrollToCursor();
        MarkNeedsPaint();
    }

    /// <summary>
    /// Ajusta el scroll horizontal para mantener el cursor visible
    /// </summary>
    private void UpdateScrollToCursor()
    {
        // Medir el texto hasta el cursor (cachear renderer sería ideal, pero por ahora usar lazy)
        // Esta función se llama solo cuando el usuario mueve el cursor, no en cada render
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Calculate desired size based on content or default
        // TextBox usually has a default width if empty, unlike Button
        float defaultWidth = 200;
        // Reduced default height from 40 to 32 to match standard UI controls better
        float defaultHeight = 32;

        if (IsMultiline)
        {
            // For multiline, we want to expand if possible, or have a reasonable default height
            defaultHeight = 100;
        }

        // Handle explicit size first
        if (Width > 0)
        {
            DesiredWidth = Width;
        }
        else
        {
            // If HorizontalAlignment is Stretch, expand to available width
            if (HorizontalAlignment == HorizontalAlignment.Stretch && availableWidth < float.PositiveInfinity)
            {
                DesiredWidth = availableWidth;
            }
            else
            {
                DesiredWidth = defaultWidth;
            }
        }

        if (Height > 0)
        {
            DesiredHeight = Height;
        }
        else
        {
            // If VerticalAlignment is Stretch, expand to available height
            if (VerticalAlignment == VerticalAlignment.Stretch && availableHeight < float.PositiveInfinity)
            {
                DesiredHeight = availableHeight;
            }
            else
            {
                DesiredHeight = defaultHeight;
            }
        }

        base.Measure(availableWidth, availableHeight);
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Cachear el renderer para mediciones precisas en selección con mouse
        _cachedRenderer = renderer;

        // Safety check: Ensure cursor and selection are within bounds before rendering
        // This prevents crashes if Text was modified externally without updating indices
        if (_cursorPosition > Text.Length) _cursorPosition = Text.Length;
        if (_cursorPosition < 0) _cursorPosition = 0;
        if (_selectionStart > Text.Length) _selectionStart = Text.Length;
        if (_selectionEnd > Text.Length) _selectionEnd = Text.Length;

        var bgColor = IsFocused ? FocusBackground : Background;
        var borderColor = IsFocused ? FocusBorderColor : BorderColor;

        // Fondo
        renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius.TopLeft, bgColor);

        // Borde
        if (BorderWidth > 0)
        {
            renderer.DrawRoundedRect(
                ComputedX + BorderWidth,
                ComputedY + BorderWidth,
                ComputedWidth - BorderWidth * 2,
                ComputedHeight - BorderWidth * 2,
                BorderRadius.TopLeft,
                bgColor
            );

            renderer.DrawRoundedRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius.TopLeft, borderColor);
            renderer.DrawRoundedRect(
                ComputedX + BorderWidth,
                ComputedY + BorderWidth,
                ComputedWidth - BorderWidth * 2,
                ComputedHeight - BorderWidth * 2,
                BorderRadius.TopLeft,
                bgColor
            );
        }

        // Área de contenido visible
        float contentX = ComputedX + Padding.Left + BorderWidth;
        float contentY = ComputedY + Padding.Top + BorderWidth;
        float contentWidth = ComputedWidth - Padding.Horizontal - BorderWidth * 2;
        float contentHeight = ComputedHeight - Padding.Vertical - BorderWidth * 2;

        // Habilitar scissor test para clipping
        renderer.PushScissor(contentX, contentY, contentWidth, contentHeight);

        // Update horizontal scroll so the cursor stays visible.
        // Must run before drawing text/selection/cursor so all use the updated offset.
        // (Word-wrapped Editor resets _scrollOffsetX = 0 and doesn't call base.Render, so no guard needed.)
        if (IsFocused)
        {
            UpdateScrollToCursorInternal(renderer, contentWidth);
        }

        try
        {
            // Dibujar selección de texto (si existe)
            if (HasSelection && IsFocused)
            {
                int selStart = Math.Min(_selectionStart, _selectionEnd);
                int selEnd = Math.Max(_selectionStart, _selectionEnd);

                if (IsMultiline)
                {
                    // Multiline selection rendering
                    float lineHeight = FontSize * 1.2f;

                    // Build a list of line start positions
                    var lineStarts = new List<int> { 0 };
                    for (int i = 0; i < Text.Length; i++)
                    {
                        if (Text[i] == '\n')
                        {
                            lineStarts.Add(i + 1);
                        }
                    }

                    // Find which lines contain the selection
                    int startLineIndex = 0;
                    int endLineIndex = 0;

                    for (int i = 0; i < lineStarts.Count; i++)
                    {
                        int lineStart = lineStarts[i];
                        int lineEnd = (i + 1 < lineStarts.Count) ? lineStarts[i + 1] - 1 : Text.Length;

                        if (selStart >= lineStart && selStart <= lineEnd)
                            startLineIndex = i;

                        if (selEnd >= lineStart && selEnd <= lineEnd)
                            endLineIndex = i;
                    }

                    // Draw selection rectangle for each line
                    for (int lineIdx = startLineIndex; lineIdx <= endLineIndex && lineIdx < lineStarts.Count; lineIdx++)
                    {
                        int lineStart = lineStarts[lineIdx];
                        int lineEnd = (lineIdx + 1 < lineStarts.Count) ? lineStarts[lineIdx + 1] - 1 : Text.Length;

                        // Determine selection start and end within this line
                        int selStartInLine = (lineIdx == startLineIndex) ? selStart : lineStart;
                        int selEndInLine = (lineIdx == endLineIndex) ? selEnd : lineEnd;

                        // Clamp to line bounds
                        selStartInLine = Math.Max(lineStart, Math.Min(selStartInLine, lineEnd));
                        selEndInLine = Math.Max(lineStart, Math.Min(selEndInLine, lineEnd));

                        // Skip if no selection on this line
                        if (selEndInLine <= selStartInLine) continue;

                        // Measure text before selection start in this line
                        string textBeforeSelInLine = Text.Substring(lineStart, selStartInLine - lineStart);
                        var sizeBeforeSel = renderer.MeasureText(textBeforeSelInLine, FontSize);

                        // Measure selected text in this line
                        string selectedTextInLine = Text.Substring(selStartInLine, selEndInLine - selStartInLine);
                        var selSize = renderer.MeasureText(selectedTextInLine, FontSize);

                        float selX = contentX + sizeBeforeSel.X - _scrollOffsetX;
                        float selY = contentY + (lineIdx * lineHeight) - _scrollOffsetY;
                        float selWidth = selSize.X;
                        float selHeight = lineHeight;

                        // Draw selection rectangle for this line
                        renderer.DrawRect(selX, selY, selWidth, selHeight, SelectionBackground);
                    }
                }
                else
                {
                    // Single-line selection rendering (original logic)
                    string textBeforeSelection = Text.Substring(0, selStart);
                    if (IsPassword && !string.IsNullOrEmpty(Text))
                    {
                        textBeforeSelection = new string('*', selStart);
                    }

                    string selectedText = Text.Substring(selStart, selEnd - selStart);
                    if (IsPassword && !string.IsNullOrEmpty(Text))
                    {
                        selectedText = new string('*', selEnd - selStart);
                    }

                    var sizeBeforeSelection = renderer.MeasureText(textBeforeSelection, FontSize);
                    var selectionSize = renderer.MeasureText(selectedText, FontSize);

                    float selectionX = contentX + sizeBeforeSelection.X - _scrollOffsetX;
                    float selectionY = contentY;
                    float selectionWidth = selectionSize.X;
                    float selectionHeight = contentHeight;

                    renderer.DrawRect(selectionX, selectionY, selectionWidth, selectionHeight, SelectionBackground);
                }
            }

            // Texto o placeholder
            string displayText = string.IsNullOrEmpty(Text) ? Placeholder : Text;
            Brush textColor = string.IsNullOrEmpty(Text) ? PlaceholderColor : TextColor;

            if (!string.IsNullOrEmpty(displayText))
            {
                if (IsPassword && !string.IsNullOrEmpty(Text))
                {
                    displayText = new string('*', Text.Length);
                }

                if (IsMultiline)
                {
                    // Multiline rendering - supports \n and \t escape characters
                    string[] lines = displayText.Split('\n');
                    float lineHeight = FontSize * 1.2f; // Simple line height calculation

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];

                        // Replace tabs with 4 spaces for rendering
                        string processedLine = line.Replace("\t", "    ");

                        // Skip lines outside visible area (simple culling)
                        float lineY = contentY + (i * lineHeight) - _scrollOffsetY;

                        if (lineY + lineHeight < contentY || lineY > contentY + contentHeight)
                            continue;

                        renderer.DrawText(processedLine, contentX - _scrollOffsetX, lineY, textColor, FontSize);
                    }
                }
                else
                {
                    // Single line rendering - supports \t escape character
                    // Replace tabs with 4 spaces
                    string processedText = displayText.Replace("\t", "    ");

                    // Medir la altura real del texto para centrarlo correctamente
                    var textSize = renderer.MeasureText(processedText, FontSize);

                    float textX = contentX - _scrollOffsetX;
                    // Centrar verticalmente en el área de contenido
                    float textY = contentY + (contentHeight - textSize.Y) / 2;

                    // Dibujar texto completo con scroll offset
                    renderer.DrawText(processedText, textX, textY, textColor, FontSize);
                }
            }

            // Cursor (si está enfocado y visible en el ciclo de parpadeo)
            if (IsFocused && IsCursorVisible())
            {
                // Calcular posición del cursor
                string textBeforeCursor = Text.Substring(0, _cursorPosition);

                float cursorX, cursorY, cursorHeight;

                if (IsMultiline)
                {
                    // Multiline cursor calculation
                    int lineIndex = 0;
                    int lastLineEnd = 0;

                    // Find which line the cursor is on
                    for (int i = 0; i < Text.Length; i++)
                    {
                        if (i == _cursorPosition) break;
                        if (Text[i] == '\n')
                        {
                            lineIndex++;
                            lastLineEnd = i + 1;
                        }
                    }

                    string currentLineText = "";
                    int cursorIndexInLine = _cursorPosition - lastLineEnd;

                    // Extract text of the current line up to cursor
                    int nextNewline = Text.IndexOf('\n', lastLineEnd);
                    if (nextNewline == -1) nextNewline = Text.Length;

                    int lineEnd = Math.Min(nextNewline, Text.Length);
                    if (lastLineEnd <= lineEnd)
                    {
                        // Get text of the line
                        string fullLine = Text.Substring(lastLineEnd, lineEnd - lastLineEnd);
                        // Get text up to cursor
                        if (cursorIndexInLine <= fullLine.Length)
                            currentLineText = fullLine.Substring(0, cursorIndexInLine);
                        else
                            currentLineText = fullLine; // Should not happen if logic is correct
                    }

                    float lineHeight = FontSize * 1.2f;
                    var textSize = renderer.MeasureText(currentLineText, FontSize);

                    cursorX = contentX + textSize.X - _scrollOffsetX;
                    cursorY = contentY + (lineIndex * lineHeight) - _scrollOffsetY;
                    cursorHeight = lineHeight;
                }
                else
                {
                    // Single line cursor calculation (existing logic)
                    if (IsPassword && !string.IsNullOrEmpty(Text))
                    {
                        textBeforeCursor = new string('*', _cursorPosition);
                    }

                    var textSize = renderer.MeasureText(textBeforeCursor, FontSize);
                    cursorX = contentX + textSize.X - _scrollOffsetX;
                    cursorY = contentY;
                    cursorHeight = contentHeight;
                }

                renderer.DrawRect(cursorX, cursorY, 2, cursorHeight, TextColor);
            }
        }
        finally
        {
            // Deshabilitar scissor test
            renderer.PopScissor();
        }
    }

    /// <summary>
    /// Implementación de IInputHandler para selección con mouse y eventos de teclado
    /// </summary>
    public void OnFocusGained()
    {
        IsFocused = true;
        ResetCursorBlink();
        FrameAnimationTicker.Register(this);
        MarkNeedsPaint();

        if (Rayo.Core.Platform.PlatformDetector.IsMobile)
        {
            Rayo.Core.Platform.VirtualKeyboardManager.Show();
        }
    }

    public void OnFocusLost()
    {
        IsFocused = false;
        FrameAnimationTicker.Unregister(this);
        ClearSelection();
        MarkNeedsPaint();

        if (Rayo.Core.Platform.PlatformDetector.IsMobile)
        {
            Rayo.Core.Platform.VirtualKeyboardManager.Hide();
        }
    }

    // =========================================================================
    // IFrameAnimation IMPLEMENTATION (cursor blink)
    // =========================================================================

    /// <summary>
    /// Called every frame while focused to update cursor blink state.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsFocused) return;

        // Check if cursor visibility should change
        var elapsed = (DateTime.UtcNow - _lastCursorActivityTime).TotalMilliseconds;
        int cycleCount = (int)(elapsed / CursorBlinkIntervalMs);
        bool shouldBeVisible = (cycleCount % 2) == 0;

        if (_cursorVisible != shouldBeVisible)
        {
            _cursorVisible = shouldBeVisible;
            MarkNeedsPaint();
        }
    }

    public virtual bool HandleInput(InputEventArgs args)
    {
        switch (args.EventType)
        {
            case InputEventType.MouseDown:
                // Detectar doble click
                var now = DateTime.UtcNow;
                var timeSinceLastClick = (now - _lastClickTime).TotalMilliseconds;

                if (timeSinceLastClick <= DoubleClickThresholdMs)
                {
                    // Doble click detectado - seleccionar todo
                    SelectAll();
                    _isMouseSelecting = false;
                    _lastClickTime = DateTime.MinValue;
                    MarkNeedsPaint();
                    return true;
                }

                // Click simple - iniciar selección con mouse
                _lastClickTime = now;
                _isMouseSelecting = true;
                int clickPosition = GetCursorPositionFromMouse(args.Position.X, args.Position.Y);
                _cursorPosition = clickPosition;
                _mouseSelectionStart = clickPosition;
                _selectionStart = clickPosition;
                _selectionEnd = clickPosition;
                ResetCursorBlink();
                MarkNeedsPaint();
                return true;

            case InputEventType.MouseDrag:
                if (_isMouseSelecting)
                {
                    // Actualizar selección mientras se arrastra
                    int dragPosition = GetCursorPositionFromMouse(args.Position.X, args.Position.Y);
                    _cursorPosition = dragPosition;
                    _selectionStart = _mouseSelectionStart;
                    _selectionEnd = dragPosition;
                    UpdateScrollToCursor();
                    MarkNeedsPaint();
                    return true;
                }
                return false;

            case InputEventType.MouseUp:
                if (_isMouseSelecting)
                {
                    _isMouseSelecting = false;
                    return true;
                }
                return false;

            // ✅ NUEVO: Manejo de eventos de teclado
            case InputEventType.KeyDown:
                return HandleKeyDown(args);

            case InputEventType.KeyRepeat:
                return HandleKeyRepeat(args);

            case InputEventType.TextInput:
                if (args.Character.HasValue)
                {
                    char c = args.Character.Value;
                    // Permitir todos los caracteres imprimibles excepto algunos especiales
                    if (!char.IsControl(c) || c == ' ')  // Permitir espacio aunque sea whitespace
                    {
                        InsertChar(c);
                        return true;
                    }
                }
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Maneja eventos KeyDown
    /// </summary>
    private bool HandleKeyDown(InputEventArgs args)
    {
        if (!args.KeyCode.HasValue) return false;

        var key = args.KeyCode.Value;
        bool isShift = args.IsShiftPressed;
        bool isCtrl = args.IsControlPressed;

        // Atajos de clipboard con Ctrl
        if (isCtrl)
        {
            switch (key)
            {
                case InputKey.A:
                    SelectAll();
                    return true;

                case InputKey.C:
                    Copy();
                    return false; // No causa render

                case InputKey.X:
                    Cut();
                    return true;

                case InputKey.V:
                    Paste();
                    return true;
            }
        }

        // Teclas de navegación y edición
        switch (key)
        {
            case InputKey.Backspace:
                DeleteChar();
                return true;

            case InputKey.Delete:
                DeleteCharForward();
                return true;

            case InputKey.Left:
                MoveCursorLeft(isShift);
                return true;

            case InputKey.Right:
                MoveCursorRight(isShift);
                return true;

            case InputKey.Up:
                if (IsMultiline)
                {
                    MoveCursorUp(isShift);
                    return true;
                }
                return false;

            case InputKey.Down:
                if (IsMultiline)
                {
                    MoveCursorDown(isShift);
                    return true;
                }
                return false;

            case InputKey.PageUp:
                if (IsMultiline)
                {
                    MoveCursorPageUp(isShift);
                    return true;
                }
                return false;

            case InputKey.PageDown:
                if (IsMultiline)
                {
                    MoveCursorPageDown(isShift);
                    return true;
                }
                return false;

            case InputKey.Home:
                MoveCursorToStart(isShift);
                return true;

            case InputKey.End:
                MoveCursorToEnd(isShift);
                return true;

            case InputKey.Return:
                if (IsMultiline)
                {
                    InsertChar('\n');
                    return true;
                }
                InvokeEnter();
                return true;

            // ✅ Fix: Mark alphanumeric keys as handled to prevent system beep
            case InputKey.Space:
            case InputKey.A: case InputKey.B: case InputKey.C: case InputKey.D: case InputKey.E:
            case InputKey.F: case InputKey.G: case InputKey.H: case InputKey.I: case InputKey.J:
            case InputKey.K: case InputKey.L: case InputKey.M: case InputKey.N: case InputKey.O:
            case InputKey.P: case InputKey.Q: case InputKey.R: case InputKey.S: case InputKey.T:
            case InputKey.U: case InputKey.V: case InputKey.W: case InputKey.X: case InputKey.Y:
            case InputKey.Z:
            case InputKey.Number0: case InputKey.Number1: case InputKey.Number2: case InputKey.Number3:
            case InputKey.Number4: case InputKey.Number5: case InputKey.Number6: case InputKey.Number7:
            case InputKey.Number8: case InputKey.Number9:
                // Only mark as handled if no modifiers (Ctrl/Alt) are pressed
                return !isCtrl && !args.IsAltPressed;
        }

        return false;
    }

    /// <summary>
    /// Maneja repetición automática de teclas
    /// </summary>
    private bool HandleKeyRepeat(InputEventArgs args)
    {
        if (!args.KeyCode.HasValue) return false;

        var key = args.KeyCode.Value;
        bool isShift = args.IsShiftPressed;

        switch (key)
        {
            case InputKey.Backspace:
                DeleteChar();
                return true;

            case InputKey.Delete:
                DeleteCharForward();
                return true;

            case InputKey.Left:
                MoveCursorLeft(isShift);
                return true;

            case InputKey.Right:
                MoveCursorRight(isShift);
                return true;

            case InputKey.Up:
                if (IsMultiline)
                {
                    MoveCursorUp(isShift);
                    return true;
                }
                return false;

            case InputKey.Down:
                if (IsMultiline)
                {
                    MoveCursorDown(isShift);
                    return true;
                }
                return false;

            case InputKey.PageUp:
                if (IsMultiline)
                {
                    MoveCursorPageUp(isShift);
                    return true;
                }
                return false;

            case InputKey.PageDown:
                if (IsMultiline)
                {
                    MoveCursorPageDown(isShift);
                    return true;
                }
                return false;

            case InputKey.Home:
                MoveCursorToStart(isShift);
                return true;

            case InputKey.End:
                MoveCursorToEnd(isShift);
                return true;

            case InputKey.Return:
                if (IsMultiline)
                {
                    InsertChar('\n');
                    return true;
                }
                return false;
        }

        return false;
    }

    /// <summary>
    /// Calcula la posición del cursor en el texto basado en la coordenada X del mouse
    /// </summary>
    protected virtual int GetCursorPositionFromMouse(float mouseX, float mouseY)
    {
        // Si el texto está vacío, retornar 0
        if (string.IsNullOrEmpty(Text))
        {
            return 0;
        }

        // Calcular el área de contenido
        float contentX = ComputedX + Padding.Left + BorderWidth;
        float contentY = ComputedY + Padding.Top + BorderWidth;

        if (IsMultiline)
        {
            // Multiline: determinar línea y posición dentro de la línea
            float lineHeight = FontSize * 1.2f;
            float localY = mouseY - contentY + _scrollOffsetY;

            // Determinar qué línea se clickeó
            int clickedLine = Math.Max(0, (int)(localY / lineHeight));

            // Dividir texto en líneas
            string[] lines = Text.Split('\n');

            // Clamp a la última línea
            clickedLine = Math.Min(clickedLine, lines.Length - 1);

            // Encontrar la posición del inicio de la línea clickeada en el texto completo
            int lineStartPos = 0;
            for (int i = 0; i < clickedLine; i++)
            {
                lineStartPos += lines[i].Length + 1; // +1 por el \n
            }

            // Obtener el texto de la línea clickeada
            string lineText = lines[clickedLine];

            // Calcular posición X local dentro de la línea
            float localX = mouseX - contentX + _scrollOffsetX;

            // Si está antes del texto de la línea, cursor al inicio de la línea
            if (localX <= 0)
            {
                return lineStartPos;
            }

            // Si está después del texto de la línea, cursor al final de la línea
            float lineWidth = MeasureTextWidth(lineText);
            if (localX >= lineWidth)
            {
                return lineStartPos + lineText.Length;
            }

            // Buscar posición dentro de la línea
            for (int i = 0; i <= lineText.Length; i++)
            {
                float widthUpTo = MeasureTextWidth(lineText.Substring(0, i));

                if (i == lineText.Length)
                {
                    return lineStartPos + i;
                }

                float widthUpToNext = MeasureTextWidth(lineText.Substring(0, i + 1));

                if (localX >= widthUpTo && localX < widthUpToNext)
                {
                    float midpoint = (widthUpTo + widthUpToNext) / 2;
                    return lineStartPos + (localX < midpoint ? i : i + 1);
                }
            }

            return lineStartPos + lineText.Length;
        }
        else
        {
            // Single line (lógica original)
            float localX = mouseX - contentX + _scrollOffsetX;

            if (localX <= 0)
            {
                return 0;
            }

            float totalWidth = MeasureTextWidth(Text);
            if (localX >= totalWidth)
            {
                return Text.Length;
            }

            int textLength = Text.Length;
            for (int i = 0; i <= textLength; i++)
            {
                if (i < 0 || i > textLength)
                    continue;

                float widthUpTo = MeasureTextWidth(Text.Substring(0, i));

                if (i == textLength)
                {
                    return i;
                }

                float widthUpToNext = MeasureTextWidth(Text.Substring(0, i + 1));

                if (localX >= widthUpTo && localX < widthUpToNext)
                {
                    float midpoint = (widthUpTo + widthUpToNext) / 2;
                    return localX < midpoint ? i : i + 1;
                }
            }

            return textLength;
        }
    }

    /// <summary>
    /// Mide el ancho del texto (usa renderer cacheado si está disponible)
    /// </summary>
    protected float MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Si tenemos renderer cacheado, usar medición precisa
        if (_cachedRenderer != null)
        {
            string displayText = text;
            if (IsPassword)
            {
                displayText = new string('*', text.Length);
            }

            var size = _cachedRenderer.MeasureText(displayText, FontSize);
            return size.X;
        }

        // Fallback: aproximación con ancho promedio de carácter
        // Usar 8 pixels por carácter como estimación
        return text.Length * (FontSize * 0.6f);
    }

    /// <summary>
    /// Actualiza el scroll interno durante el render (solo si es necesario)
    /// </summary>
    private void UpdateScrollToCursorInternal(IRenderer renderer, float visibleWidth)
    {
        int safeCursorPos = Math.Clamp(_cursorPosition, 0, Text.Length);

        string textBeforeCursor;
        if (IsMultiline)
        {
            // For multiline, measure only the text on the current line up to the cursor
            // so the width reflects the cursor's X position within its line.
            int lineStart = 0;
            for (int i = 0; i < safeCursorPos; i++)
            {
                if (Text[i] == '\n') lineStart = i + 1;
            }
            textBeforeCursor = Text.Substring(lineStart, safeCursorPos - lineStart);
        }
        else
        {
            textBeforeCursor = Text.Substring(0, safeCursorPos);
            if (IsPassword && !string.IsNullOrEmpty(Text))
                textBeforeCursor = new string('*', safeCursorPos);
        }

        var textSize = renderer.MeasureText(textBeforeCursor, FontSize);
        float cursorLocalX = textSize.X - _scrollOffsetX;

        // Cursor is right of the visible area — scroll right
        if (cursorLocalX > visibleWidth - 10)
        {
            _scrollOffsetX = textSize.X - visibleWidth + 10;
        }
        // Cursor is left of the visible area — scroll left
        else if (cursorLocalX < 10)
        {
            _scrollOffsetX = Math.Max(0, textSize.X - 10);
        }
    }
}
