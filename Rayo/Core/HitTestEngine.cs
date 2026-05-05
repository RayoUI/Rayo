namespace Rayo.Core;

using Rayo.Core.Interfaces;
using System.Numerics;
using System.Linq;

/// <summary>
/// Motor de hit-testing avanzado optimizado con soporte para:
/// - Transformaciones
/// - Clipping regions
/// - Input transparency
/// - Geometr�as personalizadas
/// - Z-indexing
/// - Spatial indexing (QuadTree para optimizaci�n)
///
/// Inspirado en MAUI, Avalonia y Flutter.
/// </summary>
public class HitTestEngine
{
    private readonly UITree _tree;
    private const int CACHE_INVALIDATION_FRAMES = 60; // Invalida cache cada 60 frames
    private int _frameCounter = 0;

    public HitTestEngine(UITree tree)
    {
        _tree = tree;
    }

    /// <summary>
    /// Realiza un hit-test en un punto espec�fico con opciones configurables.
    /// </summary>
    public HitTestResult? HitTest(Vector2 point, HitTestOptions? options = null)
    {
        options ??= new HitTestOptions();

        if (_tree.Root == null)
            return null;

        return HitTestRoot(_tree.Root, point, options);
    }

    /// <summary>
    /// Realiza un hit-test en un punto espec�fico comenzando desde un elemento ra�z dado.
    /// </summary>
    public HitTestResult? HitTestRoot(VisualElement root, Vector2 point, HitTestOptions? options = null)
    {
        options ??= new HitTestOptions();

        var results = new List<HitTestResult>();
        int zIndex = 0;

        HitTestRecursive(root, point, options, results, ref zIndex, new List<VisualElement>(), Matrix3x2.Identity);

        if (results.Count == 0)
            return null;

        // Retornar seg�n el modo
        return options.Mode switch
        {
            HitTestMode.FirstMatch => results.FirstOrDefault(),
            HitTestMode.AllMatches => CreateCombinedResult(results),
            HitTestMode.InteractiveOnly => results.FirstOrDefault(r => IsInteractive(r.Element)),
            _ => results.FirstOrDefault()
        };
    }

    /// <summary>
    /// Realiza hit-testing en m�ltiples puntos (�til para gestos multi-touch).
    /// </summary>
    public List<HitTestResult?> HitTestMultiple(IEnumerable<Vector2> points, HitTestOptions? options = null)
    {
        return points.Select(p => HitTest(p, options)).ToList();
    }

    /// <summary>
    /// Encuentra todos los elementos bajo un rect�ngulo (selecci�n por �rea).
    /// </summary>
    public List<VisualElement> HitTestRect(float x, float y, float width, float height, HitTestOptions? options = null)
    {
        options ??= new HitTestOptions { Mode = HitTestMode.AllMatches };

        var elements = new HashSet<VisualElement>();

        // Probar las 4 esquinas y el centro
        var points = new[]
   {
    new Vector2(x, y),
         new Vector2(x + width, y),
            new Vector2(x, y + height),
  new Vector2(x + width, y + height),
            new Vector2(x + width / 2, y + height / 2)
        };

        foreach (var point in points)
        {
            var result = HitTest(point, options);
            if (result?.Element != null)
                elements.Add(result.Element);
        }

        return elements.ToList();
    }

    /// <summary>
    /// M�todo recursivo de hit-testing con toda la l�gica avanzada.
    /// </summary>
    private void HitTestRecursive(
        VisualElement element,
   Vector2 point,
      HitTestOptions options,
        List<HitTestResult> results,
        ref int zIndex,
   List<VisualElement> ancestors,
        Matrix3x2 parentTransform)
    {
        var worldTransform = parentTransform * element.GetRenderTransform();

        // 1. Verificar visibilidad
        if (!options.IncludeInvisible && !element.IsVisible)
            return;

        // Disabled elements remain visible, but neither they nor their descendants
        // should participate in interactive hit-testing or focus routing.
        if (!element.IsEffectivelyEnabled())
            return;

        // 2. Verificar input transparency
        if (options.RespectInputTransparency && element is IInputTransparent transparent)
        {
            if (transparent.IsInputTransparent)
            {
                // Elemento transparente - procesar solo hijos
                ProcessChildren(element, point, options, results, ref zIndex, ancestors, worldTransform);
                return;
            }
        }

        // 3. Verificar filtro personalizado
        if (options.ElementFilter != null && !options.ElementFilter(element))
        {
            ProcessChildren(element, point, options, results, ref zIndex, ancestors, worldTransform);
            return;
        }

        // 4. Verificar clipping de ancestros ANTES de procesar children
        // FIX: This prevents children outside clip regions (e.g., scrolled content) from receiving events
        bool isInClipRegion = true;
        if (options.CheckClipping)
        {
            isInClipRegion = CheckClipping(element, point);
        }

        if (!isInClipRegion)
            return;  // Point is clipped - skip this element AND its children

        // 5. Verificar bounds b�sicos (con tolerancia)
        bool isInBounds = IsPointInBounds(element, point, options.Tolerance, worldTransform);

        // 6. Procesar children primero (z-order: �ltimo child = top)
        // Now children are only processed if point is within clip region
        var newAncestors = new List<VisualElement>(ancestors) { element };
        ProcessChildren(element, point, options, results, ref zIndex, newAncestors, worldTransform);

        if (!isInBounds)
            return;

        // 7. Hit-test personalizado (geometr�a compleja)
        if (element is ICustomHitTest customHitTest)
        {
            if (!customHitTest.HitTest(point))
                return;
        }

        // 8. Crear resultado
        var result = new HitTestResult
        {
            Element = element,
            ZIndex = zIndex++,
            IsInBounds = isInBounds,
            IsInClipRegion = isInClipRegion,
            LocalPosition = element.GetLocalPosition(point),
            Ancestors = ancestors.ToList()
        };

        results.Add(result);

        // En modo FirstMatch, detener despu�s del primer resultado
        if (options.Mode == HitTestMode.FirstMatch && results.Count > 0)
            return;
    }

