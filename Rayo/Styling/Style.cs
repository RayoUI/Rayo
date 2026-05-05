using Rayo.Core;
using Rayo.Core.Platform;

namespace Rayo.Styling;

/// <summary>
/// A fluent style rule that targets elements of type <typeparamref name="T"/> filtered
/// by an optional CSS-style selector.
///
    /// <list type="bullet">
    ///   <item><c>new Style&lt;T&gt;()</c>              — type selector: matches all T</item>
    ///   <item><c>new Style&lt;T&gt;("#my-id")</c>      — id selector: highest specificity</item>
    ///   <item><c>new Style&lt;T&gt;(".primary")</c>    — class selector</item>
    ///   <item><c>new Style&lt;T&gt;(".a.b")</c>        — multi-class AND selector</item>
    ///   <item><c>new Style&lt;T&gt;("#id.primary")</c> — id + class AND selector</item>
    /// </list>
///
/// <para><b>Condition combinators</b> — all <c>When</c> overloads add conditional setters:</para>
/// <list type="bullet">
///   <item><see cref="When(StyleTrigger,Action{Style{T}})"/>  — hover / pressed / focused / disabled</item>
///   <item><see cref="When(Breakpoint,Action{Style{T}})"/>     — named window-width tiers</item>
///   <item><see cref="When(Func{float,bool},Action{Style{T}})"/> — custom width predicate</item>
///   <item><see cref="When(PlatformType,Action{Style{T}})"/>   — OS platform</item>
///   <item><see cref="When(ColorScheme,Action{Style{T}})"/>    — light / dark OS theme</item>
///   <item><see cref="When(Orientation,Action{Style{T}})"/>    — portrait / landscape</item>
///   <item><see cref="When(ScreenDensity,Action{Style{T}})"/>  — display DPI tier</item>
/// </list>
///
/// <para><b>Structural selectors</b>:</para>
/// <list type="bullet">
///   <item><see cref="ChildOf{TParent}"/>          — CSS <c>Parent &gt; Child</c></item>
///   <item><see cref="DescendantOf{TAncestor}"/>   — CSS <c>Ancestor Child</c></item>
///   <item><see cref="SiblingOf{TSibling}"/>        — CSS <c>A ~ B</c></item>
///   <item><see cref="ImmediateSiblingOf{TSibling}"/> — CSS <c>A + B</c></item>
///   <item><see cref="FirstChild"/>                 — CSS <c>:first-child</c></item>
///   <item><see cref="LastChild"/>                  — CSS <c>:last-child</c></item>
///   <item><see cref="NthChild(int)"/>              — CSS <c>:nth-child(n)</c></item>
///   <item><see cref="Not(string)"/> / <see cref="Not{TExclude}"/> — CSS <c>:not()</c></item>
/// </list>
/// </summary>
public sealed class Style<T> : StyleRule where T : VisualElement
{
    // ------------------------------------------------------------------
    // Base (unconditional) setters
    // ------------------------------------------------------------------
    private readonly List<Action<T>> _setters = new();

    // ------------------------------------------------------------------
    // Selector filters
    // ------------------------------------------------------------------
    private readonly string?   _idFilter;
    private readonly string[]? _classFilters;   // ALL must match (AND logic)

    // ------------------------------------------------------------------
    // Negation filters  (:not)
    // ------------------------------------------------------------------
    private string? _notIdFilter;
    private string? _notClassFilter;
    private Type?   _notType;

    // ------------------------------------------------------------------
    // Structural filters
    // ------------------------------------------------------------------
    private Type? _requiredDirectParentType;
    private Type? _requiredAncestorType;
    private Type? _requiredSiblingType;
    private Type? _requiredImmediateSiblingType;

    // ------------------------------------------------------------------
    // Position filters  (:first-child / :last-child / :nth-child)
    // ------------------------------------------------------------------
    private bool _firstChild;
    private bool _lastChild;
    private int? _nthChild;   // 1-based step (every N-th child)

    // ------------------------------------------------------------------
    // State-conditional setters
    // ------------------------------------------------------------------
    private List<(StyleTrigger Trigger, List<Action<T>> Setters)>? _triggerBlocks;

