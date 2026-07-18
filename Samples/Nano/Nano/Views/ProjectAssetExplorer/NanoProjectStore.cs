using System.IO.Compression;
using System.Text;
using Nano.Views.SpriteEditor;

namespace Nano.Views.ProjectAssetStore;

/// <summary>
/// A small virtual file system persisted as a ZIP archive with Nano's .nn extension.
/// </summary>
public sealed class NanoProjectStore : IProjectAssetStore
{
    private const string DebugGameResourceName = "Nano.Assets.game.nn";

    private const string StarterScript = """
        local circle_x = 120
        local circle_y = 100
        local circle_radius = 28

        function update(dt)
            circle_x = math.max(circle_radius, math.min(
                nano.width - circle_radius,
                circle_x + nano.input.x * 160 * dt))
            circle_y = math.max(circle_radius, math.min(
                nano.height - circle_radius,
                circle_y + nano.input.y * 160 * dt))
        end

        function draw()
            nano.draw.clear(12, 16, 24)
            if nano.input.a then
                nano.draw.circle(circle_x, circle_y, circle_radius, 75, 225, 145)
            else
                nano.draw.circle(circle_x, circle_y, circle_radius, 65, 180, 255)
            end
        end
        """;

    private static readonly string[] s_previousStarterScripts =
    [
        """
        local x = 24

        function update(dt)
            x = (x + 90 * dt) % nano.width
        end

        function draw()
            nano.draw.clear(12, 16, 24)
            nano.draw.rect(x, 80, 48, 48, 65, 180, 255)
            nano.draw.circle(nano.width / 2, nano.height / 2, 32, 255, 195, 70)
            nano.draw.line(16, nano.height - 32, nano.width - 16, nano.height - 32, 90, 110, 145)
        end
        """,
        """
        local x = 60
        local y = 80

        function update(dt)
            x = math.max(0, math.min(nano.width - 48, x + nano.input.x * 140 * dt))
            y = math.max(0, math.min(nano.height - 48, y + nano.input.y * 140 * dt))
        end

        function draw()
            nano.draw.clear(12, 16, 24)
            if nano.input.a then
                nano.draw.rect(x, y, 48, 48, 75, 225, 145)
            else
                nano.draw.rect(x, y, 48, 48, 65, 180, 255)
            end
            nano.draw.line(16, nano.height - 32, nano.width - 16, nano.height - 32, 90, 110, 145)
        end
        """
    ];

    private static readonly HashSet<string> s_textExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".yaml", ".yml", ".lua", ".cs", ".js", ".ts", ".css", ".html", ".shader"
    };

    public NanoProjectStore(string? archivePath = null)
    {
        if (archivePath is null)
        {
#if DEBUG
            ArchivePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Nano",
                "Debug",
                "game.nn");
            InstallDebugGame();
            return;
#else
            ArchivePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Nano",
                "Nano.nn");
#endif
        }
        else
        {
            ArchivePath = archivePath;
        }

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

    public VirtualAsset CreateSprite(string parentDirectory, string name)
    {
        var safeName = ValidateName(name);
        var fileName = safeName.EndsWith(SpriteAssetDocument.Extension, StringComparison.OrdinalIgnoreCase)
            ? safeName
            : $"{safeName}{SpriteAssetDocument.Extension}";
        var path = string.IsNullOrEmpty(parentDirectory)
            ? fileName
            : $"{parentDirectory.TrimEnd('/')}/{fileName}";

        using var archive = ZipFile.Open(ArchivePath, ZipArchiveMode.Update);
        if (archive.GetEntry(NormalizeFilePath(path)) is not null)
            throw new ArgumentException("An asset with that name already exists.", nameof(name));

        archive.CreateEntry(NormalizeFilePath(path), CompressionLevel.Optimal);
        return new VirtualAsset(NormalizeFilePath(path), fileName, false);
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

    public bool IsSpriteFile(string path) =>
        Path.GetExtension(path).Equals(SpriteAssetDocument.Extension, StringComparison.OrdinalIgnoreCase);

#if DEBUG
    private void InstallDebugGame()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath)!);
        using var source = typeof(NanoProjectStore).Assembly.GetManifestResourceStream(DebugGameResourceName)
            ?? throw new InvalidOperationException(
                $"The embedded Debug project '{DebugGameResourceName}' was not found.");
        using var destination = new FileStream(ArchivePath, FileMode.Create, FileAccess.Write, FileShare.None);
        source.CopyTo(destination);
    }
#endif

    private void EnsureProject()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath)!);
        if (!File.Exists(ArchivePath))
        {
            using var newArchive = ZipFile.Open(ArchivePath, ZipArchiveMode.Create);
            newArchive.CreateEntry("scripts/");
            var readme = newArchive.CreateEntry("README.md");
            using (var readmeWriter = new StreamWriter(readme.Open(), Encoding.UTF8))
                readmeWriter.Write("# Nano project\n\nAssets in this project are stored in this .nn archive.");

            var main = newArchive.CreateEntry("main.lua", CompressionLevel.Optimal);
            using var mainWriter = new StreamWriter(
                main.Open(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            mainWriter.Write(StarterScript);
            return;
        }

        using var archive = ZipFile.Open(ArchivePath, ZipArchiveMode.Update);

        if (archive.GetEntry("scripts/") is null)
            archive.CreateEntry("scripts/");

        if (archive.GetEntry("README.md") is null)
        {
            var readme = archive.CreateEntry("README.md");
            using var readmeWriter = new StreamWriter(readme.Open(), Encoding.UTF8);
            readmeWriter.Write("# Nano project\n\nAssets in this project are stored in this .nn archive.");
        }

        if (archive.GetEntry("main.lua") is null)
        {
            WriteStarterScript(archive);
            return;
        }

        UpgradeStarterScript(archive);
    }

    private static void UpgradeStarterScript(ZipArchive archive)
    {
        var entry = archive.GetEntry("main.lua")!;
        string currentScript;
        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
            currentScript = reader.ReadToEnd();

        var isPreviousStarter = s_previousStarterScripts.Any(previous =>
            NormalizeScript(previous).Equals(
                NormalizeScript(currentScript),
                StringComparison.Ordinal));
        if (!isPreviousStarter)
            return;

        entry.Delete();
        WriteStarterScript(archive);
    }

    private static void WriteStarterScript(ZipArchive archive)
    {
        var main = archive.CreateEntry("main.lua", CompressionLevel.Optimal);
        using var writer = new StreamWriter(
            main.Open(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(StarterScript);
    }

    private static string NormalizeScript(string script) =>
        script.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

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
