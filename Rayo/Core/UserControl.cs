using Rayo.Controls;
using Rayo.Core.Interfaces;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Styling;

namespace Rayo.Core;

/// <summary>
/// Base class for creating reusable UI components with their own state and lifecycle.
/// Similar to Flutter's StatelessWidget/StatefulWidget or React's Class Components.
/// </summary>
public abstract class UserControl : ContentView<UserControl>, IUIBuilder, IReactiveOwner
{
    private bool _isBuilt = false;
    private bool _hasInitialized = false;
    private bool _dependenciesInjected = false;
    private StyleSheet? _componentStyles;

    /// <summary>
    /// Builds the UI tree for this component.
    /// Hooks (UseSignal, UseComputed, UseEffect) can be used safely within this method.
    /// </summary>
    public abstract VisualElement Build();

    // ===== COMPONENT LIFECYCLE HOOKS =====

    /// <summary>
    /// Called once when component is first mounted to the tree.
    /// Similar to React's componentDidMount or Flutter's initState.
    /// Use for initialization that should only happen once.
    /// </summary>
    protected virtual void OnInit() { }

    /// <summary>
    /// Called when component is about to be removed from tree.
    /// Similar to React's componentWillUnmount or Flutter's dispose.
    /// Use for cleanup (timers, subscriptions, event handlers, etc.).
    /// </summary>
    protected virtual void OnDispose() { }

    /// <summary>
    /// Called before Build() executes.
    /// Use for setup or state preparation before building the UI.
    /// </summary>
    protected virtual void OnBeforeBuild() { }

    /// <summary>
    /// Called after Build() completes successfully.
    /// Use for logic that depends on the built element tree.
    /// </summary>
    protected virtual void OnAfterBuild(VisualElement builtElement) { }

    /// <summary>
    /// Returns a <see cref="StyleSheet"/> of style rules scoped to this component's subtree.
    /// Called once after <see cref="Build"/> completes. Component styles are applied after
    /// global styles, so they take priority over application-level theme rules.
    ///
    /// Usage:
    /// <code>
    /// protected override StyleSheet? BuildStyles() =>
    /// [
    ///     new Style&lt;Button&gt;().Margin(6).Background(Brushes.DarkSalmon),
    /// ];
    /// </code>
    /// </summary>
    protected virtual StyleSheet? BuildStyles() => null;

    /// <summary>
    /// Controls how far the style sheets applied to this component's subtree cascade.
    /// <para><see cref="StyleScope.Global"/> (default): styles penetrate into all nested
    /// <see cref="UserControl"/> children — equivalent to normal CSS cascading.</para>
    /// <para><see cref="StyleScope.Local"/>: styles stop at the boundary of nested
    /// <see cref="UserControl"/> children — equivalent to CSS Shadow DOM scoping.</para>
    /// </summary>
    protected virtual StyleScope StyleScope => StyleScope.Global;

    /// <summary>
    /// Helper to render the component directly.
    /// In a real framework, the framework would call Build().
    /// Here we call it manually to get the element tree.
    /// </summary>
    public VisualElement Render()
    {
        // For backward compatibility or manual usage
        return this;
    }

    /// <summary>
    /// Forces the component to rebuild its UI tree.
    /// Useful when state has been reset externally or to force a refresh.
    /// </summary>
    public void Rebuild()
    {
        ClearContent();
        _isBuilt = false;
        _componentStyles = null;
        MarkNeedsLayout();
    }

    private void EnsureBuilt()
    {
        if (!_isBuilt)
        {
            // Inject dependencies BEFORE build so services are available in Build()
            if (!_dependenciesInjected)
            {
                InjectDependencies();
                _dependenciesInjected = true;
            }

            // Call lifecycle hook before build
            OnBeforeBuild();

            // Initialize hooks context for this component instance
            Hooks.Begin(this);
            var content = Build();

            // Set the built content
            Content = content;

            _isBuilt = true;

            // Call lifecycle hook after build
            OnAfterBuild(content);

            // Apply styles: global first, then component (component wins on conflict)
            var globalStyles = UIApplication.Current?.GlobalStyles;
            if (globalStyles != null)
                StyleEngine.Apply(globalStyles, content, StyleScope);

            var componentStyles = GetComponentStyles();
            if (componentStyles != null)
                StyleEngine.Apply(componentStyles, content, StyleScope);

            // Subscribe to global style changes for hot-reload re-application
            UIApplication.GlobalStylesChanged -= OnGlobalStylesChanged;
            UIApplication.GlobalStylesChanged += OnGlobalStylesChanged;

            RefreshStyleSubscriptions(globalStyles, componentStyles);
        }
    }

