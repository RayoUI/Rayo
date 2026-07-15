using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Rendering;
using Nano.Pages.SpriteEditor;
using Nano.Pages.CodeEditor;

namespace Nano.Pages;

public class HomePage : Component
{
    private const int InitialTabCount = 4;
    private readonly SpriteEditorPage _spriteEditorPage = new();
    private readonly CodeEditorPage _codeEditorPage = new();
    private readonly Dictionary<string, int> _openAssetTabs = new(StringComparer.OrdinalIgnoreCase);
    private VisualElement? _spriteEditorContent;
    private VisualElement? _codeEditorContent;
    private TabControl? _tabs;

    public override VisualElement Build()
    {
        _tabs = new TabControl()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .ShowTabCloseButtons(true)
            .AddTab("Inicio", _spriteEditorContent ??= _spriteEditorPage.Build())
            .AddTab("Explorar", CreateTabContent("Contenido de la segunda pestana."))
            .AddTab("Código", _codeEditorContent ??= _codeEditorPage.Build())
            .AddTab("Ajustes", CreateTabContent("Contenido de la tercera pestana."));
        _tabs.TabCloseRequested += CloseTab;
        _tabs.TabChanged += UpdateCodeEditorToolbar;
        UpdateCodeEditorToolbar(_tabs.SelectedIndex);
        return _tabs;
    }

    public void OpenTextAsset(string path, string text, Action<string> save)
    {
        if (_tabs is null)
            return;

        if (_openAssetTabs.TryGetValue(path, out var index))
        {
            _tabs.SelectedIndex = index;
            return;
        }

        var editor = new CodeEditor.CodeEditor(text, new LuaCodeLanguage())
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
        editor.TextChanged += save;
        _tabs.AddTab(Path.GetFileName(path), editor);
        _openAssetTabs[path] = _tabs.TabCount - 1;
        _tabs.SelectedIndex = _tabs.TabCount - 1;
    }

    private void CloseTab(int index)
    {
        if (_tabs is null || index < InitialTabCount || index >= _tabs.TabCount)
            return;

        var path = _openAssetTabs.FirstOrDefault(pair => pair.Value == index).Key;
        _tabs.RemoveTab(index);
        _openAssetTabs.Remove(path);
        foreach (var key in _openAssetTabs.Where(pair => pair.Value > index).Select(pair => pair.Key).ToArray())
            _openAssetTabs[key]--;
    }

    private void UpdateCodeEditorToolbar(int index)
    {
        var content = _tabs?.SelectedTab?.Content;
        var isCodeEditorTab = content is CodeEditor.CodeEditor ||
                              ReferenceEquals(content, _codeEditorContent);
        VirtualKeyboardManager.SetAccessoryKeys(
            isCodeEditorTab ? CodeEditor.CodeEditor.ProgrammingAccessoryKeys : []);
    }

    private static VisualElement CreateTabContent(string text)
    {
        return new VStack()
            .Padding(new Thickness(20))
            .Children(new Label(text).FontSize(16));
    }
}
