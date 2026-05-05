using Silk.NET.OpenGL;
using System.Numerics;

namespace Rayo.Rendering.OpenGL;

internal class OpenGLShaderProgram : IShaderProgram
{
    private readonly GL _gl;
    private uint _programId;
    private bool _isDisposed;
    private readonly Dictionary<string, int> _uniformLocationCache = new();

    public OpenGLShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));

        if (string.IsNullOrWhiteSpace(vertexSource))
            throw new ArgumentException("Vertex shader source cannot be empty", nameof(vertexSource));

        if (string.IsNullOrWhiteSpace(fragmentSource))
            throw new ArgumentException("Fragment shader source cannot be empty", nameof(fragmentSource));

        _programId = CreateProgram(vertexSource, fragmentSource);
    }

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        // Check for linking errors
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            _gl.DeleteProgram(program);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
            throw new Exception($"Shader program linking failed: {infoLog}");
        }

        // Shaders are already linked to the program, we can delete them
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        // Check for compilation errors
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            _gl.DeleteShader(shader);
            string shaderTypeName = type == ShaderType.VertexShader ? "Vertex" : "Fragment";
            throw new Exception($"{shaderTypeName} shader compilation failed: {infoLog}");
        }

        return shader;
    }

    public void Use()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        _gl.UseProgram(_programId);
    }

    private int GetUniformLocation(string name)
    {
        if (_uniformLocationCache.TryGetValue(name, out int location))
            return location;

        location = _gl.GetUniformLocation(_programId, name);

        if (location == -1)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Uniform '{name}' not found in shader program");
        }

        _uniformLocationCache[name] = location;
        return location;
    }

    public void SetUniform(string name, int value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform1(location, value);
        }
    }

    public void SetUniform(string name, float value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform1(location, value);
        }
    }

    public void SetUniform(string name, Vector2 value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform2(location, value.X, value.Y);
        }
    }

    public void SetUniform(string name, Vector3 value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }
    }

    public void SetUniform(string name, Vector4 value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }
    }

    public void SetUniform(string name, Matrix4x4 value)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            unsafe
            {
                _gl.UniformMatrix4(location, 1, false, (float*)&value);
            }
        }
    }

    /// <summary>
    /// Sets an array of floats as uniform
    /// </summary>
    public void SetUniformArray(string name, float[] values)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        if (values == null || values.Length == 0)
            return;

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform1(location, values);
        }
    }

    /// <summary>
    /// Sets an array of integers as uniform
    /// </summary>
    public void SetUniformArray(string name, int[] values)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        if (values == null || values.Length == 0)
            return;

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            _gl.Uniform1(location, values);
        }
    }

    /// <summary>
    /// Sets an array of Vector2 as uniform
    /// </summary>
    public void SetUniformArray(string name, Vector2[] values)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        if (values == null || values.Length == 0)
            return;

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            // Convert Vector2[] to float[]
            float[] floatArray = new float[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                floatArray[i * 2] = values[i].X;
                floatArray[i * 2 + 1] = values[i].Y;
            }
            _gl.Uniform2(location, floatArray);
        }
    }

    /// <summary>
    /// Sets an array of Vector3 as uniform
    /// </summary>
    public void SetUniformArray(string name, Vector3[] values)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OpenGLShaderProgram));

        if (values == null || values.Length == 0)
            return;

        int location = GetUniformLocation(name);
        if (location != -1)
        {
            // Convert Vector3[] to float[]
            float[] floatArray = new float[values.Length * 3];
            for (int i = 0; i < values.Length; i++)
            {
                floatArray[i * 3] = values[i].X;
                floatArray[i * 3 + 1] = values[i].Y;
                floatArray[i * 3 + 2] = values[i].Z;
            }
            _gl.Uniform3(location, floatArray);
        }
    }

    /// <summary>
    /// Gets information about the program
    /// </summary>
    public string GetProgramInfo()
    {
        _gl.GetProgram(_programId, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
        _gl.GetProgram(_programId, ProgramPropertyARB.ActiveAttributes, out int attributeCount);

        return $"""
            Shader Program {_programId}:
            - Active Uniforms: {uniformCount}
            - Active Attributes: {attributeCount}
            - Cached Uniform Locations: {_uniformLocationCache.Count}
            """;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_programId != 0)
        {
            _gl.DeleteProgram(_programId);
            _programId = 0;
        }

        _uniformLocationCache.Clear();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~OpenGLShaderProgram()
    {
        if (!_isDisposed && _programId != 0)
        {
            System.Diagnostics.Debug.WriteLine($"OpenGLShaderProgram {_programId} was not disposed correctly");
        }
    }
}