namespace Rayo.Animation;

using System.Collections.Generic;

/// <summary>
/// Centralized per-frame animation dispatcher used by controls that need continuous updates.
/// </summary>
public static class FrameAnimationTicker
{
    private static readonly HashSet<IFrameAnimation> _animations = new();
    private static readonly object _syncLock = new();

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
            snapshot[i].Tick(deltaTime);
        }
    }
}
