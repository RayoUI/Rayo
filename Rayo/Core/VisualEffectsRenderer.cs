namespace Rayo.Core;

using Rayo.Rendering;

/// <summary>
/// Interface para renderizadores de efectos visuales.
/// Cada efecto tiene su propio renderizador independiente.
/// SOLID: Open/Closed Principle - Nuevos efectos no requieren modificar c�digo existente
/// </summary>
public interface IEffectRenderer
{
    /// <summary>
  /// Tipo de efecto que este renderizador maneja
    /// </summary>
    Type EffectType { get; }
    
    /// <summary>
  /// Orden de renderizado (menor = antes)
    /// Pre-render: < 0
    /// Background: 0-99
    /// Post-render: 100+
    /// </summary>
    int RenderOrder { get; }
    
    /// <summary>
    /// Renderiza el efecto para un elemento espec�fico
    /// </summary>
    void Render(VisualElement element, VisualEffect effect, IRenderer renderer);
}

/// <summary>
/// Gestor de renderizado de efectos visuales.
/// SOLID: Single Responsibility - Solo se encarga de coordinar el renderizado de efectos
/// </summary>
public class VisualEffectsRenderer
{
    private readonly Dictionary<Type, IEffectRenderer> _effectRenderers = new();
  private List<IEffectRenderer> _sortedRenderers = new();
    
    public VisualEffectsRenderer()
    {
  // Registrar renderizadores por defecto
        RegisterRenderer(new ShadowEffectRenderer());
        RegisterRenderer(new InnerShadowEffectRenderer());
        RegisterRenderer(new GlowEffectRenderer());
     RegisterRenderer(new GradientEffectRenderer());
        RegisterRenderer(new OpacityEffectRenderer());
  
      // Ordenar por RenderOrder
    _sortedRenderers = _effectRenderers.Values.OrderBy(r => r.RenderOrder).ToList();
    }
    
    /// <summary>
    /// Registra un renderizador de efectos personalizado
    /// SOLID: Open/Closed - Permite extensi�n sin modificaci�n
    /// </summary>
    public void RegisterRenderer(IEffectRenderer renderer)
    {
        _effectRenderers[renderer.EffectType] = renderer;
        _sortedRenderers = _effectRenderers.Values.OrderBy(r => r.RenderOrder).ToList();
    }
    
    /// <summary>
    /// Renderiza todos los efectos de un elemento en el orden correcto
    /// </summary>
    public void RenderEffects(VisualElement element, IRenderer renderer, EffectRenderPhase phase)
    {
        var effects = element.GetVisualEffects();
        if (effects.Count == 0) return;
     
        foreach (var effectRenderer in _sortedRenderers)
        {
            // Filtrar por fase de renderizado
  if (!ShouldRenderInPhase(effectRenderer.RenderOrder, phase))
                continue;
    
     foreach (var effect in effects.Where(e => e.IsEnabled && e.GetType() == effectRenderer.EffectType))
            {
                effectRenderer.Render(element, effect, renderer);
}
      }
    }
    
    private bool ShouldRenderInPhase(int renderOrder, EffectRenderPhase phase)
    {
        return phase switch
        {
 EffectRenderPhase.PreRender => renderOrder < 0,
     EffectRenderPhase.Background => renderOrder >= 0 && renderOrder < 100,
      EffectRenderPhase.PostRender => renderOrder >= 100,
            _ => false
   };
    }
}

/// <summary>
/// Fases de renderizado de efectos
/// </summary>
public enum EffectRenderPhase
{
    PreRender,    // Antes del elemento (opacity, blur context)
    Background,   // Como fondo (shadows, gradients)
    PostRender    // Despu�s del elemento (glow, inner shadows)
}
