namespace Rayo.Animation;

using Rayo.Core;
using Rayo.Rendering;

/// <summary>
/// Fluent extensions for animations on UIElements
/// </summary>
public static class AnimationExtensions
{
    /// <summary>
    /// Animates a float property of the element
    /// </summary>
    public static T AnimateFloat<T>(this T element, float from, float to,
        Action<T, float> setValue, float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        var animation = new FloatAnimation(from, to, (e, v) => setValue((T)e, v))
        {
            Target = element,
            Duration = duration,
            EasingFunction = easing ?? Easing.OutQuad
        };
        AnimationManager.Instance.Animate(animation);
        return element;
    }

    /// <summary>
    /// Animates the color of the element
    /// </summary>
    public static T AnimateColor<T>(this T element, Color from, Color to,
        Action<T, Color> setValue, float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        var animation = new ColorAnimation(from, to, (e, c) => setValue((T)e, c))
        {
            Target = element,
            Duration = duration,
            EasingFunction = easing ?? Easing.OutQuad
        };
        AnimationManager.Instance.Animate(animation);
        return element;
    }

    /// <summary>
    /// Animates the position of the element
    /// </summary>
    public static T AnimatePosition<T>(this T element, float toX, float toY,
        float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        var animation = new MoveAnimation(element.X, element.Y, toX, toY)
        {
            Target = element,
            Duration = duration,
            EasingFunction = easing ?? Easing.OutQuad
        };

        AnimationManager.Instance.Animate(animation);
        return element;
    }

    /// <summary>
    /// Animates the size of the element
    /// </summary>
    public static T AnimateSize<T>(this T element, float toWidth, float toHeight,
        float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        var animation = new ScaleAnimation(element.Width, element.Height, toWidth, toHeight)
        {
            Target = element,
            Duration = duration,
            EasingFunction = easing ?? Easing.OutQuad
        };

        AnimationManager.Instance.Animate(animation);
        return element;
    }

    /// <summary>
    /// Fade in animation
    /// </summary>
    public static T FadeIn<T>(this T element, float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        // Requerir�a propiedad Opacity en UIElementBase
        // Por ahora, ejemplo conceptual
        return element;
    }

    /// <summary>
    /// Fade out animation
    /// </summary>
    public static T FadeOut<T>(this T element, float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        // Would require the property Opacity in UIElementBase
        return element;
    }

    /// <summary>
    /// Slide in from left
    /// </summary>
    public static T SlideInFromLeft<T>(this T element, float distance,
        float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        float originalX = element.X;
        element.X = originalX - distance;
        return element.AnimatePosition(originalX, element.Y, duration, easing);
    }

    /// <summary>
    /// Slide in from right
    /// </summary>
    public static T SlideInFromRight<T>(this T element, float distance,
        float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        float originalX = element.X;
        element.X = originalX + distance;
        return element.AnimatePosition(originalX, element.Y, duration, easing);
    }

    /// <summary>
    /// Slide in from top
    /// </summary>
    public static T SlideInFromTop<T>(this T element, float distance,
           float duration = 300, Func<float, float>? easing = null)
           where T : VisualElement
    {
        float originalY = element.Y;
        element.Y = originalY - distance;
        return element.AnimatePosition(element.X, originalY, duration, easing);
    }

    /// <summary>
    /// Slide in from bottom
    /// </summary>
    public static T SlideInFromBottom<T>(this T element, float distance,
        float duration = 300, Func<float, float>? easing = null)
        where T : VisualElement
    {
        float originalY = element.Y;
        element.Y = originalY + distance;
        return element.AnimatePosition(element.X, originalY, duration, easing);
    }

    /// <summary>
    /// Scale up animation
    /// </summary>
    public static T ScaleUp<T>(this T element, float targetScale = 1.2f,
       float duration = 300, Func<float, float>? easing = null)
    where T : VisualElement
    {
        float targetWidth = element.Width * targetScale;
        float targetHeight = element.Height * targetScale;
        return element.AnimateSize(targetWidth, targetHeight, duration, easing);
    }

    /// <summary>
    /// Scale down animation
    /// </summary>
    public static T ScaleDown<T>(this T element, float targetScale = 0.8f,
   float duration = 300, Func<float, float>? easing = null)
   where T : VisualElement
    {
        float targetWidth = element.Width * targetScale;
        float targetHeight = element.Height * targetScale;
        return element.AnimateSize(targetWidth, targetHeight, duration, easing);
    }

    /// <summary>
    /// Bounce animation
    /// </summary>
    public static T Bounce<T>(this T element, float distance = 20,
        float duration = 500, int times = 1)
        where T : VisualElement
    {
        var sequence = new SequenceAnimation { Target = element };

        for (int i = 0; i < times; i++)
        {
            // Up
            sequence.Add(new MoveAnimation(element.X, element.Y, element.X, element.Y - distance)
            {
                Duration = duration / (times * 2),
                EasingFunction = Easing.OutQuad
            });

            // Down
            sequence.Add(new MoveAnimation(element.X, element.Y - distance, element.X, element.Y)
            {
                Duration = duration / (times * 2),
                EasingFunction = Easing.InQuad
            });
        }

        AnimationManager.Instance.Animate(sequence);
        return element;
    }

    /// <summary>
    /// Shake animation
    /// </summary>
    public static T Shake<T>(this T element, float distance = 10,
        float duration = 500, int times = 3)
        where T : VisualElement
    {
        var sequence = new SequenceAnimation { Target = element };
        float originalX = element.X;

        for (int i = 0; i < times; i++)
        {
            // Right
            sequence.Add(new MoveAnimation(originalX, element.Y, originalX + distance, element.Y)
            {
                Duration = duration / (times * 2),
                EasingFunction = Easing.InOutQuad
            });

            // Left
            sequence.Add(new MoveAnimation(originalX + distance, element.Y, originalX, element.Y)
            {
                Duration = duration / (times * 2),
                EasingFunction = Easing.InOutQuad
            });
        }

        AnimationManager.Instance.Animate(sequence);
        return element;
    }

    /// <summary>
    /// Pulse animation (scale up and down)
    /// </summary>
    public static T Pulse<T>(this T element, float scale = 1.1f,
  float duration = 300)
        where T : VisualElement
    {
        float originalWidth = element.Width;
        float originalHeight = element.Height;

        var sequence = new SequenceAnimation { Target = element };

        // Scale up
        sequence.Add(new ScaleAnimation(originalWidth, originalHeight,
            originalWidth * scale, originalHeight * scale)
        {
            Duration = duration / 2,
            EasingFunction = Easing.OutQuad
        });

        // Scale down
        sequence.Add(new ScaleAnimation(originalWidth * scale, originalHeight * scale,
        originalWidth, originalHeight)
        {
            Duration = duration / 2,
            EasingFunction = Easing.InQuad
        });

        AnimationManager.Instance.Animate(sequence);
        return element;
    }

    /// <summary>
    /// Stops all animations of the element
    /// </summary>
    public static T StopAnimations<T>(this T element)
        where T : VisualElement
    {
        AnimationManager.Instance.StopAnimations(element);
        return element;
    }
}