    protected override void OnMounted()
    {
        base.OnMounted();

        // Call OnInit only once when first mounted
        if (!_hasInitialized)
        {
            OnInit();
            _hasInitialized = true;
        }
    }

    /// <summary>
    /// Injects dependencies into properties marked with [Inject] attribute.
    /// </summary>
    private void InjectDependencies()
    {
        DependencyInjector.Inject(this);
    }

    /// <summary>
    /// Re-applies both global and component styles to the built content.
    /// Called by DevToolAgent when element Classes change so class-selector rules take effect.
    /// </summary>
    internal void ReapplyStyles()
    {
        if (Content == null) return;
        ApplyStylePipeline();
        MarkNeedsPaint();
    }

    private void OnGlobalStylesChanged(StyleSheet newStyles)
    {
        if (Content == null) return;

        StyleEngine.Apply(newStyles, Content, StyleScope);
        RefreshStyleSubscriptions(newStyles, _componentStyles);
        MarkNeedsPaint();
    }

    private void OnBreakpointChanged(Breakpoint _)
    {
        if (Content == null) return;

        // Re-apply the full style pipeline so breakpoint-conditional setters
        // are evaluated against the new window size. Same path as EnsureBuilt().
        ApplyStylePipeline();
        MarkNeedsPaint();
    }

    private void OnWindowResized(float _)
    {
        if (Content == null) return;

        // Re-apply styles so custom min/max-width conditions are evaluated
        // against the new window width.
        ApplyStylePipeline();
        MarkNeedsPaint();
    }

    private void OnColorSchemeChanged(ColorScheme _)
    {
        if (Content == null) return;

        ApplyStylePipeline();
        MarkNeedsPaint();
    }

    private void OnOrientationChanged(Rayo.Styling.Orientation _)
    {
        if (Content == null) return;

        ApplyStylePipeline();
        MarkNeedsPaint();
    }

    private void OnThemeChanged(Theme _)
    {
        if (Content == null) return;

        ApplyStylePipeline();
        MarkNeedsPaint();
    }

    private void ApplyStylePipeline()
    {
        if (Content == null) return;

        var globalStyles = UIApplication.Current?.GlobalStyles;
        if (globalStyles != null)
            StyleEngine.Apply(globalStyles, Content, StyleScope);

        var componentStyles = GetComponentStyles();
        if (componentStyles != null)
            StyleEngine.Apply(componentStyles, Content, StyleScope);

        RefreshStyleSubscriptions(globalStyles, componentStyles);
    }

    private void OnClassesChanged(VisualElement element)
    {
        if (Content == null) return;

        // Walk up from the changed element to check if it belongs to our content subtree.
        var current = (VisualElement?)element;
        while (current != null)
        {
            if (current == Content)
            {
                ApplyStylesToElement(element);
                return;
            }
            current = current.Parent;
        }
    }

    private void ApplyStylesToElement(VisualElement element)
    {
        var globalStyles = UIApplication.Current?.GlobalStyles;
        if (globalStyles != null)
            StyleEngine.ApplyToElement(globalStyles, element);

        var componentStyles = GetComponentStyles();
        if (componentStyles != null)
            StyleEngine.ApplyToElement(componentStyles, element);

        element.MarkNeedsPaint();
    }

    private StyleSheet? GetComponentStyles()
    {
        _componentStyles ??= BuildStyles();
        return _componentStyles;
    }

    private void RefreshStyleSubscriptions(StyleSheet? globalStyles, StyleSheet? componentStyles)
    {
        BreakpointHelper.BreakpointChanged -= OnBreakpointChanged;
        if (HasBreakpointStyles(globalStyles, componentStyles))
            BreakpointHelper.BreakpointChanged += OnBreakpointChanged;

        BreakpointHelper.WindowResized -= OnWindowResized;
        if (HasCustomBreakpointStyles(globalStyles, componentStyles))
            BreakpointHelper.WindowResized += OnWindowResized;

        ColorSchemeHelper.ColorSchemeChanged -= OnColorSchemeChanged;
        if (HasColorSchemeStyles(globalStyles, componentStyles))
            ColorSchemeHelper.ColorSchemeChanged += OnColorSchemeChanged;

        OrientationHelper.OrientationChanged -= OnOrientationChanged;
        if (HasOrientationStyles(globalStyles, componentStyles))
            OrientationHelper.OrientationChanged += OnOrientationChanged;

        UIApplication.ThemeChanged -= OnThemeChanged;
        if (HasThemeAwareStyles(globalStyles, componentStyles))
            UIApplication.ThemeChanged += OnThemeChanged;

        VisualElement.ClassesChanged -= OnClassesChanged;
        if (HasClassStyles(globalStyles, componentStyles))
            VisualElement.ClassesChanged += OnClassesChanged;
    }

