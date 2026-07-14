namespace Rayo.Controls;

using Rayo.Animation;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using Rayo.Rendering.Graphics.VectorGraphics;
using System.Numerics;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Posicion de las tabs en el TabControl.
/// </summary>
public enum TabPosition
{
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// Controls which tab headers display their close button.
/// </summary>
public enum TabCloseButtonDisplayMode
{
    /// <summary>Every tab displays a close button.</summary>
    AllTabs,

    /// <summary>Only the currently selected tab displays a close button.</summary>
    ActiveTabOnly
}

/// <summary>
/// Representa un tab individual.
/// </summary>
public class TabItem
{
    public string Title { get; set; }

    public VisualElement Content { get; set; }

    /// <summary>
    /// When <see langword="false"/> the tab header is rendered in a disabled state
    /// and cannot be selected by the user.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public TabItem(string title, VisualElement content)
    {
        Title = title;
        Content = content;
    }
}

/// <summary>
/// Control de pestañas/tabs con soporte para drag & drop universal y navegación con scroll.
/// </summary>
public class TabControl : CompositeView<TabControl>, IFrameAnimation
{
    private const float HeaderSpacing = 2f;
    private const float AutoScrollZone = 48f;
    private const float AutoScrollStep = 8f;

    private List<TabItem> _tabs = [];
    private int _selectedIndex;

    private VisualElement _root = null!;
    private VisualElement _headerStrip = null!;
    private TabHeadersScrollView _headerScroll = null!;
    private OverlayPanel _headerOverlay = null!;
    private Grid _headerHost = null!;
    private Frame _contentFrame = null!;
    private TabScrollButton _scrollBackwardButton = null!;
    private TabScrollButton _scrollForwardButton = null!;

    private float _dragPointerX;
    private float _dragPointerY;
    private bool _autoScrollActive;
    private bool _autoScrollRegistered;
    private bool _isApplyingThemeStyle;

    #region TabBackground
    public Brush TabBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region TabActiveBackground
    public Brush TabActiveBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region TabHoverBackground
    public Brush TabHoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region TabCloseButtonColor
    public Color TabCloseButtonColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region TabCloseButtonHoverColor
    public Color TabCloseButtonHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region TabCloseButtonSize
    public float TabCloseButtonSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = 12f;
    #endregion

    #region TabCloseButtonHitSize
    public float TabCloseButtonHitSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = 20f;
    #endregion

