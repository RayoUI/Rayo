namespace Rayo.Core;

/// <summary>
/// Gestor de actualizaciones por frame - Batching de cambios para evitar renders innecesarios
/// Inspirado en React Fiber y Flutter's rendering pipeline
/// </summary>
public class FrameScheduler
{
    private bool _measureScheduled = false;
    private bool _arrangeScheduled = false;
    private bool _paintScheduled = false;
    private readonly HashSet<VisualElement> _dirtyMeasureElements = new();
    private readonly HashSet<VisualElement> _dirtyArrangeElements = new();
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
    public void ScheduleMeasure(VisualElement element)
    {
        bool wasEmpty = _dirtyMeasureElements.Count == 0 &&
            _dirtyArrangeElements.Count == 0 &&
            _dirtyPaintElements.Count == 0;

        _dirtyMeasureElements.Add(element);
        _measureScheduled = true;
        _arrangeScheduled = true;

        if (wasEmpty)
        {
            _onFrameScheduled?.Invoke();
        }
    }

    public void ScheduleArrange(VisualElement element)
    {
        bool wasEmpty = _dirtyMeasureElements.Count == 0 &&
            _dirtyArrangeElements.Count == 0 &&
            _dirtyPaintElements.Count == 0;

        _dirtyArrangeElements.Add(element);
        _arrangeScheduled = true;

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
        bool wasEmpty = _dirtyMeasureElements.Count == 0 &&
            _dirtyArrangeElements.Count == 0 &&
            _dirtyPaintElements.Count == 0;

        _dirtyPaintElements.Add(element);
        _paintScheduled = true;

        if (wasEmpty)
        {
            _onFrameScheduled?.Invoke();
        }
    }

    /// <summary>
    /// Verifica si hay trabajo programado para este frame
    /// </summary>
    public bool HasScheduledWork => _measureScheduled || _arrangeScheduled || _paintScheduled;

    /// <summary>
    /// Verifica si hay layout programado
    /// </summary>
    public bool NeedsMeasure => _measureScheduled;

    public bool NeedsArrange => _arrangeScheduled;

    /// <summary>
    /// Verifica si hay paint programado
    /// </summary>
    public bool NeedsPaint => _paintScheduled;

    /// <summary>
    /// Obtiene los elementos que necesitan layout
    /// </summary>
    public IReadOnlyCollection<VisualElement> DirtyMeasureElements => _dirtyMeasureElements;

    public IReadOnlyCollection<VisualElement> DirtyArrangeElements => _dirtyArrangeElements;

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
        _measureScheduled = false;
        _arrangeScheduled = false;
        _paintScheduled = false;
        _dirtyMeasureElements.Clear();
        _dirtyArrangeElements.Clear();
        _dirtyPaintElements.Clear();
    }

    /// <summary>
    /// Resetea el scheduler (útil para testing o reiniciar estado)
    /// </summary>
    public void Reset()
    {
        _measureScheduled = false;
        _arrangeScheduled = false;
        _paintScheduled = false;
        _dirtyMeasureElements.Clear();
        _dirtyArrangeElements.Clear();
        _dirtyPaintElements.Clear();
    }
}