    private static bool HasBreakpointStyles(StyleSheet? global, StyleSheet? component)
    {
        if (global != null)
            foreach (var rule in global)
                if (rule.HasBreakpointConditions) return true;

        if (component != null)
            foreach (var rule in component)
                if (rule.HasBreakpointConditions) return true;

        return false;
    }

    private static bool HasCustomBreakpointStyles(StyleSheet? global, StyleSheet? component)
    {
        if (global != null)
            foreach (var rule in global)
                if (rule.HasCustomBreakpoints) return true;

        if (component != null)
            foreach (var rule in component)
                if (rule.HasCustomBreakpoints) return true;

        return false;
    }

    private static bool HasColorSchemeStyles(StyleSheet? global, StyleSheet? component)
    {
        if (global != null)
            foreach (var rule in global)
                if (rule.HasColorSchemeConditions) return true;

        if (component != null)
            foreach (var rule in component)
                if (rule.HasColorSchemeConditions) return true;

        return false;
    }

    private static bool HasOrientationStyles(StyleSheet? global, StyleSheet? component)
    {
        if (global != null)
            foreach (var rule in global)
                if (rule.HasOrientationConditions) return true;

        if (component != null)
            foreach (var rule in component)
                if (rule.HasOrientationConditions) return true;

        return false;
    }

    private static bool HasClassStyles(StyleSheet? global, StyleSheet? component)
    {
        if (global != null)
            foreach (var rule in global)
                if (rule.HasClassConditions) return true;

        if (component != null)
            foreach (var rule in component)
                if (rule.HasClassConditions) return true;

        return false;
    }

    private static bool HasThemeAwareStyles(StyleSheet? global, StyleSheet? component)
        => (global != null && global.Count > 0) || (component != null && component.Count > 0);

    protected override void OnUnmounted()
    {
        base.OnUnmounted();
        UIApplication.GlobalStylesChanged -= OnGlobalStylesChanged;
        UIApplication.ThemeChanged -= OnThemeChanged;
        BreakpointHelper.BreakpointChanged -= OnBreakpointChanged;
        BreakpointHelper.WindowResized -= OnWindowResized;
        ColorSchemeHelper.ColorSchemeChanged -= OnColorSchemeChanged;
        OrientationHelper.OrientationChanged -= OnOrientationChanged;
        VisualElement.ClassesChanged -= OnClassesChanged;
        OnDispose();
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        EnsureBuilt();

        // Measure content
        if (Content != null)
        {
            Content.Measure(availableWidth - Padding.Horizontal, availableHeight - Padding.Vertical);

            // If explicit size is set, use it. Otherwise use content size.
            DesiredWidth = Width > 0 ? Width : Content.DesiredWidth + Padding.Horizontal;
            DesiredHeight = Height > 0 ? Height : Content.DesiredHeight + Padding.Vertical;
        }
        else
        {
            DesiredWidth = Width > 0 ? Width : Padding.Horizontal;
            DesiredHeight = Height > 0 ? Height : Padding.Vertical;
        }

        OnMeasured(DesiredWidth, DesiredHeight);
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        EnsureBuilt();

        // Set our computed geometry
        ComputedX = x;
        ComputedY = y;
        ComputedWidth = width;
        ComputedHeight = height;

        // Arrange content to fill our space (minus padding)
        if (Content != null)
        {
            float contentX = x + Padding.Left;
            float contentY = y + Padding.Top;
            float contentWidth = width - Padding.Horizontal;
            float contentHeight = height - Padding.Vertical;

            Content.Arrange(contentX, contentY, contentWidth, contentHeight);
        }

        OnArranged(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        EnsureBuilt();

        // We don't render anything ourselves (transparent wrapper)
        // UITree will render our content automatically via GetChildren()
    }


    public T Use<T>(T disposable) where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(disposable);
        RegisterDisposable(disposable);
        return disposable;
    }

    public Signal<T> UseSignal<T>(T initialValue)
    {
        return new Signal<T>(initialValue);
    }

    public SignalList<T> UseSignalList<T>()
    {
        return new SignalList<T>();
    }

    public SignalList<T> UseSignalList<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new SignalList<T>(items);
    }

    public Computed<T> UseComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        return Use(new Computed<T>(compute));
    }

    public Effect UseEffect(Action effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return Use(new Effect(effect));
    }

    public IDisposable UseSubscription<T>(IReadableSignal<T> signal, Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(callback);
        return Use(signal.Subscribe(callback));
    }

    public IDisposable UseSubscription(ISignal signal, Action callback)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(callback);
        return Use(signal.Subscribe(callback));
    }

}