    // ------------------------------------------------------------------
    // Breakpoint-conditional setters
    // ------------------------------------------------------------------
    private List<(Breakpoint Breakpoint, List<Action<T>> Setters)>? _breakpointBlocks;

    // ------------------------------------------------------------------
    // Custom-predicate responsive setters (window width)
    // ------------------------------------------------------------------
    private List<(Func<float, bool> Condition, List<Action<T>> Setters)>? _customBreakpointBlocks;

    // ------------------------------------------------------------------
    // Platform-conditional setters
    // ------------------------------------------------------------------
    private List<(PlatformType Platform, List<Action<T>> Setters)>? _platformBlocks;

    // ------------------------------------------------------------------
    // ColorScheme-conditional setters
    // ------------------------------------------------------------------
    private List<(ColorScheme Scheme, List<Action<T>> Setters)>? _colorSchemeBlocks;

    // ------------------------------------------------------------------
    // Orientation-conditional setters
    // ------------------------------------------------------------------
    private List<(Orientation Orient, List<Action<T>> Setters)>? _orientationBlocks;

    // ------------------------------------------------------------------
    // Screen-density-conditional setters
    // ------------------------------------------------------------------
    private List<(ScreenDensity Density, List<Action<T>> Setters)>? _densityBlocks;

    // ------------------------------------------------------------------
    // Transition metadata
    // ------------------------------------------------------------------
    public float           TransitionDuration { get; private set; }
    public Func<float, float>? TransitionEasing  { get; private set; }

    // ==================================================================
    // Constructors
    // ==================================================================

    /// <summary>Type selector — matches all elements of type <typeparamref name="T"/>.</summary>
    public Style() { }

