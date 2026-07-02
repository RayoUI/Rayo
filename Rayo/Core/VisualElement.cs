using System.Reflection;
using System.Numerics;
using Rayo.Core.Interfaces; // Added for IInputTransparent
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

namespace Rayo.Core;

using Position = Rayo.Position;
using Size = Rayo.Size;
using Thickness = Rayo.Thickness;

/// <summary>
/// Base class for all UI elements that can be rendered and measured.
/// Consolidates functionality from former VisualElement and UIElementBase classes.
/// Contains common visual properties like size, position, colors, opacity, layout management, and children.
/// </summary>
public abstract class VisualElement : BindableObject, IDisposable, IInputTransparent
{
    [ThreadStatic]
    private static VisualElement? s_themeApplicationOwner;
    [ThreadStatic]
    private static PropertyValueOrigin s_valueOrigin;

    private readonly HashSet<string> _themeManagedProperties = new();
    private readonly HashSet<string> _themeOverrides = new();
    private readonly Dictionary<string, PropertyValueOrigin> _valueOrigins = new();
    private bool _isApplyingTheme;
    private bool _isTrackingThemeOverrides;
    private ThemeData? _effectiveTheme;
    private ThemeData? _detachedTheme;

    /// <summary>
    /// Theme resolved from the nearest scope, then the application, then the built-in default.
    /// </summary>
    public ThemeData EffectiveTheme =>
        _effectiveTheme ??
        ScopedTheme ??
        _detachedTheme ??
        Parent?.EffectiveTheme ??
        UIApplication.Current?.ActiveTheme ??
        UIApplication.FallbackTheme;

    /// <summary>Theme introduced by this element for itself and its descendants.</summary>
    internal virtual ThemeData? ScopedTheme => null;

    internal void CaptureDetachedTheme(VisualElement? owner)
    {
        if (owner == null)
            return;
        _detachedTheme = owner.EffectiveTheme;
        NotifyThemeChanged(_detachedTheme);
    }

    /// <summary>
    /// Static event fired when any element's children are added, removed, or cleared.
    /// Used by DevTools to detect structural changes in the UI tree.
    /// </summary>
    public static event Action<VisualElement>? TreeStructureChanged;

    /// <summary>
    /// Static event fired whenever any element's <see cref="Classes"/> property changes.
    /// <see cref="Component"/> subscribes to this to detect when a descendant's classes
    /// change so it can re-apply its style sheet.
    /// </summary>
    public static event Action<VisualElement>? ClassesChanged;

    /// <summary>
    /// Protected helper to invoke TreeStructureChanged event.
    /// </summary>
    protected static void RaiseTreeStructureChanged(VisualElement element)
    {
        TreeStructureChanged?.Invoke(element);
    }

    internal bool HasExplicitWidth { get; set; } = false;
    internal bool HasExplicitHeight { get; set; } = false;

    #region Property-effect registration (Avalonia-style)
    // Keyed by concrete Type. Populated by [ModuleInitializer] methods in generated extension classes.
    private static readonly Dictionary<Type, HashSet<string>> s_measureProps = new();
    private static readonly Dictionary<Type, HashSet<string>> s_arrangeProps = new();
    private static readonly Dictionary<Type, HashSet<string>> s_paintProps = new();

    /// <summary>
    /// Registers properties that require a layout pass when changed.
    /// Called automatically by source-generated [ModuleInitializer] methods.
    /// Can also be called manually from a static constructor to override the
    /// generator's default classification:
    /// <code>
    /// static MyWidget()
    /// {
    ///     RegisterLayoutProperties(typeof(MyWidget), nameof(MyCustomProp));
    /// }
    /// </code>
    /// </summary>
    public static void RegisterLayoutProperties(Type type, params string[] names)
    {
        RegisterMeasureProperties(type, names);
    }

    /// <summary>
    /// Registers properties that require a measure pass when changed.
    /// Called automatically by source-generated [ModuleInitializer] methods.
    /// </summary>
    public static void RegisterMeasureProperties(Type type, params string[] names)
    {
        if (!s_measureProps.TryGetValue(type, out var set))
            s_measureProps[type] = set = new HashSet<string>();
        foreach (var n in names) set.Add(n);
    }

    /// <summary>
    /// Registers properties that require an arrange pass when changed.
    /// Called automatically by source-generated [ModuleInitializer] methods.
    /// </summary>
    public static void RegisterArrangeProperties(Type type, params string[] names)
    {
        if (!s_arrangeProps.TryGetValue(type, out var set))
            s_arrangeProps[type] = set = new HashSet<string>();
        foreach (var n in names) set.Add(n);
    }

    /// <summary>
    /// Registers properties that only require a repaint when changed.
    /// Called automatically by source-generated [ModuleInitializer] methods.
    /// Can also be called manually from a static constructor to override the
    /// generator's default classification:
    /// <code>
    /// static MyWidget()
    /// {
    ///     RegisterPaintProperties(typeof(MyWidget), nameof(MyAccentColor));
    /// }
    /// </code>
    /// </summary>
    public static void RegisterPaintProperties(Type type, params string[] names)
    {
        if (!s_paintProps.TryGetValue(type, out var set))
            s_paintProps[type] = set = new HashSet<string>();
        foreach (var n in names) set.Add(n);
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is null) return;

        if (s_themeApplicationOwner != null &&
            s_themeApplicationOwner != this)
        {
            _themeOverrides.Remove(propertyName);
        }
        else if (_isTrackingThemeOverrides &&
                 !_isApplyingTheme &&
                 CurrentValueOrigin >= PropertyValueOrigin.Binding &&
                 _themeManagedProperties.Contains(propertyName))
        {
            _themeOverrides.Add(propertyName);
        }

