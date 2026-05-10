namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
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
public class TabControl : CompositeView<TabControl>
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
    private Frame _contentFrame = null!;
    private TabScrollButton _scrollBackwardButton = null!;
    private TabScrollButton _scrollForwardButton = null!;

    private float _dragPointerX;
    private float _dragPointerY;
    private bool _autoScrollActive;
    private Action<float>? _autoScrollTick;

    #region TabBackground
    public Brush TabBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(45, 45, 48);
    #endregion

    #region TabActiveBackground
    public Brush TabActiveBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(30, 30, 30);
    #endregion

    #region TabHoverBackground
    public Brush TabHoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(60, 60, 60);
    #endregion

    #region TabCloseButtonColor
    public Color TabCloseButtonColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(200, 200, 200);
    #endregion

    #region TabCloseButtonHoverColor
    public Color TabCloseButtonHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = Color.White;
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
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(0, 122, 204);
    #endregion

    #region TabDropIndicatorColor
    public Color TabDropIndicatorColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(0, 122, 204);
    #endregion

    #region ContentBackground
    public Brush ContentBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_contentFrame != null)
                _contentFrame.Background = value;
        });
    } = new Color(30, 30, 30);
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

    public event Action<int>? TabChanged;

    public event Action<int, int>? TabReordered;

    public TabControl()
    {
        CreateVisualTree();
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

        var app = UIApplication.Current;
        if (app == null)
            return;

        _autoScrollTick = _ =>
        {
            if (!_autoScrollActive)
                return;

            var dragDrop = app.EventManager.DragDrop;
            var dragInfo = dragDrop.CurrentDragData?.Data as TabDragInfo;
            if (!dragDrop.IsDragging || dragInfo?.Owner != this)
            {
                _autoScrollActive = false;
                return;
            }

            HandleDragScroll(_dragPointerX, _dragPointerY);
        };

        app.Updated += _autoScrollTick;
    }

    protected override void OnUnmounted()
    {
        var app = UIApplication.Current;
        if (app != null && _autoScrollTick != null)
            app.Updated -= _autoScrollTick;

        _autoScrollTick = null;
        _autoScrollActive = false;

        base.OnUnmounted();
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

        _contentFrame = new Frame
        {
            Background = ContentBackground,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _root = BuildRoot();
        AddChild(_root);

        RebuildHeaders();
        UpdateContent();
    }

    private VisualElement BuildRoot()
    {
        return Position switch
        {
            TabPosition.Top => new VStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_headerOverlay, _contentFrame),
            TabPosition.Bottom => new VStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_contentFrame, _headerOverlay),
            TabPosition.Left => new HStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_headerOverlay, _contentFrame),
            TabPosition.Right => new HStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_contentFrame, _headerOverlay),
            _ => new VStack()
                .Spacing(0)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Children(_headerOverlay, _contentFrame)
        };
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
        var rightPadding = ShowTabCloseButtons ? TabCloseButtonHitSize + 14f : 10f;
        var textColor = !tab.IsEnabled
            ? new Color(120, 120, 120)
            : isSelected
                ? Color.White
                : new Color(210, 210, 210);

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
        element.InvokeOnAfterRender(renderer);

        if (element.RendersChildrenManually)
            return;

        foreach (var child in element.GetChildrenByZIndex())
        {
            RenderSubtree(child, renderer);
        }
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
            BorderWidth = 0;
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
            if (!_owner.ShowTabCloseButtons)
                return content;

            float closeSize = _owner.TabCloseButtonHitSize;
            float reserve = closeSize + CloseButtonReservePadding;

            var contentHost = new Frame
            {
                Background = Color.Transparent,
                BorderWidth = 0,
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

        private void OnTapDetected(TapGestureEventArgs e)
        {
            var app = UIApplication.Current;
            if (!IsEnabled || (app != null && app.EventManager.DragDrop.IsDragging))
                return;

            if (_owner.ShowTabCloseButtons && IsCloseButtonHit(e.Position))
            {
                _owner.RemoveTab(_index);
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

internal class TabScrollButton : IconButton
{
    private readonly TabControl _owner;
    private readonly bool _isBackward;

    public TabScrollButton(IconData iconData, Action onTap, TabControl owner, bool isBackward)
    {
        _owner = owner;
        _isBackward = isBackward;
        IconData = iconData;
        IconSize = 14f;
        IconColor = new Color(240, 240, 240, 0.98f);
        BorderWidth = 0;
        BorderColor = Color.Transparent;
        BorderRadius = new CornerRadius(0);
        Padding = new Thickness(0);
        RefreshStyle();
        Tapped += _ => onTap();
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
        bool outerFirst = _isBackward;

        var brush = new LinearGradientBrush(
            Rayo.Rendering.Brushes.GradientStop.At(0f, outerFirst ? outer : inner),
            Rayo.Rendering.Brushes.GradientStop.At(0.45f, mid),
            Rayo.Rendering.Brushes.GradientStop.At(1f, outerFirst ? inner : outer));

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

internal sealed class TabHeaderCloseButton : IconButton
{
    private readonly TabControl _owner;

    public TabHeaderCloseButton(IconData iconData, TabControl owner)
    {
        _owner = owner;
        IconData = iconData;
        IconSize = owner.TabCloseButtonSize;
        Background = Color.Transparent;
        HoverBackground = new Color(255, 255, 255, 0.08f);
        PressedBackground = new Color(255, 255, 255, 0.14f);
        BorderWidth = 0;
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