    #region TabAccentColor
    public Color TabAccentColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region TabDropIndicatorColor
    public Color TabDropIndicatorColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RefreshHeadersForStyleChange);
    } = Color.Transparent;
    #endregion

    #region ContentBackground
    public Brush ContentBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_contentFrame != null)
                _contentFrame.Background = value;
            if (_root != null)
                _root.Background = value;
        });
    } = Color.Transparent;
    #endregion

    #region HeaderBackground
    public Brush HeaderBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_headerHost != null)
                _headerHost.Background = value;
        });
    } = Color.Transparent;
    #endregion

    #region HeaderPadding
    [LayoutProperty]
    public Thickness HeaderPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = new Thickness(0);
    #endregion

    #region HeaderContentSpacing
    [LayoutProperty]
    public float HeaderContentSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 0f;
    #endregion

    #region TabHeight
    [LayoutProperty]
    public float TabHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 30f;
    #endregion

    #region TabWidth
    [LayoutProperty]
    public float TabWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 120f;
    #endregion

    #region ScrollButtonWidth
    [LayoutProperty]
    public float ScrollButtonWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 28f;
    #endregion

    #region Position
    [LayoutProperty]
    public new TabPosition Position
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = TabPosition.Top;
    #endregion

    #region VerticalTabHeight
    [LayoutProperty]
    public float VerticalTabHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 0f;
    #endregion

    #region VerticalTabWidth
    [LayoutProperty]
    public float VerticalTabWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 0f;
    #endregion

    #region EnableTabReorder
    public bool EnableTabReorder
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = true;
    #endregion

    #region ShowTabCloseButtons
    [LayoutProperty]
    public bool ShowTabCloseButtons
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = false;
    #endregion

    #region CloseButtonDisplay
    /// <summary>
    /// Selects whether close buttons are shown on every tab or only on the
    /// active tab. <see cref="ShowTabCloseButtons"/> remains the master switch.
    /// </summary>
    [LayoutProperty]
    public TabCloseButtonDisplayMode CloseButtonDisplay
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = TabCloseButtonDisplayMode.AllTabs;
    #endregion

    #region SelectedIndex
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectIndex(value, raiseEvent: true);
    }
    #endregion

    #region SelectedTab
    [NotFluent]
    public TabItem? SelectedTab => IsValidIndex(_selectedIndex) ? _tabs[_selectedIndex] : null;
    #endregion

    #region TabCount
    [NotFluent]
    public int TabCount => _tabs.Count;
    #endregion

    #region Items
    public IList<TabItem> Items
    {
        get => _tabs;
        set => SetItems(value);
    }
    #endregion

    #region TabHeaderTemplate
    /// <summary>
    /// Optional factory that creates a fully custom header element for each tab.
    /// The wrapper automatically handles selection, close hit-testing and drag/drop reordering.
    /// </summary>
    [NotFluent]
    public Func<TabItem, int, bool, VisualElement>? TabHeaderTemplate
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    }
    #endregion

    #region Header slots
    /// <summary>
    /// Optional factory for fixed content before the scrollable tab strip.
    /// For vertical tab positions this slot is placed above the tabs.
    /// </summary>
    [NotFluent]
    public Func<VisualElement>? HeaderStartTemplate
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    }

    /// <summary>
    /// Optional factory for fixed content after the scrollable tab strip.
    /// For vertical tab positions this slot is placed below the tabs.
    /// </summary>
    [NotFluent]
    public Func<VisualElement>? HeaderEndTemplate
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    }
    #endregion

    public event Action<int>? TabChanged;

    public event Action<int, int>? TabReordered;

    /// <summary>
    /// Raised when the user presses a tab close button. When there are no
    /// subscribers the tab is removed immediately.
    /// </summary>
    public event Action<int>? TabCloseRequested;

    public TabControl()
    {
        InitializeTheme();
        CreateVisualTree();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        var palette = theme.Colors;
        _isApplyingThemeStyle = true;
        try
        {
            SetThemeValue(nameof(TabBackground), (Brush)palette.SurfaceHover, value => TabBackground = value);
            SetThemeValue(nameof(TabActiveBackground), (Brush)palette.Surface, value => TabActiveBackground = value);
            SetThemeValue(nameof(TabHoverBackground), (Brush)palette.SurfacePressed, value => TabHoverBackground = value);
            SetThemeValue(nameof(TabCloseButtonColor), palette.OnDisabled, value => TabCloseButtonColor = value);
            SetThemeValue(nameof(TabCloseButtonHoverColor), palette.OnSurface, value => TabCloseButtonHoverColor = value);
            SetThemeValue(nameof(TabAccentColor), palette.Primary, value => TabAccentColor = value);
            SetThemeValue(nameof(TabDropIndicatorColor), palette.Primary, value => TabDropIndicatorColor = value);
            SetThemeValue(nameof(ContentBackground), (Brush)palette.Surface, value => ContentBackground = value);
            SetThemeValue(nameof(HeaderBackground), (Brush)palette.SurfaceHover, value => HeaderBackground = value);
        }
        finally
        {
            _isApplyingThemeStyle = false;
        }

        RefreshHeaderTheme();
    }

    private void RefreshHeaderTheme()
    {
        if (_headerStrip == null)
            return;

        foreach (var header in GetHeaderChildren().OfType<TabHeaderHost>())
        {
            header.RefreshThemeStyle();
        }

        _scrollBackwardButton?.RefreshStyle();
        _scrollForwardButton?.RefreshStyle();
        _contentFrame?.MarkNeedsPaint();
        MarkNeedsPaint();
    }

    private void RefreshHeadersForStyleChange()
    {
        if (_headerOverlay != null)
            _headerOverlay.Background = TabBackground;

        if (!_isApplyingThemeStyle)
            RebuildHeaders();
    }

    /// <summary>
    /// Sets a custom factory function that builds the header element for each tab.
    /// </summary>
    public TabControl WithTabHeaderTemplate(Func<TabItem, int, bool, VisualElement> factory)
    {
        TabHeaderTemplate = factory;
        return this;
    }

    /// <summary>
    /// Sets a factory for fixed content before the scrollable tabs.
    /// </summary>
    public TabControl HeaderStart(Func<VisualElement> factory)
    {
        HeaderStartTemplate = factory;
        return this;
    }

    /// <summary>
    /// Sets a factory for fixed content after the scrollable tabs.
    /// </summary>
    public TabControl HeaderEnd(Func<VisualElement> factory)
    {
        HeaderEndTemplate = factory;
        return this;
    }

    /// <summary>
    /// Establishes a two-way reactive binding between <paramref name="binding"/> and the selected tab.
    /// </summary>
    public TabControl BindSelectedIndex(IWritableSignal<int> binding)
    {
        var subscription = binding.Subscribe(index =>
        {
            UIUpdateQueue.EnqueueUIUpdate(this, () => SelectedIndex = index);
        });

        RegisterDisposable(subscription);
        SelectedIndex = binding.Value;

        Action<int> changedHandler = index => binding.Value = index;
        TabChanged += changedHandler;
        RegisterDisposable(new ActionDisposable(() => TabChanged -= changedHandler));

        return this;
    }

    public TabControl AddTab(string title, VisualElement content)
    {
        return AddTab(new TabItem(title, content));
    }

    public TabControl AddTab(TabItem tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        _tabs.Add(tab);
        if (_tabs.Count == 1)
            _selectedIndex = 0;

        RefreshAfterItemsChanged(selectionChanged: _tabs.Count == 1, ensureVisible: true);
        return this;
    }

    public void RemoveTab(int index)
    {
        if (!IsValidIndex(index))
            return;

        bool selectionChanged = index == _selectedIndex;
        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            _selectedIndex = 0;
        }
        else if (selectionChanged)
        {
            _selectedIndex = Math.Min(index, _tabs.Count - 1);
        }
        else if (index < _selectedIndex)
        {
            _selectedIndex--;
            selectionChanged = true;
        }

        RefreshAfterItemsChanged(selectionChanged, ensureVisible: true);
    }

    private void RequestCloseTab(int index)
    {
        if (TabCloseRequested == null)
            RemoveTab(index);
        else
            TabCloseRequested.Invoke(index);
    }

    public void DebugHitTest(float x, float y)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"[TabControl.DebugHitTest] Point ({x}, {y})");

        foreach (var child in Children)
        {
            Console.WriteLine($"[TabControl.DebugHitTest] {child.GetType().Name} @ ({child.ComputedX}, {child.ComputedY}, {child.ComputedWidth}, {child.ComputedHeight})");
        }

        Console.WriteLine("========================================");
    }

    protected override void OnMounted()
    {
        base.OnMounted();
    }

    protected override void OnUnmounted()
    {
        StopAutoScroll();
        base.OnUnmounted();
    }

    private void RegisterAutoScrollTicker()
    {
        if (_autoScrollRegistered)
        {
            return;
        }

        FrameAnimationTicker.Register(this);
        _autoScrollRegistered = true;
    }

    private void UnregisterAutoScrollTicker()
    {
        if (!_autoScrollRegistered)
        {
            return;
        }

        FrameAnimationTicker.Unregister(this);
        _autoScrollRegistered = false;
    }

    void IFrameAnimation.Tick(float deltaTime)
    {
        if (!_autoScrollActive)
        {
            return;
        }

        var app = UIApplication.Current;
        if (app == null)
        {
            StopAutoScroll();
            return;
        }

        var dragDrop = app.EventManager.DragDrop;
        var dragInfo = dragDrop.CurrentDragData?.Data as TabDragInfo;
        if (!dragDrop.IsDragging || dragInfo?.Owner != this)
        {
            StopAutoScroll();
            return;
        }

        HandleDragScroll(_dragPointerX, _dragPointerY);
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float measuredWidth = ResolveMeasuredWidth(availableWidth);
        float measuredHeight = ResolveMeasuredHeight(availableHeight);

        _root.MeasureUpdate(measuredWidth, measuredHeight);

        DesiredWidth = ResolveDesiredLength(Width, HorizontalAlignment, availableWidth, _root.DesiredWidth);
        DesiredHeight = ResolveDesiredLength(Height, VerticalAlignment, availableHeight, _root.DesiredHeight);
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
        _root.ArrangeUpdate(x, y, width, height);
        UpdateScrollButtons();
    }

    public override void Render(IRenderer renderer)
    {
        foreach (var child in GetChildrenByZIndex())
        {
            RenderSubtree(child, renderer);
        }
    }

    protected internal override bool RendersChildrenManually => true;

    private void CreateVisualTree()
    {
        _contentFrame?.ClearContent();
        _headerHost?.ClearChildren();
        ClearChildren();

        bool isHorizontal = IsHorizontalPosition();
        float headerCrossSize = GetHeaderCrossSize();

        _headerStrip = isHorizontal
            ? new HStack().Spacing(HeaderSpacing).HorizontalAlignment(HorizontalAlignment.Left)
            : new VStack().Spacing(HeaderSpacing).VerticalAlignment(VerticalAlignment.Top);

        _headerScroll = new TabHeadersScrollView
        {
            Orientation = isHorizontal ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical,
            ShowHorizontalScrollbar = false,
            ShowVerticalScrollbar = false
        };
        _headerScroll.Content(_headerStrip);

        _scrollBackwardButton = new TabScrollButton(
            isHorizontal ? Icons.ChevronLeft : Icons.ChevronUp,
            ScrollBackward,
            this,
            isBackward: true);
        _scrollForwardButton = new TabScrollButton(
            isHorizontal ? Icons.ChevronRight : Icons.ChevronDown,
            ScrollForward,
            this,
            isBackward: false);

        if (isHorizontal)
        {
            _headerScroll.Height = headerCrossSize;
            _headerScroll.HorizontalAlignment = HorizontalAlignment.Stretch;
            _scrollBackwardButton.Width = ScrollButtonWidth;
            _scrollBackwardButton.Height = headerCrossSize;
            _scrollBackwardButton.HorizontalAlignment = HorizontalAlignment.Left;
            _scrollForwardButton.Width = ScrollButtonWidth;
            _scrollForwardButton.Height = headerCrossSize;
            _scrollForwardButton.HorizontalAlignment = HorizontalAlignment.Right;
        }
        else
        {
            _headerScroll.Width = headerCrossSize;
            _headerScroll.VerticalAlignment = VerticalAlignment.Stretch;
            _scrollBackwardButton.Width = headerCrossSize;
            _scrollBackwardButton.Height = ScrollButtonWidth;
            _scrollBackwardButton.VerticalAlignment = VerticalAlignment.Top;
            _scrollForwardButton.Width = headerCrossSize;
            _scrollForwardButton.Height = ScrollButtonWidth;
            _scrollForwardButton.VerticalAlignment = VerticalAlignment.Bottom;
        }

        _scrollBackwardButton.IsVisible = false;
        _scrollForwardButton.IsVisible = false;

        _headerOverlay = new OverlayPanel();
        _headerOverlay.Background = TabBackground;
        if (isHorizontal)
        {
            _headerOverlay.Height = headerCrossSize;
            _headerOverlay.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            _headerOverlay.Width = headerCrossSize;
            _headerOverlay.VerticalAlignment = VerticalAlignment.Stretch;
        }

        _headerOverlay.AddChild(_headerScroll);
        _headerOverlay.AddChild(_scrollBackwardButton);
        _headerOverlay.AddChild(_scrollForwardButton);

        _headerHost = BuildHeaderHost(isHorizontal, headerCrossSize);

        _contentFrame = new Frame
        {
            Background = ContentBackground,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _root = BuildRoot();
        _root.Background = ContentBackground;
        AddChild(_root);

        RebuildHeaders();
        UpdateContent();
    }

    private VisualElement BuildRoot()
    {
        return Position switch
        {
            TabPosition.Top => new Grid()
                .Rows(GridLength.Auto, GridLength.Star)
                .Columns(GridLength.Star)
                .AddChild(_headerHost, 0, 0)
                .AddChild(_contentFrame, 1, 0),
            TabPosition.Bottom => new Grid()
                .Rows(GridLength.Star, GridLength.Auto)
                .Columns(GridLength.Star)
                .AddChild(_contentFrame, 0, 0)
                .AddChild(_headerHost, 1, 0),
            TabPosition.Left => new HStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_headerHost, _contentFrame),
            TabPosition.Right => new HStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_contentFrame, _headerHost),
            _ => new VStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_headerHost, _contentFrame)
        };
    }

    private Grid BuildHeaderHost(bool isHorizontal, float headerCrossSize)
    {
        var start = HeaderStartTemplate?.Invoke();
        var end = HeaderEndTemplate?.Invoke();
        var host = new Grid
        {
            Background = HeaderBackground,
            Padding = HeaderPadding,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (isHorizontal)
        {
            host.Height = headerCrossSize + HeaderPadding.Vertical;
            host.RowDefinitions.Add(GridLength.Star);
            host.ColumnSpacing = HeaderContentSpacing;

            int column = 0;
            if (start != null)
            {
                host.ColumnDefinitions.Add(GridLength.Auto);
                host.AddChild(start, 0, column++);
            }

            host.ColumnDefinitions.Add(GridLength.Star);
            host.AddChild(_headerOverlay, 0, column++);

            if (end != null)
            {
                host.ColumnDefinitions.Add(GridLength.Auto);
                host.AddChild(end, 0, column);
            }
        }
        else
        {
            host.Width = headerCrossSize + HeaderPadding.Horizontal;
            host.ColumnDefinitions.Add(GridLength.Star);
            host.RowSpacing = HeaderContentSpacing;

            int row = 0;
            if (start != null)
            {
                host.RowDefinitions.Add(GridLength.Auto);
                host.AddChild(start, row++, 0);
            }

            host.RowDefinitions.Add(GridLength.Star);
            host.AddChild(_headerOverlay, row++, 0);

            if (end != null)
            {
                host.RowDefinitions.Add(GridLength.Auto);
                host.AddChild(end, row, 0);
            }
        }

        return host;
    }

    private void RebuildLayout()
    {
        CreateVisualTree();
        InvalidateMeasure();
    }

    private void SetItems(IList<TabItem>? items)
    {
        _tabs = items?.ToList() ?? [];
        NormalizeSelectedIndex();
        RefreshAfterItemsChanged(selectionChanged: true, ensureVisible: true);
    }

    private void RefreshAfterItemsChanged(bool selectionChanged, bool ensureVisible)
    {
        RebuildHeaders();
        UpdateContent();

        if (_tabs.Count == 0)
        {
            _headerScroll.HorizontalScrollOffset = 0;
            _headerScroll.VerticalScrollOffset = 0;
        }
        else if (ensureVisible)
        {
            QueueEnsureSelectedTabVisible();
        }

        if (selectionChanged && _tabs.Count > 0)
            TabChanged?.Invoke(_selectedIndex);
        else if (selectionChanged && _tabs.Count == 0)
            TabChanged?.Invoke(0);
    }

    private void SelectIndex(int value, bool raiseEvent)
    {
        if (!IsValidIndex(value) || value == _selectedIndex)
            return;

        _selectedIndex = value;
        RebuildHeaders();
        UpdateContent();
        QueueEnsureSelectedTabVisible();
        RefreshLocalLayout();

        if (raiseEvent)
            TabChanged?.Invoke(value);
    }

    private void RebuildHeaders()
    {
        if (_headerStrip == null)
            return;

        ClearHeaderStrip();

        var headerSize = GetTabButtonSize();

        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            bool isSelected = i == _selectedIndex;
            VisualElement content = TabHeaderTemplate?.Invoke(tab, i, isSelected) ?? CreateDefaultHeaderContent(tab, isSelected);

            var header = new TabHeaderHost(this, i, tab, content)
            {
                Width = headerSize.Width,
                Height = headerSize.Height,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            AddHeaderStripChild(header);
        }

        _scrollBackwardButton?.RefreshStyle();
        _scrollForwardButton?.RefreshStyle();
        UpdateScrollButtons();
        InvalidateMeasure();
    }

    private VisualElement CreateDefaultHeaderContent(TabItem tab, bool isSelected)
    {
        var rightPadding = ShouldShowCloseButton(isSelected)
            ? TabCloseButtonHitSize + 14f
            : 10f;
        var textColor = !tab.IsEnabled
            ? EffectiveTheme.Colors.OnDisabled
            : isSelected
                ? EffectiveTheme.Colors.OnSurface
                : EffectiveTheme.Colors.OnDisabled;

        return new HStack()
            .Spacing(0)
            .Padding(new Thickness(10, 0, rightPadding, 0))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                new Label()
                    .Text(tab.Title)
                    .FontSize(13)
                    .Foreground(textColor)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Center));
    }

    private bool ShouldShowCloseButton(bool isSelected) =>
        ShowTabCloseButtons &&
        (CloseButtonDisplay == TabCloseButtonDisplayMode.AllTabs || isSelected);

    private bool ShouldShowCloseButton(int index) =>
        ShouldShowCloseButton(index == _selectedIndex);

    private void UpdateContent()
    {
        if (_tabs.Count == 0 || !IsValidIndex(_selectedIndex))
        {
            _contentFrame.ClearContent();
            _contentFrame.MarkNeedsPaint();
            return;
        }

        var content = _tabs[_selectedIndex].Content;
        if (!ReferenceEquals(_contentFrame.Content, content))
        {
            _contentFrame.ClearContent();
        }

        _contentFrame.Content = content;
        _contentFrame.InvalidateMeasure();
        _contentFrame.MarkNeedsPaint();
        TrySetFocusOnContent(content);
    }

    /// <summary>
    /// Performs a local measure/arrange pass on the TabControl internal tree
    /// without propagating a full-tree relayout to ancestor containers (e.g. an
    /// external ScrollView). This prevents the external scroll offset from
    /// being reset after a tab selection or reorder.
    /// </summary>
    private void RefreshLocalLayout()
    {
        // Re-measure and re-arrange internal tree using the last known available
        // size so that newly created TabHeaderHost elements get valid
        // ComputedWidth/Height before the next render frame.
        if (ComputedWidth > 0 && ComputedHeight > 0)
        {
            ForceMeasure(LastMeasuredAvailableWidth, LastMeasuredAvailableHeight);
            ForceArrange(ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }
        else
        {
            // Fallback: invalidate measure so the tree schedules a proper layout.
            InvalidateMeasure();
        }

        MarkNeedsPaint();
        var tree = UIApplication.Current?.Tree ?? UITree.Current;
        tree?.MarkNeedsRender();
    }

    private void QueueEnsureSelectedTabVisible()
    {
        if (_tabs.Count == 0)
            return;

        UIUpdateQueue.EnqueueUIUpdate(this, EnsureSelectedTabVisible);
    }

    private void EnsureSelectedTabVisible()
    {
        if (_tabs.Count == 0)
            return;

        var selectedHeader = GetHeaderChildren().ElementAtOrDefault(_selectedIndex);
        if (selectedHeader == null)
            return;

        float rectX = selectedHeader.ComputedX - _headerStrip.ComputedX;
        float rectY = selectedHeader.ComputedY - _headerStrip.ComputedY;
        _headerScroll.EnsureRectVisible(rectX, rectY, selectedHeader.ComputedWidth, selectedHeader.ComputedHeight);
        UpdateScrollButtons();
    }

    private void TrySetFocusOnContent(VisualElement element)
    {
        var app = UIApplication.Current;
        if (app == null)
            return;

        if (element is IFocusable && element is IInputHandler handler && handler.CanHandleInput)
        {
            app.RunOnMainThread(() => app.EventManager.SetFocus(element));
            return;
        }

        foreach (var child in element.GetChildren())
        {
            TrySetFocusOnContent(child);
            if (app.EventManager.FocusedElement != null)
                return;
        }
    }

    internal void ReorderTab(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex))
            return;

        int finalIndex = NormalizeReorderTarget(fromIndex, toIndex);
        if (finalIndex == fromIndex)
            return;

        var selectedTab = SelectedTab;
        var reordered = _tabs.ToList();
        var movedTab = reordered[fromIndex];
        reordered.RemoveAt(fromIndex);
        reordered.Insert(finalIndex, movedTab);
        _tabs = reordered;

        if (selectedTab != null)
            _selectedIndex = Math.Max(0, _tabs.IndexOf(selectedTab));
        else
            NormalizeSelectedIndex();

        TabReordered?.Invoke(fromIndex, finalIndex);
        RebuildHeaders();
        UpdateContent();
        QueueEnsureSelectedTabVisible();
        RefreshLocalLayout();
    }

    internal int CalculateInsertIndex(int targetIndex, float pointerX, float pointerY)
    {
        if (!IsValidIndex(targetIndex))
            return targetIndex;

        var targetHeader = GetHeaderChildren().ElementAtOrDefault(targetIndex);
        if (targetHeader == null)
            return targetIndex;

        bool insertAfter = IsHorizontalPosition()
            ? pointerX >= targetHeader.ComputedX + (targetHeader.ComputedWidth / 2f)
            : pointerY >= targetHeader.ComputedY + (targetHeader.ComputedHeight / 2f);

        return insertAfter ? targetIndex + 1 : targetIndex;
    }

    private int NormalizeReorderTarget(int fromIndex, int rawInsertIndex)
    {
        int normalized = Math.Clamp(rawInsertIndex, 0, _tabs.Count);
        if (fromIndex < normalized)
            normalized--;

        return Math.Clamp(normalized, 0, Math.Max(0, _tabs.Count - 1));
    }

    internal void HandleDragScroll(float pointerX, float pointerY)
    {
        _dragPointerX = pointerX;
        _dragPointerY = pointerY;
        _autoScrollActive = true;
        RegisterAutoScrollTicker();

        if (IsHorizontalPosition())
        {
            float left = _headerScroll.ComputedX;
            float right = left + _headerScroll.ComputedWidth;

            if (pointerX <= left + AutoScrollZone)
                _headerScroll.HorizontalScrollOffset -= AutoScrollStep;
            else if (pointerX >= right - AutoScrollZone)
                _headerScroll.HorizontalScrollOffset += AutoScrollStep;
        }
        else
        {
            float top = _headerScroll.ComputedY;
            float bottom = top + _headerScroll.ComputedHeight;

            if (pointerY <= top + AutoScrollZone)
                _headerScroll.VerticalScrollOffset -= AutoScrollStep;
            else if (pointerY >= bottom - AutoScrollZone)
                _headerScroll.VerticalScrollOffset += AutoScrollStep;
        }

        UpdateScrollButtons();
    }

    internal void StopAutoScroll()
    {
        _autoScrollActive = false;
        UnregisterAutoScrollTicker();
    }

    internal Brush ResolveHeaderBackground(int index, bool isHovered)
    {
        if (index == _selectedIndex)
            return TabActiveBackground;

        return isHovered ? TabHoverBackground : TabBackground;
    }

    private void ScrollBackward()
    {
        if (IsHorizontalPosition())
            _headerScroll.HorizontalScrollOffset -= GetTabScrollStep();
        else
            _headerScroll.VerticalScrollOffset -= GetTabScrollStep();

        UpdateScrollButtons();
    }

    private void ScrollForward()
    {
        if (IsHorizontalPosition())
            _headerScroll.HorizontalScrollOffset += GetTabScrollStep();
        else
            _headerScroll.VerticalScrollOffset += GetTabScrollStep();

        UpdateScrollButtons();
    }

    private void UpdateScrollButtons()
    {
        if (_headerStrip == null || _headerScroll == null)
            return;

        bool isHorizontal = IsHorizontalPosition();
        float contentSize = isHorizontal ? _headerStrip.ComputedWidth : _headerStrip.ComputedHeight;
        float viewportSize = isHorizontal ? _headerScroll.ComputedWidth : _headerScroll.ComputedHeight;
        float offset = isHorizontal ? _headerScroll.HorizontalScrollOffset : _headerScroll.VerticalScrollOffset;

        bool needsScroll = contentSize > viewportSize + 0.5f && viewportSize > 0;
        if (!needsScroll)
        {
            _scrollBackwardButton.IsVisible = false;
            _scrollForwardButton.IsVisible = false;
            return;
        }

        float maxOffset = Math.Max(0, contentSize - viewportSize);
        _scrollBackwardButton.IsVisible = offset > 0.1f;
        _scrollForwardButton.IsVisible = offset < maxOffset - 0.1f;
    }

    private IEnumerable<VisualElement> GetHeaderChildren()
    {
        return _headerStrip?.GetChildren() ?? Enumerable.Empty<VisualElement>();
    }

    private void AddHeaderStripChild(VisualElement child)
    {
        switch (_headerStrip)
        {
            case HStack h:
                h.AddChild(child);
                break;
            case VStack v:
                v.AddChild(child);
                break;
        }
    }

    private void ClearHeaderStrip()
    {
        switch (_headerStrip)
        {
            case HStack h:
                h.ClearChildren();
                break;
            case VStack v:
                v.ClearChildren();
                break;
        }
    }

    private bool IsHorizontalPosition()
    {
        return Position == TabPosition.Top || Position == TabPosition.Bottom;
    }

    private Size GetTabButtonSize()
    {
        return IsHorizontalPosition()
            ? new Size(TabWidth, TabHeight)
            : new Size(GetVerticalTabWidthValue(), GetVerticalTabHeightValue());
    }

    private float GetHeaderCrossSize()
    {
        return IsHorizontalPosition() ? TabHeight : GetVerticalTabWidthValue();
    }

    private float GetTabScrollStep()
    {
        return IsHorizontalPosition() ? TabWidth : GetVerticalTabHeightValue();
    }

    private float GetVerticalTabWidthValue()
    {
        return VerticalTabWidth > 0 ? VerticalTabWidth : TabHeight;
    }

    private float GetVerticalTabHeightValue()
    {
        return VerticalTabHeight > 0 ? VerticalTabHeight : TabWidth;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _tabs.Count;
    }

    private void NormalizeSelectedIndex()
    {
        if (_tabs.Count == 0)
        {
            _selectedIndex = 0;
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, _tabs.Count - 1);
    }

    private float ResolveMeasuredWidth(float availableWidth)
    {
        if (Width > 0)
            return Width;

        if (HorizontalAlignment == HorizontalAlignment.Stretch && !float.IsInfinity(availableWidth))
            return availableWidth;

        return availableWidth;
    }

    private float ResolveMeasuredHeight(float availableHeight)
    {
        if (Height > 0)
            return Height;

        if (VerticalAlignment == VerticalAlignment.Stretch && !float.IsInfinity(availableHeight))
            return availableHeight;

        return availableHeight;
    }

    private static float ResolveDesiredLength(float explicitLength, Enum alignment, float availableLength, float measuredLength)
    {
        if (explicitLength > 0)
            return explicitLength;

        bool isStretch = alignment.Equals(HorizontalAlignment.Stretch) || alignment.Equals(VerticalAlignment.Stretch);
        if (isStretch && !float.IsInfinity(availableLength))
            return availableLength;

        if (float.IsNaN(measuredLength) || float.IsInfinity(measuredLength) || measuredLength <= 0)
            return 0;

        if (float.IsInfinity(availableLength))
            return measuredLength;

        return Math.Min(measuredLength, availableLength);
    }

    private static void RenderSubtree(VisualElement element, IRenderer renderer)
    {
        if (!element.IsVisible)
            return;

        element.InvokeOnBeforeRender(renderer);
        element.Render(renderer);

        if (element.RendersChildrenManually)
        {
            element.InvokeOnAfterRender(renderer);
            return;
        }

        foreach (var child in element.GetChildrenByZIndex())
        {
            RenderSubtree(child, renderer);
        }

        element.InvokeOnAfterRender(renderer);
    }

    private sealed record TabDragInfo(TabControl Owner, int Index);

    private sealed class TabHeaderHost : Frame, IPointerHandler, ITappable, IGestureRecognizerHost, IDraggable, IDropTarget
    {
        private const float AccentThickness = 2f;
        private const float CloseButtonReservePadding = 8f;
        private static readonly IconData CloseIcon = Icons.Close;
        private readonly DropConstraints _constraints = new DropConstraints()
            .AcceptType("tab")
            .WithEffects(DragDropEffect.Move);

        private readonly TabControl _owner;
        private readonly int _index;
        private readonly TabItem _tab;
        private readonly TapRecognizer _tapRecognizer;
        private readonly VisualElement _contentRoot;

        public bool IsDragging { get; set; }

        public bool IsDropTargetActive { get; set; }

        public List<IGestureRecognizer> GestureRecognizers { get; } = [];

        public event Action<TapGestureEventArgs>? Tapped;

        public TabHeaderHost(TabControl owner, int index, TabItem tab, VisualElement content)
        {
            _owner = owner;
            _index = index;
            _tab = tab;
            Padding = new Thickness(0);
            BorderThickness = 0;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            IsEnabled = tab.IsEnabled;
            IsInputTransparent = false;

            _contentRoot = BuildContentRoot(content);
            Content = _contentRoot;

            _tapRecognizer = new TapRecognizer(
                maxMovementThreshold: 15f,
                maxPressDurationMs: 500,
                doubleTapWindowMs: 300);
            _tapRecognizer.TapDetected += OnTapDetected;
            GestureRecognizers.Add(_tapRecognizer);

            RefreshVisualState();
        }

        private VisualElement BuildContentRoot(VisualElement content)
        {
            if (!_owner.ShouldShowCloseButton(_index))
                return content;

            float closeSize = _owner.TabCloseButtonHitSize;
            float reserve = closeSize + CloseButtonReservePadding;

            var contentHost = new Frame
            {
                Background = Color.Transparent,
                BorderThickness = 0,
                Padding = new Thickness(0, 0, reserve, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = content
            };

            var closeButton = new TabHeaderCloseButton(CloseIcon, _owner)
            {
                Width = closeSize,
                Height = closeSize,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                IsInputTransparent = true
            };

            var overlay = new OverlayPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            overlay.AddChild(contentHost);
            overlay.AddChild(closeButton);
            return overlay;
        }

        protected internal override bool RendersChildrenManually => true;

        public override void Render(IRenderer renderer)
        {
            base.Render(renderer);

            foreach (var child in GetChildrenByZIndex())
            {
                RenderSubtree(child, renderer);
            }
        }

        public void OnPointerEntered(PointerEventArgs e)
        {
            if (!IsEnabled)
                return;

            IsHovered = true;
            RefreshVisualState();
        }

        public void OnPointerExited(PointerEventArgs e)
        {
            IsHovered = false;
            IsPressed = false;
            _tapRecognizer.Reset();
            RefreshVisualState();
        }

        public void OnPointerPressed(PointerEventArgs e)
        {
            if (!IsEnabled)
                return;

            IsPressed = true;
            _tapRecognizer.ProcessPointerEvent(e);
        }

        public void OnPointerReleased(PointerEventArgs e)
        {
            IsPressed = false;
            if (!IsEnabled)
                return;

            _tapRecognizer.ProcessPointerEvent(e);
        }

        public void OnPointerMoved(PointerEventArgs e)
        {
            if (!IsEnabled)
                return;

            _tapRecognizer.ProcessPointerEvent(e);
        }

        public void OnPointerCanceled(PointerEventArgs e)
        {
            IsPressed = false;
            _tapRecognizer.Reset();
            RefreshVisualState();
        }

        protected override void OnAfterRender(IRenderer renderer)
        {
            bool isSelected = _owner.SelectedIndex == _index;
            if (isSelected || IsDragging)
                DrawAccent(renderer);

            if (IsDropTargetActive)
                renderer.DrawRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, 2f, _owner.TabDropIndicatorColor);

        }

        public DragData? OnDragStart(float mouseX, float mouseY)
        {
            if (!_owner.EnableTabReorder || !IsEnabled)
                return null;

            return new DragData("tab", new TabDragInfo(_owner, _index), this)
                .WithAllowedEffects(DragDropEffect.Move)
                .WithMetadata("title", _tab.Title);
        }

        public void OnDragging(float mouseX, float mouseY)
        {
            if (_owner.EnableTabReorder && IsDragging)
                _owner.HandleDragScroll(mouseX, mouseY);
        }

        public void OnDragEnd(bool wasDropped)
        {
            _owner.StopAutoScroll();
            _tapRecognizer.Reset();
            RefreshVisualState();
            MarkNeedsPaint();
        }

        public bool CanAcceptDataType(string dataType)
        {
            return _owner.EnableTabReorder && dataType == "tab";
        }

        public DropConstraints? Constraints => _owner.EnableTabReorder ? _constraints : null;

        public DragDropEffect? AllowedEffects => _owner.EnableTabReorder ? DragDropEffect.Move : null;

        public bool OnDragEnter(DragData dragData)
        {
            if (!_owner.EnableTabReorder || dragData.Data is not TabDragInfo dragInfo || dragInfo.Owner != _owner || dragInfo.Index == _index)
                return false;

            IsDropTargetActive = true;
            RefreshVisualState();
            MarkNeedsPaint();
            return true;
        }

        public void OnDragOver(DragData dragData, float mouseX, float mouseY)
        {
            _owner.HandleDragScroll(mouseX, mouseY);
        }

        public void OnDragLeave(DragData dragData)
        {
            IsDropTargetActive = false;
            RefreshVisualState();
            MarkNeedsPaint();
        }

        public bool OnDrop(DragData dragData, float mouseX, float mouseY)
        {
            IsDropTargetActive = false;
            RefreshVisualState();
            MarkNeedsPaint();

            if (dragData.Data is not TabDragInfo dragInfo || dragInfo.Owner != _owner || dragInfo.Index == _index)
                return false;

            int insertIndex = _owner.CalculateInsertIndex(_index, mouseX, mouseY);
            UIUpdateQueue.EnqueueUIUpdate(_owner, () => _owner.ReorderTab(dragInfo.Index, insertIndex));
            return true;
        }

        private void RefreshVisualState()
        {
            Background = _owner.ResolveHeaderBackground(_index, IsHovered || IsDropTargetActive);
            MarkNeedsPaint();
        }

        public void RefreshThemeStyle()
        {
            RefreshVisualState();

            if (_owner.TabHeaderTemplate == null)
            {
                var textColor = !_tab.IsEnabled
                    ? EffectiveTheme.Colors.OnDisabled
                    : _owner.SelectedIndex == _index
                        ? EffectiveTheme.Colors.OnSurface
                        : EffectiveTheme.Colors.OnDisabled;
                ApplyHeaderTextColor(_contentRoot, textColor);
            }
        }

        private static void ApplyHeaderTextColor(VisualElement element, Color color)
        {
            if (element is Label label)
                label.Foreground = color;

            foreach (var child in element.GetChildren())
            {
                ApplyHeaderTextColor(child, color);
            }
        }

        private void OnTapDetected(TapGestureEventArgs e)
        {
            var app = UIApplication.Current;
            if (!IsEnabled || (app != null && app.EventManager.DragDrop.IsDragging))
                return;

            if (_owner.ShouldShowCloseButton(_index) && IsCloseButtonHit(e.Position))
            {
                _owner.RequestCloseTab(_index);
                Tapped?.Invoke(e);
                return;
            }

            if (_owner.SelectedIndex != _index)
                _owner.SelectedIndex = _index;

            Tapped?.Invoke(e);
        }

        private void DrawAccent(IRenderer renderer)
        {
            if (_owner.IsHorizontalPosition())
            {
                float y = _owner.Position == TabPosition.Bottom
                    ? ComputedY + ComputedHeight - AccentThickness
                    : ComputedY;
                renderer.DrawRect(ComputedX, y, ComputedWidth, AccentThickness, _owner.TabAccentColor);
            }
            else
            {
                float x = _owner.Position == TabPosition.Right
                    ? ComputedX + ComputedWidth - AccentThickness
                    : ComputedX;
                renderer.DrawRect(x, ComputedY, AccentThickness, ComputedHeight, _owner.TabAccentColor);
            }
        }

        private bool IsCloseButtonHit(System.Numerics.Vector2 position)
        {
            var bounds = GetCloseButtonBounds();
            return position.X >= bounds.x && position.X <= bounds.x + bounds.size &&
                   position.Y >= bounds.y && position.Y <= bounds.y + bounds.size;
        }

        private (float x, float y, float size) GetCloseButtonBounds()
        {
            float size = _owner.TabCloseButtonHitSize;
            float x = ComputedX + ComputedWidth - size;
            float y = ComputedY + (ComputedHeight - size) / 2f;
            return (x, y, size);
        }
    }

    private sealed class OverlayPanel : CompositeView<OverlayPanel>
    {
        protected internal override bool RendersChildrenManually => true;

        protected override void Measure(float availableWidth, float availableHeight)
        {
            float desiredWidth = Width > 0 ? Width : 0;
            float desiredHeight = Height > 0 ? Height : 0;

            foreach (var child in Children)
            {
                child.MeasureUpdate(
                    Width > 0 ? Width : availableWidth,
                    Height > 0 ? Height : availableHeight);

                desiredWidth = Math.Max(desiredWidth, child.DesiredWidth);
                desiredHeight = Math.Max(desiredHeight, child.DesiredHeight);
            }

            DesiredWidth = desiredWidth;
            DesiredHeight = desiredHeight;
        }

        protected override void Arrange(float x, float y, float width, float height)
        {
            base.Arrange(x, y, width, height);

            foreach (var child in Children)
            {
                float childWidth = child.HorizontalAlignment == HorizontalAlignment.Stretch ? width : child.DesiredWidth;
                float childHeight = child.VerticalAlignment == VerticalAlignment.Stretch ? height : child.DesiredHeight;
                float childX = x;
                float childY = y;

                if (child.HorizontalAlignment == HorizontalAlignment.Right)
                    childX = x + width - childWidth;
                else if (child.HorizontalAlignment == HorizontalAlignment.Center)
                    childX = x + (width - childWidth) / 2f;

                if (child.VerticalAlignment == VerticalAlignment.Bottom)
                    childY = y + height - childHeight;
                else if (child.VerticalAlignment == VerticalAlignment.Center)
                    childY = y + (height - childHeight) / 2f;

                child.ArrangeUpdate(childX, childY, childWidth, childHeight);
            }
        }

        public override void Render(IRenderer renderer)
        {
            foreach (var child in GetChildrenByZIndex())
            {
                RenderSubtree(child, renderer);
            }
        }
    }

    private sealed class ActionDisposable : IDisposable
    {
        private Action? _dispose;

        public ActionDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}

