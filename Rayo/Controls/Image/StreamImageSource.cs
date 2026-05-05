namespace Rayo.Controls;

/// <summary>
/// Fuente de imagen desde un stream de datos
/// </summary>
public class StreamImageSource : ImageSource
{
    #region Fields

    private readonly Func<Task<Stream?>> _streamFactory;
    private readonly string _cacheKey;

    #endregion

    #region Constructors

    /// <summary>
    /// Crea un StreamImageSource con una función que provee el stream
    /// </summary>
    /// <param name="streamFactory">Función que retorna el stream de la imagen</param>
    /// <param name="cacheKey">Clave opcional para el caché (si no se provee, no se cachea)</param>
    public StreamImageSource(Func<Task<Stream?>> streamFactory, string? cacheKey = null)
    {
        _streamFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
        _cacheKey = cacheKey ?? $"stream://{Guid.NewGuid()}";
    }

    /// <summary>
    /// Crea un StreamImageSource desde un stream existente
    /// </summary>
    /// <param name="stream">Stream con los datos de la imagen</param>
    /// <param name="cacheKey">Clave opcional para el caché</param>
    public StreamImageSource(Stream stream, string? cacheKey = null)
        : this(() => Task.FromResult<Stream?>(stream), cacheKey)
    {
    }

    /// <summary>
    /// Crea un StreamImageSource desde bytes
    /// </summary>
    /// <param name="bytes">Bytes de la imagen</param>
    /// <param name="cacheKey">Clave opcional para el caché</param>
    public StreamImageSource(byte[] bytes, string? cacheKey = null)
        : this(() => Task.FromResult<Stream?>(new MemoryStream(bytes)), cacheKey)
    {
    }

    #endregion

    #region ImageSource Implementation

    public override string GetCacheKey() => _cacheKey;

    public override async Task<Stream?> GetStreamAsync()
    {
        IsLoading = true;
        OnLoadingStateChanged();

        try
        {
            var stream = await _streamFactory();

            if (stream == null)
            {
                Error = "Stream factory returned null";
                IsLoading = false;
                IsLoaded = false;
                OnLoadingStateChanged();
                return null;
            }

            IsLoading = false;
            IsLoaded = true;
            Error = null;
            OnLoadingStateChanged();

            return stream;
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
}
