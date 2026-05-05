namespace Rayo.Controls;

/// <summary>
/// Clase base abstracta para fuentes de imágenes.
/// Permite cargar imágenes desde archivos locales, URLs de red o streams.
/// </summary>
public abstract class ImageSource
{
    #region Properties

    /// <summary>
    /// Indica si la imagen está cargando
    /// </summary>
    public bool IsLoading { get; protected set; }

    /// <summary>
    /// Indica si la imagen se cargó correctamente
    /// </summary>
    public bool IsLoaded { get; protected set; }

    /// <summary>
    /// Mensaje de error si la carga falló
    /// </summary>
    public string? Error { get; protected set; }

    #endregion

    #region Events

    /// <summary>
    /// Evento que se dispara cuando cambia el estado de carga
    /// </summary>
    public event Action? LoadingStateChanged;

    protected void OnLoadingStateChanged() => LoadingStateChanged?.Invoke();

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Obtiene un stream con los datos de la imagen
    /// </summary>
    public abstract Task<Stream?> GetStreamAsync();

    /// <summary>
    /// Obtiene una clave única para el caché
    /// </summary>
    public abstract string GetCacheKey();

    #endregion

    #region Implicit Conversions

    /// <summary>
    /// Conversión implícita desde string.
    /// Si empieza con http:// o https://, crea UriImageSource.
    /// De lo contrario, crea FileImageSource.
    /// </summary>
    public static implicit operator ImageSource(string source)
    {
        if (string.IsNullOrEmpty(source))
            return new FileImageSource("");

        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new UriImageSource(new Uri(source));
        }

        return new FileImageSource(source);
    }

    /// <summary>
    /// Conversión implícita desde Uri
    /// </summary>
    public static implicit operator ImageSource(Uri uri) => new UriImageSource(uri);

    #endregion
}