/// <summary>
/// ScrollView especializado para las cabeceras del TabControl.
/// Desactiva el drag-scroll legacy para no competir con el drag & drop de tabs.
/// </summary>
internal class TabHeadersScrollView : ScrollView, IInputHandler
{
    bool IInputHandler.HandleInput(InputEventArgs args)
    {
        return false;
    }
}

internal class TabScrollButton : ButtonIcon
{
    private readonly TabControl _owner;
    private readonly bool _isBackward;

    public TabScrollButton(IconData iconData, Action onTap, TabControl owner, bool isBackward)
    {
        _owner = owner;
        _isBackward = isBackward;
        IconData = iconData;
        IconSize = 14f;
        BorderThickness = 0;
        BorderBrush = Color.Transparent;
        BorderRadius = new CornerRadius(0);
        Padding = new Thickness(0);
        RefreshStyle();
        Tapped += _ => onTap();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        base.OnThemeApplied(theme);
        SetThemeValue(
            nameof(IconColor),
            (Brush)theme.Colors.OnSurface,
            value => IconColor = value);
    }

    public void RefreshStyle()
    {
        var baseColor = _owner.TabBackground.PrimaryColor;
        var outer = new Color(baseColor.R, baseColor.G, baseColor.B, 0.10f);
        var mid = new Color(baseColor.R, baseColor.G, baseColor.B, 0.52f);
        var inner = new Color(baseColor.R, baseColor.G, baseColor.B, 0.92f);

        Background = CreateGradientBrush(outer, mid, inner);
        HoverBackground = CreateGradientBrush(
            outer.WithAlpha(0.18f),
            mid.WithAlpha(0.68f),
            inner.WithAlpha(0.98f));
        PressedBackground = CreateGradientBrush(
            outer.WithAlpha(0.24f),
            mid.WithAlpha(0.78f),
            inner.WithAlpha(1f));
    }

