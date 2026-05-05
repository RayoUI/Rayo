using Silk.NET.OpenGL;

namespace Rayo.Rendering.OpenGL;

public class OpenGLTexture : ITexture
{
    private readonly GL _gl;
    private uint _textureId;
    private bool _isDisposed;

    public int Width { get; }
    public int Height { get; }
    public TextureFormat Format { get; }

    public OpenGLTexture(GL gl, int width, int height, byte[] data, TextureFormat format)
    {
        if (width <= 0 || height <= 0)
 throw new ArgumentException("Texture dimensions must be positive");

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
   Width = width;
      Height = height;
        Format = format;

        _textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _textureId);

        var (internalFormat, pixelFormat, pixelType) = GetGLFormat(format);

        unsafe
        {
            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    (int)internalFormat,
                    (uint)width,
                    (uint)height,
                    0,
                    pixelFormat,
                    pixelType,
                    ptr
                );
            }
        }

   // Configuraci�n por defecto para texturas UI
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
     _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    // Constructor interno para TextureManager
    internal OpenGLTexture(GL gl, uint textureId, int width, int height, TextureFormat format = TextureFormat.RGBA8)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _textureId = textureId;
        Width = width;
        Height = height;
        Format = format;
    }

    /// <summary>
    /// Creates an OpenGLTexture from an existing texture ID.
    /// Useful for wrapping FBO textures or textures created externally.
    /// </summary>
    public OpenGLTexture(GL gl, uint textureId, int width, int height, bool ownsTexture = false)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _textureId = textureId;
        Width = width;
        Height = height;
        Format = TextureFormat.RGBA8; // Assume RGBA8 for now

        // If we don't own the texture, we shouldn't delete it in Dispose.
        // But currently Dispose deletes it.
        // We should add a flag or just assume the caller knows what they are doing.
        // For Viewport3D, we want to manage the texture lifecycle in Viewport3D,
        // so we might NOT want OpenGLTexture to delete it when the wrapper is disposed.
        // However, Viewport3D creates a new wrapper every frame? No, that would be bad.
        // Viewport3D should cache the wrapper.

        // Let's add a field to control disposal
        _ownsTexture = ownsTexture;
    }

    private bool _ownsTexture = true;

    public uint Id => _textureId;
    internal uint TextureId => _textureId;

    /// <summary>
    /// Actualiza los datos de la textura
    /// </summary>
    public void UpdateData(byte[] data, int x = 0, int y = 0, int? width = null, int? height = null)
    {
        if (_isDisposed)
   throw new ObjectDisposedException(nameof(OpenGLTexture));

  if (data == null)
 throw new ArgumentNullException(nameof(data));

  int w = width ?? Width;
        int h = height ?? Height;

        if (x < 0 || y < 0 || x + w > Width || y + h > Height)
            throw new ArgumentException("Update region out of bounds");

        var (_, pixelFormat, pixelType) = GetGLFormat(Format);

     _gl.BindTexture(TextureTarget.Texture2D, _textureId);

        unsafe
        {
      fixed (byte* ptr = data)
       {
           _gl.TexSubImage2D(
         TextureTarget.Texture2D,
           0,
    x,
               y,
      (uint)w,
          (uint)h,
            pixelFormat,
pixelType,
   ptr
             );
            }
}

      _gl.BindTexture(TextureTarget.Texture2D, 0);
}

    /// <summary>
    /// Configura el filtro de minificaci�n
    /// </summary>
    public void SetMinFilter(TextureMinFilter filter)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLTexture));

_gl.BindTexture(TextureTarget.Texture2D, _textureId);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
 /// Configura el filtro de magnificaci�n
    /// </summary>
    public void SetMagFilter(TextureMagFilter filter)
    {
      if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLTexture));

        _gl.BindTexture(TextureTarget.Texture2D, _textureId);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
      _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
 /// Configura el modo de wrap
    /// </summary>
    public void SetWrapMode(TextureWrapMode wrapS, TextureWrapMode wrapT)
    {
        if (_isDisposed)
      throw new ObjectDisposedException(nameof(OpenGLTexture));

        _gl.BindTexture(TextureTarget.Texture2D, _textureId);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

 /// <summary>
    /// Genera mipmaps para la textura
    /// </summary>
    public void GenerateMipmaps()
    {
        if (_isDisposed)
    throw new ObjectDisposedException(nameof(OpenGLTexture));

        _gl.BindTexture(TextureTarget.Texture2D, _textureId);
   _gl.GenerateMipmap(TextureTarget.Texture2D);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// Bind la textura a una unidad de textura espec�fica
    /// </summary>
    public void Bind(uint textureUnit = 0)
    {
     if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLTexture));

        _gl.ActiveTexture(TextureUnit.Texture0 + (int)textureUnit);
        _gl.BindTexture(TextureTarget.Texture2D, _textureId);
    }

    /// <summary>
    /// Unbind la textura
    /// </summary>
    public void Unbind()
    {
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private (InternalFormat, PixelFormat, PixelType) GetGLFormat(TextureFormat format)
    {
  return format switch
   {
    TextureFormat.RGBA8 => (InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte),
       TextureFormat.RGB8 => (InternalFormat.Rgb, PixelFormat.Rgb, PixelType.UnsignedByte),
      TextureFormat.Alpha8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
    TextureFormat.R8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
            _ => (InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte)
};
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_textureId != 0 && _ownsTexture)
        {
            _gl?.DeleteTexture(_textureId);
            _textureId = 0;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~OpenGLTexture()
    {
     // En caso de que no se llame Dispose expl�citamente
        if (!_isDisposed && _textureId != 0)
        {
 // Nota: No es seguro llamar a OpenGL desde el finalizador
   System.Diagnostics.Debug.WriteLine($"OpenGLTexture {_textureId} no fue disposed correctamente");
        }
    }
}