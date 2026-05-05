namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Animation;


public enum DrawerPosition

{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// Drawer - A slide-out Frame that appears from the edge of the screen with animation.
/// </summary>
public class Drawer : Rayo.Core.CompositeView<Drawer>, IFrameAnimation
{
    private bool _isOpen = false;

    private bool _isAnimating = false;
    private DrawerOverlay? _overlay;
    private VisualElement? _content;

    private float _animationProgress = 0f;
    private float _animationStartValue = 0f;
    private float _animationTarget = 0f;
    private float _animationElapsedTime = 0f;

    private static Drawer? _currentlyOpenDrawer;



    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region Position
    public new DrawerPosition Position
    {
        get => field;
        set => this.SetProperty(ref field, value, OnLayoutAffectingPropertyChanged);
    }
    #endregion

    #region DrawerWidth
    public float DrawerWidth
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value, OnLayoutAffectingPropertyChanged))
            {
                DrawerWidthChanged?.Invoke();
            }
        }
    }
    #endregion

    #region DrawerHeight
    public float DrawerHeight
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value, OnLayoutAffectingPropertyChanged))
            {
                DrawerHeightChanged?.Invoke();
            }
        }
    }
    #endregion

    #region ShowOverlay
    public bool ShowOverlay
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value, MarkOverlayNeedsPaint))
            {
                ShowOverlayChanged?.Invoke();
            }
        }
    }
    #endregion

    #region CloseOnOverlayClick
    public bool CloseOnOverlayClick
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region AnimationDuration
    public float AnimationDuration
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value, () => { if (value < 0f) AnimationDuration = 0f; }))
            {
                AnimationDurationChanged?.Invoke(value);
            }
        }
    }
    #endregion

    #region OverlayColor
    public Brush OverlayColor
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value, MarkOverlayNeedsPaint))
            {
                OverlayColorChanged?.Invoke();
            }
        }
    }
    #endregion

    #region BorderColor
    [PaintProperty]
    public Brush BorderColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region BorderWidth
    [LayoutProperty]
    public float BorderWidth
    {
        get => field;
        set => this.SetProperty(ref field, value);
    }
    #endregion

    #region CornerRadius
    public CornerRadius CornerRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, OnLayoutAffectingPropertyChanged);
    }
    #endregion

    #region IsOpen
    [NotFluent]
    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                IsOpenChanged?.Invoke(_isOpen);
                if (_isOpen)
                    Opened?.Invoke();
                else
                    Closed?.Invoke();
            }
        }
    } 
    #endregion

    // =========================================================================
    // EVENTS
    // =========================================================================
    public event Action? Opened;
    public event Action? Closed;
    public event Action<bool>? IsOpenChanged;

#pragma warning disable CS0067
    public event Action? OverlayColorChanged;
    public event Action<float>? AnimationDurationChanged;
    public event Action? ShowOverlayChanged;
    public event Action? DrawerHeightChanged;
    public event Action? DrawerWidthChanged;
    public event Action? PositionChanged;
