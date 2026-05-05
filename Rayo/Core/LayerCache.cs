namespace Rayo.Core;

using Rayo.Rendering;

/// <summary>
/// Sistema de caching de capas en GPU para evitar re-renderizado innecesario.
/// Similar al compositor de navegadores modernos.
/// </summary>
public class LayerCache : IDisposable
{
    private readonly Dictionary<string, CachedLayer> _layers = new();
    private readonly IRenderer _renderer;
    private bool _disposed = false;

    public LayerCache(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Obtiene o crea una capa cacheada para un elemento.
    /// </summary>
    public CachedLayer GetOrCreateLayer(string layerId, float width, float height)
    {
        if (_layers.TryGetValue(layerId, out var existingLayer))
        {
            // Verificar si el tama�o cambi�
            if (existingLayer.Width == width && existingLayer.Height == height)
            {
                return existingLayer;
            }
        
            // Tama�o cambi�, recrear
            existingLayer.Dispose();
            _layers.Remove(layerId);
        }

        // Crear nueva capa
        var layer = new CachedLayer
        {
            LayerId = layerId,
            Width = width,
            Height = height,
            IsDirty = true,
            Texture = CreateRenderTarget(width, height)
        };

        _layers[layerId] = layer;
        return layer;
    }

    /// <summary>
    /// Marca una capa como sucia (necesita re-renderizado).
    /// </summary>
    public void MarkLayerDirty(string layerId)
    {
        if (_layers.TryGetValue(layerId, out var layer))
        {
            layer.IsDirty = true;
        }
    }

    /// <summary>
    /// Marca todas las capas como sucias.
    /// </summary>
    public void MarkAllDirty()
    {
        foreach (var layer in _layers.Values)
        {
            layer.IsDirty = true;
        }
    }

    /// <summary>
    /// Limpia capas no usadas recientemente.
    /// </summary>
    public void Cleanup(TimeSpan maxAge)
    {
        var now = DateTime.UtcNow;
        var toRemove = new List<string>();

        foreach (var kvp in _layers)
        {
            if (now - kvp.Value.LastUsed > maxAge)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            _layers[key].Dispose();
            _layers.Remove(key);
        }
    }

    /// <summary>
    /// Obtiene estad�sticas del cache.
    /// </summary>
    public CacheStats GetStats()
    {
        long totalMemory = 0;
        int dirtyCount = 0;

        foreach (var layer in _layers.Values)
        {
            totalMemory += (long)(layer.Width * layer.Height * 4); // RGBA
            if (layer.IsDirty) dirtyCount++;
        }

        return new CacheStats
        {
            TotalLayers = _layers.Count,
            DirtyLayers = dirtyCount,
            TotalMemoryBytes = totalMemory,
            TotalMemoryMB = totalMemory / (1024.0 * 1024.0)
        };
    }

    private ITexture CreateRenderTarget(float width, float height)
    {
        // Delegar al renderer para crear render targets
        return _renderer.CreateRenderTarget((int)width, (int)height);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var layer in _layers.Values)
        {
            layer.Dispose();
        }

    _layers.Clear();
     _disposed = true;
    }
}

/// <summary>
/// Representa una capa cacheada en memoria de GPU.
/// </summary>
public class CachedLayer : IDisposable
{
    public required string LayerId { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public required ITexture? Texture { get; init; }
    public bool IsDirty { get; set; } = true;
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public int DrawCalls { get; set; } = 0;

    public void MarkUsed()
    {
 LastUsed = DateTime.UtcNow;
    }

    public void Dispose()
    {
        Texture?.Dispose();
    }
}

/// <summary>
/// Estad�sticas del sistema de caching de capas.
/// </summary>
public struct CacheStats
{
    public int TotalLayers { get; init; }
    public int DirtyLayers { get; init; }
    public long TotalMemoryBytes { get; init; }
    public double TotalMemoryMB { get; init; }

    public override string ToString() =>
        $"Layers: {TotalLayers} ({DirtyLayers} dirty) | Memory: {TotalMemoryMB:F2} MB";
}

/// <summary>
/// Pol�tica de caching para diferentes tipos de elementos.
/// </summary>
public enum CachingPolicy
{
    /// <summary>
    /// No cachear, siempre re-renderizar (para elementos muy din�micos).
    /// </summary>
    Never,

    /// <summary>
    /// Cachear solo si el elemento est� completamente est�tico.
    /// </summary>
    StaticOnly,

    /// <summary>
    /// Cachear agresivamente (para la mayor�a de elementos UI).
    /// </summary>
    Aggressive,

    /// <summary>
    /// Cachear y mantener en GPU incluso si no se usa (para elementos cr�ticos).
    /// </summary>
    Persistent
}
