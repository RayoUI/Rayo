namespace Rayo.Core;

/// <summary>
/// Gestor de actualizaciones por frame - Batching de cambios para evitar renders innecesarios
/// Inspirado en React Fiber y Flutter's rendering pipeline
/// </summary>
public class FrameScheduler
{
    private bool _layoutScheduled = false;
    private bool _paintScheduled = false;
    private readonly HashSet<VisualElement> _dirtyLayoutElements = new();
    private readonly HashSet<VisualElement> _dirtyPaintElements = new();
    private Action? _onFrameScheduled;

    /// <summary>
    /// Callback que se invoca cuando se programa un frame (solo una vez por frame)
    /// </summary>
    public Action? OnFrameScheduled
    {
        get => _onFrameScheduled;
        set => _onFrameScheduled = value;
    }

    /// <summary>
    /// Programa un layout para el siguiente frame
    /// Múltiples llamadas en el mismo frame se batchean en una sola
    /// </summary>
    public void ScheduleLayout(VisualElement element)
    {
        bool wasEmpty = _dirtyLayoutElements.Count == 0 && _dirtyPaintElements.Count == 0;

        _dirtyLayoutElements.Add(element);
        _layoutScheduled = true;

        // Solo notificar si es el primer cambio de este frame
        if (wasEmpty)
        {
            _onFrameScheduled?.Invoke();
        }
    }

    /// <summary>
    /// Programa solo un repaint (sin layout) para el siguiente frame
    /// Más eficiente que layout cuando solo cambian visuales
    /// </summary>
    public void SchedulePaint(VisualElement element)
    {
        bool wasEmpty = _dirtyLayoutElements.Count == 0 && _dirtyPaintElements.Count == 0;

        _dirtyPaintElements.Add(element);
        _paintScheduled = true;

        // Solo notificar si es el primer cambio de este frame
        if (wasEmpty)
        {
            _onFrameScheduled?.Invoke();
        }
    }

    /// <summary>
    /// Verifica si hay trabajo programado para este frame
    /// </summary>
    public bool HasScheduledWork => _layoutScheduled || _paintScheduled;

    /// <summary>
    /// Verifica si hay layout programado
    /// </summary>
    public bool NeedsLayout => _layoutScheduled;

    /// <summary>
    /// Verifica si hay paint programado
    /// </summary>
    public bool NeedsPaint => _paintScheduled;

    /// <summary>
    /// Obtiene los elementos que necesitan layout
    /// </summary>
    public IReadOnlyCollection<VisualElement> DirtyLayoutElements => _dirtyLayoutElements;

    /// <summary>
    /// Obtiene los elementos que necesitan paint
    /// </summary>
    public IReadOnlyCollection<VisualElement> DirtyPaintElements => _dirtyPaintElements;

    /// <summary>
    /// Limpia el estado después de procesar el frame
    /// Debe llamarse después de completar layout y paint
    /// </summary>
    public void FrameComplete()
    {
        _layoutScheduled = false;
        _paintScheduled = false;
        _dirtyLayoutElements.Clear();
        _dirtyPaintElements.Clear();
    }

    /// <summary>
    /// Resetea el scheduler (útil para testing o reiniciar estado)
    /// </summary>
    public void Reset()
    {
        _layoutScheduled = false;
        _paintScheduled = false;
        _dirtyLayoutElements.Clear();
        _dirtyPaintElements.Clear();
    }
}