        var type = GetType();
        if (s_measureProps.TryGetValue(type, out var mp) && mp.Contains(propertyName))
            InvalidateMeasure();
        else if (s_arrangeProps.TryGetValue(type, out var ap) && ap.Contains(propertyName))
            InvalidateArrange();
        else if (s_paintProps.TryGetValue(type, out var pp) && pp.Contains(propertyName))
            MarkNeedsPaint();
    }
    #endregion

    public override bool SetProperty<T>(
        ref T field,
        T value,
        Action? onBeforeChanged = null,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PrepareCompositeThemeWrite(propertyName);
        if (!CanApplyValue(propertyName))
        {
            UpdateSuppressedStyleBaseline(propertyName, value);
            return false;
        }
        TrackThemeOverride(propertyName);
        var changed = base.SetProperty(ref field, value, onBeforeChanged, propertyName);
        if (changed)
            RecordValueOrigin(propertyName);
        return changed;
    }

    public override bool SetPropertyCondition<T>(
        ref T field,
        T value,
        Func<T, T, bool>? shouldUpdate = null,
        Action? onBeforeChanged = null,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PrepareCompositeThemeWrite(propertyName);
        if (!CanApplyValue(propertyName))
        {
            UpdateSuppressedStyleBaseline(propertyName, value);
            return false;
        }
        TrackThemeOverride(propertyName);
        var changed = base.SetPropertyCondition(
            ref field,
            value,
            shouldUpdate,
            onBeforeChanged,
            propertyName);
        if (changed)
            RecordValueOrigin(propertyName);
        return changed;
    }

    public PropertyValueOrigin GetValueOrigin(string propertyName) =>
        _valueOrigins.TryGetValue(propertyName, out var origin)
            ? origin
            : PropertyValueOrigin.Default;

    /// <summary>
    /// Applies a reactive binding update with binding precedence.
    /// Generated signal bindings use this method automatically.
    /// </summary>
    public void ApplyBindingValue(Action setter)
    {
        ArgumentNullException.ThrowIfNull(setter);
        using var valueOrigin = EnterValueOrigin(PropertyValueOrigin.Binding);
        setter();
    }

    internal static IDisposable EnterValueOrigin(PropertyValueOrigin origin)
    {
        var previous = s_valueOrigin;
        s_valueOrigin = origin;
        return new ValueOriginScope(previous);
    }

    private bool CanApplyValue(string? propertyName)
    {
        if (propertyName == null || !_valueOrigins.TryGetValue(propertyName, out var current))
            return true;
        var incoming = CurrentValueOrigin;
        return incoming >= current;
    }

    private void UpdateSuppressedStyleBaseline<T>(string? propertyName, T value)
    {
        if (propertyName == null ||
            CurrentValueOrigin != PropertyValueOrigin.Theme ||
            GetValueOrigin(propertyName) != PropertyValueOrigin.Style ||
            _styleBaseline == null)
        {
            return;
        }

        _styleBaseline[propertyName] = value;
        _styleBaselineOrigins ??= new Dictionary<string, PropertyValueOrigin>();
        _styleBaselineOrigins[propertyName] = PropertyValueOrigin.Theme;
    }

    private void RecordValueOrigin(string? propertyName)
    {
        if (propertyName == null)
            return;
        var origin = CurrentValueOrigin;
        if (origin != PropertyValueOrigin.Default)
            _valueOrigins[propertyName] = origin;
    }

    private PropertyValueOrigin CurrentValueOrigin =>
        _isApplyingTheme || s_themeApplicationOwner != null
            ? PropertyValueOrigin.Theme
            : s_valueOrigin != PropertyValueOrigin.Default
                ? s_valueOrigin
                : _isTrackingThemeOverrides
                    ? PropertyValueOrigin.Local
                    : PropertyValueOrigin.Default;

    private void PrepareCompositeThemeWrite(string? propertyName)
    {
        if (propertyName == null ||
            s_themeApplicationOwner == null ||
            s_themeApplicationOwner == this)
        {
            return;
        }

        _themeOverrides.Remove(propertyName);
        if (_valueOrigins.GetValueOrDefault(propertyName) == PropertyValueOrigin.Local)
            _valueOrigins.Remove(propertyName);
    }

    private readonly struct ValueOriginScope(PropertyValueOrigin previous) : IDisposable
    {
        public void Dispose() => s_valueOrigin = previous;
    }

    private void TrackThemeOverride(string? propertyName)
    {
        if (propertyName == null)
            return;

        // A composite control may style one of its internal children while its
        // own theme is being applied. That value belongs to the theme cascade,
        // not to the consumer, so it must replace any construction-time
        // override previously recorded by the child.
        if (s_themeApplicationOwner != null && s_themeApplicationOwner != this)
        {
            _themeOverrides.Remove(propertyName);
            return;
        }

        if (_isTrackingThemeOverrides &&
            !_isApplyingTheme &&
            CurrentValueOrigin >= PropertyValueOrigin.Binding &&
            _themeManagedProperties.Contains(propertyName))
        {
            _themeOverrides.Add(propertyName);
        }
    }

    protected VisualElement()
    {

    }

    /// <summary>
    /// Applies a theme value unless the property was explicitly changed by the consumer.
    /// </summary>
    protected void SetThemeValue<T>(string propertyName, T value, Action<T> setter)
    {
        if (_themeOverrides.Contains(propertyName))
            return;

        _themeManagedProperties.Add(propertyName);
        value = EffectiveTheme.Components.Resolve(GetType(), propertyName, value);
        bool wasApplyingTheme = _isApplyingTheme;
        _isApplyingTheme = true;
        try
        {
            setter(value);
        }
        finally
        {
            _isApplyingTheme = wasApplyingTheme;
        }
    }

    /// <summary>
    /// Applies the current theme during construction and starts tracking explicit overrides.
    /// </summary>
    protected void InitializeTheme()
    {
        ApplyTheme(EffectiveTheme);
    }

    /// <summary>
    /// Clears local theme-property overrides and immediately reapplies the active theme.
    /// </summary>
    protected void ResetThemeValues()
    {
        _themeOverrides.Clear();
        foreach (var propertyName in _themeManagedProperties)
        {
            if (_valueOrigins.GetValueOrDefault(propertyName) == PropertyValueOrigin.Local)
                _valueOrigins.Remove(propertyName);
        }
        ApplyTheme(EffectiveTheme);
    }

    /// <summary>Called whenever a theme must be applied to this element.</summary>
    protected virtual void OnThemeApplied(ThemeData theme) { }

    private void ApplyTheme(ThemeData theme)
    {
        _effectiveTheme = theme;
        var previousOwner = s_themeApplicationOwner;
        bool wasApplyingTheme = _isApplyingTheme;
        s_themeApplicationOwner = this;
        _isApplyingTheme = true;
        try
        {
            OnThemeApplied(theme);
            ApplyComponentThemeOverrides(theme);
            _isTrackingThemeOverrides = true;
        }
        finally
        {
            _isApplyingTheme = wasApplyingTheme;
            s_themeApplicationOwner = previousOwner;
        }
    }

    private void ApplyComponentThemeOverrides(ThemeData theme)
    {
        foreach (var (propertyName, value) in theme.Components.GetOverrides(GetType()))
        {
            if (_themeOverrides.Contains(propertyName))
                continue;

            var property = GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property?.CanWrite != true)
                continue;
            if (value != null && !property.PropertyType.IsInstanceOfType(value))
                continue;

            _themeManagedProperties.Add(propertyName);
            property.SetValue(this, value);
        }
    }

    internal void NotifyThemeChanged(ThemeData theme)
    {
        var effectiveTheme = ScopedTheme ?? _detachedTheme ?? theme;
        // Apply descendants first. Composite controls then get the final word
        // over their implementation details, matching a style cascade while
        // preserving explicit customizations on public/user-owned children.
        foreach (var child in GetChildren().ToArray())
        {
            child.NotifyThemeChanged(effectiveTheme);
        }
        ApplyTheme(effectiveTheme);
    }

    internal virtual CornerRadius VisualCornerRadius => CornerRadius.None;

    #region Id
    [PaintProperty]
    public string? Id
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Classes
    /// <summary>
    /// One or more space-separated style class names, identical in concept to the HTML
    /// <c>class</c> attribute. Use <see cref="HasClass"/> to test membership.
    ///
    /// Example: <c>Classes = "primary large"</c>
    /// </summary>
    [PaintProperty]
    public string? Classes
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
                ClassesChanged?.Invoke(this);
        }
    }

    /// <summary>Returns true if <paramref name="className"/> appears in <see cref="Classes"/>.</summary>
    public bool HasClass(string className)
    {
        if (string.IsNullOrWhiteSpace(Classes) || string.IsNullOrWhiteSpace(className))
            return false;

        foreach (var part in Classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (string.Equals(part, className, StringComparison.Ordinal))
                return true;

        return false;
    }

    /// <summary>Adds <paramref name="className"/> to <see cref="Classes"/> if not already present.</summary>
    public void AddClass(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return;
        Classes = string.IsNullOrWhiteSpace(Classes)
            ? className
            : HasClass(className) ? Classes : Classes + " " + className;
    }

    /// <summary>Removes <paramref name="className"/> from <see cref="Classes"/>.</summary>
    public void RemoveClass(string className = "")
    {
        if (string.IsNullOrWhiteSpace(Classes) || string.IsNullOrWhiteSpace(className))
        {
            Classes = null;
            return;
        }

        var parts = Classes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.Equals(p, className, StringComparison.Ordinal));

        Classes = string.Join(' ', parts);
    }
    #endregion

    #region Cursor
    /// <summary>
    /// Desktop cursor shape requested while the pointer is over this element.
    /// </summary>
    [PaintProperty]
    public CursorShape Cursor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = CursorShape.Default;
    #endregion

    #region Parent
    public VisualElement? Parent
    {
        get => field;
        internal set
        {
            var oldParent = field;
            if (this.SetProperty(ref field, value))
            {
                // Check if element was mounted or unmounted
                if (oldParent == null && field != null)
                {
                    // Element was just mounted (added to tree)
                    NotifyMounted();
                }
                else if (oldParent != null && field == null)
                {
                    // Element was just unmounted (removed from tree)
                    NotifyUnmounted();
                }
            }
        }
    }
    #endregion

    #region Position (X, Y)
    [ArrangeProperty]
    public float X
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    [ArrangeProperty]
    public float Y
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Size (Width, Height, MinWidth, MinHeight, MaxWidth, MaxHeight)
    [MeasureProperty]
    public float Width
    {
        get => field;
        set => this.SetProperty(ref field, value, () => HasExplicitWidth = true);
    } = 0;

    [MeasureProperty]
    public float Height
    {
        get => field;
        set => this.SetProperty(ref field, value, () => HasExplicitHeight = true);
    } = 0;

    [MeasureProperty]
    public float MinWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    [MeasureProperty]
    public float MinHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    [MeasureProperty]
    public float MaxWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = float.PositiveInfinity;

    [MeasureProperty]
    public float MaxHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = float.PositiveInfinity;
    #endregion

    #region Margin
    [MeasureProperty]
    public Thickness Margin
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Thickness();
    #endregion

    #region Padding
    [MeasureProperty]
    public Thickness Padding
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Thickness();
    #endregion

    #region ClipToBounds
    [PaintProperty]
    public bool ClipToBounds
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Alignment
    [MeasureProperty]
    public HorizontalAlignment HorizontalAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            // Stretch alignment overrides any explicit width: the parent decides the size.
            if (value == HorizontalAlignment.Stretch)
                HasExplicitWidth = false;
        });
    } = HorizontalAlignment.Left;

    [MeasureProperty]
    public VerticalAlignment VerticalAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (value == VerticalAlignment.Stretch)
                HasExplicitHeight = false;
        });
    } = VerticalAlignment.Top;
    #endregion

    #region Visual Properties
    [PaintProperty]
    public Brush Background
    {
        get => field;
        set => SetProperty(ref field, value);
    } = new SolidColorBrush(Color.Transparent);

    [PaintProperty]
    public float Opacity
    {
        get => field;
        set => this.SetProperty(ref field, Math.Clamp(value, 0f, 1f));
    } = 1.0f;

    [MeasureProperty]
    public bool IsVisible
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;

    [PaintProperty]
    public bool IsEnabled
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = true;

    /// <summary>
    /// Returns whether this element and all of its ancestors are enabled for interaction.
    /// Mirrors the effective enabled behavior used by mature UI frameworks.
    /// </summary>
    public bool IsEffectivelyEnabled()
    {
        var current = this;
        while (current != null)
        {
            if (!current.IsEnabled)
                return false;

            current = current.Parent;
        }

        return true;
    }

    /// <summary>
    /// Controls the rendering and hit-test order among siblings.
    /// Higher values render on top of (and receive input before) lower values.
    /// Elements with equal ZIndex are ordered by their position in the parent's Children list.
    /// Equivalent to MAUI's VisualElement.ZIndex.
    /// </summary>
    [PaintProperty]
    public int ZIndex
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    [PaintProperty]
    public float Rotation
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0f;

    [PaintProperty]
    public float Scale
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 1f;

    [PaintProperty]
    public float TranslationX
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0f;

    [PaintProperty]
    public float TranslationY
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0f;
    #endregion

    #region Input
    [NotFluent]
    public bool IsInputTransparent { get; set; } = false;

    /// <summary>
    /// True when the pointer is over this element. Set internally by <c>EventManager</c>.
    /// Can be used in <c>Style&lt;T&gt;.When(StyleTrigger.Hover, ...)</c> rules.
    /// </summary>
    [NotFluent]
    public bool IsHovered
    {
        get => field;
        internal set => this.SetProperty(ref field, value);
    }

    /// <summary>
    /// True while the primary mouse button is held down over this element.
    /// Set internally by <c>EventManager</c>.
    /// Can be used in <c>Style&lt;T&gt;.When(StyleTrigger.Pressed, ...)</c> rules.
    /// </summary>
    [NotFluent]
    public bool IsPressed
    {
        get => field;
        internal set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Computed Layout

    [NotFluent]
    internal bool NeedsMeasure { get; set; } = true;

    [NotFluent]
    internal bool NeedsArrange { get; set; } = true;

    [NotFluent]
    internal bool NeedsLayout
    {
        get => NeedsMeasure || NeedsArrange;
        set
        {
            NeedsMeasure = value;
            NeedsArrange = value;
        }
    }

    [NotFluent]
    internal bool NeedsPaint { get; set; } = true;

    [NotFluent]
    public float ComputedX { get; set; }

    [NotFluent]
    public float ComputedY { get; set; }

    [NotFluent]
    public float ComputedWidth { get; set; }

    [NotFluent]
    public float ComputedHeight { get; set; }

    [NotFluent]
    public float DesiredWidth { get; set; }

    [NotFluent]
    public float DesiredHeight { get; set; }

    [NotFluent]
    internal float LastMeasuredAvailableWidth { get; private set; } = float.NaN;

    [NotFluent]
    internal float LastMeasuredAvailableHeight { get; private set; } = float.NaN;

    [NotFluent]
    internal bool HasValidMeasure { get; private set; }

    private const int MeasureCacheCapacity = 4;
    private const float MeasureCacheQuantizationStep = 0.25f;
    private readonly MeasureCacheEntry[] _measureCacheEntries = new MeasureCacheEntry[MeasureCacheCapacity];
    private int _measureCacheCount;
    private int _measureCacheNextIndex;

    [NotFluent]
    internal float LastArrangedX { get; private set; } = float.NaN;

    [NotFluent]
    internal float LastArrangedY { get; private set; } = float.NaN;

    [NotFluent]
    internal float LastArrangedWidth { get; private set; } = float.NaN;

    [NotFluent]
    internal float LastArrangedHeight { get; private set; } = float.NaN;

    [NotFluent]
    internal bool HasValidArrange { get; private set; }

    #endregion

    #region Disposables
    private List<IDisposable>? _disposables;

    public void RegisterDisposable(IDisposable disposable)
    {
        _disposables ??= new List<IDisposable>();
        _disposables.Add(disposable);
    }

    private void DisposeSubscriptions()
    {
        if (_disposables != null)
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
    }

    public void Dispose()
    {
        DisposeSubscriptions();
    }
    #endregion

    #region Style Baseline

    // Per-type cache: avoids re-running reflection on every element of the same type.
    private static readonly Dictionary<Type, PropertyInfo[]> _styleablePropsCache = [];

    // Properties that must never be saved/restored by the style baseline:
    //   Id      — identity, not a visual property
    //   Classes — CRITICAL: restoring Classes would re-fire ClassesChanged → infinite loop
    //   X / Y   — layout-managed position, not a style concern
    private static readonly HashSet<string> _baselineExcludedProps =
        new(StringComparer.Ordinal) { nameof(Id), nameof(Classes), "X", "Y" };

    /// <summary>
    /// Returns all public instance properties of <paramref name="type"/> that are
    /// candidates for baseline snapshotting. Result is cached per type.
    /// </summary>
    private static PropertyInfo[] GetStyleableProperties(Type type)
    {
        if (_styleablePropsCache.TryGetValue(type, out var cached))
            return cached;

        var props = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                     && p.CanWrite
                     && p.GetIndexParameters().Length == 0
                     && !_baselineExcludedProps.Contains(p.Name))
            .ToArray();

        _styleablePropsCache[type] = props;
        return props;
    }

    /// <summary>
    /// Snapshot of property values captured before the first <see cref="StyleEngine"/> pass.
    /// Used to restore inline-set values when <see cref="Classes"/> changes and styles are re-applied.
    /// </summary>
    private Dictionary<string, object?>? _styleBaseline;
    private Dictionary<string, PropertyValueOrigin>? _styleBaselineOrigins;

    /// <summary>
    /// Captures ALL public settable properties of this element's runtime type as the
    /// style baseline — the "inline" values established during construction.
    /// Works automatically for built-in and custom controls without any manual registration.
    /// Has no effect if the baseline was already captured.
    /// </summary>
    internal void CaptureStyleBaseline()
    {
        if (_styleBaseline != null) return;

        var props = GetStyleableProperties(GetType());
        _styleBaseline = new Dictionary<string, object?>(props.Length + 2);
        _styleBaselineOrigins = new Dictionary<string, PropertyValueOrigin>(props.Length);

        foreach (var prop in props)
        {
            try
            {
                _styleBaseline[prop.Name] = prop.GetValue(this);
                _styleBaselineOrigins[prop.Name] = GetValueOrigin(prop.Name);
            }
            catch { /* skip properties that throw on read */ }
        }

        // Width/Height setters have a side-effect: they set HasExplicitWidth/Height = true.
        // Save the pre-style values of these internal fields so Restore can undo that effect.
        _styleBaseline["__HasExplicitWidth"]  = HasExplicitWidth;
        _styleBaseline["__HasExplicitHeight"] = HasExplicitHeight;
    }

    /// <summary>
    /// Restores every property to its baseline value so that previously-applied style
    /// rules are cleared before new matching rules run.
    /// </summary>
    internal void RestoreStyleBaseline()
    {
        if (_styleBaseline == null) return;

        var props = GetStyleableProperties(GetType());

        foreach (var prop in props)
        {
            if (GetValueOrigin(prop.Name) != PropertyValueOrigin.Style ||
                !_styleBaseline.TryGetValue(prop.Name, out var value))
            {
                continue;
            }

            var baselineOrigin = _styleBaselineOrigins?.GetValueOrDefault(prop.Name)
                ?? PropertyValueOrigin.Default;
            _valueOrigins.Remove(prop.Name);
            try
            {
                using var valueOrigin = EnterValueOrigin(baselineOrigin);
                prop.SetValue(this, value);
                if (baselineOrigin == PropertyValueOrigin.Default)
                    _valueOrigins.Remove(prop.Name);
            }
            catch { /* skip read-only or incompatible properties */ }
        }

        // Undo the HasExplicitWidth/Height side-effect caused by the Width/Height setters.
        if (_styleBaseline.TryGetValue("__HasExplicitWidth", out var hew))
            HasExplicitWidth = (bool)hew!;
        if (_styleBaseline.TryGetValue("__HasExplicitHeight", out var heh))
            HasExplicitHeight = (bool)heh!;
    }

    /// <summary>
    /// Called by <see cref="StyleEngine"/> before every style pass.
    /// Captures the baseline on the first pass; restores it on every subsequent pass.
    /// </summary>
    internal void PrepareForStyleApplication()
    {
        if (_styleBaseline == null)
            CaptureStyleBaseline();
        else
            RestoreStyleBaseline();
    }

    /// <summary>
    /// Updates a single entry in the style baseline so that external tools (e.g. DevTools)
    /// that modify properties after the first style pass can persist their changes through
    /// subsequent style reapplications.
    /// Has no effect if the baseline has not been captured yet.
    /// </summary>
    internal void UpdateStyleBaselineEntry(string propertyName, object? value)
    {
        if (_styleBaseline == null) return;
        _styleBaseline[propertyName] = value;
        _styleBaselineOrigins ??= new Dictionary<string, PropertyValueOrigin>();
        _styleBaselineOrigins[propertyName] = PropertyValueOrigin.Local;

        // Keep the HasExplicitWidth/Height sentinels in sync when Width/Height are overridden.
        if (propertyName == nameof(Width))
            _styleBaseline["__HasExplicitWidth"] = HasExplicitWidth;
        else if (propertyName == nameof(Height))
            _styleBaseline["__HasExplicitHeight"] = HasExplicitHeight;
    }

    #endregion

    #region Dirty Tracking
    public void MarkNeedsLayout()
    {
        InvalidateMeasure();
    }

    public void InvalidateMeasure()
    {
        bool wasDirty = NeedsMeasure;
        if (!NeedsMeasure)
        {
            NeedsMeasure = true;
            NeedsArrange = true;
            NeedsPaint = true;
        }

        HasValidMeasure = false;
        ClearMeasureCache();

        if (!wasDirty)
            Rayo.DevTools.PerformanceTracker.RecordMeasureDirty(this);

        var current = this;
        while (current.Parent != null &&
               !current.CreatesMeasureBoundaryForParent() &&
               !current.Parent.AbsorbsDescendantMeasureChange())
        {
            current = current.Parent;
            if (current.NeedsMeasure)
                break;

            current.NeedsMeasure = true;
            current.NeedsArrange = true;
            current.NeedsPaint = true;
        }

        (UIApplication.Current?.Tree ?? UITree.Current)?.MarkElementNeedsMeasure(this);
    }

    public void InvalidateArrange()
    {
        if (!NeedsArrange)
        {
            NeedsArrange = true;
            NeedsPaint = true;
            Rayo.DevTools.PerformanceTracker.RecordArrangeDirty(this);
        }

        if (Parent != null && !Parent.NeedsArrange)
        {
            Parent.NeedsArrange = true;
            Parent.NeedsPaint = true;
        }

        (UIApplication.Current?.Tree ?? UITree.Current)?.MarkElementNeedsArrange(this);
    }

    public void MarkNeedsPaint()
    {
        if (!NeedsPaint)
        {
            NeedsPaint = true;

            // Record in performance tracker (dirty heatmap + dirty log).
            Rayo.DevTools.PerformanceTracker.RecordPaintDirty(this);
        }

        // Always notify UITree to trigger a render (works on Desktop and Android)
        (UIApplication.Current?.Tree ?? UITree.Current)?.MarkElementNeedsPaint(this);
    }
    #endregion

    #region Pointer Events Support
    internal bool HasRenderTransform =>
        Rotation != 0f ||
        Scale != 1f ||
        TranslationX != 0f ||
        TranslationY != 0f;

    internal Matrix3x2 GetRenderTransform()
    {
        if (!HasRenderTransform)
            return Matrix3x2.Identity;

        Matrix3x2 transform = Matrix3x2.Identity;
        var origin = new Vector2(
            ComputedX + ComputedWidth * 0.5f,
            ComputedY + ComputedHeight * 0.5f);

        if (Scale != 1f)
            transform *= Matrix3x2.CreateScale(Scale, origin);

        if (Rotation != 0f)
            transform *= Matrix3x2.CreateRotation(MathF.PI * Rotation / 180f, origin);

        if (TranslationX != 0f || TranslationY != 0f)
            transform *= Matrix3x2.CreateTranslation(TranslationX, TranslationY);

        return transform;
    }

    internal Matrix3x2 GetWorldRenderTransform()
    {
        Matrix3x2 transform = Matrix3x2.Identity;

        var chain = new Stack<VisualElement>();
        var current = this;
        while (current != null)
        {
            chain.Push(current);
            current = current.Parent;
        }

        while (chain.Count > 0)
        {
            transform *= chain.Pop().GetRenderTransform();
        }

        return transform;
    }

    internal bool TryGetInverseWorldRenderTransform(out Matrix3x2 inverse)
    {
        var world = GetWorldRenderTransform();
        return Matrix3x2.Invert(world, out inverse);
    }

    /// <summary>
    /// Converts window coordinates to element-relative coordinates.
    /// Used by PointerEventManager for LocalPosition calculation in pointer events.
    /// </summary>
    /// <param name="windowPosition">Position in window coordinates</param>
    /// <returns>Position relative to this element's top-left corner</returns>
    public Vector2 GetLocalPosition(Vector2 windowPosition)
    {
        if (TryGetInverseWorldRenderTransform(out var inverse))
        {
            var untransformed = Vector2.Transform(windowPosition, inverse);
            return new Vector2(untransformed.X - ComputedX, untransformed.Y - ComputedY);
        }

        return new Vector2(windowPosition.X - ComputedX, windowPosition.Y - ComputedY);
    }

    public bool ContainsWindowPoint(Vector2 windowPosition, float tolerance = 0f)
    {
        Vector2 probe = windowPosition;
        if (TryGetInverseWorldRenderTransform(out var inverse))
            probe = Vector2.Transform(windowPosition, inverse);

        float minX = ComputedX - tolerance;
        float minY = ComputedY - tolerance;
        float maxX = ComputedX + ComputedWidth + tolerance;
        float maxY = ComputedY + ComputedHeight + tolerance;

        return probe.X >= minX && probe.X <= maxX &&
               probe.Y >= minY && probe.Y <= maxY;
    }
    #endregion

    #region Layout Methods

    /// <summary>
    /// Gets all children of this element (for rendering and layout).
    /// Default returns empty. Override in CompositeElement, ContentView, and Layout.
    /// </summary>
    internal virtual IEnumerable<VisualElement> GetChildren()
    {
        // Default: no children (for leaf elements like Label, Image, etc.)
        return Enumerable.Empty<VisualElement>();
    }

    /// <summary>
    /// Returns children sorted by ZIndex ascending (then by insertion order for ties),
    /// matching MAUI's rendering and hit-test semantics.
    /// </summary>
    internal IReadOnlyList<VisualElement> GetChildrenByZIndex()
    {
        var source = GetChildren();
        if (source is IReadOnlyList<VisualElement> readOnlyList)
        {
            var readOnlySnapshot = CreateChildrenSnapshot(readOnlyList);
            if (!HasNonDefaultZIndex(readOnlySnapshot))
                return readOnlySnapshot;

            StableSortByZIndex(readOnlySnapshot);
            return readOnlySnapshot;
        }

        if (source is IList<VisualElement> list)
        {
            var listSnapshot = CreateChildrenSnapshot(list);
            if (!HasNonDefaultZIndex(listSnapshot))
                return listSnapshot;

            StableSortByZIndex(listSnapshot);
            return listSnapshot;
        }

        var snapshot = new List<VisualElement>();
        using var enumerator = source.GetEnumerator();
        while (true)
        {
            try
            {
                if (!enumerator.MoveNext())
                    break;

                snapshot.Add(enumerator.Current);
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }

        if (!HasNonDefaultZIndex(snapshot))
            return snapshot;

        StableSortByZIndex(snapshot);
        return snapshot;
    }

    private static bool HasNonDefaultZIndex(IReadOnlyList<VisualElement> children)
    {
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].ZIndex != 0)
                return true;
        }

        return false;
    }

    private static List<VisualElement> CreateChildrenSnapshot(IReadOnlyList<VisualElement> children)
    {
        var snapshot = new List<VisualElement>(children.Count);
        for (int i = 0; i < children.Count; i++)
        {
            try
            {
                snapshot.Add(children[i]);
            }
            catch (ArgumentOutOfRangeException)
            {
                break;
            }
        }

        return snapshot;
    }

    private static List<VisualElement> CreateChildrenSnapshot(IList<VisualElement> children)
    {
        var snapshot = new List<VisualElement>(children.Count);
        for (int i = 0; i < children.Count; i++)
        {
            try
            {
                snapshot.Add(children[i]);
            }
            catch (ArgumentOutOfRangeException)
            {
                break;
            }
        }

        return snapshot;
    }

    private static void StableSortByZIndex(List<VisualElement> children)
    {
        for (int i = 1; i < children.Count; i++)
        {
            var current = children[i];
            int currentZIndex = current.ZIndex;
            int j = i - 1;

            while (j >= 0 && children[j].ZIndex > currentZIndex)
            {
                children[j + 1] = children[j];
                j--;
            }

            children[j + 1] = current;
        }
    }

    /// <summary>
    /// If true, the element renders its children manually (used for clipping, etc).
    /// UITree will not render children automatically in this case.
    /// </summary>
    protected internal virtual bool RendersChildrenManually => false;

    protected internal virtual bool CreatesMeasureBoundaryForParent()
    {
        return HasExplicitWidth && HasExplicitHeight;
    }

    protected internal virtual bool AbsorbsDescendantMeasureChange()
    {
        return HasExplicitWidth && HasExplicitHeight;
    }

    protected internal virtual bool AbsorbsDescendantArrangeChange()
    {
        return HasExplicitWidth && HasExplicitHeight;
    }

    private void ExecuteMeasure(float availableWidth, float availableHeight, bool force)
    {
        if (!force && !NeedsMeasure && TryApplyCachedMeasure(availableWidth, availableHeight))
            return;

        var constrainedAvailable = ApplyMeasureConstraints(availableWidth, availableHeight);
        Measure(constrainedAvailable.Width, constrainedAvailable.Height);
        ApplyDesiredSizeConstraints();
        LastMeasuredAvailableWidth = availableWidth;
        LastMeasuredAvailableHeight = availableHeight;
        HasValidMeasure = true;
        StoreMeasureCache(availableWidth, availableHeight, DesiredWidth, DesiredHeight);
        NeedsMeasure = false;
        NeedsArrange = true;
        NeedsPaint = true;
        OnMeasured(DesiredWidth, DesiredHeight);
    }

    private void ExecuteArrange(float x, float y, float width, float height, bool force)
    {
        var constrainedSize = ApplySizeConstraints(width, height);
        width = constrainedSize.Width;
        height = constrainedSize.Height;

        bool rectChanged = !HasValidArrange ||
            LastArrangedX != x ||
            LastArrangedY != y ||
            LastArrangedWidth != width ||
            LastArrangedHeight != height;

        if (!force && !NeedsArrange && !rectChanged)
            return;

        Arrange(x, y, width, height);
        LastArrangedX = x;
        LastArrangedY = y;
        LastArrangedWidth = width;
        LastArrangedHeight = height;
        HasValidArrange = true;
        NeedsArrange = false;
        NeedsPaint = true;
        OnArranged(x, y, width, height);
    }

    public bool IsMeasureValidFor(float availableWidth, float availableHeight)
    {
        return TryFindMeasureCacheEntry(availableWidth, availableHeight, out _);
    }

    public void MeasureUpdate(float availableWidth, float availableHeight)
    {
        if (!NeedsMeasure)
        {
            if (TryApplyCachedMeasure(availableWidth, availableHeight))
            {
                Rayo.DevTools.PerformanceTracker.RecordMeasureCacheHit();
                Rayo.DevTools.PerformanceTracker.RecordMeasureSkipped();
                return;
            }

            Rayo.DevTools.PerformanceTracker.RecordMeasureCacheMiss();
        }

        ExecuteMeasure(availableWidth, availableHeight, force: false);
    }

    internal void ForceMeasure(float availableWidth, float availableHeight)
    {
        ExecuteMeasure(availableWidth, availableHeight, force: true);
    }

    public void ArrangeUpdate(float x, float y, float width, float height)
    {
        var constrainedSize = ApplySizeConstraints(width, height);
        width = constrainedSize.Width;
        height = constrainedSize.Height;

        bool rectChanged = !HasValidArrange ||
            LastArrangedX != x ||
            LastArrangedY != y ||
            LastArrangedWidth != width ||
            LastArrangedHeight != height;

        if (!NeedsArrange && !rectChanged)
        {
            Rayo.DevTools.PerformanceTracker.RecordArrangeSkipped();
            return;
        }

        ExecuteArrange(x, y, width, height, force: false);
    }

    internal void ForceArrange(float x, float y, float width, float height)
    {
        ExecuteArrange(x, y, width, height, force: true);
    }

    protected virtual void Measure(float availableWidth, float availableHeight)
    {
        foreach (var child in GetChildren().ToArray())
        {
            child.MeasureUpdate(availableWidth, availableHeight);
        }
    }

    private bool TryApplyCachedMeasure(float availableWidth, float availableHeight)
    {
        if (!TryFindMeasureCacheEntry(availableWidth, availableHeight, out var cacheEntry))
        {
            return false;
        }

        var normalizedConstraints = NormalizeMeasureConstraints(availableWidth, availableHeight);
        var activeNormalizedConstraints = NormalizeMeasureConstraints(LastMeasuredAvailableWidth, LastMeasuredAvailableHeight);
        bool isActiveMeasure =
            HasValidMeasure &&
            activeNormalizedConstraints.Width == normalizedConstraints.Width &&
            activeNormalizedConstraints.Height == normalizedConstraints.Height;

        if (isActiveMeasure)
        {
            return true;
        }

        bool desiredChanged = DesiredWidth != cacheEntry.DesiredWidth || DesiredHeight != cacheEntry.DesiredHeight;
        DesiredWidth = cacheEntry.DesiredWidth;
        DesiredHeight = cacheEntry.DesiredHeight;
        LastMeasuredAvailableWidth = availableWidth;
        LastMeasuredAvailableHeight = availableHeight;
        HasValidMeasure = true;
        NeedsMeasure = false;
        NeedsArrange = true;
        if (desiredChanged)
        {
            NeedsPaint = true;
        }

        return true;
    }

    private bool TryFindMeasureCacheEntry(float availableWidth, float availableHeight, out MeasureCacheEntry cacheEntry)
    {
        var normalizedConstraints = NormalizeMeasureConstraints(availableWidth, availableHeight);
        for (int i = 0; i < _measureCacheCount; i++)
        {
            if (_measureCacheEntries[i].AvailableWidth == normalizedConstraints.Width &&
                _measureCacheEntries[i].AvailableHeight == normalizedConstraints.Height)
            {
                cacheEntry = _measureCacheEntries[i];
                return true;
            }
        }

        cacheEntry = default;
        return false;
    }

    private void StoreMeasureCache(float availableWidth, float availableHeight, float desiredWidth, float desiredHeight)
    {
        var normalizedConstraints = NormalizeMeasureConstraints(availableWidth, availableHeight);
        var cacheEntry = new MeasureCacheEntry(normalizedConstraints.Width, normalizedConstraints.Height, desiredWidth, desiredHeight);

        for (int i = 0; i < _measureCacheCount; i++)
        {
            if (_measureCacheEntries[i].AvailableWidth == normalizedConstraints.Width &&
                _measureCacheEntries[i].AvailableHeight == normalizedConstraints.Height)
            {
                _measureCacheEntries[i] = cacheEntry;
                return;
            }
        }

        if (_measureCacheCount < MeasureCacheCapacity)
        {
            _measureCacheEntries[_measureCacheCount++] = cacheEntry;
            return;
        }

        _measureCacheEntries[_measureCacheNextIndex] = cacheEntry;
        _measureCacheNextIndex = (_measureCacheNextIndex + 1) % MeasureCacheCapacity;
    }

    private void ClearMeasureCache()
    {
        _measureCacheCount = 0;
        _measureCacheNextIndex = 0;
    }

    private (float Width, float Height) NormalizeMeasureConstraints(float availableWidth, float availableHeight)
    {
        var constrainedAvailable = ApplyMeasureConstraints(availableWidth, availableHeight);
        var explicitSize = ApplySizeConstraints(Width, Height);
        float normalizedWidth = HasExplicitWidth ? explicitSize.Width : constrainedAvailable.Width;
        float normalizedHeight = HasExplicitHeight ? explicitSize.Height : constrainedAvailable.Height;
        return (QuantizeMeasureConstraint(normalizedWidth), QuantizeMeasureConstraint(normalizedHeight));
    }

    private (float Width, float Height) ApplyMeasureConstraints(float width, float height)
    {
        return (
            ApplyMeasureConstraint(width, GetEffectiveMinWidth(), GetEffectiveMaxWidth()),
            ApplyMeasureConstraint(height, GetEffectiveMinHeight(), GetEffectiveMaxHeight()));
    }

    private (float Width, float Height) ApplySizeConstraints(float width, float height)
    {
        return (
            ApplySizeConstraint(width, GetEffectiveMinWidth(), GetEffectiveMaxWidth()),
            ApplySizeConstraint(height, GetEffectiveMinHeight(), GetEffectiveMaxHeight()));
    }

    private void ApplyDesiredSizeConstraints()
    {
        var constrainedSize = ApplySizeConstraints(DesiredWidth, DesiredHeight);
        DesiredWidth = constrainedSize.Width;
        DesiredHeight = constrainedSize.Height;
    }

    private float GetEffectiveMinWidth() => SanitizeMinSize(MinWidth);

    private float GetEffectiveMinHeight() => SanitizeMinSize(MinHeight);

    private float GetEffectiveMaxWidth() => SanitizeMaxSize(MaxWidth, GetEffectiveMinWidth());

    private float GetEffectiveMaxHeight() => SanitizeMaxSize(MaxHeight, GetEffectiveMinHeight());

    private static float SanitizeMinSize(float value)
    {
        if (float.IsNaN(value))
            return 0f;

        return MathF.Max(0f, value);
    }

    private static float SanitizeMaxSize(float value, float minValue)
    {
        if (float.IsNaN(value))
            return minValue;

        return MathF.Max(minValue, value);
    }

    private static float ApplyMeasureConstraint(float value, float minValue, float maxValue)
    {
        if (float.IsNaN(value))
            return 0f;

        if (float.IsPositiveInfinity(value))
            return maxValue;

        return ApplySizeConstraint(value, minValue, maxValue);
    }

    private static float ApplySizeConstraint(float value, float minValue, float maxValue)
    {
        if (float.IsNaN(value))
            value = 0f;

        return MathF.Max(minValue, MathF.Min(value, maxValue));
    }

    private static float QuantizeMeasureConstraint(float value)
    {
        if (!float.IsFinite(value))
        {
            return value;
        }

        return MathF.Round(value / MeasureCacheQuantizationStep) * MeasureCacheQuantizationStep;
    }

    protected virtual void Arrange(float x, float y, float width, float height)
    {
        ComputedX = x;
        ComputedY = y;
        ComputedWidth = width;
        ComputedHeight = height;
    }

    public abstract void Render(IRenderer renderer);
    #endregion

    private readonly record struct MeasureCacheEntry(float AvailableWidth, float AvailableHeight, float DesiredWidth, float DesiredHeight);

    #region Lifecycle Hooks
    protected virtual void OnMounted() { }
    protected virtual void OnUnmounted() { }
    protected virtual void OnMeasured(float desiredWidth, float desiredHeight) { }
    protected virtual void OnArranged(float x, float y, float width, float height) { }
    protected virtual void OnBeforeRender(IRenderer renderer) { }
    protected virtual void OnAfterRender(IRenderer renderer) { }
    protected virtual void OnVisible() { }
    protected virtual void OnInvisible() { }

    internal void NotifyMounted()
    {
        ApplyTheme(
            ScopedTheme ??
            _detachedTheme ??
            Parent?.EffectiveTheme ??
            UIApplication.Current?.ActiveTheme ??
            UIApplication.FallbackTheme);
        OnMounted();
        // Use ToArray to avoid collection modification during iteration
        foreach (var child in GetChildren().ToArray())
        {
            child.NotifyMounted();
        }
    }

    internal void NotifyUnmounted()
    {
        OnUnmounted();
        // Use ToArray to avoid collection modification during iteration
        foreach (var child in GetChildren().ToArray())
        {
            child.NotifyUnmounted();
        }
    }

    internal void InvokeOnBeforeRender(IRenderer renderer)
    {
        OnBeforeRender(renderer);
    }

    internal void InvokeOnAfterRender(IRenderer renderer)
    {
        OnAfterRender(renderer);
    }
    #endregion

    #region Ref
    public VisualElement Ref<T>(out T? element) where T : VisualElement
    {
        element = this as T;
        return this;
    }

    public VisualElement Ref<T>(Action<T> refAction) where T : VisualElement
    {
        if (this is T typedThis)
        {
            refAction(typedThis);
        }
        return this;
    }
    #endregion

    #region Size (Width, Height) Fluent API
    public virtual VisualElement Size(Size size)
    {
        Width = size.Width;
        Height = size.Height;
        return this;
    }

    /// <summary>
    /// Sets both width and height to the same value (for square elements).
    /// </summary>
    public virtual VisualElement Size(float size)
    {
        Width = size;
        Height = size;
        return this; ;
    }

    /// <summary>
    /// Sets width and height separately.
    /// </summary>
    public virtual VisualElement Size(float width, float height)
    {
        Width = width;
        Height = height;
        return this;
    }

    public virtual VisualElement Position(float x, float y)
    {
        X = x;
        Y = y;
        return this;
    }

    public virtual VisualElement Rotate(float degrees)
    {
        Rotation = degrees;
        return this;
    }

    public virtual VisualElement Translate(float x, float y)
    {
        TranslationX = x;
        TranslationY = y;
        return this;
    }

    #endregion
}


public abstract partial class VisualElement<T> : VisualElement where T : VisualElement<T>
{
    #region Ref

    public T Ref(out T reference)
    {
        base.Ref<T>(out var typedReference);
        reference = typedReference ?? (T)this;
        return (T)this;
    }

    public T Ref(Action<T> refAction)
    {
        return (T)base.Ref(refAction);
    }
    #endregion

    #region Size (Width, Height) API
    public new virtual T Size(Size size)
    {
        base.Size(size);
        return (T)this;
    }

    /// <summary>
    /// Sets both width and height to the same value (for square elements).
    /// </summary>
    public new virtual T Size(float size)
    {
        base.Size(size);
        return (T)this; ;
    }

    /// <summary>
    /// Sets width and height separately.
    /// </summary>
    public new virtual T Size(float width, float height)
    {
        base.Size(width, height);
        return (T)this;
    }

    public new virtual T Position(float x, float y)
    {
        base.Position(x, y);
        return (T)this;
    }

    public new virtual T Rotate(float degrees)
    {
        base.Rotate(degrees);
        return (T)this;
    }

    public new virtual T Translate(float x, float y)
    {
        base.Translate(x, y);
        return (T)this;
    }
    #endregion
}
