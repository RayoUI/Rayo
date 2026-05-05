namespace Rayo.Core;

/// <summary>
/// Resultado de una prueba de hit-testing que contiene informaci�n detallada
/// sobre el elemento encontrado y su contexto.
/// </summary>
public class HitTestResult
{
    /// <summary>
    /// El elemento que fue encontrado en la prueba de hit-testing.
    /// </summary>
    public VisualElement? Element { get; set; }

    /// <summary>
    /// La distancia desde el frente (z-order). 0 = m�s al frente.
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// Si el punto est� dentro de los bounds del elemento.
    /// </summary>
    public bool IsInBounds { get; set; }

    /// <summary>
    /// Si el punto est� dentro de la regi�n de clipping.
    /// </summary>
    public bool IsInClipRegion { get; set; }

    /// <summary>
    /// La posici�n relativa al elemento (local coordinates).
    /// </summary>
    public System.Numerics.Vector2 LocalPosition { get; set; }

    /// <summary>
    /// Los ancestros en orden desde el elemento hasta la ra�z.
    /// </summary>
    public List<VisualElement> Ancestors { get; set; } = new();

    /// <summary>
    /// Metadata adicional del hit-test.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Opciones configurables para el hit-testing.
/// </summary>
public class HitTestOptions
{
    /// <summary>
    /// Si se deben considerar elementos invisibles.
    /// </summary>
    public bool IncludeInvisible { get; set; } = false;

    /// <summary>
    /// Si se debe respetar el input transparency.
    /// </summary>
    public bool RespectInputTransparency { get; set; } = true;

    /// <summary>
    /// Si se debe verificar clipping de ancestros.
    /// </summary>
    public bool CheckClipping { get; set; } = true;

    /// <summary>
    /// Filtro personalizado para elementos.
    /// </summary>
    public Func<VisualElement, bool>? ElementFilter { get; set; }

    /// <summary>
    /// Modo de b�squeda: primero que coincida o todos los coincidentes.
    /// </summary>
    public HitTestMode Mode { get; set; } = HitTestMode.FirstMatch;

    /// <summary>
    /// Radio de tolerancia para el hit-testing (�til para touch).
    /// </summary>
    public float Tolerance { get; set; } = 0f;
}

/// <summary>
/// Modo de hit-testing.
/// </summary>
public enum HitTestMode
{
    /// <summary>
    /// Retorna el primer elemento que coincida (m�s r�pido).
    /// </summary>
    FirstMatch,

    /// <summary>
    /// Retorna todos los elementos que coincidan (para debugging).
    /// </summary>
    AllMatches,

    /// <summary>
    /// Solo elementos en el frente que sean interactivos.
    /// </summary>
    InteractiveOnly
}