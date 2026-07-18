using Rayo.Core;

namespace Nano.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private int _fixedTabCount;

    public HomeViewModel(int fixedTabCount = 0)
    {
        _fixedTabCount = Math.Max(0, fixedTabCount);
    }

    public List<TextAssetDocument> Documents { get; } = [];

    public void SetFixedTabCount(int count) => _fixedTabCount = Math.Max(0, count);

    public DocumentOpenResult OpenTextAsset(
        string path,
        string text,
        Action<string> save)
    {
        var existingIndex = Documents.FindIndex(
            document => document.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            return new DocumentOpenResult(
                _fixedTabCount + existingIndex,
                false,
                null);
        }

        var document = new TextAssetDocument(
            path,
            Path.GetFileName(path),
            text,
            save);
        Documents.Add(document);
        return new DocumentOpenResult(
            _fixedTabCount + Documents.Count - 1,
            true,
            document);
    }

    public bool CloseTextAsset(int tabIndex)
    {
        if (tabIndex < _fixedTabCount)
            return false;

        var documentIndex = tabIndex - _fixedTabCount;
        if (documentIndex < 0 || documentIndex >= Documents.Count)
            return false;

        Documents[documentIndex].SaveIfModified();
        Documents.RemoveAt(documentIndex);
        return true;
    }

    public void SaveAllTextAssets()
    {
        foreach (var document in Documents)
            document.SaveIfModified();
    }
}

public sealed class TextAssetDocument
{
    private readonly Action<string> _save;
    private string _savedText;

    public TextAssetDocument(string path, string title, string text, Action<string> save)
    {
        Path = path;
        Title = title;
        Text = text;
        _savedText = text;
        _save = save;
    }

    public string Path { get; }
    public string Title { get; }
    public string Text { get; private set; }
    public bool IsModified => !string.Equals(Text, _savedText, StringComparison.Ordinal);

    public void UpdateText(string text)
    {
        if (string.Equals(Text, text, StringComparison.Ordinal))
            return;

        Text = text;
        SaveIfModified();
    }

    public void SaveIfModified()
    {
        if (!IsModified)
            return;

        _save(Text);
        _savedText = Text;
    }
}

public sealed record DocumentOpenResult(
    int TabIndex,
    bool IsNew,
    TextAssetDocument? Document);
