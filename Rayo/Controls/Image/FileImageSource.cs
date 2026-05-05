using Rayo.Core.Assets;

namespace Rayo.Controls;

/// <summary>
/// Image source that loads from a local file path or a packaged asset path.
/// When the file does not exist on disk (e.g. on Android where assets are packaged
/// inside the APK), the load is retried through <see cref="AssetManager"/> so that
/// the platform-specific stream provider (registered via
/// <see cref="AssetManager.AssetStreamProvider"/>) can resolve the asset.
/// </summary>
public class FileImageSource : ImageSource
{
    #region Properties

    /// <summary>
    /// Ruta del archivo de imagen
    /// </summary>
    public string FilePath { get; }

    #endregion

    #region Constructors

    public FileImageSource(string filePath)
    {
        FilePath = filePath;
    }

    #endregion

    #region ImageSource Implementation

    public override string GetCacheKey() => $"file://{FilePath}";

    public override Task<Stream?> GetStreamAsync()
    {
        IsLoading = true;
        OnLoadingStateChanged();

        try
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Error = "File path is empty";
                IsLoading = false;
                IsLoaded = false;
                OnLoadingStateChanged();
                return Task.FromResult<Stream?>(null);
            }

            // Always resolve through AssetManager so that both on-disk files (desktop)
            // and packaged assets (Android APK, etc.) are handled by a single code path.
            // AssetManager.TryOpenAssetStream first tries File.Exists/File.OpenRead via
            // ResolvePath, and falls back to the platform stream provider when the file
            // is not on the real filesystem.
            if (AssetManager.Instance.TryOpenAssetStream(FilePath, out var stream, out _)
                && stream != null)
            {
                IsLoading = false;
                IsLoaded = true;
                Error = null;
                OnLoadingStateChanged();
                return Task.FromResult<Stream?>(stream);
            }

            Error = $"File not found: {FilePath}";
            IsLoading = false;
            IsLoaded = false;
            OnLoadingStateChanged();
            return Task.FromResult<Stream?>(null);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            IsLoading = false;
            IsLoaded = false;
            OnLoadingStateChanged();
            return Task.FromResult<Stream?>(null);
        }
    }

    #endregion

    #region Implicit Conversion

    public static implicit operator FileImageSource(string filePath) => new(filePath);

    #endregion
}
