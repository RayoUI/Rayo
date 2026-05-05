namespace Rayo.Core;

using Rayo.Rendering;

/// <summary>
/// Renderizador de sombras exteriores
/// SOLID: Single Responsibility - Solo renderiza shadows
/// </summary>
public class ShadowEffectRenderer : IEffectRenderer
{
    public Type EffectType => typeof(ShadowEffect);
    public int RenderOrder => 10; // Background, antes del elemento

    public void Render(VisualElement element, VisualEffect effect, IRenderer renderer)
    {
        var shadow = (ShadowEffect)effect;

        float maxBlur = Math.Max(0f, shadow.BlurRadius);
        float spread = shadow.Spread;
        var cornerRadius = element.BorderRadius;

        if (maxBlur <= 0f)
        {
            float expansion = spread;
            float shadowX = element.ComputedX + shadow.OffsetX - expansion;
            float shadowY = element.ComputedY + shadow.OffsetY - expansion;
            float shadowW = element.ComputedWidth + expansion * 2;
            float shadowH = element.ComputedHeight + expansion * 2;
            float shadowTopLeft = Math.Max(0f, cornerRadius.TopLeft + expansion);
            float shadowTopRight = Math.Max(0f, cornerRadius.TopRight + expansion);
            float shadowBottomRight = Math.Max(0f, cornerRadius.BottomRight + expansion);
            float shadowBottomLeft = Math.Max(0f, cornerRadius.BottomLeft + expansion);

            var path = Rayo.Rendering.Graphics.VectorGraphics.VectorPath.RoundedRectangle(
                shadowX,
                shadowY,
                shadowW,
                shadowH,
                shadowTopLeft,
                shadowTopRight,
                shadowBottomRight,
                shadowBottomLeft);

            renderer.DrawPath(path, shadow.Color);
            return;
        }

        int layers = Math.Clamp((int)MathF.Ceiling(maxBlur * 1.5f), 12, 40);
        float invLayers = 1f / layers;

        for (int i = layers; i >= 1; i--)
        {
            float t = i * invLayers;
            float expansion = spread + maxBlur * t;
            float falloff = 1f - t;
            float alpha = shadow.Color.A * falloff * falloff * invLayers * 2.5f;
            var shadowColor = shadow.Color.WithAlpha(Math.Clamp(alpha, 0f, shadow.Color.A));

            float shadowX = element.ComputedX + shadow.OffsetX - expansion;
            float shadowY = element.ComputedY + shadow.OffsetY - expansion;
            float shadowW = element.ComputedWidth + expansion * 2;
            float shadowH = element.ComputedHeight + expansion * 2;
            float shadowTopLeft = Math.Max(0f, cornerRadius.TopLeft + expansion);
            float shadowTopRight = Math.Max(0f, cornerRadius.TopRight + expansion);
            float shadowBottomRight = Math.Max(0f, cornerRadius.BottomRight + expansion);
            float shadowBottomLeft = Math.Max(0f, cornerRadius.BottomLeft + expansion);

            var path = Rayo.Rendering.Graphics.VectorGraphics.VectorPath.RoundedRectangle(
                shadowX,
                shadowY,
                shadowW,
                shadowH,
                shadowTopLeft,
                shadowTopRight,
                shadowBottomRight,
                shadowBottomLeft);

            renderer.DrawPath(path, shadowColor);
        }
    }
}

/// <summary>
/// Renderizador de sombras interiores
/// SOLID: Single Responsibility - Solo renderiza inner shadows
/// </summary>
public class InnerShadowEffectRenderer : IEffectRenderer
{
    public Type EffectType => typeof(InnerShadowEffect);
    public int RenderOrder => 100; // Post-render, despu�s del elemento

    public void Render(VisualElement element, VisualEffect effect, IRenderer renderer)
    {
        var shadow = (InnerShadowEffect)effect;

        var shadowColor = new Color(
            (byte)(shadow.Color.R * 255),
         (byte)(shadow.Color.G * 255),
              (byte)(shadow.Color.B * 255),
           (byte)(shadow.Color.A * 255 * 0.3f)
            );

        renderer.DrawRoundedRectOutline(
       element.ComputedX + shadow.OffsetX,
   element.ComputedY + shadow.OffsetY,
         element.ComputedWidth,
   element.ComputedHeight,
    element.BorderRadius.TopLeft,
   shadow.BlurRadius,
  shadowColor
 );
    }
}

/// <summary>
/// Renderizador de efectos glow
/// SOLID: Single Responsibility - Solo renderiza glow
/// </summary>
public class GlowEffectRenderer : IEffectRenderer
{
    public Type EffectType => typeof(GlowEffect);
    public int RenderOrder => 110; // Post-render, al final

