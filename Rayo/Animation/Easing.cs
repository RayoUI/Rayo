namespace Rayo.Animation;

/// <summary>
/// Easing functions for animations (inspired by MAUI and CSS)
/// </summary>
public static class Easing
{
    /// <summary>
    /// Linear - no acceleration
    /// </summary>
    public static float Linear(float t) => t;
    
    /// <summary>
    /// Ease In Quad - gradually accelerates
    /// </summary>
    public static float InQuad(float t) => t * t;
    
    /// <summary>
    /// Ease Out Quad - gradually decelerates
    /// </summary>
    public static float OutQuad(float t) => t * (2 - t);
    
    /// <summary>
    /// Ease In Out Quad - accelerates then decelerates
    /// </summary>
    public static float InOutQuad(float t)
    {
        return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
    }
    
    /// <summary>
    /// Ease In Cubic - accelerates faster
    /// </summary>
    public static float InCubic(float t) => t * t * t;
    
    /// <summary>
    /// Ease Out Cubic - decelerates faster
    /// </summary>
    public static float OutCubic(float t)
    {
        float f = t - 1;
        return f * f * f + 1;
    }
    
    /// <summary>
    /// Ease In Out Cubic - smoothly accelerates and decelerates
    /// </summary>
    public static float InOutCubic(float t)
    {
        return t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
    }
    
    /// <summary>
    /// Ease In Quart - accelerates very fast
    /// </summary>
    public static float InQuart(float t) => t * t * t * t;
    
    /// <summary>
    /// Ease Out Quart - decelerates very fast
    /// </summary>
    public static float OutQuart(float t)
    {
        float f = t - 1;
        return 1 - f * f * f * f;
    }
    
    /// <summary>
    /// Ease In Out Quart
    /// </summary>
    public static float InOutQuart(float t)
    {
        return t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
    }
    
    /// <summary>
    /// Ease In Sine - smoothly accelerates
    /// </summary>
    public static float InSine(float t) => 1 - MathF.Cos(t * MathF.PI / 2);
    
    /// <summary>
    /// Ease Out Sine - smoothly decelerates
    /// </summary>
    public static float OutSine(float t) => MathF.Sin(t * MathF.PI / 2);
    
    /// <summary>
    /// Ease In Out Sine
    /// </summary>
    public static float InOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1) / 2;
    
    /// <summary>
    /// Ease In Expo - accelerates exponentially
    /// </summary>
    public static float InExpo(float t) => t == 0 ? 0 : MathF.Pow(2, 10 * t - 10);
    
    /// <summary>
    /// Ease Out Expo - decelerates exponentially
    /// </summary>
    public static float OutExpo(float t) => t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t);
    
    /// <summary>
    /// Ease In Out Expo
    /// </summary>
    public static float InOutExpo(float t)
    {
        if (t == 0) return 0;
        if (t == 1) return 1;
        return t < 0.5f 
            ? MathF.Pow(2, 20 * t - 10) / 2
            : (2 - MathF.Pow(2, -20 * t + 10)) / 2;
    }
    
    /// <summary>
    /// Ease In Back - moves backward before advancing
    /// </summary>
    public static float InBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return c3 * t * t * t - c1 * t * t;
    }
    
    /// <summary>
    /// Ease Out Back - overshoots before stopping
    /// </summary>
    public static float OutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
    }
    
    /// <summary>
    /// Ease In Out Back
    /// </summary>
    public static float InOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        return t < 0.5f
            ? (MathF.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
            : (MathF.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
    }
    
    /// <summary>
    /// Ease In Elastic - spring effect at the start
    /// </summary>
    public static float InElastic(float t)
    {
        const float c4 = (2 * MathF.PI) / 3;
        return t == 0 ? 0 : t == 1 ? 1 
            : -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * c4);
    }
    
    /// <summary>
    /// Ease Out Elastic - spring effect at the end
    /// </summary>
    public static float OutElastic(float t)
    {
        const float c4 = (2 * MathF.PI) / 3;
        return t == 0 ? 0 : t == 1 ? 1
            : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1;
    }
    
    /// <summary>
    /// Ease In Out Elastic
    /// </summary>
    public static float InOutElastic(float t)
    {
        const float c5 = (2 * MathF.PI) / 4.5f;
        return t == 0 ? 0 : t == 1 ? 1 : t < 0.5f
            ? -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * c5)) / 2
            : (MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * c5)) / 2 + 1;
    }
    
    /// <summary>
    /// Ease In Bounce - bounce effect at the start
    /// </summary>
    public static float InBounce(float t) => 1 - OutBounce(1 - t);
    
    /// <summary>
    /// Ease Out Bounce - bounce effect at the end
    /// </summary>
    public static float OutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (t < 1 / d1)
        {
            return n1 * t * t;
        }
        else if (t < 2 / d1)
        {
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        }
        else if (t < 2.5 / d1)
        {
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        }
        else
        {
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
    
    /// <summary>
    /// Ease In Out Bounce
    /// </summary>
    public static float InOutBounce(float t)
    {
        return t < 0.5f
            ? (1 - OutBounce(1 - 2 * t)) / 2
            : (1 + OutBounce(2 * t - 1)) / 2;
    }
    
    /// <summary>
    /// Spring - physical spring effect
    /// </summary>
    public static float Spring(float t)
    {
        return 1 + (-MathF.Pow(MathF.E, -6 * t) * MathF.Cos(-12 * t));
    }
}
