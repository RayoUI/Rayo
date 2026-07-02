using Notepad;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Styling;

namespace Notepad.Controls;

public sealed class NotepadWorkspace
{
    private static readonly List<string> TextFileExtensions =
        [".txt", ".md", ".json", ".xml", ".csv", ".log", ".cs", ".csproj", ".html", ".css", ".js", ".ts"];

    private TabControl? _tabs;
    private int _untitledCounter = 1;
    private bool _initialized;

    public Signal<string> StatusText { get; } = new("Ready");
    public Signal<string> CaretText { get; } = new("Ln 1, Col 1");
    public bool IsLightTheme() =>
        (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Brightness == ThemeBrightness.Light;
    public bool IsDarkTheme() =>
        (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Name == RayoThemes.Dark.Name;
    public bool IsNeonTheme() =>
        (UIApplication.Current?.ActiveTheme ?? RayoThemes.Light).Name == NotepadThemes.Neon.Name;

    public void Attach(TabControl tabs)
    {
        if (ReferenceEquals(_tabs, tabs))
            return;

        _tabs = tabs;
        tabs.TabChanged += _ => UpdateStatusFromSelection();
        tabs.TabCloseRequested += RequestCloseDocument;

        if (_initialized)
            return;

        _initialized = true;
        AddDocument("Untitled-1.txt", "Welcome to Rayo Notepad!\n\nThis is a modular tabbed text editor.");
        AddDocument("Notes.md", "- Item 1\n- Item 2\n- Item 3");
        AddDocument("Config.json", "{\n  \"theme\": \"dark\",\n  \"fontSize\": 14\n}");
        tabs.SelectedIndex = 0;
        UpdateStatusFromSelection();
    }

    public void NewDocument()
    {
        AddDocument($"Untitled-{++_untitledCounter}.txt", "");
        StatusText.Value = "New document";
    }

    public void OpenDocument()
    {
        FilePicker.ShowDialog(
            OpenPath,
            configure: picker =>
            {
                picker.DialogTitle = "Open text file";
                picker.FileExtensions = TextFileExtensions.ToList();
                picker.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            });
    }

    public void SaveDocument()
    {
        var editor = SelectedEditor;
        if (editor == null)
            return;

        if (string.IsNullOrWhiteSpace(editor.FilePath))
            SaveDocumentAs();
        else
            SaveToPath(editor, editor.FilePath);
    }

    public void SaveDocumentAs()
    {
        var editor = SelectedEditor;
        if (editor == null)
            return;

        SaveFilePicker.ShowDialog(
            path => SaveToPath(editor, path),
            configure: picker =>
            {
                picker.DialogTitle = "Save text file";
                picker.DefaultFileName = editor.Title;
                picker.FileExtensions = TextFileExtensions.ToList();
                picker.SaveConflictBehavior = SaveFileConflictBehavior.Overwrite;
                picker.DefaultDirectory = editor.FilePath is { } filePath
                    ? Path.GetDirectoryName(filePath)
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            });
    }

    public void CloseDocument()
    {
        if (_tabs == null)
            return;

        RequestCloseDocument(_tabs.SelectedIndex);
    }

    private void RequestCloseDocument(int index)
    {
        if (_tabs == null ||
            index < 0 ||
            index >= _tabs.TabCount ||
            _tabs.Items[index].Content is not EditorTab editor)
        {
            return;
        }

        if (editor.IsDirty)
        {
            Dialog.Show(
                "Discard changes?",
                $"{editor.Title} has unsaved changes.",
                showCancelButton: true,
                onAccepted: () => RemoveEditor(editor),
                okText: "Discard",
                cancelText: "Keep editing");
            return;
        }

        RemoveEditor(editor);
    }

    public void Exit()
    {
        bool hasUnsavedChanges = _tabs?.Items
            .Select(item => item.Content)
            .OfType<EditorTab>()
            .Any(editor => editor.IsDirty) == true;

        if (!hasUnsavedChanges)
        {
            UIApplication.Current?.Exit();
            return;
        }

        Dialog.Show(
            "Exit without saving?",
            "One or more documents have unsaved changes.",
            showCancelButton: true,
            onAccepted: () => UIApplication.Current?.Exit(),
            okText: "Exit",
            cancelText: "Cancel");
    }

    public void Undo() => WithEditor(editor => editor.Undo(), "Undo");
    public void Redo() => WithEditor(editor => editor.Redo(), "Redo");
    public void Cut() => WithEditor(editor => editor.Cut(), "Cut");
    public void Copy() => WithEditor(editor => editor.Copy(), "Copied selection");
    public void Paste() => WithEditor(editor => editor.Paste(), "Paste");
    public void SelectAll() => WithEditor(editor => editor.SelectAll(), "Selected all");

    public void ZoomIn() => ChangeView(editor => editor.Zoom(1), editor => $"Zoom: {editor.FontSize:0} pt");
    public void ZoomOut() => ChangeView(editor => editor.Zoom(-1), editor => $"Zoom: {editor.FontSize:0} pt");
    public void ResetZoom() => ChangeView(editor => editor.ResetZoom(), _ => "Zoom: 14 pt");
    public void ToggleWordWrap() =>
        ChangeView(editor => editor.ToggleWordWrap(), editor => $"Word wrap: {(editor.WordWrap ? "on" : "off")}");

    public void UseLightTheme()
    {
        UIApplication.Current?.UseTheme(RayoThemes.Light);
        StatusText.Value = "Light theme";
    }

    public void UseDarkTheme()
    {
        UIApplication.Current?.UseTheme(RayoThemes.Dark);
        StatusText.Value = "Dark theme";
    }

    public void UseNeonTheme()
    {
        UIApplication.Current?.UseTheme(NotepadThemes.Neon);
        StatusText.Value = "Neon theme";
    }

    private EditorTab? SelectedEditor => _tabs?.SelectedTab?.Content as EditorTab;

    private void AddDocument(string title, string content, string? filePath = null)
    {
        if (_tabs == null)
            return;

        var editor = new EditorTab(title, content, filePath);
        editor.StateChanged += RefreshEditorTitle;
        editor.CaretChanged += OnCaretChanged;
        editor.CommandRequested += ExecuteCommand;
        _tabs.AddTab(editor.DisplayTitle, editor);
        _tabs.SelectedIndex = _tabs.TabCount - 1;
        editor.FocusEditor();
    }

    private void OpenPath(string path)
    {
        if (_tabs == null)
            return;

        int existingIndex = _tabs.Items
            .Select((tab, index) => (tab, index))
            .FirstOrDefault(pair =>
                pair.tab.Content is EditorTab editor &&
                string.Equals(editor.FilePath, path, StringComparison.OrdinalIgnoreCase))
            .index;

        if (existingIndex > 0 || (_tabs.TabCount > 0 &&
            _tabs.Items[0].Content is EditorTab first &&
            string.Equals(first.FilePath, path, StringComparison.OrdinalIgnoreCase)))
        {
            _tabs.SelectedIndex = existingIndex;
            StatusText.Value = $"Already open: {Path.GetFileName(path)}";
            return;
        }

        try
        {
            string content = File.ReadAllText(path);
            AddDocument(Path.GetFileName(path), content, path);
            StatusText.Value = $"Opened {Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            ShowFileError("Could not open file", path, ex);
        }
    }

    private void SaveToPath(EditorTab editor, string path)
    {
        try
        {
            File.WriteAllText(path, editor.Text);
            editor.MarkSaved(path);
            StatusText.Value = $"Saved {Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            ShowFileError("Could not save file", path, ex);
        }
    }

    private void RemoveEditor(EditorTab editor)
    {
        if (_tabs == null)
            return;

        int index = _tabs.Items.ToList().FindIndex(tab => ReferenceEquals(tab.Content, editor));
        if (index >= 0)
            _tabs.RemoveTab(index);

        if (_tabs.TabCount == 0)
            NewDocument();

        StatusText.Value = "Document closed";
        UpdateStatusFromSelection();
    }

    private void RefreshEditorTitle(EditorTab editor)
    {
        if (_tabs == null)
            return;

        var item = _tabs.Items.FirstOrDefault(tab => ReferenceEquals(tab.Content, editor));
        if (item == null || item.Title == editor.DisplayTitle)
            return;

        int selectedIndex = _tabs.SelectedIndex;
        item.Title = editor.DisplayTitle;
        _tabs.Items = _tabs.Items.ToList();
        _tabs.SelectedIndex = selectedIndex;
    }

    private void OnCaretChanged(EditorTab editor, int line, int column)
    {
        if (ReferenceEquals(editor, SelectedEditor))
            CaretText.Value = $"Ln {line}, Col {column}";
    }

    private void UpdateStatusFromSelection()
    {
        var editor = SelectedEditor;
        if (editor == null)
        {
            CaretText.Value = string.Empty;
            return;
        }

        var (line, column) = editor.GetCaretPosition();
        CaretText.Value = $"Ln {line}, Col {column}";
        StatusText.Value = editor.IsDirty ? "Modified" : editor.Title;
    }

    private void WithEditor(Action<EditorTab> action, string status)
    {
        var editor = SelectedEditor;
        if (editor == null)
            return;

        action(editor);
        StatusText.Value = status;
    }

    private void ChangeView(Action<EditorTab> action, Func<EditorTab, string> status)
    {
        var editor = SelectedEditor;
        if (editor == null)
            return;

        action(editor);
        StatusText.Value = status(editor);
    }

    private void ExecuteCommand(NotepadCommand command)
    {
        switch (command)
        {
            case NotepadCommand.New: NewDocument(); break;
            case NotepadCommand.Open: OpenDocument(); break;
            case NotepadCommand.Save: SaveDocument(); break;
            case NotepadCommand.SaveAs: SaveDocumentAs(); break;
            case NotepadCommand.Close: CloseDocument(); break;
        }
    }

    private static void ShowFileError(string title, string path, Exception exception)
    {
        Dialog.Show(title, $"{Path.GetFileName(path)}\n\n{exception.Message}");
    }
}
