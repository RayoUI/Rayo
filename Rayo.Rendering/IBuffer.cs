namespace Rayo.Rendering;

/// <summary>
/// Representa un buffer de datos en la GPU
/// </summary>
public interface IBuffer : IDisposable
{
    /// <summary>
    /// Tamaño del buffer en bytes
    /// </summary>
    int SizeInBytes { get; }

    /// <summary>
    /// Actualiza los datos del buffer
    /// </summary>
    void SetData<T>(T[] data) where T : struct;

    /// <summary>
    /// Actualiza una porción de los datos del buffer
    /// </summary>
    void SetSubData<T>(T[] data, int offsetInBytes, int sizeInBytes) where T : struct;

    /// <summary>
    /// Bind este buffer para uso
    /// </summary>
    void Bind();

    /// <summary>
    /// Unbind este buffer
    /// </summary>
    void Unbind();
}

/// <summary>
/// Tipo de buffer
/// </summary>
public enum BufferType
{
    VertexBuffer,
    IndexBuffer,
    UniformBuffer
}