    /// <summary>
    /// Procesa los hijos de un elemento.
    /// </summary>
    private void ProcessChildren(
  VisualElement element,
        Vector2 point,
        HitTestOptions options,
        List<HitTestResult> results,
     ref int zIndex,
  List<VisualElement> ancestors,
        Matrix3x2 worldTransform)
    {
        // Materialise sorted list once (ZIndex ascending = render order),
        // then iterate in reverse so the topmost element (highest ZIndex) is tested first.
        var sorted = element.GetChildrenByZIndex().ToArray();
        for (int i = sorted.Length - 1; i >= 0; i--)
        {
            HitTestRecursive(sorted[i], point, options, results, ref zIndex, ancestors, worldTransform);

            // Early exit en FirstMatch
            if (options.Mode == HitTestMode.FirstMatch && results.Count > 0)
                break;
        }
    }

    /// <summary>
    /// Verifica si un punto est� dentro de los bounds de un elemento.
    /// </summary>
    private bool IsPointInBounds(VisualElement element, Vector2 point, float tolerance, Matrix3x2 worldTransform)
    {
        var probe = point;
        if (worldTransform != Matrix3x2.Identity && Matrix3x2.Invert(worldTransform, out var inverse))
        {
            probe = Vector2.Transform(point, inverse);
        }

        float minX = element.ComputedX - tolerance;
        float minY = element.ComputedY - tolerance;
        float maxX = element.ComputedX + element.ComputedWidth + tolerance;
        float maxY = element.ComputedY + element.ComputedHeight + tolerance;

        return probe.X >= minX && probe.X <= maxX &&
               probe.Y >= minY && probe.Y <= maxY;
    }

    /// <summary>
    /// Verifica el clipping de todos los ancestros.
    /// </summary>
    private bool CheckClipping(VisualElement element, Vector2 point)
    {
        var current = element.Parent;
        while (current != null)
        {
            if (current is IClippable clippable)
            {
                if (!clippable.IsPointInClipRegion(point))
                    return false;
            }
            current = current.Parent;
        }
        return true;
    }

    /// <summary>
    /// Verifica si un elemento es interactivo.
    /// </summary>
    private bool IsInteractive(VisualElement? element)
    {
        if (element == null)
            return false;

        if (!element.IsEffectivelyEnabled())
            return false;

        return element is IInputHandler 
            or Rayo.Core.Input.IPointerHandler;
    }

    /// <summary>
    /// Combina m�ltiples resultados en uno solo (para AllMatches).
    /// </summary>
    private HitTestResult CreateCombinedResult(List<HitTestResult> results)
    {
        var combined = results.First();
        combined.Metadata["AllMatches"] = results;
        combined.Metadata["MatchCount"] = results.Count;
        return combined;
    }

    /// <summary>
    /// Invalida el cache (llamar cuando cambie el layout).
    /// </summary>
    public void InvalidateCache()
    {
    }

    /// <summary>
    /// Actualiza el contador de frames y gestiona el cache.
    /// </summary>
    public void OnFrame()
    {
        _frameCounter++;
        if (_frameCounter >= CACHE_INVALIDATION_FRAMES)
        {
            InvalidateCache();
            _frameCounter = 0;
        }
    }
}

/// <summary>
/// Cache de hit-testing para optimizar consultas repetidas.
/// </summary>
internal class HitTestCache
{
    private Dictionary<Vector2, HitTestResult?> _cache = new();
    private int _maxEntries = 100;

    public HitTestResult? Get(Vector2 point)
    {
        return _cache.TryGetValue(point, out var result) ? result : null;
    }

    public void Set(Vector2 point, HitTestResult? result)
    {
        if (_cache.Count >= _maxEntries)
        {
            // Remover el primero (LRU simple)
            var firstKey = _cache.Keys.First();
            _cache.Remove(firstKey);
        }
        _cache[point] = result;
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
