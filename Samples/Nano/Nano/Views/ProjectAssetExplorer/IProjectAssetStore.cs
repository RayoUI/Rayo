namespace Nano.Views.ProjectAssetStore;

public interface IProjectAssetStore
{
    string ArchivePath { get; }

    IReadOnlyList<VirtualAsset> GetChildren(string directory);

    void CreateDirectory(string parentDirectory, string name);

    VirtualAsset CreateSprite(string parentDirectory, string name);

    string ReadText(string path);

    byte[] ReadBytes(string path);

    void WriteText(string path, string text);

    bool IsTextFile(string path);

    bool IsSpriteFile(string path);
}

public interface ITextAssetHost
{
    void OpenTextAsset(string path, string text, Action<string> save);
}

public interface ISpriteAssetHost
{
    void OpenSpriteAsset(string path, string text, Action<string> save);
}
