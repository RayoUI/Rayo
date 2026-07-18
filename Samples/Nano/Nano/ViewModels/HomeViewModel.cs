using Rayo.Core;

namespace Nano.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private const int FixedTabCount = 3;
    public List<TextAssetDocument> Documents { get; } = [];

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
                FixedTabCount + existingIndex,
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
            FixedTabCount + Documents.Count - 1,
            true,
            document);
    }

    public bool CloseTextAsset(int tabIndex)
    {
        if (tabIndex < FixedTabCount)
            return false;

        var documentIndex = tabIndex - FixedTabCount;
        if (documentIndex < 0 || documentIndex >= Documents.Count)
            return false;

        Documents.RemoveAt(documentIndex);
        return true;
    }
}

public sealed record TextAssetDocument(
    string Path,
    string Title,
    string Text,
    Action<string> Save);

public sealed record DocumentOpenResult(
    int TabIndex,
    bool IsNew,
    TextAssetDocument? Document);
