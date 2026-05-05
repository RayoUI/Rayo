namespace Rayo.Controls;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Fuente de imagen desde una URL de red con soporte de caché
/// </summary>
public class UriImageSource : ImageSource
{
    #region Static Fields

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly string _cacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rayo",
        "ImageCache"
    );

    #endregion

    #region Properties

    /// <summary>
    /// URI de la imagen
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Tiempo de validez del caché (default: 1 día)
    /// </summary>
    public TimeSpan CacheValidity { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Indica si el caché está habilitado (default: true)
    /// </summary>
    public bool CachingEnabled { get; set; } = true;

    #endregion

    #region Constructors

    public UriImageSource(Uri uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
    }

    public UriImageSource(string url) : this(new Uri(url))
    {
    }

    #endregion

    #region ImageSource Implementation

    public override string GetCacheKey() => Uri.ToString();

    public override async Task<Stream?> GetStreamAsync()
    {
        IsLoading = true;
        OnLoadingStateChanged();

        try
        {
            // Intentar obtener del caché primero
            if (CachingEnabled)
            {
                var cachedStream = await GetFromCacheAsync();
                if (cachedStream != null)
                {
                    IsLoading = false;
                    IsLoaded = true;
                    Error = null;
                    OnLoadingStateChanged();
                    return cachedStream;
                }
            }

            // Descargar de la red
            var response = await _httpClient.GetAsync(Uri);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // Guardar en caché
            if (CachingEnabled)
            {
                await SaveToCacheAsync(bytes);
            }

            IsLoading = false;
            IsLoaded = true;
            Error = null;
            OnLoadingStateChanged();

            return new MemoryStream(bytes);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            IsLoading = false;
            IsLoaded = false;
            OnLoadingStateChanged();
            return null;
        }
    }

    #endregion

    #region Cache Methods

    private string GetCacheFilePath()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Uri.ToString())))[..32];
        var extension = Path.GetExtension(Uri.LocalPath);
        if (string.IsNullOrEmpty(extension))
            extension = ".img";

        return Path.Combine(_cacheDirectory, $"{hash}{extension}");
    }

    private async Task<Stream?> GetFromCacheAsync()
    {
        try
        {
            var cachePath = GetCacheFilePath();

            if (!File.Exists(cachePath))
                return null;

            var fileInfo = new FileInfo(cachePath);
            var age = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;

            if (age > CacheValidity)
            {
                // Caché expirado
                File.Delete(cachePath);
                return null;
            }

            // Leer del caché a memoria para evitar bloquear el archivo
            var bytes = await File.ReadAllBytesAsync(cachePath);
            return new MemoryStream(bytes);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveToCacheAsync(byte[] data)
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            var cachePath = GetCacheFilePath();
            await File.WriteAllBytesAsync(cachePath, data);
        }
        catch
        {
            // Ignorar errores de caché
        }
    }

    #endregion

    #region Static Methods

    /// <summary>
    /// Limpia todo el caché de imágenes
    /// </summary>
    public static void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignorar errores
        }
    }

    /// <summary>
    /// Limpia entradas de caché expiradas
    /// </summary>
    public static void ClearExpiredCache(TimeSpan maxAge)
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                return;

            var now = DateTime.UtcNow;
            foreach (var file in Directory.GetFiles(_cacheDirectory))
            {
                var fileInfo = new FileInfo(file);
                if (now - fileInfo.LastWriteTimeUtc > maxAge)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Ignorar errores
        }
    }

    #endregion

    #region Implicit Conversion

    public static implicit operator UriImageSource(Uri uri) => new(uri);

    #endregion
}
