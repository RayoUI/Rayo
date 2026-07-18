using Nano.Views.CodeEditor;
using Nano.Views.ProjectAssetStore;
using Nano.Views.SpriteEditor;
using Nano.Views.LevelEditor;
using Nano.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Rendering;
using Nano.Views.CodeEditor.Components;

namespace Nano.Views;

public class HomePage : ViewBase<HomeViewModel>, ITextAssetHost, ISpriteAssetHost
{
    private readonly SpriteEditorView _spriteEditorPage = new();
    private readonly CodeEditorView _codeEditorPage = new();
    private readonly LevelEditorView _levelEditorPage = new();
    private VisualElement? _spriteEditorContent;
    private VisualElement? _codeEditorContent;
    private VisualElement? _levelEditorContent;
    private TabControl? _tabs;

    public HomePage()
    {
        SetViewModel(new HomeViewModel());
    }

    public override VisualElement Build()
    {
        _tabs = new TabControl()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .ShowTabCloseButtons(true)
            .AddTab("Inicio", _spriteEditorContent ??= _spriteEditorPage.Build())
            .AddTab("Nivel", _levelEditorContent ??= _levelEditorPage.Build())
            .AddTab("Código", _codeEditorContent ??= _codeEditorPage.Build());

        foreach (var document in ViewModel.Documents)
        {
            _tabs.AddTab(document.Title, CreateDocumentEditor(document));
        }

        _tabs.TabCloseRequested += CloseTab;
        _tabs.TabChanged += UpdateCodeEditorToolbar;
        UpdateCodeEditorToolbar(_tabs.SelectedIndex);
        return _tabs;
    }

    public void OpenTextAsset(string path, string text, Action<string> save)
    {
        if (_tabs is null)
            return;

        var result = ViewModel.OpenTextAsset(path, text, save);
        if (!result.IsNew)
        {
            _tabs.SelectedIndex = result.TabIndex;
            return;
        }

        var document = result.Document!;
        _tabs.AddTab(document.Title, CreateDocumentEditor(document));
        _tabs.SelectedIndex = _tabs.TabCount - 1;
    }

    public void OpenSpriteAsset(string path, string text, Action<string> save)
    {
        if (_tabs is null)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            SpriteDimensionsDialog.Show((width, height) =>
                OpenSpriteEditor(path, SpriteAssetDocument.CreateBlank(width, height), save));
            return;
        }

        try
        {
            var document = SpriteAssetDocument.Deserialize(text);
            document.Validate();
            OpenSpriteEditor(path, document, save);
        }
        catch (Exception)
        {
            ToastService.ShowInfo("This sprite asset is invalid.");
        }
    }

    private void CloseTab(int index)
    {
        if (_tabs is null ||
            index < 0 ||
            index >= _tabs.TabCount ||
            !ViewModel.CloseTextAsset(index))
        {
            return;
        }

        _tabs.RemoveTab(index);
    }

    private void UpdateCodeEditorToolbar(int index)
    {
        var content = _tabs?.SelectedTab?.Content;
        var isCodeEditorTab = ((content is CodeEditor.Components.CodeEdit)) ||
                              ReferenceEquals(content, _codeEditorContent);
        VirtualKeyboardManager.SetAccessoryKeys(
            isCodeEditorTab ? CodeEditor.Components.CodeEdit.ProgrammingAccessoryKeys : []);
    }

    private void OpenSpriteEditor(string path, SpriteAssetDocument document, Action<string> save)
    {
        if (_tabs is null)
            return;

        var editor = new SpriteEditorView(document, save)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        _tabs.AddTab(Path.GetFileName(path), editor.Build());
        _tabs.SelectedIndex = _tabs.TabCount - 1;
        save(document.Serialize());
    }

    private static VisualElement CreateDocumentEditor(TextAssetDocument document)
    {
        var editor = new CodeEdit(document.Text, new LuaCodeLanguage())
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        editor.TextChanged += document.Save;
        return editor;
    }
}