    private Brush CreateGradientBrush(Color outer, Color mid, Color inner)
    {
        bool horizontal = _owner.Position is TabPosition.Top or TabPosition.Bottom;
        // The most opaque (and therefore visually darkest) stop faces the
        // outer edge of the control: left/top for backward, right/bottom for
        // forward. The gradient fades toward the tab strip content.
        bool darkEdgeFirst = _isBackward;

        var brush = new LinearGradientBrush(
            Rayo.Rendering.Brushes.GradientStop.At(0f, darkEdgeFirst ? inner : outer),
            Rayo.Rendering.Brushes.GradientStop.At(0.45f, mid),
            Rayo.Rendering.Brushes.GradientStop.At(1f, darkEdgeFirst ? outer : inner));

        if (horizontal)
        {
            brush.StartPoint = new Vector2(0, 0.5f);
            brush.EndPoint = new Vector2(1, 0.5f);
        }
        else
        {
            brush.StartPoint = new Vector2(0.5f, 0);
            brush.EndPoint = new Vector2(0.5f, 1);
        }

        return brush;
    }
}

internal sealed class TabHeaderCloseButton : ButtonIcon
{
    private readonly TabControl _owner;

    public TabHeaderCloseButton(IconData iconData, TabControl owner)
    {
        _owner = owner;
        IconData = iconData;
        IconSize = owner.TabCloseButtonSize;
        Variant = ButtonVariant.Ghost;
        BorderThickness = 0;
        BorderRadius = new CornerRadius(4);
        Padding = new Thickness(0);
        UpdateIconColor();
    }

    public override void Render(IRenderer renderer)
    {
        UpdateIconColor();
        base.Render(renderer);
    }

    private void UpdateIconColor()
    {
        IconColor = IsHovered || IsPressed
            ? _owner.TabCloseButtonHoverColor
            : _owner.TabCloseButtonColor;
    }
}
