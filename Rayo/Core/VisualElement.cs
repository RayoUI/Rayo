using System.Reflection;
using System.Numerics;
using Rayo.Core.Interfaces; // Added for IInputTransparent
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

namespace Rayo.Core;

using CornerRadius = Rayo.CornerRadius;
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
    /// <summary>
    /// Static event fired when any element's children are added, removed, or cleared.
    /// Used by DevTools to detect structural changes in the UI tree.
    /// </summary>
    public static event Action<VisualElement>? TreeStructureChanged;

    /// <summary>
    /// Static event fired whenever any element's <see cref="Classes"/> property changes.
    /// <see cref="UserControl"/> subscribes to this to detect when a descendant's classes
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
    private static readonly Dictionary<Type, HashSet<string>> s_layoutProps = new();
    private static readonly Dictionary<Type, HashSet<string>> s_paintProps  = new();

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
        if (!s_layoutProps.TryGetValue(type, out var set))
            s_layoutProps[type] = set = new HashSet<string>();
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

        var type = GetType();
        if (s_layoutProps.TryGetValue(type, out var lp) && lp.Contains(propertyName))
            MarkNeedsLayout();
        else if (s_paintProps.TryGetValue(type, out var pp) && pp.Contains(propertyName))
            MarkNeedsPaint();
    }
    #endregion

    protected VisualElement()
    {

    }

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
    [LayoutProperty]
    public float X
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }

    [LayoutProperty]
    public float Y
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region Size (Width, Height, MinWidth, MinHeight, MaxWidth, MaxHeight)
    [LayoutProperty]
    public float Width
    {
        get => field;
        set => this.SetProperty(ref field, value, () => HasExplicitWidth = true);
    } = 0;

    [LayoutProperty]
    public float Height
    {
        get => field;
        set => this.SetProperty(ref field, value, () => HasExplicitHeight = true);
    } = 0;

    [LayoutProperty]
    public float MinWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    [LayoutProperty]
    public float MinHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 0;

    [LayoutProperty]
    public float MaxWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = float.PositiveInfinity;

    [LayoutProperty]
    public float MaxHeight
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = float.PositiveInfinity;
    #endregion

    #region Margin
    [LayoutProperty]
    public Thickness Margin
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Thickness();
    #endregion

    #region Padding
    [LayoutProperty]
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

    #region BorderRadius
    [PaintProperty]
    public CornerRadius BorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = CornerRadius.None;
    #endregion

    #region Alignment
    [LayoutProperty]
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

    [LayoutProperty]
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

    [LayoutProperty]
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
    internal bool NeedsLayout { get; set; } = true;

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

        foreach (var prop in props)
        {
            try { _styleBaseline[prop.Name] = prop.GetValue(this); }
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
            if (_styleBaseline.TryGetValue(prop.Name, out var value))
            {
                try { prop.SetValue(this, value); }
                catch { /* skip read-only or incompatible properties */ }
            }
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
        if (!NeedsLayout)
        {
            NeedsLayout = true;
            NeedsPaint = true;

            // Record in performance tracker (dirty heatmap + dirty log).
            Rayo.DevTools.PerformanceTracker.RecordLayoutDirty(this);

            if (Parent != null && !Parent.NeedsLayout)
            {
                Parent.MarkNeedsLayout();
            }
        }

        // Always notify UITree to trigger layout AND render (works on Desktop and Android)
        (UIApplication.Current?.Tree ?? UITree.Current)?.MarkNeedsLayout();
    }

    public void MarkNeedsPaint()
    {
        if (!NeedsPaint)
        {
            NeedsPaint = true;

            // Record in performance tracker (dirty heatmap + dirty log).
            Rayo.DevTools.PerformanceTracker.RecordPaintDirty(this);

            if (Parent != null && !Parent.NeedsPaint)
            {
                Parent.MarkNeedsPaint();
            }
        }

        // Always notify UITree to trigger a render (works on Desktop and Android)
        (UIApplication.Current?.Tree ?? UITree.Current)?.MarkNeedsRender();
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
    internal IEnumerable<VisualElement> GetChildrenByZIndex()
    {
        var children = GetChildren();
        // Avoid LINQ overhead when no child has a non-default ZIndex
        if (!children.Any(c => c.ZIndex != 0))
            return children;

        return children
            .Select((child, index) => (child, index))
            .OrderBy(t => t.child.ZIndex)
            .ThenBy(t => t.index)
            .Select(t => t.child);
    }

    /// <summary>
    /// If true, the element renders its children manually (used for clipping, etc).
    /// UITree will not render children automatically in this case.
    /// </summary>
    protected internal virtual bool RendersChildrenManually => false;

    public virtual void Measure(float availableWidth, float availableHeight)
    {
        // Use ToArray to avoid collection modification during iteration
        foreach (var child in GetChildren().ToArray())
        {
            child.Measure(availableWidth, availableHeight);
        }

        OnMeasured(DesiredWidth, DesiredHeight);
    }

    public virtual void Arrange(float x, float y, float width, float height)
    {
        ComputedX = x;
        ComputedY = y;
        ComputedWidth = width;
        ComputedHeight = height;

        OnArranged(x, y, width, height);
    }

    public abstract void Render(IRenderer renderer);
    #endregion

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
