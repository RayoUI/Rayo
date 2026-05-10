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

/// <summary>
/// Optional cadence hint for frame-driven components that do not need to run at the full render rate.
/// </summary>
public interface IFrameAnimationThrottle
{
    /// <summary>
    /// Desired maximum tick frequency. Values less than or equal to zero disable throttling.
    /// </summary>
    float TargetFps { get; }
}