    /// <summary>
    /// CSS-style selector scoped to type <typeparamref name="T"/>.
    /// <para><c>"#my-id"</c>       — id selector (highest specificity).</para>
    /// <para><c>".primary"</c>     — single-class selector.</para>
    /// <para><c>".a.b"</c>         — multi-class AND selector (both classes required).</para>
    /// <para><c>"#id.primary"</c>  — id + class AND selector.</para>
    /// </summary>
    public Style(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        if (selector.StartsWith('#'))
        {
            var parts = selector[1..]
                .Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                throw new ArgumentException("Selector must include an id.", nameof(selector));

            _idFilter = parts[0];

            if (parts.Length > 1)
                _classFilters = parts[1..];
        }
        else if (selector.StartsWith('.'))
        {
            _classFilters = selector.TrimStart('.')
                .Split('.', StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            throw new ArgumentException(
                "Selector must start with '#' for id or '.' for class.", nameof(selector));
        }
    }

    // ==================================================================
    // StyleRule overrides
    // ==================================================================

    /// <inheritdoc/>
    public override Type? TargetType => typeof(T);

    /// <inheritdoc/>
    public override int Specificity
    {
        get
        {
            var specificity = 1;
            if (_idFilter != null)
                specificity += 100;
            if (_classFilters != null)
                specificity += _classFilters.Length * 10;
            return specificity;
        }
    }

    /// <inheritdoc/>
    public override bool HasStateTriggers        => _triggerBlocks      is { Count: > 0 };
    /// <inheritdoc/>
    public override bool HasBreakpointConditions => _breakpointBlocks   is { Count: > 0 };
    /// <inheritdoc/>
    public override bool HasCustomBreakpoints    => _customBreakpointBlocks is { Count: > 0 };
    /// <inheritdoc/>
    public override bool HasColorSchemeConditions => _colorSchemeBlocks  is { Count: > 0 };
    /// <inheritdoc/>
    public override bool HasOrientationConditions => _orientationBlocks  is { Count: > 0 };
    /// <inheritdoc/>
    public override bool HasDensityConditions    => _densityBlocks       is { Count: > 0 };
    /// <inheritdoc/>
    public override bool HasPlatformConditions   => _platformBlocks      is { Count: > 0 };

    /// <inheritdoc/>
    public override bool Matches(VisualElement element)
    {
        if (element is not T) return false;

        // ── selector filters ─────────────────────────────────────────────
        if (_idFilter     != null && element.Id != _idFilter)           return false;
        if (_classFilters != null && !_classFilters.All(element.HasClass)) return false;

        // ── negation (:not) ───────────────────────────────────────────────
        if (_notIdFilter    != null && element.Id == _notIdFilter)        return false;
        if (_notClassFilter != null && element.HasClass(_notClassFilter)) return false;
        if (_notType        != null && _notType.IsInstanceOfType(element)) return false;

        // ── structural: parent / ancestor ────────────────────────────────
        if (_requiredDirectParentType != null &&
            (element.Parent == null || !_requiredDirectParentType.IsInstanceOfType(element.Parent)))
            return false;

        if (_requiredAncestorType != null && !HasAncestor(element, _requiredAncestorType))
            return false;

        // ── structural: siblings ─────────────────────────────────────────
        if (_requiredSiblingType != null && !HasSibling(element, _requiredSiblingType, immediate: false))
            return false;

        if (_requiredImmediateSiblingType != null && !HasSibling(element, _requiredImmediateSiblingType, immediate: true))
            return false;

        // ── position (:first-child / :last-child / :nth-child) ───────────
        if (_firstChild || _lastChild || _nthChild.HasValue)
        {
            var parent = element.Parent;
            if (parent == null) return false;
            var siblings = parent.GetChildren().ToList();
            int index = siblings.IndexOf(element);
            if (index < 0) return false;

            if (_firstChild && index != 0)                                   return false;
            if (_lastChild  && index != siblings.Count - 1)                  return false;
            if (_nthChild.HasValue && (index + 1) % _nthChild.Value != 0)    return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override void Apply(VisualElement element)
    {
        if (element is not T typed) return;

        // 1. Base setters (always)
        foreach (var setter in _setters)
            setter(typed);

        // 2. State-conditional setters
        if (_triggerBlocks != null)
            foreach (var (trigger, setters) in _triggerBlocks)
                if (IsTriggerActive(element, trigger))
                    foreach (var setter in setters) setter(typed);

        // 3. Named breakpoint setters
        if (_breakpointBlocks != null)
        {
            var current = BreakpointHelper.Current;
            foreach (var (bp, setters) in _breakpointBlocks)
                if (bp == current)
                    foreach (var setter in setters) setter(typed);
        }

        // 4. Custom-predicate responsive setters
        if (_customBreakpointBlocks != null)
        {
            var width = Rayo.Core.UIApplication.Current?.WindowWidth ?? 1024f;
            foreach (var (condition, setters) in _customBreakpointBlocks)
                if (condition(width))
                    foreach (var setter in setters) setter(typed);
        }

        // 5. Platform-conditional setters
        if (_platformBlocks != null)
        {
            var current = PlatformDetector.CurrentPlatform;
            foreach (var (platform, setters) in _platformBlocks)
                if (platform == current)
                    foreach (var setter in setters) setter(typed);
        }

        // 6. ColorScheme-conditional setters
        if (_colorSchemeBlocks != null)
        {
            var current = ColorSchemeHelper.Current;
            foreach (var (scheme, setters) in _colorSchemeBlocks)
                if (scheme == current)
                    foreach (var setter in setters) setter(typed);
        }

        // 7. Orientation-conditional setters
        if (_orientationBlocks != null)
        {
            var current = OrientationHelper.Current;
            foreach (var (orient, setters) in _orientationBlocks)
                if (orient == current)
                    foreach (var setter in setters) setter(typed);
        }

        // 8. ScreenDensity-conditional setters
        if (_densityBlocks != null)
        {
            var current = ScreenDensityHelper.Current;
            foreach (var (density, setters) in _densityBlocks)
                if (density == current)
                    foreach (var setter in setters) setter(typed);
        }
    }

    // ==================================================================
    // Fluent builder — base setters
    // ==================================================================

    /// <summary>Adds a custom setter for any property not covered by generated helpers.</summary>
    public Style<T> Set(Action<T> setter)
    {
        _setters.Add(setter);
        return this;
    }

    // ==================================================================
    // Fluent builder — When overloads
    // ==================================================================

    /// <summary>
    /// Adds conditional setters applied when the element matches <paramref name="trigger"/>
    /// (Hover, Pressed, Focused, or Disabled).
    /// </summary>
    public Style<T> When(StyleTrigger trigger, Action<Style<T>> configure)
    {
        _triggerBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _triggerBlocks.Add((trigger, block._setters));
        return this;
    }

    /// <summary>
    /// Adds conditional setters applied when the window width matches the named
    /// <paramref name="breakpoint"/> tier.
    /// </summary>
    public Style<T> When(Breakpoint breakpoint, Action<Style<T>> configure)
    {
        _breakpointBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _breakpointBlocks.Add((breakpoint, block._setters));
        return this;
    }

    /// <summary>
    /// Adds conditional setters applied when <paramref name="condition"/> returns <c>true</c>
    /// for the current window width. Supports any boolean expression, including named predicates:
    /// <code>
    /// static Func&lt;float, bool&gt; Mobile = w => w &lt; 600;
    /// new Style&lt;Label&gt;().When(Mobile, s => s.FontSize(12))
    /// </code>
    /// </summary>
    public Style<T> When(Func<float, bool> condition, Action<Style<T>> configure)
    {
        _customBreakpointBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _customBreakpointBlocks.Add((condition, block._setters));
        return this;
    }

    /// <summary>
    /// Adds conditional setters applied when the app is running on <paramref name="platform"/>.
    /// Evaluated once; the platform never changes at runtime.
    /// </summary>
    public Style<T> When(PlatformType platform, Action<Style<T>> configure)
    {
        _platformBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _platformBlocks.Add((platform, block._setters));
        return this;
    }

    /// <summary>
    /// Adds conditional setters applied when the OS color-scheme preference matches
    /// <paramref name="scheme"/> (Light or Dark).
    /// </summary>
    public Style<T> When(ColorScheme scheme, Action<Style<T>> configure)
    {
        _colorSchemeBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _colorSchemeBlocks.Add((scheme, block._setters));
        return this;
    }

    /// <summary>
    /// Adds conditional setters applied when the window orientation matches
    /// <paramref name="orientation"/> (Portrait or Landscape).
    /// </summary>
    public Style<T> When(Orientation orientation, Action<Style<T>> configure)
    {
        _orientationBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _orientationBlocks.Add((orientation, block._setters));
        return this;
    }

    /// <summary>
    /// Adds conditional setters applied when the display density matches
    /// <paramref name="density"/> (Low, Normal, High, ExtraHigh).
    /// </summary>
    public Style<T> When(ScreenDensity density, Action<Style<T>> configure)
    {
        _densityBlocks ??= new();
        var block = new Style<T>();
        configure(block);
        _densityBlocks.Add((density, block._setters));
        return this;
    }

    // ==================================================================
    // Fluent builder — structural selectors
    // ==================================================================

    /// <summary>
    /// CSS child combinator: matches T whose <em>direct parent</em> is
    /// <typeparamref name="TParent"/> (<c>TParent &gt; T</c>).
    /// </summary>
    public Style<T> ChildOf<TParent>() where TParent : VisualElement
    {
        _requiredDirectParentType = typeof(TParent);
        return this;
    }

    /// <summary>
    /// CSS descendant combinator: matches T that has an ancestor of type
    /// <typeparamref name="TAncestor"/> (<c>TAncestor T</c>).
    /// </summary>
    public Style<T> DescendantOf<TAncestor>() where TAncestor : VisualElement
    {
        _requiredAncestorType = typeof(TAncestor);
        return this;
    }

    /// <summary>
    /// CSS general sibling combinator: matches T that shares a parent with at least
    /// one element of type <typeparamref name="TSibling"/> (<c>TSibling ~ T</c>).
    /// </summary>
    public Style<T> SiblingOf<TSibling>() where TSibling : VisualElement
    {
        _requiredSiblingType = typeof(TSibling);
        return this;
    }

    /// <summary>
    /// CSS adjacent sibling combinator: matches T that is <em>immediately preceded</em>
    /// by an element of type <typeparamref name="TSibling"/> (<c>TSibling + T</c>).
    /// </summary>
    public Style<T> ImmediateSiblingOf<TSibling>() where TSibling : VisualElement
    {
        _requiredImmediateSiblingType = typeof(TSibling);
        return this;
    }

    // ==================================================================
    // Fluent builder — position selectors
    // ==================================================================

    /// <summary>CSS <c>:first-child</c> — matches the first child of its parent.</summary>
    public Style<T> FirstChild() { _firstChild = true; return this; }

    /// <summary>CSS <c>:last-child</c> — matches the last child of its parent.</summary>
    public Style<T> LastChild() { _lastChild = true; return this; }

    /// <summary>
    /// CSS <c>:nth-child(n)</c> — matches every <paramref name="step"/>-th child
    /// (1-based, so <c>NthChild(2)</c> matches the 2nd, 4th, 6th… child).
    /// </summary>
    public Style<T> NthChild(int step) { _nthChild = step; return this; }

    // ==================================================================
    // Fluent builder — negation (:not)
    // ==================================================================

    /// <summary>
    /// CSS <c>:not(selector)</c> — excludes elements matching the selector.
    /// Accepts <c>"#id"</c> or <c>".class"</c> format.
    /// </summary>
    public Style<T> Not(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        if (selector.StartsWith('#'))
            _notIdFilter = selector[1..];
        else if (selector.StartsWith('.'))
            _notClassFilter = selector[1..];
        else
            throw new ArgumentException("Selector must start with '#' or '.'.", nameof(selector));
        return this;
    }

    /// <summary>
    /// CSS <c>:not(Type)</c> — excludes elements whose runtime type is
    /// <typeparamref name="TExclude"/> or a subtype.
    /// </summary>
    public Style<T> Not<TExclude>() where TExclude : VisualElement
    {
        _notType = typeof(TExclude);
        return this;
    }

    // ==================================================================
    // Fluent builder — composition
    // ==================================================================

    /// <summary>
    /// Copies all base setters from <paramref name="baseStyle"/> into this rule,
    /// equivalent to CSS <c>@extend</c>.
    /// </summary>
    public Style<T> Extend(Style<T> baseStyle)
    {
        _setters.AddRange(baseStyle._setters);
        return this;
    }

    /// <summary>
    /// Records transition metadata used by <see cref="StyleApplier"/> when re-applying
    /// state-conditional rules.
    /// </summary>
    public Style<T> WithTransition(float durationMs, Func<float, float>? easing = null)
    {
        TransitionDuration = durationMs;
        TransitionEasing   = easing ?? Rayo.Animation.Easing.OutQuad;
        return this;
    }

    /// <summary>
    /// Marks this rule as <c>!important</c>: it is applied after all normal rules,
    /// regardless of specificity.
    /// </summary>
    public Style<T> Important()
    {
        IsImportant = true;
        return this;
    }

    // ==================================================================
    // Helpers
    // ==================================================================

    private static bool IsTriggerActive(VisualElement element, StyleTrigger trigger) => trigger switch
    {
        StyleTrigger.Hover    => element.IsHovered,
        StyleTrigger.Pressed  => element.IsPressed,
        StyleTrigger.Focused  => element == Rayo.Core.UIApplication.Current?.EventManager?.FocusedElement,
        StyleTrigger.Disabled => !element.IsEnabled,
        _                     => false,
    };

    private static bool HasAncestor(VisualElement element, Type ancestorType)
    {
        var parent = element.Parent;
        while (parent != null)
        {
            if (ancestorType.IsInstanceOfType(parent)) return true;
            parent = parent.Parent;
        }
        return false;
    }

    private static bool HasSibling(VisualElement element, Type siblingType, bool immediate)
    {
        var parent = element.Parent;
        if (parent == null) return false;

        VisualElement? prev = null;
        foreach (var child in parent.GetChildren())
        {
            if (child == element)
            {
                if (!immediate) break;
                // For immediate sibling, only the element directly before qualifies
                return prev != null && siblingType.IsInstanceOfType(prev);
            }
            if (!immediate && siblingType.IsInstanceOfType(child)) return true;
            prev = child;
        }

        return false;
    }
}

// ===========================================================================
// Factory helpers
// ===========================================================================

/// <summary>
/// Factory for CSS-style selector rules not bound to a specific element type.
/// </summary>
public static class Style
{
    public static Style<VisualElement> Id(string id)       => new("#" + id);
    public static Style<T> Id<T>(string id) where T : VisualElement => new("#" + id);

    public static Style<VisualElement> Class(string className)       => new("." + className);
    public static Style<T> Class<T>(string className) where T : VisualElement => new("." + className);
}
