namespace Rayo.Animation;

using Rayo.Core;

/// <summary>
/// Abstract base class for all animations.
/// Inspired by MAUI Animation and WPF Storyboard.
/// </summary>
public abstract class AnimationBase
{
    /// <summary>
    /// Duration of the animation in milliseconds.
    /// </summary>
    public float Duration { get; set; } = 300;

    /// <summary>
    /// Delay before starting the animation in milliseconds.
    /// </summary>
    public float Delay { get; set; } = 0;

    /// <summary>
    /// Easing function for the animation.
    /// </summary>
    public Func<float, float> EasingFunction { get; set; } = Easing.OutQuad;

    /// <summary>
    /// Whether the animation should repeat.
    /// </summary>
    public bool Repeat { get; set; } = false;

    /// <summary>
    /// Number of repetitions (-1 = infinite).
    /// </summary>
    public int RepeatCount { get; set; } = 1;

    /// <summary>
    /// Whether the animation should auto-reverse direction on each repeat.
    /// </summary>
    public bool AutoReverse { get; set; } = false;

    /// <summary>
    /// If the animation is currently running.
    /// </summary>
    public bool IsRunning { get; protected set; }

    /// <summary>
    /// If the animation is paused.
    /// </summary>
    public bool IsPaused { get; protected set; }

    /// <summary>
    /// Elapsed time since the animation started (including delay).
    /// </summary>
    protected float ElapsedTime { get; set; }

    /// <summary>
    /// Number of completed repetitions.
    /// </summary>
    protected int CompletedRepeats { get; set; }

    /// <summary>
    /// Current direction (1 = forward, -1 = reverse).
    /// </summary>
    protected int CurrentDirection { get; set; } = 1;

    /// <summary>
    /// Element to which this animation belongs.
    /// </summary>
    public VisualElement? Target { get; set; }

    /// <summary>
    /// Event triggered when the animation completes.
    /// </summary>
    public event Action? OnComplete;

    /// <summary>
    /// Event triggered whenever the animation updates.
    /// </summary>
    public event Action<float>? OnUpdate;

    /// <summary>
    /// Protected method to invoke OnComplete from derived classes.
    /// </summary>
    protected void InvokeOnComplete()
    {
        OnComplete?.Invoke();
    }

    /// <summary>
    /// Protected method to invoke OnUpdate from derived classes.
    /// </summary>
    protected void InvokeOnUpdate(float progress)
    {
        OnUpdate?.Invoke(progress);
    }

    /// <summary>
    /// Starts the animation.
    /// </summary>
    public virtual void Start()
    {
        IsRunning = true;
        IsPaused = false;
        ElapsedTime = 0;
        CompletedRepeats = 0;
        CurrentDirection = 1;
    }

    /// <summary>
    /// Pauses the animation.
    /// </summary>
    public virtual void Pause()
    {
        IsPaused = true;
    }

    /// <summary>
    /// Resumes the animation.
    /// </summary>
    public virtual void Resume()
    {
        IsPaused = false;
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public virtual void Stop()
    {
        IsRunning = false;
        IsPaused = false;
        ElapsedTime = 0;
    }

    /// <summary>
    /// Updates the animation with the elapsed time.
    /// </summary>
    /// <param name="deltaTime">Time since the last frame in milliseconds.</param>
    /// <returns>True if the animation is still active.</returns>
    public virtual bool Update(float deltaTime)
    {
        if (!IsRunning || IsPaused)
            return IsRunning;

        ElapsedTime += deltaTime;

        // Wait for the delay
        if (ElapsedTime < Delay)
            return true;

        // Calculate progress (0-1)
        float animationTime = ElapsedTime - Delay;
        float progress = Math.Min(1.0f, animationTime / Duration);

        // Apply easing
        float easedProgress = EasingFunction(progress);

        // Apply direction if in reverse
        if (CurrentDirection < 0)
            easedProgress = 1 - easedProgress;

        // Apply the animation
        ApplyAnimation(easedProgress);

        // Notify update
        InvokeOnUpdate(easedProgress);

        // Check if completed
        if (progress >= 1.0f)
        {
            CompletedRepeats++;

            // Check if it should repeat
            if (Repeat && (RepeatCount < 0 || CompletedRepeats < RepeatCount))
            {
                // Restart animation
                ElapsedTime = Delay;

                // Reverse direction if AutoReverse is enabled
                if (AutoReverse)
                    CurrentDirection *= -1;

                return true;
            }

            // Animation complete
            IsRunning = false;
            InvokeOnComplete();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Abstract method that applies the animation to the element.
    /// </summary>
    /// <param name="progress">Progress from 0 to 1 with easing applied.</param>
    protected abstract void ApplyAnimation(float progress);
}
