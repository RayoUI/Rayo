using Rayo.Rendering;

namespace Rayo.Core;

/// <summary>
/// Tipos de efectos visuales disponibles
/// </summary>
public enum VisualEffectType
{
    None,
    Shadow,
    InnerShadow,
    Blur,
    Glow,
    Gradient,
    Opacity
}

/// <summary>
/// Configuraci�n base para efectos visuales
/// </summary>
public abstract class VisualEffect
{
    public VisualEffectType Type { get; protected set; }
    public bool IsEnabled { get; set; } = true;

    public abstract void Apply(VisualElement element);
}

/// <summary>
/// Efecto de sombra exterior (Drop Shadow)
/// </summary>
public class ShadowEffect : VisualEffect
{
    public Color Color { get; set; } = new Color(0, 0, 0, 128);
    public float OffsetX { get; set; } = 0;
    public float OffsetY { get; set; } = 4;
    public float BlurRadius { get; set; } = 8;
    public float Spread { get; set; } = 0;

    public ShadowEffect()
    {
        Type = VisualEffectType.Shadow;
    }

    public ShadowEffect(float offsetX, float offsetY, float blurRadius, Color color)
    {
        Type = VisualEffectType.Shadow;
        OffsetX = offsetX;
        OffsetY = offsetY;
        BlurRadius = blurRadius;
        Color = color;
    }

    public override void Apply(VisualElement element)
    {
        // La implementaci�n real se hace en el renderer
    }
}

/// <summary>
/// Efecto de sombra interior (Inner Shadow)
/// </summary>
public class InnerShadowEffect : VisualEffect
{
    public Color Color { get; set; } = new Color(0, 0, 0, 128);
    public float OffsetX { get; set; } = 0;
    public float OffsetY { get; set; } = 2;
    public float BlurRadius { get; set; } = 4;

    public InnerShadowEffect()
    {
        Type = VisualEffectType.InnerShadow;
    }

    public override void Apply(VisualElement element)
    {
        // La implementaci�n real se hace en el renderer
    }
}

/// <summary>
/// Efecto de desenfoque (Blur)
/// </summary>
public class BlurEffect : VisualEffect
{
    public float Radius { get; set; } = 4;

    public BlurEffect()
    {
        Type = VisualEffectType.Blur;
    }

    public BlurEffect(float radius)
    {
        Type = VisualEffectType.Blur;
        Radius = radius;
    }

    public override void Apply(VisualElement element)
    {
        // La implementaci�n real se hace en el renderer
    }
}

/// <summary>
/// Efecto de resplandor (Glow)
/// </summary>
public class GlowEffect : VisualEffect
{
    public Color Color { get; set; } = new Color(255, 255, 255);
    public float Radius { get; set; } = 8;
    public float Intensity { get; set; } = 1.0f;

    public GlowEffect()
    {
        Type = VisualEffectType.Glow;
    }

    public GlowEffect(Color color, float radius, float intensity = 1.0f)
    {
        Type = VisualEffectType.Glow;
        Color = color;
        Radius = radius;
        Intensity = intensity;
    }

    public override void Apply(VisualElement element)
    {
        // La implementaci�n real se hace en el renderer
    }
}

/// <summary>
/// Tipos de gradiente
/// </summary>
public enum GradientType
{
    Linear,
    Radial,
    Conic
}

/// <summary>
/// Stop de color para gradientes
/// </summary>
public struct GradientStop
{
    public float Position { get; set; } // 0.0 - 1.0
    public Color Color { get; set; }

    public GradientStop(float position, Color color)
    {
        Position = position;
        Color = color;
    }
}

/// <summary>
/// Efecto de gradiente
/// </summary>
public class GradientEffect : VisualEffect
{
    public GradientType GradientType { get; set; } = GradientType.Linear;
    public List<GradientStop> Stops { get; set; } = new();

    // Para gradientes lineales
    public float StartX { get; set; } = 0;

    public float StartY { get; set; } = 0;
    public float EndX { get; set; } = 0;
    public float EndY { get; set; } = 1;

    // Para gradientes radiales
    public float CenterX { get; set; } = 0.5f;

    public float CenterY { get; set; } = 0.5f;
    public float RadiusX { get; set; } = 0.5f;
    public float RadiusY { get; set; } = 0.5f;

    public GradientEffect()
    {
        Type = VisualEffectType.Gradient;
    }

    public GradientEffect AddStop(float position, Color color)
    {
        Stops.Add(new GradientStop(position, color));
        return this;
    }

    public override void Apply(VisualElement element)
    {
        // La implementaci�n real se hace en el renderer
    }
}

/// <summary>
/// Efecto de opacidad
/// </summary>
public class OpacityEffect : VisualEffect
{
    public float Opacity { get; set; } = 1.0f; // 0.0 - 1.0

    public OpacityEffect()
    {
        Type = VisualEffectType.Opacity;
    }

    public OpacityEffect(float opacity)
    {
        Type = VisualEffectType.Opacity;
        Opacity = Math.Clamp(opacity, 0f, 1f);
    }

    public override void Apply(VisualElement element)
    {
        // La implementaci�n real se hace en el renderer
    }
}