namespace Rayo.Core;

using System.Numerics;

/// <summary>
/// Sistema de tracking de regiones sucias (dirty regions) para optimizar el renderizado.
/// Solo re-dibuja las �reas que cambiaron, no toda la pantalla.
/// </summary>
public class DirtyRegionTracker
{
    private readonly List<DirtyRegion> _dirtyRegions = new();
    private readonly object _lock = new();
    private bool _fullScreenDirty = false;

  /// <summary>
 /// Marca toda la pantalla como sucia (por ejemplo, al redimensionar).
    /// </summary>
    public void MarkFullScreenDirty()
    {
        lock (_lock)
        {
     _fullScreenDirty = true;
        _dirtyRegions.Clear();
        }
    }

    /// <summary>
    /// Marca una regi�n espec�fica como sucia.
    /// </summary>
    public void MarkDirty(float x, float y, float width, float height, DirtyReason reason = DirtyReason.ContentChanged)
    {
  lock (_lock)
        {
     // Si ya est� todo sucio, no hacer nada
     if (_fullScreenDirty) return;

            // Expandir la regi�n ligeramente para evitar artifacts
   const float padding = 2f;
var region = new DirtyRegion
            {
           X = x - padding,
      Y = y - padding,
                Width = width + padding * 2,
         Height = height + padding * 2,
     Reason = reason
         };

    // Intentar fusionar con regiones existentes
   bool merged = TryMergeRegion(region);
  if (!merged)
         {
     _dirtyRegions.Add(region);
            }

        // Si hay demasiadas regiones peque�as, marcar todo como sucio
            if (_dirtyRegions.Count > 20)
            {
     _fullScreenDirty = true;
     _dirtyRegions.Clear();
            }
        }
    }

    /// <summary>
    /// Marca un elemento como sucio (usa sus bounds).
 /// </summary>
public void MarkElementDirty(VisualElement element, DirtyReason reason = DirtyReason.ContentChanged)
{
        if (!element.IsVisible) return;
    MarkDirty(element.ComputedX, element.ComputedY, element.ComputedWidth, element.ComputedHeight, reason);
  }

 /// <summary>
    /// Obtiene todas las regiones sucias para este frame.
    /// </summary>
    public IReadOnlyList<DirtyRegion> GetDirtyRegions()
    {
        lock (_lock)
        {
            return _dirtyRegions.ToList();
        }
  }

    /// <summary>
    /// Verifica si toda la pantalla est� sucia.
    /// </summary>
    public bool IsFullScreenDirty()
    {
        lock (_lock)
        {
 return _fullScreenDirty;
    }
  }

    /// <summary>
    /// Limpia todas las regiones sucias despu�s de renderizar.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
  _dirtyRegions.Clear();
   _fullScreenDirty = false;
        }
    }

  /// <summary>
    /// Verifica si un elemento intersecta con alguna regi�n sucia.
    /// </summary>
    public bool IntersectsWithDirtyRegion(VisualElement element)
    {
        lock (_lock)
        {
        if (_fullScreenDirty) return true;
            if (_dirtyRegions.Count == 0) return false;

    float elemX = element.ComputedX;
     float elemY = element.ComputedY;
      float elemW = element.ComputedWidth;
      float elemH = element.ComputedHeight;

            foreach (var region in _dirtyRegions)
            {
             if (RectanglesIntersect(elemX, elemY, elemW, elemH, 
    region.X, region.Y, region.Width, region.Height))
        {
          return true;
   }
       }

            return false;
        }
    }

    private bool TryMergeRegion(DirtyRegion newRegion)
    {
        for (int i = 0; i < _dirtyRegions.Count; i++)
        {
            var existing = _dirtyRegions[i];
 
            // Si se solapan o est�n muy cerca, fusionar
if (RegionsOverlapOrClose(existing, newRegion, threshold: 20f))
          {
      _dirtyRegions[i] = MergeRegions(existing, newRegion);
            return true;
 }
      }
      return false;
    }

    private bool RegionsOverlapOrClose(DirtyRegion a, DirtyRegion b, float threshold)
    {
        // Expandir temporalmente para verificar si est�n cerca
      float ax1 = a.X - threshold;
        float ay1 = a.Y - threshold;
      float ax2 = a.X + a.Width + threshold;
        float ay2 = a.Y + a.Height + threshold;

        float bx1 = b.X;
        float by1 = b.Y;
        float bx2 = b.X + b.Width;
      float by2 = b.Y + b.Height;

        return !(ax2 < bx1 || ax1 > bx2 || ay2 < by1 || ay1 > by2);
    }

    private DirtyRegion MergeRegions(DirtyRegion a, DirtyRegion b)
    {
        float x1 = Math.Min(a.X, b.X);
        float y1 = Math.Min(a.Y, b.Y);
        float x2 = Math.Max(a.X + a.Width, b.X + b.Width);
     float y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

 return new DirtyRegion
        {
            X = x1,
         Y = y1,
            Width = x2 - x1,
            Height = y2 - y1,
         Reason = DirtyReason.Merged
        };
    }

    private bool RectanglesIntersect(float x1, float y1, float w1, float h1,
          float x2, float y2, float w2, float h2)
    {
        return !(x1 + w1 < x2 || x1 > x2 + w2 || y1 + h1 < y2 || y1 > y2 + h2);
    }
}

/// <summary>
/// Representa una regi�n rectangular que necesita ser re-renderizada.
/// </summary>
public struct DirtyRegion
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public DirtyReason Reason { get; set; }

    public override string ToString() => $"DirtyRegion({X}, {Y}, {Width}x{Height}) - {Reason}";
}

/// <summary>
/// Raz�n por la que una regi�n est� sucia (�til para debugging y optimizaci�n).
/// </summary>
public enum DirtyReason
{
    ContentChanged,  // El contenido del elemento cambi�
    HoverChanged,    // Estado hover cambi�
 LayoutChanged,   // Layout o posici�n cambi�
    AnimationFrame,  // Frame de animaci�n
    Merged,          // Regi�n fusionada
    FullScreen       // Pantalla completa
}
