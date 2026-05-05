namespace Rayo.Core.Assets;

using Rayo.Rendering;

/// <summary>
/// Centralized asset management system for Rayo applications.
/// Manages fonts, images, and other resources at the application level.
/// Similar to MAUI's asset configuration system.
/// </summary>
public class AssetManager : IDisposable
{
    private static AssetManager? _instance;
    private readonly Dictionary<string, FontAsset> _fonts = new();
    private readonly Dictionary<string, ImageAsset> _images = new();
    private readonly List<string> _assetSearchPaths = new();
    private Func<string, Stream?>? _assetStreamProvider;

    private string? _defaultFontFamily;

    /// <summary>
    /// Gets the singleton instance of the AssetManager
    /// </summary>
    public static AssetManager Instance => _instance ??= new AssetManager();

    /// <summary>
    /// Gets the default font family name
    /// </summary>
    public string? DefaultFontFamily => _defaultFontFamily;

    /// <summary>
    /// Gets all registered font aliases
    /// </summary>
    public IEnumerable<string> FontFamilies => _fonts.Keys;

    /// <summary>
    /// Gets all registered image aliases
    /// </summary>
    public IEnumerable<string> ImageAliases => _images.Keys;

    private AssetManager()
    {
        // Add default search paths
        _assetSearchPaths.Add("Assets");
        _assetSearchPaths.Add(".");
    }

    /// <summary>
    /// Initializes the AssetManager. Called automatically by UIApplication.
    /// </summary>
    internal void Initialize()
    {
        // Load any pending assets that were registered before initialization
        LoadPendingAssets();
    }

    public void ConfigureAssets(Action<AssetConfiguration> configure)
    {
        var configuration = new AssetConfiguration(this);
        configure(configuration);
    }

    /// <summary>
    /// Sets a provider for opening asset streams (for platforms like Android where assets are packaged).
    /// </summary>
    public void AssetStreamProvider(Func<string, Stream?> provider)
    {
        _assetStreamProvider = provider;
        LoadPendingAssets();
    }

    /// <summary>
    /// Adds a search path for assets
    /// </summary>
    public void AddSearchPath(string path)
    {
        if (!_assetSearchPaths.Contains(path))
        {
            _assetSearchPaths.Insert(0, path); // Higher priority
            // Try loading any pending assets that might exist under the new path
            LoadPendingAssets();
        }
    }

    /// <summary>
    /// Registers a font with an alias for later use
    /// </summary>
    public void RegisterFont(string path, string alias, float defaultSize = 14f)
    {
        var fontAsset = new FontAsset
        {
            Path = path,
            Alias = alias,
            DefaultSize = defaultSize,
            IsLoaded = false
        };

        _fonts[alias] = fontAsset;

        // Attempt to load immediately (will retry later if path not found yet)
        LoadFont(fontAsset);
    }

    /// <summary>
    /// Registers an image with an alias for later use
    /// </summary>
    public void RegisterImage(string path, string alias)
    {
        var imageAsset = new ImageAsset
        {
            Path = path,
            Alias = alias,
            IsLoaded = false
        };

        _images[alias] = imageAsset;

        // Images are loaded on-demand, not at startup
    }

    /// <summary>
    /// Sets the default font family for the application
    /// </summary>
    public void SetDefaultFont(string alias)
    {
        _defaultFontFamily = alias;
    }

    /// <summary>
    /// Gets a font by its alias
    /// </summary>
    public FontAsset? GetFont(string alias)
    {
        if (_fonts.TryGetValue(alias, out var font))
        {
            if (!font.IsLoaded)
            {
                LoadFont(font);
            }
            return font;
        }
        return null;
    }

    internal bool TryOpenAssetStream(string relativePath, out Stream? stream, out string? resolvedPath)
    {
        stream = null;
        resolvedPath = null;

        var fullPath = ResolvePath(relativePath);
        if (fullPath != null)
        {
            resolvedPath = fullPath;
            stream = File.OpenRead(fullPath);
            return true;
        }

        if (_assetStreamProvider == null)
        {
            return false;
        }

        foreach (var candidate in GetAssetCandidates(relativePath))
        {
            try
            {
                stream = _assetStreamProvider(candidate);
            }
            catch
            {
                stream = null;
            }

            if (stream != null)
            {
                resolvedPath = candidate;
                return true;
            }
        }

        return false;
    }

