using Nano.Views.ProjectAssetStore;
using Nano.Views.SpriteEditor;
using Nano.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Rendering;
using Nano.Views.CodeEditor.Components;

namespace Nano.Views;

public class HomePage : ViewBase<HomeViewModel>, ITextAssetHost, ISpriteAssetHost
{
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
            .WithEmptyContent(() =>
                new Label("No documents are open.")
                    .FontSize(15)
                    .Foreground(new Color(148, 163, 184))
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .TextVerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center));

        ViewModel.SetFixedTabCount(_tabs.TabCount);
        foreach (var document in ViewModel.Documents)
        {
            _tabs.AddTab(document.Title, CreateDocumentEditor(document));
        }

        _tabs.TabCloseRequested += CloseTab;
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

    public void SaveAllTextAssets() => ViewModel.SaveAllTextAssets();

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
        editor.TextChanged += document.UpdateText;
        return editor;
    }
}
