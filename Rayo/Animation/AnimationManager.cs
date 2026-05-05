namespace Rayo.Animation;

using Rayo.Core;

/// <summary>
/// Global animation manager
/// Updates all active animations every frame
/// </summary>
public class AnimationManager
{
    private static AnimationManager? _instance;
    public static AnimationManager Instance => _instance ??= new AnimationManager();

    private List<AnimationBase> _activeAnimations = new();
    private List<AnimationBase> _animationsToRemove = new();
    private object _lock = new();

    /// <summary>
    /// Registers an animation to be executed
    /// </summary>
    public void Animate(AnimationBase animation)
    {
        lock (_lock)
        {
            animation.Start();
            _activeAnimations.Add(animation);
        }
    }

    /// <summary>
    /// Stops all animations for an element
    /// </summary>
    public void StopAnimations(VisualElement element)
    {
        lock (_lock)
        {
            foreach (var anim in _activeAnimations)
            {
                if (anim.Target == element)
                {
                    anim.Stop();
                    _animationsToRemove.Add(anim);
                }
            }
        }
    }

    /// <summary>
    /// Stops a specific animation
    /// </summary>
    public void StopAnimation(AnimationBase animation)
    {
        lock (_lock)
        {
            animation.Stop();
            _animationsToRemove.Add(animation);
        }
    }

    /// <summary>
    /// Updates all active animations
    /// Must be called every frame
    /// </summary>
    /// <param name="deltaTime">Time since the last frame in milliseconds</param>
    public void Update(float deltaTime)
    {
        lock (_lock)
        {
            // Clear animations marked for removal
            foreach (var anim in _animationsToRemove)
            {
                _activeAnimations.Remove(anim);
            }
            _animationsToRemove.Clear();

            // Update active animations
            for (int i = _activeAnimations.Count - 1; i >= 0; i--)
            {
                var anim = _activeAnimations[i];

                if (!anim.Update(deltaTime))
                {
                    // Animation completed
                    _activeAnimations.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Clears all animations
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var anim in _activeAnimations)
            {
                anim.Stop();
            }
            _activeAnimations.Clear();
            _animationsToRemove.Clear();
        }
    }

    /// <summary>
    /// Gets the number of active animations
    /// </summary>
    public int ActiveAnimationCount
    {
        get
        {
            lock (_lock)
            {
                return _activeAnimations.Count;
            }
        }
    }
}
