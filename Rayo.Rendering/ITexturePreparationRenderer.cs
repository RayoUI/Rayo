namespace Rayo.Rendering;

/// <summary>
/// Marker for renderer-specific image data that has been decoded without using
/// the graphics device and is ready to be uploaded on the render thread.
/// </summary>
public interface IPreparedTexture
{
    string CacheKey { get; }
}

/// <summary>
/// Optional renderer capability that separates CPU image decoding from GPU
/// texture creation.
/// </summary>
public interface ITexturePreparationRenderer
{
    /// <summary>Returns an already uploaded texture when the renderer cache contains it.</summary>
    ITexture? TryGetLoadedTexture(string cacheKey);

    /// <summary>Decodes encoded image bytes without accessing the graphics device.</summary>
    Task<IPreparedTexture?> PrepareTextureAsync(
        ReadOnlyMemory<byte> encodedImage,
        string cacheKey,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads prepared pixels. This method must be called on the render thread.</summary>
    ITexture? UploadPreparedTexture(IPreparedTexture preparedTexture);
}
