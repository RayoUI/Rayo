namespace Rayo.Animation;

using Rayo.Core;
using Rayo.Rendering;

/// <summary>
/// Animates a float property of the element
/// </summary>
public class FloatAnimation : AnimationBase
{
    public float From { get; set; }
    public float To { get; set; }
    public Action<VisualElement, float> SetValue { get; set; } = null!;

    public FloatAnimation(float from, float to, Action<VisualElement, float> setValue)
    {
        From = from;
        To = to;
        SetValue = setValue;
    }

    protected override void ApplyAnimation(float progress)
    {
        if (Target == null) return;

        float value = From + (To - From) * progress;
        SetValue(Target, value);
        Target.MarkNeedsPaint();
    }
}

/// <summary>
/// Animates the color of an element
/// </summary>
public class ColorAnimation : AnimationBase
{
    public Color From { get; set; }
    public Color To { get; set; }
    public Action<VisualElement, Color> SetValue { get; set; } = null!;

    public ColorAnimation(Color from, Color to, Action<VisualElement, Color> setValue)
    {
        From = from;
        To = to;
        SetValue = setValue;
    }

    protected override void ApplyAnimation(float progress)
    {
        if (Target == null) return;

        Color value = new Color(
            From.R + (To.R - From.R) * progress,
            From.G + (To.G - From.G) * progress,
            From.B + (To.B - From.B) * progress,
            From.A + (To.A - From.A) * progress
        );

        SetValue(Target, value);
        Target.MarkNeedsPaint();
    }
}

/// <summary>
/// Animates the opacity of the element
/// </summary>
public class FadeAnimation : FloatAnimation
{
    public FadeAnimation(float from, float to)
        : base(from, to, (element, value) =>
        {
            // Specific implementation for fade
            // Would require an Opacity property in UIElementBase
        })
    {
    }
}

/// <summary>
/// Animates the position of the element
/// </summary>
public class MoveAnimation : AnimationBase
{
    public float FromX { get; set; }
    public float FromY { get; set; }
    public float ToX { get; set; }
    public float ToY { get; set; }

    public MoveAnimation(float fromX, float fromY, float toX, float toY)
    {
        FromX = fromX;
        FromY = fromY;
        ToX = toX;
        ToY = toY;
    }

    protected override void ApplyAnimation(float progress)
    {
        if (Target == null) return;

        Target.X = FromX + (ToX - FromX) * progress;
        Target.Y = FromY + (ToY - FromY) * progress;
        Target.MarkNeedsLayout();
    }
}

/// <summary>
/// Animates the size of the element
/// </summary>
public class ScaleAnimation : AnimationBase
{
    public float FromWidth { get; set; }
    public float FromHeight { get; set; }
    public float ToWidth { get; set; }
    public float ToHeight { get; set; }

    public ScaleAnimation(float fromWidth, float fromHeight, float toWidth, float toHeight)
    {
        FromWidth = fromWidth;
        FromHeight = fromHeight;
        ToWidth = toWidth;
        ToHeight = toHeight;
    }

    protected override void ApplyAnimation(float progress)
    {
        if (Target == null) return;

        Target.Width = FromWidth + (ToWidth - FromWidth) * progress;
        Target.Height = FromHeight + (ToHeight - FromHeight) * progress;
        Target.MarkNeedsLayout();
    }
}

/// <summary>
/// Animates multiple properties simultaneously
/// </summary>
public class ParallelAnimation : AnimationBase
{
    private List<AnimationBase> _animations = new();

    public ParallelAnimation Add(AnimationBase animation)
    {
        _animations.Add(animation);
        animation.Target = Target;
        return this;
    }

    public override void Start()
    {
        base.Start();
        foreach (var anim in _animations)
        {
            anim.Duration = Duration;
            anim.Delay = Delay;
            anim.EasingFunction = EasingFunction;
            anim.Target = Target;
            anim.Start();
        }
    }

    protected override void ApplyAnimation(float progress)
    {
        // Parallel animations are updated in Update
    }

    public override bool Update(float deltaTime)
    {
        if (!IsRunning || IsPaused)
            return IsRunning;

        bool anyRunning = false;
        foreach (var anim in _animations)
        {
            if (anim.Update(deltaTime))
                anyRunning = true;
        }

        if (!anyRunning)
        {
            IsRunning = false;
            InvokeOnComplete();
            return false;
        }

        return true;
    }
}

/// <summary>
/// Executes animations sequentially
/// </summary>
public class SequenceAnimation : AnimationBase
{
    private List<AnimationBase> _animations = new();
    private int _currentIndex = 0;

    public SequenceAnimation Add(AnimationBase animation)
    {
        _animations.Add(animation);
        return this;
    }

    public override void Start()
    {
        base.Start();
        _currentIndex = 0;
        if (_animations.Count > 0)
        {
            _animations[0].Target = Target;
            _animations[0].Start();
        }
    }

    protected override void ApplyAnimation(float progress)
    {
        // Sequential animations are updated in Update
    }

    public override bool Update(float deltaTime)
    {
        if (!IsRunning || IsPaused || _animations.Count == 0)
            return IsRunning;

        // Update current animation
        if (!_animations[_currentIndex].Update(deltaTime))
        {
            // Current animation completed, move to the next
            _currentIndex++;

            if (_currentIndex >= _animations.Count)
            {
                // All animations completed
                IsRunning = false;
                InvokeOnComplete();
                return false;
            }

            // Start next animation
            _animations[_currentIndex].Target = Target;
            _animations[_currentIndex].Start();
        }

        return true;
    }
}
