namespace Rayo.Controls;

using System;
using System.Threading;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Rendering.Graphics.VectorGraphics;
using Rayo.Styling;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Text input field with support for single-line and multi-line text editing.
/// </summary>
public abstract class TextBox<T> : BorderView<T>, IInputHandler, IFocusable, Rayo.Core.Platform.IVirtualKeyboardOptions where T : BorderView<T>
{
    // =========================================================================
    // INTERFACE IMPLEMENTATIONS
    // =========================================================================

    // IInputHandler (virtual to allow override in derived classes like Editor)
    public virtual bool CanHandleInput => true;

    // IFocusable � set by EventManager; [NotFluent] suppresses builder generation,
    // [PaintProperty] documents that focus changes require a repaint.
    [NotFluent, PaintProperty]
    public bool IsFocused
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    // IVirtualKeyboardOptions � controls on-screen keyboard type on mobile platforms.
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
    /// Does not use SetProperty � marks paint explicitly after side effects.
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
            InvalidateMultilineCache();

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
    } = Color.Transparent;
    #endregion

    #region FocusBackground
    [PaintProperty]
    public Brush FocusBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region TextColor
    [PaintProperty]
    public Brush TextColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region PlaceholderColor
    [PaintProperty]
    public Brush PlaceholderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region FocusBorderBrush
    [PaintProperty]
    public Brush FocusBorderBrush
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region IsPassword
    [PaintProperty]
    public bool IsPassword
    {
        get => field;
        set => this.SetProperty(ref field, value, InvalidateMultilineCache);
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

    #region TextHorizontalAlignment
    /// <summary>
    /// Horizontal alignment used by single-line text, selection, and cursor rendering.
    /// Multiline editors continue to use left alignment.
    /// </summary>
    [PaintProperty]
    public HorizontalAlignment TextHorizontalAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = HorizontalAlignment.Left;
    #endregion

    #region SelectionBackground
    [PaintProperty]
    public Brush SelectionBackground
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
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

    // Sistema de selecci�n de texto
    protected int _selectionStart = 0;  // Inicio de la selecci�n
    protected int _selectionEnd = 0;    // Fin de la selecci�n

    // Estado para selecci�n con mouse
    private bool _isMouseSelecting = false;

    private int _mouseSelectionStart = 0;
    protected IRenderer? _cachedRenderer = null;  // Para mediciones precisas

    // Estado para doble click
    private DateTime _lastClickTime = DateTime.MinValue;
    private const double DoubleClickThresholdMs = 500;  // 500ms para detectar doble click

    // Cursor blink state
    private DateTime _lastCursorActivityTime = DateTime.UtcNow;
    protected bool _cursorVisible = true;
    private const int CursorBlinkIntervalMs = 530;  // Standard Windows caret blink rate
    private const float CaretWidth = 2f;
    private readonly object _cursorBlinkTimerLock = new();
    private Timer? _cursorBlinkTimer;
    private volatile bool _cursorBlinkActive;
    private readonly List<TextLineInfo> _multilineLines = new();
    private bool _multilineLinesDirty = true;
    private string _lastMultilineText = string.Empty;
    private bool _lastMultilinePasswordMode;
    private float _multilineContentWidth;
    private readonly float[] _emptyPrefixWidths = new float[] { 0 };
    private float[] _singleLinePrefixWidths = new float[] { 0 };
    private float _singleLineWidth;
    private string _lastSingleLineText = string.Empty;
    private bool _lastSingleLinePasswordMode;
    private float _lastSingleLineFontSize = -1;
    private IRenderer? _lastSingleLineRenderer;
    private readonly Dictionary<string, float> _measureWidthCache = new(StringComparer.Ordinal);
    private IRenderer? _measureWidthCacheRenderer;
    private float _measureWidthCacheFontSize = -1;
    private bool _measureWidthCachePasswordMode;
    private const int MeasureWidthCacheLimit = 256;

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
        InitializeTheme();
        // Remove hardcoded size - let it size dynamically
        Padding = new Thickness(10, 6, 10, 6);
        BorderThickness = 2;
        BorderRadius = new CornerRadius(4);
    }

    protected override void OnThemeApplied(Theme theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(Background), (Brush)palette.Surface, value => Background = value);
        SetThemeValue(nameof(FocusBackground), (Brush)palette.SurfaceHover, value => FocusBackground = value);
        SetThemeValue(nameof(TextColor), (Brush)palette.OnSurface, value => TextColor = value);
        SetThemeValue(nameof(PlaceholderColor), (Brush)palette.OnDisabled, value => PlaceholderColor = value);
        SetThemeValue(nameof(FocusBorderBrush), (Brush)palette.Focus, value => FocusBorderBrush = value);
        SetThemeValue(
            nameof(SelectionBackground),
            (Brush)GetSelectionBackground(theme),
            value => SelectionBackground = value);
        SetThemeValue(nameof(BorderBrush), (Brush)palette.Border, value => BorderBrush = value);
    }

    private static Color GetSelectionBackground(Theme theme)
    {
        var palette = theme.Colors;
        return theme == RayoThemes.Dark
            ? palette.Primary.WithAlpha(0.42f)
            : palette.Primary.WithAlpha(0.20f);
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
        bool wasVisible = _cursorVisible;
        _cursorVisible = true;

        if (IsFocused)
        {
            _cursorBlinkActive = true;
            RestartCursorBlinkTimer(CursorBlinkIntervalMs);
            if (!wasVisible)
            {
                MarkNeedsPaint();
            }
        }
        else
        {
            _cursorBlinkActive = false;
            StopCursorBlinkTimer(dispose: false);
        }
    }

    private void RestartCursorBlinkTimer(int dueTimeMs)
    {
        if (!IsFocused || !_cursorBlinkActive)
        {
            return;
        }

        lock (_cursorBlinkTimerLock)
        {
            _cursorBlinkTimer ??= new Timer(OnCursorBlinkTimer);
            _cursorBlinkTimer.Change(Math.Max(1, dueTimeMs), Timeout.Infinite);
        }
    }

    private void StopCursorBlinkTimer(bool dispose)
    {
        Timer? timerToDispose = null;

        lock (_cursorBlinkTimerLock)
        {
            if (_cursorBlinkTimer == null)
            {
                return;
            }

            _cursorBlinkTimer.Change(Timeout.Infinite, Timeout.Infinite);
            if (dispose)
            {
                timerToDispose = _cursorBlinkTimer;
                _cursorBlinkTimer = null;
            }
        }

        timerToDispose?.Dispose();
    }

    private void OnCursorBlinkTimer(object? state)
    {
        var app = UIApplication.Current;
        if (app != null)
        {
            app.RunOnMainThread(UpdateCursorBlink);
        }
        else
        {
            UpdateCursorBlink();
        }
    }

    private void UpdateCursorBlink()
    {
        if (!IsFocused || !_cursorBlinkActive)
        {
            StopCursorBlinkTimer(dispose: false);
            return;
        }

        double elapsed = (DateTime.UtcNow - _lastCursorActivityTime).TotalMilliseconds;
        int cycleCount = (int)(elapsed / CursorBlinkIntervalMs);
        bool shouldBeVisible = (cycleCount % 2) == 0;

        if (_cursorVisible != shouldBeVisible)
        {
            _cursorVisible = shouldBeVisible;
            MarkNeedsPaint();
        }

        int nextDelay = CursorBlinkIntervalMs - (int)(elapsed % CursorBlinkIntervalMs);
        RestartCursorBlinkTimer(nextDelay);
    }

    private void InvalidateMultilineCache()
    {
        _multilineLinesDirty = true;
        _lastMultilineText = string.Empty;
        _multilineContentWidth = 0;
        _lastSingleLineText = string.Empty;
    }

    protected override void OnUnmounted()
    {
        _cursorBlinkActive = false;
        StopCursorBlinkTimer(dispose: true);
        base.OnUnmounted();
    }

    private void EnsureMultilineCache()
    {
        if (!_multilineLinesDirty &&
            ReferenceEquals(_lastMultilineText, Text) &&
            _lastMultilinePasswordMode == IsPassword)
        {
            return;
        }

        _multilineLines.Clear();
        _multilineContentWidth = 0;

        var text = Text;
        int lineStart = 0;
        int lineIndex = 0;

        for (int i = 0; i <= text.Length; i++)
        {
            if (i != text.Length && text[i] != '\n')
                continue;

            int lineLength = i - lineStart;
            string displayLine;

            if (lineLength <= 0)
            {
                displayLine = string.Empty;
            }
            else if (IsPassword)
            {
                displayLine = new string('*', lineLength);
            }
            else
            {
                displayLine = text.Substring(lineStart, lineLength).Replace("\t", "    ");
            }

            float[] prefixWidths = BuildPrefixWidths(text, lineStart, lineLength, IsPassword);
            float lineWidth = prefixWidths[lineLength];
            if (lineWidth > _multilineContentWidth)
            {
                _multilineContentWidth = lineWidth;
            }

            _multilineLines.Add(new TextLineInfo(lineStart, lineLength, displayLine, lineIndex, prefixWidths, lineWidth));
            lineStart = i + 1;
            lineIndex++;
        }

        if (_multilineLines.Count == 0)
        {
            _multilineLines.Add(new TextLineInfo(0, 0, string.Empty, 0, _emptyPrefixWidths, 0));
        }

        _multilineLinesDirty = false;
        _lastMultilineText = text;
        _lastMultilinePasswordMode = IsPassword;
    }

    private void EnsureSingleLinePrefixCache()
    {
        if (ReferenceEquals(_lastSingleLineText, Text) &&
            _lastSingleLinePasswordMode == IsPassword &&
            Math.Abs(_lastSingleLineFontSize - FontSize) < 0.01f &&
            ReferenceEquals(_lastSingleLineRenderer, _cachedRenderer))
        {
            return;
        }

        _singleLinePrefixWidths = BuildPrefixWidths(Text, 0, Text.Length, IsPassword);
        _singleLineWidth = _singleLinePrefixWidths[Text.Length];
        _lastSingleLineText = Text;
        _lastSingleLinePasswordMode = IsPassword;
        _lastSingleLineFontSize = FontSize;
        _lastSingleLineRenderer = _cachedRenderer;
    }

    private float[] BuildPrefixWidths(string sourceText, int start, int length, bool passwordMode)
    {
        float[] prefixWidths = new float[length + 1];

        for (int i = 1; i <= length; i++)
        {
            string prefixText = passwordMode
                ? new string('*', i)
                : sourceText.Substring(start, i).Replace("\t", "    ");
            prefixWidths[i] = MeasureTextWidth(prefixText);
        }

        return prefixWidths;
    }

    private static float GetPrefixWidth(in TextLineInfo lineInfo, int charOffset)
    {
        if (charOffset <= 0)
        {
            return 0;
        }

        if (charOffset >= lineInfo.PrefixWidths.Length)
        {
            return lineInfo.Width;
        }

        return lineInfo.PrefixWidths[charOffset];
    }

    private float GetSingleLinePrefixWidth(int charOffset)
    {
        EnsureSingleLinePrefixCache();

        if (charOffset <= 0)
        {
            return 0;
        }

        if (charOffset >= _singleLinePrefixWidths.Length)
        {
            return _singleLineWidth;
        }

        return _singleLinePrefixWidths[charOffset];
    }

    private TextLineInfo GetLineInfoForCursorPosition(int cursorPosition)
    {
        EnsureMultilineCache();

        int safeCursorPos = Math.Clamp(cursorPosition, 0, Text.Length);
        for (int i = 0; i < _multilineLines.Count; i++)
        {
            var line = _multilineLines[i];
            if (safeCursorPos >= line.Start && safeCursorPos <= line.Start + line.Length)
                return line;
        }

        return _multilineLines[_multilineLines.Count - 1];
    }

    protected float GetMultilineContentWidth()
    {
        EnsureMultilineCache();
        return _multilineContentWidth;
    }

    protected float GetCursorOffsetWithinLine(int cursorPosition)
    {
        int safeCursorPos = Math.Clamp(cursorPosition, 0, Text.Length);

        if (IsMultiline)
        {
            var lineInfo = GetLineInfoForCursorPosition(safeCursorPos);
            int cursorIndexInLine = Math.Clamp(safeCursorPos - lineInfo.Start, 0, lineInfo.Length);
            return GetPrefixWidth(lineInfo, cursorIndexInLine);
        }

        return GetSingleLinePrefixWidth(safeCursorPos);
    }

    /// <summary>
    /// Returns true if cursor should be visible based on current blink state.
    /// </summary>
    private bool IsCursorVisible()
    {
        return _cursorVisible;
    }

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

        // Si hay selecci�n, eliminarla primero
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

        // Si hay selecci�n, eliminarla
        if (HasSelection)
        {
            DeleteSelection();
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();
            return;
        }

        // Si no hay selecci�n, borrar car�cter anterior
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
    /// Elimina el car�cter a la derecha del cursor (tecla Delete)
    /// </summary>
    public void DeleteCharForward()
    {
        // Check read-only mode
        if (IsReadOnly) return;

        // Si hay selecci�n, eliminarla
        if (HasSelection)
        {
            DeleteSelection();
            ResetCursorBlink();
            UpdateScrollToCursor();
            MarkNeedsPaint();
            return;
        }

        // Si no hay selecci�n, borrar car�cter siguiente
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
            // Si no hay selecci�n, iniciar desde la posici�n actual
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = Math.Max(0, _cursorPosition - 1);
            _selectionEnd = _cursorPosition;
        }
        else
        {
            // Si hay selecci�n, mover al inicio de la selecci�n
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
            // Si no hay selecci�n, iniciar desde la posici�n actual
            if (!HasSelection)
            {
                _selectionStart = _cursorPosition;
            }
            _cursorPosition = Math.Min(Text.Length, _cursorPosition + 1);
            _selectionEnd = _cursorPosition;
        }
        else
        {
            // Si hay selecci�n, mover al final de la selecci�n
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
    /// Mueve el cursor una l�nea hacia arriba (solo para multiline)
    /// </summary>
    public virtual void MoveCursorUp(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        EnsureMultilineCache();
        var currentLine = GetLineInfoForCursorPosition(_cursorPosition);
        int currentLineIndex = currentLine.LineIndex;
        int charIndexInLine = _cursorPosition - currentLine.Start;

        // Si estamos en la primera l�nea, no podemos subir m�s
        if (currentLineIndex == 0)
        {
            MoveCursorToStart(shiftPressed);
            return;
        }

        // Calcular nueva posici�n en la l�nea anterior
        var previousLine = _multilineLines[currentLineIndex - 1];
        
        // Mover al mismo �ndice de car�cter o al final de la l�nea anterior (lo que sea menor)
        int newCharIndex = Math.Min(charIndexInLine, previousLine.Length);
        int newCursorPos = previousLine.Start + newCharIndex;

        // Manejar selecci�n si Shift est� presionado
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
    /// Mueve el cursor una l�nea hacia abajo (solo para multiline)
    /// </summary>
    public virtual void MoveCursorDown(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        EnsureMultilineCache();
        var currentLine = GetLineInfoForCursorPosition(_cursorPosition);
        int currentLineIndex = currentLine.LineIndex;
        int charIndexInLine = _cursorPosition - currentLine.Start;

        // Si estamos en la �ltima l�nea, no podemos bajar m�s
        if (currentLineIndex >= _multilineLines.Count - 1)
        {
            MoveCursorToEnd(shiftPressed);
            return;
        }

        // Calcular nueva posici�n en la l�nea siguiente
        var nextLine = _multilineLines[currentLineIndex + 1];
        
        // Mover al mismo �ndice de car�cter o al final de la l�nea siguiente (lo que sea menor)
        int newCharIndex = Math.Min(charIndexInLine, nextLine.Length);
        int newCursorPos = nextLine.Start + newCharIndex;

        // Manejar selecci�n si Shift est� presionado
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
    /// Limpia la selecci�n actual
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

            // Si hay selecci�n, reemplazarla
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
    /// Mueve el cursor una p�gina hacia arriba (multiline - aprox. 10 l�neas)
    /// </summary>
    public void MoveCursorPageUp(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        // Dividir texto en l�neas
        string[] lines = Text.Split('\n');
        
        // Encontrar l�nea actual
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

        // Calcular l�nea de destino (10 l�neas hacia arriba, o primera l�nea)
        int targetLineIndex = Math.Max(0, currentLineIndex - 10);
        
        // Calcular nueva posici�n absoluta
        int newCursorPos = 0;
        for (int i = 0; i < targetLineIndex; i++)
        {
            newCursorPos += lines[i].Length + 1; // +1 por el '\n'
        }

        // Manejar selecci�n
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
    /// Mueve el cursor una p�gina hacia abajo (multiline - aprox. 10 l�neas)
    /// </summary>
    public void MoveCursorPageDown(bool shiftPressed = false)
    {
        if (!IsMultiline) return;

        // Dividir texto en l�neas
        string[] lines = Text.Split('\n');
        
        // Encontrar l�nea actual
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

        // Calcular l�nea de destino (10 l�neas hacia abajo, o �ltima l�nea)
        int targetLineIndex = Math.Min(lines.Length - 1, currentLineIndex + 10);
        
        // Calcular nueva posici�n absoluta (al final de la l�nea de destino)
        int newCursorPos = 0;
        for (int i = 0; i < targetLineIndex; i++)
        {
            newCursorPos += lines[i].Length + 1; // +1 por el '\n'
        }
        newCursorPos += lines[targetLineIndex].Length; // Al final de la l�nea

        // Manejar selecci�n
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
        // Medir el texto hasta el cursor (cachear renderer ser�a ideal, pero por ahora usar lazy)
        // Esta funci�n se llama solo cuando el usuario mueve el cursor, no en cada render
    }

    protected override void Measure(float availableWidth, float availableHeight)
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

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Cachear el renderer para mediciones precisas en selecci�n con mouse
        _cachedRenderer = renderer;

        // Safety check: Ensure cursor and selection are within bounds before rendering
        // This prevents crashes if Text was modified externally without updating indices
        if (_cursorPosition > Text.Length) _cursorPosition = Text.Length;
        if (_cursorPosition < 0) _cursorPosition = 0;
        if (_selectionStart > Text.Length) _selectionStart = Text.Length;
        if (_selectionEnd > Text.Length) _selectionEnd = Text.Length;

        var bgColor = IsFocused ? FocusBackground : Background;
        var borderColor = IsFocused ? FocusBorderBrush : BorderBrush;

        // Fondo y borde con radios independientes por esquina.
        if (BorderThickness.Left > 0)
        {
            renderer.DrawPath(CreateRoundedRectPath(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius), borderColor);

            var innerRadius = new CornerRadius(
                MathF.Max(0, BorderRadius.TopLeft - BorderThickness.Left),
                MathF.Max(0, BorderRadius.TopRight - BorderThickness.Right),
                MathF.Max(0, BorderRadius.BottomRight - BorderThickness.Right),
                MathF.Max(0, BorderRadius.BottomLeft - BorderThickness.Left));

            renderer.DrawPath(
                CreateRoundedRectPath(
                    ComputedX + BorderThickness.Left,
                    ComputedY + BorderThickness.Top,
                    MathF.Max(0, ComputedWidth - BorderThickness.Horizontal),
                    MathF.Max(0, ComputedHeight - BorderThickness.Vertical),
                    innerRadius),
                bgColor);
        }
        else
        {
            renderer.DrawPath(CreateRoundedRectPath(ComputedX, ComputedY, ComputedWidth, ComputedHeight, BorderRadius), bgColor);
        }

        // �rea de contenido visible
        float contentX = ComputedX + Padding.Left + BorderThickness.Left;
        float contentY = ComputedY + Padding.Top + BorderThickness.Top;
        float contentWidth = ComputedWidth - Padding.Horizontal - BorderThickness.Horizontal;
        float contentHeight = ComputedHeight - Padding.Vertical - BorderThickness.Vertical;

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
            // Dibujar selecci�n de texto (si existe)
            if (HasSelection && IsFocused)
            {
                int selStart = Math.Min(_selectionStart, _selectionEnd);
                int selEnd = Math.Max(_selectionStart, _selectionEnd);

                if (IsMultiline)
                {
                    // Multiline selection rendering
                    EnsureMultilineCache();
                    float lineHeight = FontSize * 1.2f;

                    // Find which lines contain the selection
                    int startLineIndex = 0;
                    int endLineIndex = 0;

                    for (int i = 0; i < _multilineLines.Count; i++)
                    {
                        int lineStart = _multilineLines[i].Start;
                        int lineEnd = lineStart + _multilineLines[i].Length;

                        if (selStart >= lineStart && selStart <= lineEnd)
                            startLineIndex = i;

                        if (selEnd >= lineStart && selEnd <= lineEnd)
                            endLineIndex = i;
                    }

                    // Draw selection rectangle for each line
                    for (int lineIdx = startLineIndex; lineIdx <= endLineIndex && lineIdx < _multilineLines.Count; lineIdx++)
                    {
                        var lineInfo = _multilineLines[lineIdx];
                        int lineStart = lineInfo.Start;
                        int lineEnd = lineStart + lineInfo.Length;

                        // Determine selection start and end within this line
                        int selStartInLine = (lineIdx == startLineIndex) ? selStart : lineStart;
                        int selEndInLine = (lineIdx == endLineIndex) ? selEnd : lineEnd;

                        // Clamp to line bounds
                        selStartInLine = Math.Max(lineStart, Math.Min(selStartInLine, lineEnd));
                        selEndInLine = Math.Max(lineStart, Math.Min(selEndInLine, lineEnd));

                        // Skip if no selection on this line
                        if (selEndInLine <= selStartInLine) continue;

                        // Measure text before selection start in this line
                        int selectionStartOffset = selStartInLine - lineStart;
                        int selectionEndOffset = selEndInLine - lineStart;
                        float selStartX = GetPrefixWidth(lineInfo, selectionStartOffset);
                        float selWidth = GetPrefixWidth(lineInfo, selectionEndOffset) - selStartX;

                        float selX = contentX + selStartX - _scrollOffsetX;
                        float selY = contentY + (lineIdx * lineHeight) - _scrollOffsetY;
                        float selHeight = lineHeight;

                        // Draw selection rectangle for this line
                        renderer.DrawRect(selX, selY, selWidth, selHeight, SelectionBackground);
                    }
                }
                else
                {
                    // Single-line selection rendering (original logic)
                    float selectionStartX = GetSingleLinePrefixWidth(selStart);
                    float selectionWidth = GetSingleLinePrefixWidth(selEnd) - selectionStartX;
                    float alignmentOffset = GetSingleLineAlignmentOffset(contentWidth);
                    float selectionX = contentX + alignmentOffset + selectionStartX - _scrollOffsetX;
                    float selectionY = contentY;
                    float selectionHeight = contentHeight;

                    renderer.DrawRect(selectionX, selectionY, selectionWidth, selectionHeight, SelectionBackground);
                }
            }

            // Texto o placeholder
            bool showPlaceholder = string.IsNullOrEmpty(Text);
            string displayText = showPlaceholder
                ? Placeholder
                : IsPassword
                    ? new string('*', Text.Length)
                    : Text;
            Brush textColor = showPlaceholder ? PlaceholderColor : TextColor;

            if (!string.IsNullOrEmpty(displayText))
            {
                if (IsMultiline)
                {
                    EnsureMultilineCache();
                    float lineHeight = FontSize * 1.2f; // Simple line height calculation
                    int firstVisibleLine = Math.Max(0, (int)MathF.Floor(_scrollOffsetY / lineHeight));
                    int lastVisibleLine = Math.Min(_multilineLines.Count - 1, (int)MathF.Ceiling((_scrollOffsetY + contentHeight) / lineHeight));

                    for (int i = firstVisibleLine; i <= lastVisibleLine; i++)
                    {
                        string processedLine = _multilineLines[i].DisplayText;

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

                    float alignmentOffset = GetHorizontalAlignmentOffset(contentWidth, textSize.X);
                    float textX = contentX + alignmentOffset - _scrollOffsetX;
                    // Centrar verticalmente en el �rea de contenido
                    float textY = contentY + (contentHeight - textSize.Y) / 2;

                    // Dibujar texto completo con scroll offset
                    renderer.DrawText(processedText, textX, textY, textColor, FontSize);
                }
            }

            // Cursor (si est� enfocado y visible en el ciclo de parpadeo)
            if (IsFocused && IsCursorVisible())
            {
                // Calcular posici�n del cursor
                float cursorX, cursorY, cursorHeight;

                if (IsMultiline)
                {
                    var currentLine = GetLineInfoForCursorPosition(_cursorPosition);
                    int lineIndex = currentLine.LineIndex;
                    float lineHeight = FontSize * 1.2f;
                    cursorX = contentX + GetCursorOffsetWithinLine(_cursorPosition) - _scrollOffsetX;
                    cursorY = contentY + (lineIndex * lineHeight) - _scrollOffsetY;
                    cursorHeight = lineHeight;
                }
                else
                {
                    float alignmentOffset = GetSingleLineAlignmentOffset(contentWidth);
                    cursorX = contentX + alignmentOffset + GetCursorOffsetWithinLine(_cursorPosition) - _scrollOffsetX;
                    cursorY = contentY;
                    cursorHeight = contentHeight;
                }

                float maxCursorX = Math.Max(contentX, contentX + contentWidth - CaretWidth);
                cursorX = Math.Clamp(cursorX, contentX, maxCursorX);
                renderer.DrawRect(cursorX, cursorY, CaretWidth, cursorHeight, TextColor);
            }
        }
        finally
        {
            // Deshabilitar scissor test
            renderer.PopScissor();
        }
    }

    /// <summary>
    /// Implementaci�n de IInputHandler para selecci�n con mouse y eventos de teclado
    /// </summary>
    public void OnFocusGained()
    {
        IsFocused = true;
        ResetCursorBlink();
        MarkNeedsPaint();

        if (Rayo.Core.Platform.PlatformDetector.IsMobile)
        {
            Rayo.Core.Platform.VirtualKeyboardManager.Show();
        }
    }

    public void OnFocusLost()
    {
        IsFocused = false;
        _cursorBlinkActive = false;
        StopCursorBlinkTimer(dispose: false);
        ClearSelection();
        MarkNeedsPaint();

        if (Rayo.Core.Platform.PlatformDetector.IsMobile)
        {
            Rayo.Core.Platform.VirtualKeyboardManager.Hide();
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

                // Click simple - iniciar selecci�n con mouse
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
                    // Actualizar selecci�n mientras se arrastra
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

        // Teclas de navegaci�n y edici�n
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
    /// Maneja repetici�n autom�tica de teclas
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
    /// Calcula la posici�n del cursor en el texto basado en la coordenada X del mouse
    /// </summary>
    protected virtual int GetCursorPositionFromMouse(float mouseX, float mouseY)
    {
        // Si el texto est� vac�o, retornar 0
        if (string.IsNullOrEmpty(Text))
        {
            return 0;
        }

        // Calcular el �rea de contenido
        float contentX = ComputedX + Padding.Left + BorderThickness.Left;
        float contentY = ComputedY + Padding.Top + BorderThickness.Top;

        if (IsMultiline)
        {
            // Multiline: determinar l�nea y posici�n dentro de la l�nea
            float lineHeight = FontSize * 1.2f;
            float localY = mouseY - contentY + _scrollOffsetY;

            // Determinar qu� l�nea se clicke�
            int clickedLine = Math.Max(0, (int)(localY / lineHeight));
            EnsureMultilineCache();
            clickedLine = Math.Min(clickedLine, _multilineLines.Count - 1);
            var lineInfo = _multilineLines[clickedLine];
            int lineStartPos = lineInfo.Start;
            // Calcular posici�n X local dentro de la l�nea
            float localX = mouseX - contentX + _scrollOffsetX;

            // Si est� antes del texto de la l�nea, cursor al inicio de la l�nea
            if (localX <= 0)
            {
                return lineStartPos;
            }

            // Si est� despu�s del texto de la l�nea, cursor al final de la l�nea
            if (localX >= lineInfo.Width)
            {
                return lineStartPos + lineInfo.Length;
            }

            // Buscar posici�n dentro de la l�nea
            for (int i = 0; i <= lineInfo.Length; i++)
            {
                float widthUpTo = GetPrefixWidth(lineInfo, i);

                if (i == lineInfo.Length)
                {
                    return lineStartPos + i;
                }

                float widthUpToNext = GetPrefixWidth(lineInfo, i + 1);

                if (localX >= widthUpTo && localX < widthUpToNext)
                {
                    float midpoint = (widthUpTo + widthUpToNext) / 2;
                    return lineStartPos + (localX < midpoint ? i : i + 1);
                }
            }

            return lineStartPos + lineInfo.Length;
        }
        else
        {
            // Single line (l�gica original)
            float contentWidth = ComputedWidth - Padding.Horizontal - BorderThickness.Horizontal;
            float alignmentOffset = GetSingleLineAlignmentOffset(contentWidth);
            float localX = mouseX - contentX - alignmentOffset + _scrollOffsetX;

            if (localX <= 0)
            {
                return 0;
            }

            EnsureSingleLinePrefixCache();
            if (localX >= _singleLineWidth)
            {
                return Text.Length;
            }

            int textLength = Text.Length;
            for (int i = 0; i <= textLength; i++)
            {
                if (i < 0 || i > textLength)
                    continue;

                float widthUpTo = GetSingleLinePrefixWidth(i);

                if (i == textLength)
                {
                    return i;
                }

                float widthUpToNext = GetSingleLinePrefixWidth(i + 1);

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
    /// Mide el ancho del texto (usa renderer cacheado si est� disponible)
    /// </summary>
    protected float MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (_measureWidthCacheRenderer != _cachedRenderer ||
            Math.Abs(_measureWidthCacheFontSize - FontSize) >= 0.01f ||
            _measureWidthCachePasswordMode != IsPassword)
        {
            _measureWidthCache.Clear();
            _measureWidthCacheRenderer = _cachedRenderer;
            _measureWidthCacheFontSize = FontSize;
            _measureWidthCachePasswordMode = IsPassword;
        }

        if (_measureWidthCache.TryGetValue(text, out float cachedWidth))
            return cachedWidth;

        // Si tenemos renderer cacheado, usar medici�n precisa
        float measuredWidth;
        if (_cachedRenderer != null)
        {
            string displayText = text;
            if (IsPassword)
            {
                displayText = new string('*', text.Length);
            }

            var size = _cachedRenderer.MeasureText(displayText, FontSize);
            measuredWidth = size.X;
        }
        else
        {
            // Fallback: aproximaci�n con ancho promedio de car�cter
            // Usar 8 pixels por car�cter como estimaci�n
            measuredWidth = text.Length * (FontSize * 0.6f);
        }

        if (_measureWidthCache.Count >= MeasureWidthCacheLimit)
            _measureWidthCache.Clear();

        _measureWidthCache[text] = measuredWidth;
        return measuredWidth;
    }

    /// <summary>
    /// Actualiza el scroll interno durante el render (solo si es necesario)
    /// </summary>
    private void UpdateScrollToCursorInternal(IRenderer renderer, float visibleWidth)
    {
        int safeCursorPos = Math.Clamp(_cursorPosition, 0, Text.Length);

        float cursorX = GetCursorOffsetWithinLine(safeCursorPos);
        float cursorLocalX = cursorX - _scrollOffsetX;

        // Cursor is right of the visible area � scroll right
        if (cursorLocalX > visibleWidth - 10)
        {
            _scrollOffsetX = cursorX - visibleWidth + 10;
        }
        // Cursor is left of the visible area � scroll left
        else if (cursorLocalX < 10)
        {
            _scrollOffsetX = Math.Max(0, cursorX - 10);
        }
    }

    private float GetSingleLineAlignmentOffset(float contentWidth)
    {
        EnsureSingleLinePrefixCache();
        return GetHorizontalAlignmentOffset(contentWidth, _singleLineWidth);
    }

    private float GetHorizontalAlignmentOffset(float contentWidth, float textWidth)
    {
        if (textWidth >= contentWidth)
        {
            return 0;
        }

        return TextHorizontalAlignment switch
        {
            HorizontalAlignment.Center => (contentWidth - textWidth) / 2f,
            HorizontalAlignment.Right => Math.Max(0, contentWidth - textWidth - CaretWidth),
            _ => 0
        };
    }

    private static VectorPath CreateRoundedRectPath(float x, float y, float width, float height, CornerRadius radius)
    {
        return VectorPath.RoundedRectangle(
            x,
            y,
            width,
            height,
            radius.TopLeft,
            radius.TopRight,
            radius.BottomRight,
            radius.BottomLeft);
    }

    private readonly record struct TextLineInfo(int Start, int Length, string DisplayText, int LineIndex, float[] PrefixWidths, float Width);
}