#pragma warning restore CS0067



    // =========================================================================
    // INTIAliZATION
    // =========================================================================
    public Drawer()
    {
        Position = DrawerPosition.Left;
        DrawerWidth = 280f;
        DrawerHeight = 400f;
        ShowOverlay = true;
        CloseOnOverlayClick = true;
        AnimationDuration = 0.25f;

        Background = new Color(30, 32, 40);
        OverlayColor = new Color(0, 0, 0, 0.5f);
        BorderColor = new Color(60, 65, 80);
        BorderWidth = 1;
        CornerRadius = new CornerRadius(0);
        Width = 0;
        Height = 0;

        PositionChanged += OnLayoutAffectingPropertyChanged;
        DrawerWidthChanged += OnLayoutAffectingPropertyChanged;
        DrawerHeightChanged += OnLayoutAffectingPropertyChanged;
        ShowOverlayChanged += MarkOverlayNeedsPaint;
        OverlayColorChanged += MarkOverlayNeedsPaint;
        AnimationDurationChanged += newValue =>
        {
            if (newValue < 0f)
            {
                AnimationDuration = 0f;
            }
        };
        ClipToBounds = true;
    }

    public Drawer DrawerSize(float width, float height)
    {
        DrawerWidth = width;
        DrawerHeight = height;
        return this;
    }

    public Drawer Content(VisualElement content)
    {
        _content = content;
        return this;
    }


    public void Open()
    {
        if (_isOpen) return;

        // Close any previously open drawer
        if (_currentlyOpenDrawer != null && _currentlyOpenDrawer != this)
        {
            _currentlyOpenDrawer.Close();
        }

        IsOpen = true;
        _currentlyOpenDrawer = this;

        // Create and show overlay
        _overlay = new DrawerOverlay(this);

        // Use UIApplication if available, otherwise use UITree (for Android/iOS)
        var app = UIApplication.Current;
        if (app != null)
        {
            app.AddOverlay(_overlay);
        }
        else
        {
            // Find the UITree from the element hierarchy
            var tree = FindUITree();
            tree?.AddOverlay(_overlay);
        }

        // Start open animation
        StartAnimation(1f);
    }

    /// <summary>
    /// Finds the UITree by traversing up the parent hierarchy.
    /// </summary>
    private UITree? FindUITree()
    {
        // Walk up to find the root element and get its tree
        var current = this as VisualElement;
        while (current?.Parent != null)
        {
            current = current.Parent;
        }

        // The root element should be managed by a UITree
        // We can access it through the EventManager attached to the tree
        // For now, use a static reference that Android sets
        return _currentUITree;
    }

    // Static reference to UITree for platforms without UIApplication
    private static UITree? _currentUITree;

    private void RequestTreeLayoutForHeadlessPlatforms()
    {
        if (UIApplication.Current != null)
        {
            return;
        }

        var tree = _currentUITree ?? FindUITree();
        tree?.MarkNeedsLayout();
    }

    /// <summary>
    /// Sets the UITree reference for platforms without UIApplication (Android/iOS).
    /// Should be called during initialization.
    /// </summary>
    public static void UITree(UITree tree)
    {
        _currentUITree = tree;
    }

    public void Close()
    {
        if (!_isOpen) return;

        // Start close animation
        StartAnimation(0f);
    }

    private void StartAnimation(float target)
    {
        if (_isAnimating)
        {
            FrameAnimationTicker.Unregister(this);
        }

        _animationTarget = target;
        _animationStartValue = _animationProgress;
        _animationElapsedTime = 0f;
        _isAnimating = true;

        FrameAnimationTicker.Register(this);

        // Enable continuous rendering during animation
        var app = UIApplication.Current;
        if (app != null)
        {
            app.ContinuousRendering = true;
        }
        else
        {
            RequestTreeLayoutForHeadlessPlatforms();
        }
    }

    void IFrameAnimation.Tick(float deltaTime)
    {
        if (!_isAnimating) return;

        _animationElapsedTime += deltaTime;
        float t = AnimationDuration > 0 ? Math.Min(1f, _animationElapsedTime / AnimationDuration) : 1f;

        // Ease out cubic for smooth deceleration (applied to both opening and closing)
        float progressFactor = 1f - MathF.Pow(1f - t, 3f);

        _animationProgress = _animationStartValue + (_animationTarget - _animationStartValue) * progressFactor;

        if (_animationElapsedTime >= AnimationDuration)
        {
            _animationProgress = _animationTarget;
            _isAnimating = false;
            FrameAnimationTicker.Unregister(this);

            // Restore rendering mode
            var app = UIApplication.Current;
            if (app != null)
            {
                app.ContinuousRendering = false;
            }

            // If closing animation completed, remove overlay
            if (_animationTarget == 0f)
            {
                CompleteClose();
            }
        }

        _overlay?.MarkNeedsLayout();

        if (UIApplication.Current == null)
        {
            RequestTreeLayoutForHeadlessPlatforms();
            var tree = _currentUITree ?? FindUITree();
            tree?.MarkNeedsRender();
        }
    }

    private void CompleteClose()

    {
        if (_overlay != null)
        {
            // Use UIApplication if available, otherwise use UITree (for Android/iOS)
            var app = UIApplication.Current;
            if (app != null)
            {
                app.RemoveOverlay(_overlay);
            }
            else
            {
                var tree = FindUITree();
                tree?.RemoveOverlay(_overlay);
            }
            _overlay = null;
        }

        IsOpen = false;
        _animationProgress = 0f;

        if (_currentlyOpenDrawer == this)
        {
            _currentlyOpenDrawer = null;
        }
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    private void OnLayoutAffectingPropertyChanged()
    {
        MarkNeedsLayout();
        _overlay?.MarkNeedsLayout();
    }

    private void MarkOverlayNeedsPaint()
    {
        _overlay?.MarkNeedsPaint();
    }

    internal float GetAnimationProgress() => _animationProgress;
    internal bool IsAnimating => _isAnimating;
    internal VisualElement? GetContent() => _content;
    internal CornerRadius GetDrawerCornerRadius()
    {
        if (CornerRadius.TopLeft > 0 || CornerRadius.TopRight > 0 ||
            CornerRadius.BottomLeft > 0 || CornerRadius.BottomRight > 0)
        {
            return CornerRadius;
        }

        // Default corner radius based on position
        float r = 12;
        return Position switch
        {
            DrawerPosition.Left => new CornerRadius(0, r, r, 0),
            DrawerPosition.Right => new CornerRadius(r, 0, 0, r),
            DrawerPosition.Top => new CornerRadius(0, 0, r, r),
            DrawerPosition.Bottom => new CornerRadius(r, r, 0, 0),
            _ => new CornerRadius(0)
        };
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = 0;
        DesiredHeight = 0;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, 0, 0);
    }

    public override void Render(IRenderer renderer)
    {
        // Drawer renders as overlay, nothing to render here
    }

    /// <summary>
    /// Closes the currently open drawer if any
    /// </summary>
    public static void CloseCurrentDrawer()
    {
        _currentlyOpenDrawer?.Close();
    }
}

