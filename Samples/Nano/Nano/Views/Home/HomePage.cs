using Nano.Views.CodeEditor;
using Nano.Views.ProjectAssetStore;
using Nano.Views.SpriteEditor;
using Nano.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Rendering;
using Nano.Views.CodeEditor.Components;

namespace Nano.Views;

public class HomePage : ViewBase<HomeViewModel>, ITextAssetHost
{
    private readonly SpriteEditorView _spriteEditorPage = new();
    private readonly CodeEditorView _codeEditorPage = new();
    private VisualElement? _spriteEditorContent;
    private VisualElement? _codeEditorContent;
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

    private static VisualElement CreateDocumentEditor(TextAssetDocument document)
    {
        var editor = new CodeEdit(document.Text, new LuaCodeLanguage())
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        editor.TextChanged += document.Save;
        return editor;
    }
}