    public void Render(VisualElement element, VisualEffect effect, IRenderer renderer)
    {
        var glow = (GlowEffect)effect;

        // Dibujar m�ltiples capas con alpha decreciente
        for (int i = 0; i < 3; i++)
        {
            float expansion = glow.Radius * (i + 1) / 3f;
            float alpha = glow.Color.A * glow.Intensity * (1f - i / 3f);
            var glowColor = new Color(
    (byte)(glow.Color.R * 255),
       (byte)(glow.Color.G * 255),
        (byte)(glow.Color.B * 255),
                (byte)(alpha * 255 * 0.3f)
    );

            renderer.DrawRoundedRect(
            element.ComputedX - expansion,
              element.ComputedY - expansion,
                          element.ComputedWidth + expansion * 2,
              element.ComputedHeight + expansion * 2,
                     element.BorderRadius.TopLeft + expansion,
                glowColor
             );
        }
    }
}

/// <summary>
/// Renderizador de gradientes
/// SOLID: Single Responsibility - Solo renderiza gradientes
/// </summary>
public class GradientEffectRenderer : IEffectRenderer
{
    public Type EffectType => typeof(GradientEffect);
    public int RenderOrder => 5; // Background, antes de shadows

    public void Render(VisualElement element, VisualEffect effect, IRenderer renderer)
    {
        var gradient = (GradientEffect)effect;

        if (gradient.GradientType == GradientType.Linear)
        {
            RenderLinearGradient(element, gradient, renderer);
        }
        else if (gradient.GradientType == GradientType.Radial)
        {
            RenderRadialGradient(element, gradient, renderer);
        }
    }

    private void RenderLinearGradient(VisualElement element, GradientEffect gradient, IRenderer renderer)
    {
        int steps = Math.Max(gradient.Stops.Count - 1, 10);
        float stepHeight = element.ComputedHeight / steps;

        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / steps;
            var color = ColorInterpolator.InterpolateGradient(gradient, t);

            renderer.DrawRect(
                 element.ComputedX,
              element.ComputedY + i * stepHeight,
               element.ComputedWidth,
               stepHeight + 1,
                        color
                    );
        }
    }

    private void RenderRadialGradient(VisualElement element, GradientEffect gradient, IRenderer renderer)
    {
        int rings = 20;
        float centerX = element.ComputedX + element.ComputedWidth * gradient.CenterX;
        float centerY = element.ComputedY + element.ComputedHeight * gradient.CenterY;
        float maxRadius = Math.Max(element.ComputedWidth, element.ComputedHeight) * gradient.RadiusX;

        for (int i = rings; i > 0; i--)
        {
            float t = (float)i / rings;
            var color = ColorInterpolator.InterpolateGradient(gradient, 1f - t);
            float radius = maxRadius * t;

            renderer.DrawCircle(centerX, centerY, radius, color);
        }
    }
}

/// <summary>
/// Renderizador de opacidad (placeholder para futura implementaci�n)
/// SOLID: Single Responsibility - Solo maneja opacity
/// </summary>
public class OpacityEffectRenderer : IEffectRenderer
{
    public Type EffectType => typeof(OpacityEffect);
    public int RenderOrder => -10; // Pre-render

    public void Render(VisualElement element, VisualEffect effect, IRenderer renderer)
    {
        // TODO: Implementar cuando IRenderer soporte PushOpacity/PopOpacity
        // var opacity = (OpacityEffect)effect;
        // renderer.PushOpacity(opacity.Opacity);
    }
}

/// <summary>
/// Utilidad para interpolaci�n de colores
/// SOLID: Single Responsibility - Solo interpola colores
/// </summary>
public static class ColorInterpolator
{
    public static Color InterpolateGradient(GradientEffect gradient, float t)
    {
        if (gradient.Stops.Count == 0)
            return Color.White;

        if (gradient.Stops.Count == 1)
            return gradient.Stops[0].Color;

        var orderedStops = gradient.Stops.OrderBy(s => s.Position).ToList();

        for (int i = 0; i < orderedStops.Count - 1; i++)
        {
            var stop1 = orderedStops[i];
            var stop2 = orderedStops[i + 1];

            if (t >= stop1.Position && t <= stop2.Position)
            {
                float localT = (t - stop1.Position) / (stop2.Position - stop1.Position);
                return Lerp(stop1.Color, stop2.Color, localT);
            }
        }

        return t < orderedStops[0].Position ? orderedStops[0].Color : orderedStops[^1].Color;
    }

    public static Color Lerp(Color a, Color b, float t)
    {
        return new Color(
            (byte)((a.R * 255 * (1 - t) + b.R * 255 * t)),
   (byte)((a.G * 255 * (1 - t) + b.G * 255 * t)),
     (byte)((a.B * 255 * (1 - t) + b.B * 255 * t)),
            (byte)((a.A * 255 * (1 - t) + b.A * 255 * t))
  );
    }
}