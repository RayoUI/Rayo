using Rayo.Rendering;

namespace Rayo.Core;

/// <summary>
/// Extensiones para agregar efectos visuales a elementos UI
/// </summary>
public static class VisualEffectsExtensions
{
    // Diccionario para almacenar efectos por elemento
    private static readonly Dictionary<VisualElement, List<VisualEffect>> _effectsCache = new();

    /// <summary>
    /// Obtiene la lista de efectos visuales de un elemento
    /// </summary>
    public static List<VisualEffect> GetVisualEffects(this VisualElement element)
    {
        if (!_effectsCache.ContainsKey(element))
        {
            _effectsCache[element] = new List<VisualEffect>();
        }
        return _effectsCache[element];
    }

    /// <summary>
    /// Agrega un efecto visual al elemento
    /// </summary>
    public static T AddVisualEffect<T>(this T element, VisualEffect effect) where T : VisualElement
    {
        element.GetVisualEffects().Add(effect);
        element.MarkNeedsPaint();
        return element;
    }

    /// <summary>
    /// Limpia todos los efectos visuales del elemento
    /// </summary>
    public static T ClearVisualEffects<T>(this T element) where T : VisualElement
    {
        element.GetVisualEffects().Clear();
        element.MarkNeedsPaint();
        return element;
    }

    // ========== SOMBRAS ==========

    /// <summary>
    /// Agrega una sombra exterior (drop shadow) al elemento
    /// </summary>
    public static T WithShadow<T>(this T element, float offsetX = 0, float offsetY = 4, float blurRadius = 8, Color? color = null) where T : VisualElement
    {
        var shadowColor = color ?? new Color(0, 0, 0, 128);
        return element.AddVisualEffect(new ShadowEffect(offsetX, offsetY, blurRadius, shadowColor));
    }

    /// <summary>
    /// Agrega una sombra interior (inner shadow) al elemento
    /// </summary>
    public static T WithInnerShadow<T>(this T element, float offsetX = 0, float offsetY = 2, float blurRadius = 4, Color? color = null) where T : VisualElement
    {
        var shadowColor = color ?? new Color(0, 0, 0, 128);
        var effect = new InnerShadowEffect { OffsetX = offsetX, OffsetY = offsetY, BlurRadius = blurRadius, Color = shadowColor };
        return element.AddVisualEffect(effect);
    }

    // ========== BLUR & GLOW ==========

    /// <summary>
    /// Agrega un efecto de desenfoque (blur) al elemento
    /// </summary>
    public static T WithBlur<T>(this T element, float radius = 4) where T : VisualElement
    {
        return element.AddVisualEffect(new BlurEffect(radius));
    }

    /// <summary>
    /// Agrega un efecto de resplandor (glow) al elemento
    /// </summary>
    public static T WithGlow<T>(this T element, Color? color = null, float radius = 8, float intensity = 1.0f) where T : VisualElement
    {
        var glowColor = color ?? new Color(255, 255, 255);
        return element.AddVisualEffect(new GlowEffect(glowColor, radius, intensity));
    }

    // ========== GRADIENTES ==========

    /// <summary>
    /// Agrega un gradiente lineal vertical (de arriba hacia abajo)
    /// </summary>
    public static T WithLinearGradient<T>(this T element, Color startColor, Color endColor) where T : VisualElement
    {
        var gradient = new GradientEffect
        {
            GradientType = GradientType.Linear,
            StartX = 0,
            StartY = 0,
            EndX = 0,
            EndY = 1
        };
        gradient.AddStop(0, startColor);
        gradient.AddStop(1, endColor);
        return element.AddVisualEffect(gradient);
    }

    /// <summary>
    /// Agrega un gradiente lineal con direcci�n personalizada
    /// </summary>
    public static T WithLinearGradient<T>(this T element, float startX, float startY, float endX, float endY, params (float position, Color color)[] stops) where T : VisualElement
    {
        var gradient = new GradientEffect
        {
            GradientType = GradientType.Linear,
            StartX = startX,
            StartY = startY,
            EndX = endX,
            EndY = endY
        };
        foreach (var stop in stops)
        {
            gradient.AddStop(stop.position, stop.color);
        }
        return element.AddVisualEffect(gradient);
    }

    /// <summary>
    /// Agrega un gradiente radial (desde el centro)
    /// </summary>
    public static T WithRadialGradient<T>(this T element, Color centerColor, Color edgeColor) where T : VisualElement
    {
        var gradient = new GradientEffect
        {
            GradientType = GradientType.Radial,
            CenterX = 0.5f,
            CenterY = 0.5f,
            RadiusX = 0.5f,
            RadiusY = 0.5f
        };
        gradient.AddStop(0, centerColor);
        gradient.AddStop(1, edgeColor);
        return element.AddVisualEffect(gradient);
    }

    // ========== OPACIDAD ==========

    /// <summary>
    /// Aplica un efecto de opacidad al elemento
    /// </summary>
    public static T WithOpacity<T>(this T element, float opacity) where T : VisualElement
    {
        return element.AddVisualEffect(new OpacityEffect(opacity));
    }
}