    private IEnumerable<string> GetAssetCandidates(string relativePath)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in EnumerateCandidates(relativePath))
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private IEnumerable<string> EnumerateCandidates(string relativePath)
    {
        yield return NormalizeAssetPath(relativePath);

        foreach (var searchPath in _assetSearchPaths)
        {
            var combined = Path.Combine(searchPath, relativePath);
            yield return NormalizeAssetPath(combined);
        }
    }

    private static string NormalizeAssetPath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    /// Gets an image by its alias
    /// </summary>
    public ImageAsset? GetImage(string alias)
    {
        return _images.TryGetValue(alias, out var image) ? image : null;
    }

    /// <summary>
    /// Gets the full path to an asset, searching through registered paths
    /// </summary>
    public string? ResolvePath(string relativePath)
    {
        // Determine base directory (bin folder when running the app)
        string baseDir = AppContext.BaseDirectory;

        // First try the path as-is relative to current working directory
        if (File.Exists(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        // Then try relative to the application's base directory
        var baseRelative = Path.Combine(baseDir, relativePath);
        if (File.Exists(baseRelative))
        {
            return Path.GetFullPath(baseRelative);
        }

        // Search through registered paths (both current directory and base directory)
        foreach (var searchPath in _assetSearchPaths)
        {
            var fullPath = Path.Combine(searchPath, relativePath);
            if (File.Exists(fullPath))
            {
                return Path.GetFullPath(fullPath);
            }

            var baseFullPath = Path.Combine(baseDir, searchPath, relativePath);
            if (File.Exists(baseFullPath))
            {
                return Path.GetFullPath(baseFullPath);
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a font family is registered
    /// </summary>
    public bool HasFont(string alias)
    {
        return _fonts.ContainsKey(alias);
    }

    /// <summary>
    /// Checks if an image is registered
    /// </summary>
    public bool HasImage(string alias)
    {
        return _images.ContainsKey(alias);
    }

    private void LoadPendingAssets()
    {
        foreach (var font in _fonts.Values.Where(f => !f.IsLoaded))
        {
            LoadFont(font);
        }
    }

    private void LoadFont(FontAsset fontAsset)
    {
        if (fontAsset.IsLoaded)
            return;
        if (!TryOpenAssetStream(fontAsset.Path, out var stream, out var resolvedPath))
        {
            Console.WriteLine($"[AssetManager] Warning: Font not found: {fontAsset.Path}");
            return;
        }

        try
        {
            var loadedStream = stream ?? throw new InvalidOperationException($"Could not open font stream for '{fontAsset.Path}'.");
            using (loadedStream)
            using (var memoryStream = new MemoryStream())
            {
                loadedStream.CopyTo(memoryStream);
                fontAsset.FontData = memoryStream.ToArray();
            }

            fontAsset.FullPath = resolvedPath;
            fontAsset.IsLoaded = true;

            Console.WriteLine($"[AssetManager] Loaded font: {fontAsset.Alias} from {resolvedPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AssetManager] Error loading font {fontAsset.Path}: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all registered assets and releases resources
    /// </summary>
    public void Clear()
    {
        foreach (var font in _fonts.Values)
        {
            font.Dispose();
        }
        _fonts.Clear();

        foreach (var image in _images.Values)
        {
            image.Dispose();
        }
        _images.Clear();

        _defaultFontFamily = null;
    }

    public void Dispose()
    {
        Clear();
        _instance = null;
    }
}

/// <summary>
/// Represents a registered font asset
/// </summary>
public class FontAsset : IDisposable
{
    public string Path { get; set; } = "";
    public string Alias { get; set; } = "";
    public string? FullPath { get; set; }
    public float DefaultSize { get; set; } = 14f;
    public bool IsLoaded { get; set; }
    public byte[]? FontData { get; set; }

    public void Dispose()
    {
        FontData = null;
    }
}

/// <summary>
/// Represents a registered image asset
/// </summary>
public class ImageAsset : IDisposable
{
    public string Path { get; set; } = "";
    public string Alias { get; set; } = "";
    public string? FullPath { get; set; }
    public bool IsLoaded { get; set; }

    public void Dispose()
    {
        IsLoaded = false;
    }
}
