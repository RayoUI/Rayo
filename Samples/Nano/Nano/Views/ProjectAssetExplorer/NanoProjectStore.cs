using System.IO.Compression;
using System.Text;

namespace Nano.Views.ProjectAssetStore;

/// <summary>
/// A small virtual file system persisted as a ZIP archive with Nano's .nn extension.
/// </summary>
public sealed class NanoProjectStore : IProjectAssetStore
{
    private static readonly HashSet<string> s_textExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".yaml", ".yml", ".lua", ".cs", ".js", ".ts", ".css", ".html", ".shader"
    };

    public NanoProjectStore(string? archivePath = null)
    {
        ArchivePath = archivePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Nano",
            "Nano.nn");
        EnsureProject();
    }

    public string ArchivePath { get; }

    public IReadOnlyList<VirtualAsset> GetChildren(string directory)
    {
        var prefix = ToDirectoryPath(directory);
        using var archive = ZipFile.OpenRead(ArchivePath);
        var children = new Dictionary<string, VirtualAsset>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remaining = entry.FullName[prefix.Length..];
            if (string.IsNullOrEmpty(remaining))
                continue;

            var separator = remaining.IndexOf('/');
            var name = separator < 0 ? remaining : remaining[..separator];
            var path = string.IsNullOrEmpty(directory) ? name : $"{directory.TrimEnd('/')}/{name}";
            var isDirectory = separator >= 0 || entry.FullName.EndsWith('/');
            children[name] = new VirtualAsset(path, name, isDirectory);
        }

        return children.Values
            .OrderByDescending(asset => asset.IsDirectory)
            .ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void CreateDirectory(string parentDirectory, string name)
    {
        var safeName = ValidateName(name);
        var path = string.IsNullOrEmpty(parentDirectory)
            ? safeName
            : $"{parentDirectory.TrimEnd('/')}/{safeName}";

        using var archive = ZipFile.Open(ArchivePath, ZipArchiveMode.Update);
        if (archive.GetEntry(ToDirectoryPath(path)) is null)
            archive.CreateEntry(ToDirectoryPath(path));
    }

    public string ReadText(string path)
    {
        using var archive = ZipFile.OpenRead(ArchivePath);
        var entry = archive.GetEntry(NormalizeFilePath(path))
            ?? throw new FileNotFoundException("The virtual asset does not exist.", path);
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public void WriteText(string path, string text)
    {
        var normalizedPath = NormalizeFilePath(path);
        using var archive = ZipFile.Open(ArchivePath, ZipArchiveMode.Update);
        archive.GetEntry(normalizedPath)?.Delete();
        var entry = archive.CreateEntry(normalizedPath, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(text);
    }

    public bool IsTextFile(string path) => s_textExtensions.Contains(Path.GetExtension(path));

    private void EnsureProject()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath)!);
        if (File.Exists(ArchivePath))
            return;

        using var archive = ZipFile.Open(ArchivePath, ZipArchiveMode.Create);
        archive.CreateEntry("scripts/");
        var readme = archive.CreateEntry("README.md");
        using var writer = new StreamWriter(readme.Open(), Encoding.UTF8);
        writer.Write("# Nano project\n\nAssets in this project are stored in this .nn archive.");
    }

    private static string ValidateName(string name)
    {
        var trimmed = name.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Contains('/') || trimmed is "." or "..")
            throw new ArgumentException("Use a single, non-empty directory name.", nameof(name));
        return trimmed;
    }

    private static string NormalizeFilePath(string path) => path.Trim().TrimStart('/').Replace('\\', '/');
    private static string ToDirectoryPath(string path)
    {
        var normalized = NormalizeFilePath(path);
        return string.IsNullOrEmpty(normalized) ? string.Empty : $"{normalized.TrimEnd('/')}/";
    }
}

public sealed record VirtualAsset(string Path, string Name, bool IsDirectory);
