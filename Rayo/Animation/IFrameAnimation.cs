namespace Rayo.Animation;

/// <summary>
/// Defines a component that requires per-frame animation updates.
/// </summary>
public interface IFrameAnimation
{
    /// <summary>
    /// Advances the animation state by the specified delta time (in seconds).
    /// </summary>
    /// <param name="deltaTime">Elapsed time in seconds since the previous tick.</param>
    void Tick(float deltaTime);
}
