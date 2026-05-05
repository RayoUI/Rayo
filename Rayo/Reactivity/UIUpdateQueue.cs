using Rayo.Core;

namespace Rayo.Reactivity;

/// <summary>
/// Sistema para diferir invalidaciones de UI hasta después del render loop.
/// Implementa batching automático: múltiples cambios al mismo elemento se agrupan
/// en una sola actualización para mejorar el rendimiento.
/// Thread-safe: puede ser llamado desde cualquier hilo.
/// </summary>
public static class UIUpdateQueue
{
    private static readonly Queue<Action> _pendingUpdates = new();
    private static readonly Dictionary<VisualElement, List<Action>> _batchedUpdates = new();
    private static readonly object _lock = new();
    private static bool _isProcessing = false;

    /// <summary>
    /// Encola una acción de UI para ejecutarse después del render loop (sin batching)
    /// Thread-safe: puede ser llamado desde cualquier hilo.
    /// </summary>
    public static void EnqueueUIUpdate(Action action)
    {
        if (action == null) return;

        lock (_lock)
        {
            _pendingUpdates.Enqueue(action);
        }
    }

    /// <summary>
    /// Encola una acción de UI con batching por elemento.
    /// Múltiples acciones para el mismo elemento se ejecutan juntas antes de marcar como dirty.
    /// Thread-safe: puede ser llamado desde cualquier hilo.
    /// </summary>
    public static void EnqueueUIUpdate(VisualElement element, Action action)
    {
        if (element == null || action == null) return;

        lock (_lock)
        {
            if (!_batchedUpdates.ContainsKey(element))
            {
                _batchedUpdates[element] = new List<Action>();
            }

            _batchedUpdates[element].Add(action);
            //System.Console.WriteLine($"[UIUpdateQueue] Enqueued update for {element.GetType().Name}. Total batched elements: {_batchedUpdates.Count}");
        }
    }

    /// <summary>
    /// Procesa todas las actualizaciones pendientes.
    /// Debe ser llamado por UIApplication después de cada render loop completo.
    /// Solo debe ser llamado desde el hilo de UI.
    /// </summary>
    public static void ProcessPendingUpdates()
    {
        if (_isProcessing) return;

        _isProcessing = true;

        // Copy updates to local variables under lock to minimize lock time
        Queue<Action> pendingCopy;
        Dictionary<VisualElement, List<Action>> batchedCopy;

        lock (_lock)
        {
            pendingCopy = new Queue<Action>(_pendingUpdates);
            // Deep copy the dictionary and lists to avoid shared references
            batchedCopy = new Dictionary<VisualElement, List<Action>>();
            foreach (var kvp in _batchedUpdates)
            {
                batchedCopy[kvp.Key] = new List<Action>(kvp.Value);
            }
            _pendingUpdates.Clear();
            _batchedUpdates.Clear();
        }

        bool hadUpdates = pendingCopy.Count > 0 || batchedCopy.Count > 0;

        try
        {
            //System.Console.WriteLine($"[UIUpdateQueue] ProcessPendingUpdates: {pendingCopy.Count} pending, {batchedCopy.Count} batched elements");

            // Process non-batched updates first (legacy support)
            while (pendingCopy.Count > 0)
            {
                var action = pendingCopy.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[UIUpdateQueue] ERROR in pending action: {ex.Message}");
                }
            }

            // Process batched updates per element
            foreach (var kvp in batchedCopy)
            {
                var element = kvp.Key;
                var actions = kvp.Value;

                //System.Console.WriteLine($"[UIUpdateQueue] Processing {actions.Count} actions for {element.GetType().Name}");

                // Execute all actions for this element
                foreach (var action in actions)
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"[UIUpdateQueue] ERROR in batched action: {ex.Message}");
                    }
                }

                // Now mark the element as dirty once after all updates
                // (Note: MarkNeedsPaint is already idempotent, but this ensures
                // all property changes happen before any paint marking)
            }
        }
        finally
        {
            _isProcessing = false;
        }

        if (hadUpdates)
        {
            UIApplication.Current?.Tree.MarkNeedsRender();
        }
    }

    /// <summary>
    /// Limpia todas las actualizaciones pendientes (útil para cleanup)
    /// Thread-safe: puede ser llamado desde cualquier hilo.
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _pendingUpdates.Clear();
            _batchedUpdates.Clear();
        }
    }

    /// <summary>
    /// Retorna true si hay actualizaciones pendientes
    /// Thread-safe: puede ser llamado desde cualquier hilo.
    /// </summary>
    public static bool HasPendingUpdates
    {
        get
        {
            lock (_lock)
            {
                return _pendingUpdates.Count > 0 || _batchedUpdates.Count > 0;
            }
        }
    }
}
