using Rayo.Controls;
using Rayo.Core;

namespace Notepad.Controls;

public enum NotepadCommand
{
    New,
    Open,
    Save,
    SaveAs,
    Close
}

public sealed class EditorTab : Component
{
    private const int HistoryLimit = 100;
    private readonly List<string> _history;
    private string _content;
    private string _savedContent;
    private int _historyIndex;
    private bool _applyingHistory;
    private NotepadEditor? _editor;

    public EditorTab(string title, string content = "", string? filePath = null)
    {
        Title = title;
        FilePath = filePath;
        _content = content;
        _savedContent = content;
        _history = [content];
    }

    public string Title { get; private set; }
    public string? FilePath { get; private set; }
    public bool IsDirty { get; private set; }
    public string DisplayTitle => $"{Title}{(IsDirty ? " *" : string.Empty)}";
    public string Text => _editor?.Text ?? _content;
    public float FontSize => _editor?.FontSize ?? 14f;
    public bool WordWrap => _editor?.WordWrap ?? true;

    public event Action<EditorTab>? StateChanged;
    public event Action<EditorTab, int, int>? CaretChanged;
    public event Action<NotepadCommand>? CommandRequested;

    protected override void OnInit()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public override VisualElement Build()
    {
        var editor = new NotepadEditor
        {
            Text = _content,
            FontSize = 14,
            BorderThickness = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        editor.TextChanged += OnTextChanged;
        editor.CaretChanged += (line, column) => CaretChanged?.Invoke(this, line, column);
        editor.UndoRequested += Undo;
        editor.RedoRequested += Redo;
        editor.CommandRequested += command => CommandRequested?.Invoke(command);
        _editor = editor;
        return editor;
    }

    public void MarkSaved(string path)
    {
        FilePath = path;
        Title = Path.GetFileName(path);
        _savedContent = Text;
        UpdateDirtyState();
        StateChanged?.Invoke(this);
    }

    public void Undo()
    {
        if (_historyIndex <= 0)
            return;

        _historyIndex--;
        ApplyHistoryEntry();
    }

    public void Redo()
    {
        if (_historyIndex >= _history.Count - 1)
            return;

        _historyIndex++;
        ApplyHistoryEntry();
    }

    public void Cut()
    {
        _editor?.Cut();
        _editor?.PublishCaret();
        FocusEditor();
    }

    public void Copy()
    {
        _editor?.Copy();
        FocusEditor();
    }

    public void Paste()
    {
        _editor?.Paste();
        _editor?.PublishCaret();
        FocusEditor();
    }

    public void SelectAll()
    {
        _editor?.SelectAll();
        _editor?.PublishCaret();
        FocusEditor();
    }

    public void Zoom(float delta)
    {
        if (_editor == null)
            return;

        _editor.FontSize = Math.Clamp(_editor.FontSize + delta, 8f, 36f);
        _editor.MarkNeedsLayout();
        FocusEditor();
    }

    public void ResetZoom()
    {
        if (_editor == null)
            return;

        _editor.FontSize = 14f;
        _editor.MarkNeedsLayout();
        FocusEditor();
    }

    public void ToggleWordWrap()
    {
        if (_editor == null)
            return;

        _editor.WordWrap = !_editor.WordWrap;
        _editor.MarkNeedsLayout();
        FocusEditor();
    }

    public void FocusEditor()
    {
        if (_editor != null)
            UIApplication.Current?.EventManager?.SetFocus(_editor);
    }

    public (int Line, int Column) GetCaretPosition() =>
        _editor?.GetCaretPosition() ?? (1, 1);

    private void OnTextChanged(string text)
    {
        _content = text;

        if (!_applyingHistory)
        {
            if (_historyIndex < _history.Count - 1)
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);

            if (_history.Count == 0 || _history[^1] != text)
            {
                _history.Add(text);
                if (_history.Count > HistoryLimit)
                    _history.RemoveAt(0);
                _historyIndex = _history.Count - 1;
            }
        }

        UpdateDirtyState();
    }

    private void ApplyHistoryEntry()
    {
        if (_editor == null)
            return;

        _applyingHistory = true;
        _editor.Text = _history[_historyIndex];
        _content = _editor.Text;
        _applyingHistory = false;
        UpdateDirtyState();
        _editor.PublishCaret();
        FocusEditor();
    }

    private void UpdateDirtyState()
    {
        bool wasDirty = IsDirty;
        IsDirty = !string.Equals(Text, _savedContent, StringComparison.Ordinal);
        if (wasDirty != IsDirty)
            StateChanged?.Invoke(this);
    }
}

internal sealed class NotepadEditor : Editor
{
    private int _lastPublishedLine = -1;
    private int _lastPublishedColumn = -1;

    public event Action<int, int>? CaretChanged;
    public event Action? UndoRequested;
    public event Action? RedoRequested;
    public event Action<NotepadCommand>? CommandRequested;

    public override bool HandleInput(InputEventArgs args)
    {
        if (args.EventType == InputEventType.KeyDown && args.IsControlPressed && args.KeyCode.HasValue)
        {
            bool handled = true;
            switch (args.KeyCode.Value)
            {
                case InputKey.Z:
                    UndoRequested?.Invoke();
                    break;
                case InputKey.Y:
                    RedoRequested?.Invoke();
                    break;
                case InputKey.N:
                    CommandRequested?.Invoke(NotepadCommand.New);
                    break;
                case InputKey.O:
                    CommandRequested?.Invoke(NotepadCommand.Open);
                    break;
                case InputKey.S:
                    CommandRequested?.Invoke(args.IsShiftPressed ? NotepadCommand.SaveAs : NotepadCommand.Save);
                    break;
                case InputKey.W:
                    CommandRequested?.Invoke(NotepadCommand.Close);
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
                return true;
        }

        bool result = base.HandleInput(args);
        PublishCaret();
        return result;
    }

    public (int Line, int Column) GetCaretPosition()
    {
        int safePosition = Math.Clamp(_cursorPosition, 0, Text.Length);
        int line = 1;
        int lineStart = 0;

        for (int i = 0; i < safePosition; i++)
        {
            if (Text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, safePosition - lineStart + 1);
    }

    public void PublishCaret()
    {
        var (line, column) = GetCaretPosition();
        if (_lastPublishedLine == line && _lastPublishedColumn == column)
            return;

        _lastPublishedLine = line;
        _lastPublishedColumn = column;
        CaretChanged?.Invoke(line, column);
    }
}
