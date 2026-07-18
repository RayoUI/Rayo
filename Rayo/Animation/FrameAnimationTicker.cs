namespace Rayo.Animation;

using System.Collections.Generic;

/// <summary>
/// Centralized per-frame animation dispatcher used by controls that need continuous updates.
/// </summary>
public static class FrameAnimationTicker
{
    private static readonly HashSet<IFrameAnimation> _animations = new();
    private static readonly Dictionary<IFrameAnimation, float> _accumulators = new();
    private static readonly object _syncLock = new();
    private const float DefaultTargetFps = 60f;

    /// <summary>
    /// Returns true when at least one animation is registered.
    /// </summary>
    public static bool HasActiveAnimations
    {
        get
        {
            lock (_syncLock)
            {
                return _animations.Count > 0;
            }
        }
    }

    /// <summary>
    /// Highest requested cadence among registered animations.
    /// Used by the host render loop to avoid waking faster than necessary.
    /// </summary>
    public static float RecommendedFps
    {
        get
        {
            lock (_syncLock)
            {
                if (_animations.Count == 0)
                {
                    return 0f;
                }

                float maxFps = 1f;
                foreach (var animation in _animations)
                {
                    float targetFps = GetTargetFps(animation);
                    maxFps = Math.Max(maxFps, targetFps);
                }

                return maxFps;
            }
        }
    }

    /// <summary>
    /// Registers an animation to receive per-frame ticks.
    /// </summary>
    public static void Register(IFrameAnimation animation)
    {
        if (animation == null)
        {
            return;
        }

        lock (_syncLock)
        {
            _animations.Add(animation);
            _accumulators.TryAdd(animation, 0f);
        }
    }

    /// <summary>
    /// Removes a previously registered animation.
    /// </summary>
    public static void Unregister(IFrameAnimation animation)
    {
        if (animation == null)
        {
            return;
        }

        lock (_syncLock)
        {
            _animations.Remove(animation);
            _accumulators.Remove(animation);
        }
    }

    /// <summary>
    /// Advances all registered animations.
    /// </summary>
    public static void Tick(float deltaTime)
    {
        if (deltaTime <= 0)
        {
            return;
        }

        IFrameAnimation[] snapshot;

        lock (_syncLock)
        {
            if (_animations.Count == 0)
            {
                return;
            }

            snapshot = new IFrameAnimation[_animations.Count];
            _animations.CopyTo(snapshot);
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            var animation = snapshot[i];
            float elapsedForAnimation = deltaTime;

            // Ordinary frame animations follow every host frame. Only animations that
            // explicitly opt into IFrameAnimationThrottle are cadence-limited.
            if (animation is IFrameAnimationThrottle throttled && throttled.TargetFps > 0f)
            {
                float targetFrameTime = 1f / throttled.TargetFps;
                lock (_syncLock)
                {
                    if (!_accumulators.TryGetValue(animation, out float accumulated))
                    {
                        continue;
                    }

                    accumulated += deltaTime;
                    if (accumulated + 0.000001f < targetFrameTime)
                    {
                        _accumulators[animation] = accumulated;
                        continue;
                    }

                    int elapsedSteps = Math.Max(1, (int)MathF.Floor(
                        (accumulated + 0.000001f) / targetFrameTime));
                    elapsedForAnimation = elapsedSteps * targetFrameTime;
                    _accumulators[animation] = Math.Max(0, accumulated - elapsedForAnimation);
                }
            }

            animation.Tick(elapsedForAnimation);
        }
    }

    private static float GetTargetFps(IFrameAnimation animation)
    {
        if (animation is IFrameAnimationThrottle throttled && throttled.TargetFps > 0f)
        {
            return throttled.TargetFps;
        }

        return DefaultTargetFps;
    }
}