/// <summary>
/// Internal overlay that renders the drawer with animation.
/// Uses IPointerHandler for pointer events.
/// </summary>
internal class DrawerOverlay : Rayo.Core.CompositeView<DrawerOverlay>, Rayo.Core.Input.IPointerHandler
{
    private readonly Drawer _drawer;
    private Frame? _drawerFrame;
    private VStack? _contentContainer;

    // Store last touch/click position for Android/iOS support
    private System.Numerics.Vector2 _lastPointerPosition;

    public DrawerOverlay(Drawer drawer)
    {
        _drawer = drawer;
        BuildDrawerFrame();
    }

    private void BuildDrawerFrame()
    {
        _contentContainer = new VStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var content = _drawer.GetContent();
        if (content != null)
        {
            // On re-open, content may still have its previous container as parent.
            // Remove it from the old VStack so it can be re-attached without throwing.
            if (content.Parent is VStack oldContainer)
                oldContainer.Remove(content);

            _contentContainer.AddChild(content);
        }

        _drawerFrame = new Frame()
            .Background(_drawer.Background)
            .BorderColor(_drawer.BorderColor.PrimaryColor)
            .BorderWidth(_drawer.BorderWidth);
        _drawerFrame.Content(_contentContainer);
        _drawerFrame.BorderRadius = _drawer.GetDrawerCornerRadius();
        _drawerFrame.ClipToBounds = true;

        AddChild(_drawerFrame);
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Calculate drawer dimensions
        float drawerW = _drawer.Position == DrawerPosition.Left || _drawer.Position == DrawerPosition.Right
            ? _drawer.DrawerWidth
            : availableWidth;

        float drawerH = _drawer.Position == DrawerPosition.Top || _drawer.Position == DrawerPosition.Bottom
            ? _drawer.DrawerHeight
            : availableHeight;

        _drawerFrame?.Measure(drawerW, drawerH);

        DesiredWidth = availableWidth;
        DesiredHeight = availableHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_drawerFrame == null) return;

        ArrangeDrawerFrame(x, y, width, height, _drawer.GetAnimationProgress());
    }


    private void ArrangeDrawerFrame(float x, float y, float width, float height, float progress)
    {
        if (_drawerFrame == null) return;

        float drawerW = _drawer.Position == DrawerPosition.Left || _drawer.Position == DrawerPosition.Right
            ? _drawer.DrawerWidth
            : width;

        float drawerH = _drawer.Position == DrawerPosition.Top || _drawer.Position == DrawerPosition.Bottom
            ? _drawer.DrawerHeight
            : height;

        float drawerX = 0, drawerY = 0;
        switch (_drawer.Position)
        {
            case DrawerPosition.Left:
                drawerX = -drawerW + (drawerW * progress);
                drawerY = 0;
                break;

            case DrawerPosition.Right:
                drawerX = width - (drawerW * progress);
                drawerY = 0;
                break;

            case DrawerPosition.Top:
                drawerX = 0;
                drawerY = -drawerH + (drawerH * progress);
                break;

            case DrawerPosition.Bottom:
                drawerX = 0;
                drawerY = height - (drawerH * progress);
                break;
        }

        _drawerFrame.Arrange(x + drawerX, y + drawerY, drawerW, drawerH);
    }

    public override void Render(IRenderer renderer)
    {
        float progress = _drawer.GetAnimationProgress();

        // Render overlay background with animated opacity

        if (_drawer.ShowOverlay)
        {
            var overlayColor = _drawer.OverlayColor.PrimaryColor;
            var animatedOverlay = new Color(
                overlayColor.R,
                overlayColor.G,
                overlayColor.B,
                overlayColor.A * progress
            );
            renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, animatedOverlay);
        }

        // Render drawer Frame
        _drawerFrame?.Render(renderer);
    }

    // IPointerHandler implementation for touch and mouse support
    public void OnPointerEntered(Rayo.Core.Input.PointerEventArgs args) { }
    public void OnPointerExited(Rayo.Core.Input.PointerEventArgs args) { }
    public void OnPointerMoved(Rayo.Core.Input.PointerEventArgs args) { }

    public void OnPointerPressed(Rayo.Core.Input.PointerEventArgs args)
    {
        _lastPointerPosition = args.Position;
    }

    public void OnPointerReleased(Rayo.Core.Input.PointerEventArgs args)
    {
        _lastPointerPosition = args.Position;

        // Check if release is outside drawer Frame (for touch)
        if (_drawer.CloseOnOverlayClick && _drawerFrame != null)
        {
            bool insideDrawer = _lastPointerPosition.X >= _drawerFrame.ComputedX &&
                               _lastPointerPosition.X <= _drawerFrame.ComputedX + _drawerFrame.ComputedWidth &&
                               _lastPointerPosition.Y >= _drawerFrame.ComputedY &&
                               _lastPointerPosition.Y <= _drawerFrame.ComputedY + _drawerFrame.ComputedHeight;

            if (!insideDrawer)
            {
                _drawer.Close();
            }
        }
    }
}
