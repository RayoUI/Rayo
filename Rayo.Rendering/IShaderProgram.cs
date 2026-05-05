using System.Numerics;

namespace Rayo.Rendering;

/// <summary>
/// Represents a backend-independent shader program.
/// </summary>
public interface IShaderProgram : IDisposable
{
    /// <summary>
    /// Activates this shader program.
    /// </summary>
    void Use();

    /// <summary>
    /// Sets an integer uniform.
    /// </summary>
    void SetUniform(string name, int value);

    /// <summary>
    /// Sets a float uniform.
    /// </summary>
    void SetUniform(string name, float value);

    /// <summary>
    /// Sets a Vector2 uniform.
    /// </summary>
    void SetUniform(string name, Vector2 value);

    /// <summary>
    /// Sets a Vector3 uniform.
    /// </summary>
    void SetUniform(string name, Vector3 value);

    /// <summary>
    /// Sets a Vector4 uniform.
    /// </summary>
    void SetUniform(string name, Vector4 value);

    /// <summary>
    /// Sets a Matrix4x4 uniform.
    /// </summary>
    void SetUniform(string name, Matrix4x4 value);
}