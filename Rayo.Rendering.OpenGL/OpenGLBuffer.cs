using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace Rayo.Rendering.OpenGL;

internal class OpenGLBuffer : IBuffer
{
    private readonly GL _gl;
    private uint _bufferId;
    private readonly BufferTargetARB _target;
    private bool _isDisposed;

    public int SizeInBytes { get; private set; }

    public OpenGLBuffer(GL gl, BufferTargetARB target, int sizeInBytes)
    {
        if (sizeInBytes <= 0)
            throw new ArgumentException("Buffer size must be positive", nameof(sizeInBytes));

        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _target = target;
        SizeInBytes = sizeInBytes;

        _bufferId = _gl.GenBuffer();
        _gl.BindBuffer(_target, _bufferId);
        unsafe
        {
            _gl.BufferData(_target, (nuint)sizeInBytes, null, BufferUsageARB.DynamicDraw);
        }
        _gl.BindBuffer(_target, 0);
    }

    public void SetData<T>(T[] data) where T : struct
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        int elementSize = Marshal.SizeOf<T>();
        int totalSize = data.Length * elementSize;

        _gl.BindBuffer(_target, _bufferId);
        unsafe
        {
            fixed (void* ptr = data)
            {
                _gl.BufferData(_target, (nuint)totalSize, ptr, BufferUsageARB.DynamicDraw);
            }
        }
        _gl.BindBuffer(_target, 0);

        SizeInBytes = totalSize;
    }

    public void SetSubData<T>(T[] data, int offsetInBytes, int sizeInBytes) where T : struct
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (offsetInBytes < 0)
            throw new ArgumentException("Offset cannot be negative", nameof(offsetInBytes));

        if (sizeInBytes <= 0)
            throw new ArgumentException("Size must be positive", nameof(sizeInBytes));

        if (offsetInBytes + sizeInBytes > SizeInBytes)
            throw new ArgumentException("Update region exceeds buffer size");

        _gl.BindBuffer(_target, _bufferId);
        unsafe
        {
            fixed (void* ptr = data)
            {
                _gl.BufferSubData(_target, offsetInBytes, (nuint)sizeInBytes, ptr);
            }
        }
        _gl.BindBuffer(_target, 0);
    }

    /// <summary>
    /// Actualiza todo el buffer con nuevos datos (m�s eficiente que SetData si el tama�o es el mismo)
    /// </summary>
    public void UpdateData<T>(T[] data) where T : struct
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        int elementSize = Marshal.SizeOf<T>();
        int totalSize = data.Length * elementSize;

        if (totalSize != SizeInBytes)
        {
            // Si el tama�o cambi�, recrear el buffer
            SetData(data);
        }
        else
        {
            // Si el tama�o es el mismo, solo actualizar
            _gl.BindBuffer(_target, _bufferId);
            unsafe
            {
                fixed (void* ptr = data)
                {
                    _gl.BufferSubData(_target, 0, (nuint)totalSize, ptr);
                }
            }
            _gl.BindBuffer(_target, 0);
        }
    }

    /// <summary>
    /// Lee datos del buffer de la GPU (�til para debugging)
    /// </summary>
    public T[] GetData<T>(int count) where T : struct
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        int elementSize = Marshal.SizeOf<T>();
        int totalSize = count * elementSize;

        if (totalSize > SizeInBytes)
            throw new ArgumentException("Requested data size exceeds buffer size");

        T[] data = new T[count];

        _gl.BindBuffer(_target, _bufferId);
        unsafe
        {
            fixed (void* ptr = data)
            {
                _gl.GetBufferSubData(_target, 0, (nuint)totalSize, ptr);
            }
        }
        _gl.BindBuffer(_target, 0);

        return data;
    }

    /// <summary>
    /// Cambia el modo de uso del buffer
    /// </summary>
    public void SetUsage(BufferUsageARB usage)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        // Para cambiar el usage, necesitamos recrear el buffer con los mismos datos
        // Por simplicidad, solo cambiamos el buffer a vac�o con el nuevo usage
        _gl.BindBuffer(_target, _bufferId);
        unsafe
        {
            _gl.BufferData(_target, (nuint)SizeInBytes, null, usage);
        }
        _gl.BindBuffer(_target, 0);
    }

    public void Bind()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        _gl.BindBuffer(_target, _bufferId);
    }

    public void Unbind()
    {
        _gl.BindBuffer(_target, 0);
    }

    /// <summary>
    /// Copia datos de este buffer a otro
    /// </summary>
    public void CopyTo(OpenGLBuffer destination, int readOffset, int writeOffset, int size)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLBuffer));

        if (destination == null)
            throw new ArgumentNullException(nameof(destination));

        if (destination._isDisposed)
            throw new ObjectDisposedException(nameof(destination));

        if (readOffset < 0 || writeOffset < 0 || size <= 0)
            throw new ArgumentException("Invalid copy parameters");

        if (readOffset + size > SizeInBytes)
            throw new ArgumentException("Read region exceeds source buffer size");

        if (writeOffset + size > destination.SizeInBytes)
            throw new ArgumentException("Write region exceeds destination buffer size");

        _gl.BindBuffer(BufferTargetARB.CopyReadBuffer, _bufferId);
        _gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, destination._bufferId);
        _gl.CopyBufferSubData(
                CopyBufferSubDataTarget.CopyReadBuffer,
                CopyBufferSubDataTarget.CopyWriteBuffer,
                readOffset,
                writeOffset,
                (nuint)size
      );
        _gl.BindBuffer(BufferTargetARB.CopyReadBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.CopyWriteBuffer, 0);
    }
    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_bufferId != 0)
        {
            _gl.DeleteBuffer(_bufferId);
            _bufferId = 0;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~OpenGLBuffer()
    {
        if (!_isDisposed && _bufferId != 0)
        {
            System.Diagnostics.Debug.WriteLine($"OpenGLBuffer {_bufferId} no fue disposed correctamente");
        }
    }
}