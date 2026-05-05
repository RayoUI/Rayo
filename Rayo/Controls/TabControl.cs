namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using System.Collections.Generic;
using IRenderer = Rayo.Rendering.IRenderer;
using Rayo.Rendering; // Para extension methods
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;

/// <summary>
/// Posición de las tabs en el TabControl
/// </summary>
public enum TabPosition
{
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// Representa un tab individual
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
    private List<TabItem> _tabs = new();
    private int _selectedIndex = 0;
    private TabHeadersScrollView _tabHeadersScroll = null!;
    private VisualElement _tabHeadersStack = null!; // Base class para HStack o VStack
    private Frame _contentFrame = null!;
    private VisualElement _headersContainer = null!;
    private TabScrollButton _scrollLeftButton = null!;
    private TabScrollButton _scrollRightButton = null!;
    private bool _headersInputSuppressed;
    private float _autoScrollMouseX;
    private float _autoScrollMouseY;
    private bool _autoScrollActive;
    private Action<float>? _autoScrollTick;

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region TabBackground
    public Brush TabBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(40, 40, 45);
    #endregion

    #region TabActiveBackground
    public Brush TabActiveBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(59, 130, 246);
    #endregion

    #region TabHoverBackground
    public Brush TabHoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = new Color(50, 50, 55);
    #endregion

    #region TabCloseButtonColor
    public Color TabCloseButtonColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(200, 200, 200);
    #endregion

    #region TabCloseButtonHoverColor
    public Color TabCloseButtonHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(255, 255, 255);
    #endregion

    #region TabCloseButtonSize
    public float TabCloseButtonSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 12f;
    #endregion

    #region TabCloseButtonHitSize
    public float TabCloseButtonHitSize
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = 20f;
    #endregion

    #region TabAccentColor
    public Color TabAccentColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 122, 204);
    #endregion

    #region TabDropIndicatorColor
    public Color TabDropIndicatorColor
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = new Color(0, 122, 204);
    #endregion

    #region ContentBackground
    public Brush ContentBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_contentFrame != null)
                _contentFrame.Background(ContentBackground);
        });
    } = new Color(30, 30, 35);
    #endregion

    #region TabHeight
    public float TabHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 36;
    #endregion

    #region TabWidth
    public float TabWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = 120;
    #endregion

    #region ScrollButtonWidth
    public float ScrollButtonWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 24;
    #endregion

    #region Position
    public new TabPosition Position
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = TabPosition.Top;
    #endregion

    #region VerticalTabHeight
    public float VerticalTabHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 36;
    #endregion

    #region VerticalTabWidth
    public float VerticalTabWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildLayout);
    } = 150;
    #endregion

    #region EnableTabReorder
    public bool EnableTabReorder
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = true;
    #endregion

    #region ShowTabCloseButtons
    public bool ShowTabCloseButtons
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildHeaders);
    } = true;
    #endregion

    #region SelectedIndex
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value && value >= 0 && value < _tabs.Count)
            {
                _selectedIndex = value;
                UpdateContent();
                TabChanged?.Invoke(value);
                RebuildHeaders();
                QueueEnsureSelectedTabVisible();
            }
        }
    }
    #endregion

    #region SelectedTab
    [NotFluent]
    public TabItem? SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabs.Count ? _tabs[_selectedIndex] : null;
    #endregion

    #region TabCount
    [NotFluent]
    public int TabCount => _tabs.Count;
    #endregion

    #region Items
    /// <summary>
    /// Gets or sets the complete tab-item collection. Assigning a
    /// <see cref="SignalList{T}"/> via the generated fluent overload causes the
    /// headers to rebuild automatically whenever items are added, removed, or replaced.
    /// </summary>
    public IList<TabItem> Items
    {
        get => _tabs;
        set
        {
            _tabs = value?.ToList() ?? new List<TabItem>();
            if (_selectedIndex >= _tabs.Count)
                _selectedIndex = Math.Max(0, _tabs.Count - 1);
            if (_contentFrame != null)
            {
                RebuildHeaders();
                if (_tabs.Count > 0)
                {
                    UpdateContent();
                    QueueEnsureSelectedTabVisible();
                }
                else
                    _contentFrame.ClearContent();
            }
        }
    }
    #endregion

    #region TabHeaderTemplate
    /// <summary>
    /// Optional factory that creates a fully custom header element for each tab.
    /// The factory receives the <see cref="TabItem"/>, its zero-based index, and
    /// <see langword="true"/> when that tab is currently selected.
    /// The wrapper automatically handles tap-to-select and drag-and-drop reordering.
    /// <para>
    /// <b>Tip:</b> Return a purely decorative element (e.g. <c>HStack</c> of
    /// <c>TextBlock</c>s) so that pointer events bubble up to the wrapper and tab
    /// selection works correctly. Interactive children (e.g. <c>Button</c>) would
    /// capture pointer events and prevent the tab from being selected.
    /// </para>
    /// </summary>
    [NotFluent]
    public Func<TabItem, int, bool, VisualElement>? TabHeaderTemplate
    {
        get => field;
        set
        {
            field = value;
            RebuildHeaders();
        }
    }
    #endregion


    // =========================================================================
    // EVENTS
    // =========================================================================

    public event Action<int>? TabChanged;

    public event Action<int, int>? TabReordered;

    /// <summary>
    /// Sets a custom factory function that builds the header element for each tab.
    /// See <see cref="TabHeaderTemplate"/> for details and usage notes.
    /// </summary>
    public TabControl WithTabHeaderTemplate(Func<TabItem, int, bool, VisualElement> factory)
    {
        TabHeaderTemplate = factory;
        return this;
    }

    /// <summary>
    /// Establishes a two-way reactive binding between <paramref name="binding"/> and
    /// the selected-tab index. Changes in the control push the new index into the
    /// binding and changes to the binding update the selected tab.
    /// Subscriptions are cleaned up automatically when the element is unmounted.
    /// </summary>
    public TabControl BindSelectedIndex(IWritableSignal<int> binding)
    {
        // Binding → Control: when the signal value changes, update SelectedIndex.
        var subscription = binding.Subscribe(v =>
        {
            UIUpdateQueue.EnqueueUIUpdate(this, () => SelectedIndex = v);
        });
        RegisterDisposable(subscription);
        SelectedIndex = binding.Value;

        // Control → Binding: when the user selects a tab, push the index back.
        Action<int> handler = index => { binding.Value = index; };
        TabChanged += handler;
        // Clean up the event subscription when the element is disposed/unmounted.
        RegisterDisposable(new ActionDisposable(() => TabChanged -= handler));

        return this;
    }


    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public TabControl()
    {
        // Initialize reactive properties
        TabBackground = new Color(45, 45, 48); // Flat Dark
        TabActiveBackground = new Color(30, 30, 30); // Matches content
        TabHoverBackground = new Color(60, 60, 60);
        ContentBackground = new Color(30, 30, 30);
        TabHeight = 30;
        TabWidth = 120;
        ScrollButtonWidth = 20;
        Position = TabPosition.Top;
        VerticalTabHeight = 0;
        VerticalTabWidth = 0;
        EnableTabReorder = true;
        ShowTabCloseButtons = false;

        CreateLayout();
    }

    private void CreateLayout()
    {
        // Limpiar hijos anteriores si existen
        ClearChildren();

        // Crear stack de headers con orientación según posición
        bool isHorizontal = IsHorizontalPosition();
        float headerThickness = GetHeaderCrossSize();
        _tabHeadersStack = isHorizontal
            ? new HStack().Spacing(2).HorizontalAlignment(HorizontalAlignment.Left)
            : new VStack().Spacing(2).VerticalAlignment(VerticalAlignment.Top);

        _tabHeadersScroll = new TabHeadersScrollView();
        _tabHeadersScroll.Content(_tabHeadersStack);
        _tabHeadersScroll.Orientation = isHorizontal ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical;

        _tabHeadersScroll.ShowHorizontalScrollbar = false;
        _tabHeadersScroll.ShowVerticalScrollbar = false;

        var leftIcon = isHorizontal ? Icons.ChevronLeft : Icons.ChevronUp;
        var rightIcon = isHorizontal ? Icons.ChevronRight : Icons.ChevronDown;
        var leftGradientStart = isHorizontal ? new System.Numerics.Vector2(1f, 0.5f) : new System.Numerics.Vector2(0.5f, 1f);
        var leftGradientEnd = isHorizontal ? new System.Numerics.Vector2(0f, 0.5f) : new System.Numerics.Vector2(0.5f, 0f);
        var rightGradientStart = isHorizontal ? new System.Numerics.Vector2(0f, 0.5f) : new System.Numerics.Vector2(0.5f, 0f);
        var rightGradientEnd = isHorizontal ? new System.Numerics.Vector2(1f, 0.5f) : new System.Numerics.Vector2(0.5f, 1f);

        var leftButtonSize = isHorizontal
            ? new Size(ScrollButtonWidth, headerThickness)
            : new Size(headerThickness, ScrollButtonWidth);

        _scrollLeftButton = new TabScrollButton(this, leftIcon, leftGradientEnd, leftGradientStart, () => ScrollLeft());
        _scrollLeftButton.Width = leftButtonSize.Width;
        _scrollLeftButton.Height = leftButtonSize.Height;
        _scrollLeftButton.Background = new Color(37, 37, 38);
        _scrollLeftButton.IsVisible = false;
        _scrollLeftButton.ZIndex = 1; // Ensure scroll buttons receive input before scrollview content

        var rightButtonSize = isHorizontal
            ? new Size(ScrollButtonWidth, headerThickness)
            : new Size(headerThickness, ScrollButtonWidth);

        _scrollRightButton = new TabScrollButton(this, rightIcon, rightGradientEnd, rightGradientStart, () => ScrollRight());
        _scrollRightButton.Width = rightButtonSize.Width;
        _scrollRightButton.Height = rightButtonSize.Height;
        _scrollRightButton.Background = new Color(37, 37, 38);
        _scrollRightButton.IsVisible = false;
        _scrollRightButton.ZIndex = 1; // Ensure scroll buttons receive input before scrollview content

        // Configurar container de headers con botones
        var overlay = new OverlayFrame();

        if (isHorizontal)
        {
            _tabHeadersScroll.Height(headerThickness).HorizontalAlignment(HorizontalAlignment.Stretch);
            overlay.Height = headerThickness;
            overlay.HorizontalAlignment = HorizontalAlignment.Stretch;

            // Capa 1: ScrollView (Fondo)
            overlay.AddChild(_tabHeadersScroll);

            // Capa 2: Botones (Frente) - Alineados a los extremos
            _scrollLeftButton.HorizontalAlignment(HorizontalAlignment.Left);
            _scrollRightButton.HorizontalAlignment(HorizontalAlignment.Right);

            overlay.AddChild(_scrollLeftButton);
            overlay.AddChild(_scrollRightButton);
        }
        else
        {
            _tabHeadersScroll.Width(headerThickness).VerticalAlignment(VerticalAlignment.Stretch);
            overlay.Width = headerThickness;
            overlay.VerticalAlignment = VerticalAlignment.Stretch;

            // Capa 1: ScrollView (Fondo)
            overlay.AddChild(_tabHeadersScroll);

            // Capa 2: Botones (Frente) - Alineados a los extremos
            _scrollLeftButton.VerticalAlignment(VerticalAlignment.Top);
            _scrollRightButton.VerticalAlignment(VerticalAlignment.Bottom);

            overlay.AddChild(_scrollLeftButton);
            overlay.AddChild(_scrollRightButton);
        }

        _headersContainer = overlay;

        _contentFrame = new Frame();
        _contentFrame.Background = ContentBackground;
        _contentFrame.HorizontalAlignment = HorizontalAlignment.Stretch;
        _contentFrame.VerticalAlignment = VerticalAlignment.Stretch;

        // Crear container según posición
        VisualElement container;
        switch (Position)
        {
            case TabPosition.Top:
                container = new VStack()
                    .Children(_headersContainer, _contentFrame)
                    .Spacing(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
                break;

            case TabPosition.Bottom:
                container = new VStack()
                    .Children(_contentFrame, _headersContainer)
                    .Spacing(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
                break;

            case TabPosition.Left:
                container = new HStack()
                    .Children(_headersContainer, _contentFrame)
                    .Spacing(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
                break;

            case TabPosition.Right:
                container = new HStack()
                    .Children(_contentFrame, _headersContainer)
                    .Spacing(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
                break;

            default:
                container = new VStack()
                    .Children(_headersContainer, _contentFrame)
                    .Spacing(0)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
                break;
        }

        AddChild(container);
    }

    private void RebuildLayout()
    {
        // Guard against calls during initialization
        // CreateLayout() will initialize all necessary fields
        CreateLayout();
        RebuildHeaders();
        if (_tabs.Count > 0)
            UpdateContent();
        MarkNeedsLayout();
    }

    public TabControl AddTab(string title, VisualElement content)
    {
        _tabs.Add(new TabItem(title, content));
        RebuildHeaders();
        if (_tabs.Count == 1)
        {
            UpdateContent();
        }
        return this;
    }

    public TabControl AddTab(TabItem tab)
    {
        _tabs.Add(tab);
        RebuildHeaders();
        if (_tabs.Count == 1)
        {
            UpdateContent();
        }
        return this;
    }

    public void RemoveTab(int index)
    {
        if (index < 0 || index >= _tabs.Count)
            return;

        _tabs.RemoveAt(index);

        // Ajustar el índice seleccionado
        if (_selectedIndex >= _tabs.Count)
        {
            _selectedIndex = _tabs.Count - 1;
        }

        RebuildHeaders();

        // Si había tabs, actualizar el contenido
        if (_tabs.Count > 0)
        {
            UpdateContent();
        }
        else
        {
            // No hay tabs, limpiar el contenido
            _contentFrame.ClearContent();
        }
    }

    private void ScrollLeft()
    {
        bool isHorizontal = IsHorizontalPosition();
        float scrollStep = GetTabScrollStep();

        if (isHorizontal)
        {
            float newOffset = Math.Max(0, _tabHeadersScroll.HorizontalScrollOffset - scrollStep);
            _tabHeadersScroll.HorizontalScrollOffset = newOffset;
        }
        else
        {
            float newOffset = Math.Max(0, _tabHeadersScroll.VerticalScrollOffset - scrollStep);
            _tabHeadersScroll.VerticalScrollOffset = newOffset;
        }

        UpdateScrollButtonStates();
        MarkNeedsLayout();
    }

    private void ScrollRight()
    {
        bool isHorizontal = IsHorizontalPosition();
        float scrollStep = GetTabScrollStep();

        if (isHorizontal)
        {
            float contentWidth = _tabHeadersStack.ComputedWidth;
            float viewWidth = _tabHeadersScroll.ComputedWidth;
            float maxOffset = Math.Max(0, contentWidth - viewWidth);
            float newOffset = Math.Min(maxOffset, _tabHeadersScroll.HorizontalScrollOffset + scrollStep);
            _tabHeadersScroll.HorizontalScrollOffset = newOffset;
        }
        else
        {
            float contentHeight = _tabHeadersStack.ComputedHeight;
            float viewHeight = _tabHeadersScroll.ComputedHeight;
            float maxOffset = Math.Max(0, contentHeight - viewHeight);
            float newOffset = Math.Min(maxOffset, _tabHeadersScroll.VerticalScrollOffset + scrollStep);
            _tabHeadersScroll.VerticalScrollOffset = newOffset;
        }

        UpdateScrollButtonStates();
        MarkNeedsLayout();
    }

    private void UpdateScrollButtonStates()
    {
        // Guard against calls during initialization
        if (_tabHeadersStack == null || _tabHeadersScroll == null || _scrollLeftButton == null || _scrollRightButton == null)
            return;

        bool isHorizontal = IsHorizontalPosition();

        float contentSize, viewSize, currentOffset;

        if (isHorizontal)
        {
            contentSize = _tabHeadersStack.ComputedWidth;
            viewSize = _tabHeadersScroll.ComputedWidth;
            currentOffset = _tabHeadersScroll.HorizontalScrollOffset;
        }
        else
        {
            contentSize = _tabHeadersStack.ComputedHeight;
            viewSize = _tabHeadersScroll.ComputedHeight;
            currentOffset = _tabHeadersScroll.VerticalScrollOffset;
        }

        if (contentSize <= viewSize || contentSize == 0 || viewSize == 0)
        {
            if (_scrollLeftButton.IsVisible || _scrollRightButton.IsVisible)
            {
                _scrollLeftButton.IsVisible = false;
                _scrollRightButton.IsVisible = false;
                _scrollLeftButton.MarkNeedsPaint();
                _scrollRightButton.MarkNeedsPaint();
                MarkNeedsLayout();
            }
            return;
        }

        float maxOffset = Math.Max(0, contentSize - viewSize);

        bool canScrollLeft = currentOffset > 0.1f;
        bool leftChanged = _scrollLeftButton.IsVisible != canScrollLeft;
        _scrollLeftButton.IsVisible = canScrollLeft;
        if (leftChanged)
        {
            _scrollLeftButton.MarkNeedsPaint();
        }

        bool canScrollRight = currentOffset < maxOffset - 0.1f;
        bool rightChanged = _scrollRightButton.IsVisible != canScrollRight;
        _scrollRightButton.IsVisible = canScrollRight;
        if (rightChanged)
        {
            _scrollRightButton.MarkNeedsPaint();
        }

        if (!canScrollLeft && !canScrollRight)
        {
            SetHeaderInputSuppressed(false);
        }

        if (_scrollLeftButton.IsVisible || _scrollRightButton.IsVisible)
        {
            MarkNeedsLayout();
            MarkNeedsPaint();
        }
    }

    internal void SetHeaderInputSuppressed(bool suppressed)
    {
        if (_headersInputSuppressed == suppressed)
            return;

        _headersInputSuppressed = suppressed;

        if (_tabHeadersStack == null)
            return;

        foreach (var child in _tabHeadersStack.GetChildren())
        {
            child.IsInputTransparent = suppressed;
            if (suppressed)
            {
                child.IsHovered = false;
                child.IsPressed = false;
                child.MarkNeedsPaint();
            }
        }
    }

    private bool IsHorizontalPosition()
        => Position == TabPosition.Top || Position == TabPosition.Bottom;

    private float GetHorizontalTabWidthValue() => TabWidth;

    private float GetHorizontalTabHeightValue() => TabHeight;

    private float GetVerticalTabWidthValue() => VerticalTabWidth > 0 ? VerticalTabWidth : TabHeight;

    private float GetVerticalTabHeightValue() => VerticalTabHeight > 0 ? VerticalTabHeight : TabWidth;

    private Size GetTabButtonSize()
        => IsHorizontalPosition()
            ? new Size(GetHorizontalTabWidthValue(), GetHorizontalTabHeightValue())
            : new Size(GetVerticalTabWidthValue(), GetVerticalTabHeightValue());

    private float GetHeaderCrossSize()
        => IsHorizontalPosition() ? GetHorizontalTabHeightValue() : GetVerticalTabWidthValue();

    private float GetTabScrollStep()
        => IsHorizontalPosition() ? GetHorizontalTabWidthValue() : GetVerticalTabHeightValue();

    private void RebuildHeaders()
    {
        // Guard against calls during initialization before CreateLayout() runs
        if (_tabHeadersStack == null) return;

        // Cast to Layout to access ClearChildren
        if (_tabHeadersStack is HStack headerHStack)
        {
            headerHStack.ClearChildren();
        }
        else if (_tabHeadersStack is VStack headerVStack)
        {
            headerVStack.ClearChildren();
        }

        var tabSize = GetTabButtonSize();

        for (int i = 0; i < _tabs.Count; i++)
        {
            int index = i;
            var tab = _tabs[i];
            var isSelected = i == _selectedIndex;

            VisualElement header;

            if (TabHeaderTemplate != null)
            {
                // Build the user-supplied custom content and wrap it so the control
                // can handle tap-to-select and drag-and-drop automatically.
                var customContent = TabHeaderTemplate(tab, i, isSelected);
                var wrapper = new CustomTabHeaderWrapper(i, customContent, this);
                wrapper.Width = tabSize.Width;
                wrapper.Height = tabSize.Height;
                wrapper.Background = isSelected ? TabActiveBackground : TabBackground;
                wrapper.IsEnabled = tab.IsEnabled;
                header = wrapper;
            }
            else
            {
                var tabButton = new DraggableTabButton(i, tab.Title, this);
                tabButton.Background(isSelected ? TabActiveBackground : TabBackground);
                tabButton.HoverBackground(isSelected ? TabActiveBackground : TabHoverBackground);
                tabButton.Size(tabSize);
                tabButton.BorderRadius(0);
                tabButton.IsEnabled = tab.IsEnabled;
                tabButton.OnTapped(() =>
                {
                    if (index != _selectedIndex)
                    {
                        SelectedIndex = index;
                    }
                });
                header = tabButton;
            }

            // Cast to Layout to access AddChild
            if (_tabHeadersStack is HStack h)
            {
                h.AddChild(header);
            }
            else if (_tabHeadersStack is VStack v)
            {
                v.AddChild(header);
            }
        }

        UpdateScrollButtonStates();
        MarkNeedsLayout();
    }

    private void UpdateContent()
    {
        _contentFrame.ClearContent();

        if (_selectedIndex >= 0 && _selectedIndex < _tabs.Count)
        {
            var content = _tabs[_selectedIndex].Content;
            _contentFrame.Content(content);

            // Try to set focus on first focusable element in the content
            TrySetFocusOnContent(content);
        }

        MarkNeedsLayout();
    }

    private void QueueEnsureSelectedTabVisible()
    {
        if (_tabHeadersScroll == null || _tabHeadersStack == null)
            return;

        UIUpdateQueue.EnqueueUIUpdate(this, EnsureSelectedTabVisible);
    }

    private void EnsureSelectedTabVisible()
    {
        if (_tabHeadersScroll == null || _tabHeadersStack == null)
            return;

        VisualElement? header = null;
        int index = 0;
        foreach (var child in _tabHeadersStack.GetChildren())
        {
            if (index == _selectedIndex)
            {
                header = child;
                break;
            }
            index++;
        }

        if (header == null)
            return;

        float rectX = header.ComputedX - _tabHeadersScroll.ComputedX + _tabHeadersScroll.HorizontalScrollOffset;
        float rectY = header.ComputedY - _tabHeadersScroll.ComputedY + _tabHeadersScroll.VerticalScrollOffset;
        _tabHeadersScroll.EnsureRectVisible(rectX, rectY, header.ComputedWidth, header.ComputedHeight);
        UpdateScrollButtonStates();
    }

    private void TrySetFocusOnContent(VisualElement element)
    {
        if (UIApplication.Current == null) return;

        // Check if the element itself is focusable
        if (element is IFocusable && element is IInputHandler handler && handler.CanHandleInput)
        {
            // Delay focus setting to allow the element to be fully built and arranged
            UIApplication.Current.RunOnMainThread(() =>
            {
                UIApplication.Current.EventManager.SetFocus(element);
            });
            return;
        }

        // Check children recursively
        foreach (var child in element.GetChildren())
        {
            TrySetFocusOnContent(child);
            if (UIApplication.Current.EventManager.FocusedElement != null)
                return; // Found and set focus, stop searching
        }
    }

    internal void ReorderTab(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || fromIndex < 0 || fromIndex >= _tabs.Count || toIndex < 0 || toIndex >= _tabs.Count)
            return;

        var draggedTab = _tabs[fromIndex];
        _tabs.RemoveAt(fromIndex);
        _tabs.Insert(toIndex, draggedTab);

        if (_selectedIndex == fromIndex)
        {
            _selectedIndex = toIndex;
        }
        else if (fromIndex < _selectedIndex && toIndex >= _selectedIndex)
        {
            _selectedIndex--;
        }
        else if (fromIndex > _selectedIndex && toIndex <= _selectedIndex)
        {
            _selectedIndex++;
        }

        TabReordered?.Invoke(fromIndex, toIndex);
        RebuildHeaders();
    }

    internal void HandleDragScroll(float mouseX, float mouseY)
    {
        const float ScrollZone = 50f;
        const float ScrollSpeed = 5f;

        _autoScrollMouseX = mouseX;
        _autoScrollMouseY = mouseY;
        _autoScrollActive = true;

        if (IsHorizontalPosition())
        {
            float scrollViewX = _tabHeadersScroll.ComputedX;
            float scrollViewWidth = _tabHeadersScroll.ComputedWidth;
            float scrollViewRight = scrollViewX + scrollViewWidth;

            if (mouseX < scrollViewX + ScrollZone && _tabHeadersScroll.HorizontalScrollOffset > 0)
            {
                float newOffset = Math.Max(0, _tabHeadersScroll.HorizontalScrollOffset - ScrollSpeed);
                _tabHeadersScroll.HorizontalScrollOffset = newOffset;
                UpdateScrollButtonStates();
                MarkNeedsLayout();
            }
            else if (mouseX > scrollViewRight - ScrollZone)
            {
                float contentWidth = _tabHeadersStack.ComputedWidth;
                float viewWidth = _tabHeadersScroll.ComputedWidth;
                float maxOffset = Math.Max(0, contentWidth - viewWidth);

                if (_tabHeadersScroll.HorizontalScrollOffset < maxOffset)
                {
                    float newOffset = Math.Min(maxOffset, _tabHeadersScroll.HorizontalScrollOffset + ScrollSpeed);
                    _tabHeadersScroll.HorizontalScrollOffset = newOffset;
                    UpdateScrollButtonStates();
                    MarkNeedsLayout();
                }
            }
        }
        else
        {
            float scrollViewY = _tabHeadersScroll.ComputedY;
            float scrollViewHeight = _tabHeadersScroll.ComputedHeight;
            float scrollViewBottom = scrollViewY + scrollViewHeight;

            if (mouseY < scrollViewY + ScrollZone && _tabHeadersScroll.VerticalScrollOffset > 0)
            {
                float newOffset = Math.Max(0, _tabHeadersScroll.VerticalScrollOffset - ScrollSpeed);
                _tabHeadersScroll.VerticalScrollOffset = newOffset;
                UpdateScrollButtonStates();
                MarkNeedsLayout();
            }
            else if (mouseY > scrollViewBottom - ScrollZone)
            {
                float contentHeight = _tabHeadersStack.ComputedHeight;
                float viewHeight = _tabHeadersScroll.ComputedHeight;
                float maxOffset = Math.Max(0, contentHeight - viewHeight);

                if (_tabHeadersScroll.VerticalScrollOffset < maxOffset)
                {
                    float newOffset = Math.Min(maxOffset, _tabHeadersScroll.VerticalScrollOffset + ScrollSpeed);
                    _tabHeadersScroll.VerticalScrollOffset = newOffset;
                    UpdateScrollButtonStates();
                    MarkNeedsLayout();
                }
            }
        }
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
            var sourceElement = dragDrop.CurrentDragData?.SourceElement;
            if (!dragDrop.IsDragging || !IsDragFromThisControl(sourceElement))
            {
                _autoScrollActive = false;
                return;
            }

            HandleDragScroll(_autoScrollMouseX, _autoScrollMouseY);
        };

        app.Updated += _autoScrollTick;
    }

    protected override void OnUnmounted()
    {
        var app = UIApplication.Current;
        if (app != null && _autoScrollTick != null)
        {
            app.Updated -= _autoScrollTick;
        }

        _autoScrollTick = null;
        _autoScrollActive = false;

        base.OnUnmounted();
    }

    private bool IsDragFromThisControl(VisualElement? sourceElement)
    {
        var current = sourceElement;
        while (current != null)
        {
            if (current == this)
                return true;

            current = current.Parent;
        }

        return false;
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        // ✅ CORREGIDO: Implementar medición correcta respetando Alignment

        // Calcular el tamaño deseado basándose en:
        // 1. Width/Height explícitos si están definidos
        // 2. Alignment (Stretch → usar todo el disponible)
        // 3. Contenido (mínimo necesario)

        float desiredWidth = Width;
        float desiredHeight = Height;

        // Ancho: Si es 0 (no especificado), verificar alignment
        if (desiredWidth == 0)
        {
            if (HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                // Stretch → usar todo el ancho disponible
                desiredWidth = availableWidth;
            }
            else
            {
                // No stretch → calcular el mínimo necesario
                // Para TabControl, el mínimo depende de la orientación de las tabs
                float minWidth = IsHorizontalPosition()
                    ? _tabs.Count * GetHorizontalTabWidthValue() + ScrollButtonWidth * 2
                    : GetHeaderCrossSize() + 200;
                desiredWidth = Math.Min(minWidth, availableWidth);
            }
        }

        // Alto: Si es 0 (no especificado), verificar alignment
        if (desiredHeight == 0)
        {
            if (VerticalAlignment == VerticalAlignment.Stretch)
            {
                // Stretch → usar todo el alto disponible
                desiredHeight = availableHeight;
            }
            else
            {
                // No stretch → calcular el mínimo necesario
                // Para TabControl, el mínimo depende de la orientación
                float minHeight = IsHorizontalPosition()
                    ? GetHorizontalTabHeightValue() + 100
                    : _tabs.Count * GetVerticalTabHeightValue() + 100;
                desiredHeight = Math.Min(minHeight, availableHeight);
            }
        }

        // ✅ CRÍTICO: No modificar Width/Height aquí
        // Solo establecer DesiredSize para que el padre pueda decidir
        // El tamaño final se establece en Arrange

        // Medir los hijos con el espacio disponible (no el deseado)
        foreach (var child in Children.ToArray())
        {
            child.Measure(desiredWidth, desiredHeight);
        }
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Arrange del container (headers + content)
        if (Children.Count > 0)
            Children[0].Arrange(x, y, width, height);

        UpdateScrollButtonStates();
    }

    public override void Render(IRenderer renderer)
    {
        // Los hijos se renderizan automáticamente en el orden correcto
    }

    /// <summary>
    /// ✅ Método de debugging para verificar hit-testing en un punto.
    /// </summary>
    public void DebugHitTest(float x, float y)
    {
        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine($"[TabControl.DebugHitTest] Testing point ({x}, {y})");

        // Verificar cada child manualmente
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            Console.WriteLine($"[TabControl.DebugHitTest] Child[{i}]: {child.GetType().Name}");
            Console.WriteLine($"[TabControl.DebugHitTest]    IsVisible: {child.IsVisible}");
            Console.WriteLine($"[TabControl.DebugHitTest]    Bounds: ({child.ComputedX}, {child.ComputedY}, {child.ComputedWidth}, {child.ComputedHeight})");

            bool isInside = x >= child.ComputedX && x <= child.ComputedX + child.ComputedWidth &&
      y >= child.ComputedY && y <= child.ComputedY + child.ComputedHeight;
            Console.WriteLine($"[TabControl.DebugHitTest]    Point inside? {isInside}");

            // Verificar interfaces modernas del sistema de input
            if (child is IPointerHandler)
                Console.WriteLine($"[TabControl.DebugHitTest]    ✓ Implements IPointerHandler");
            if (child is ITappable)
                Console.WriteLine($"[TabControl.DebugHitTest]    ✓ Implements ITappable");
            if (child is IInputHandler)
                Console.WriteLine($"[TabControl.DebugHitTest]    ✓ Implements IInputHandler");
            if (child is IGestureRecognizerHost)
                Console.WriteLine($"[TabControl.DebugHitTest]    ✓ Implements IGestureRecognizerHost");
        }

        Console.WriteLine("════════════════════════════════════════");
    }

    /// <summary>
    /// Frame simple que permite superponer hijos (Overlay).
    /// Reemplaza al Grid para asegurar que los botones queden encima del ScrollView.
    /// </summary>
    private class OverlayFrame : CompositeView<OverlayFrame>
    {
        public override void Measure(float availableWidth, float availableHeight)
        {
            // Si tiene tamaño explícito, usarlo
            float w = Width > 0 ? Width : availableWidth;
            float h = Height > 0 ? Height : availableHeight;

            float maxChildW = 0;
            float maxChildH = 0;

            // Measure all children
            foreach (var child in Children.ToArray())
            {
                if (child.IsVisible)
                {
                    child.Measure(w, h);
                    maxChildW = Math.Max(maxChildW, child.DesiredWidth);
                    maxChildH = Math.Max(maxChildH, child.DesiredHeight);
                }
            }

            DesiredWidth = Width > 0 ? Width : maxChildW;
            DesiredHeight = Height > 0 ? Height : maxChildH;
        }

        public override void Arrange(float x, float y, float width, float height)
        {
            base.Arrange(x, y, width, height);

            foreach (var child in Children.ToArray())
            {
                if (!child.IsVisible) continue;

                float childW = child.DesiredWidth;
                float childH = child.DesiredHeight;
                float childX = x;
                float childY = y;

                // Horizontal Alignment
                switch (child.HorizontalAlignment)
                {
                    case HorizontalAlignment.Stretch:
                        childW = width - child.Margin.Horizontal;
                        childX = x + child.Margin.Left;
                        break;
                    case HorizontalAlignment.Center:
                        childX = x + (width - childW) / 2 + child.Margin.Left;
                        break;
                    case HorizontalAlignment.Right:
                        childX = x + width - childW - child.Margin.Right;
                        break;
                    case HorizontalAlignment.Left:
                    default:
                        childX = x + child.Margin.Left;
                        break;
                }

                // Vertical Alignment
                switch (child.VerticalAlignment)
                {
                    case VerticalAlignment.Stretch:
                        childH = height - child.Margin.Vertical;
                        childY = y + child.Margin.Top;
                        break;
                    case VerticalAlignment.Center:
                        childY = y + (height - childH) / 2 + child.Margin.Top;
                        break;
                    case VerticalAlignment.Bottom:
                        childY = y + height - childH - child.Margin.Bottom;
                        break;
                    case VerticalAlignment.Top:
                    default:
                        childY = y + child.Margin.Top;
                        break;
                }

                child.Arrange(childX, childY, childW, childH);
            }
        }

        public override void Render(IRenderer renderer)
        {
            // Render all children in order (overlay effect)
            foreach (var child in Children.ToArray())
            {
                if (child.IsVisible)
                {
                    child.Render(renderer);
                }
            }
        }
    }

    // =========================================================================
    // CUSTOM TAB HEADER WRAPPER
    // =========================================================================

    /// <summary>
    /// Wraps a custom header element produced by <see cref="TabHeaderTemplate"/>.
    /// Handles tap-to-select via <see cref="IPointerHandler"/> and drag-and-drop
    /// reordering via <see cref="IDraggable"/> / <see cref="IDropTarget"/>.
    /// The accent bar and drop-target outline are painted in
    /// <see cref="OnAfterRender"/> so they appear above the content.
    /// </summary>
    private class CustomTabHeaderWrapper : Frame, IDraggable, IDropTarget, Rayo.Core.Input.IPointerHandler
    {
        private const float CloseButtonOffset = 5f;
        private readonly int _tabIndex;
        private readonly TabControl _owner;
        private readonly DropConstraints _constraints;
        private readonly TabCloseGlyph? _closeGlyph;

        public bool IsDragging { get; set; }
        public bool IsDropTargetActive { get; set; }

        public CustomTabHeaderWrapper(int tabIndex, VisualElement content, TabControl owner)
        {
            _tabIndex = tabIndex;
            _owner = owner;
            _constraints = new DropConstraints().AcceptType("tab").WithEffects(DragDropEffect.Move);

            if (_owner.ShowTabCloseButtons)
            {
                var overlay = new OverlayFrame();
                overlay.HorizontalAlignment = HorizontalAlignment.Stretch;
                overlay.VerticalAlignment = VerticalAlignment.Stretch;
                overlay.AddChild(content);

                _closeGlyph = new TabCloseGlyph(_owner, _tabIndex)
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, CloseButtonOffset, 0),
                    ZIndex = 1
                };
                overlay.AddChild(_closeGlyph);
                Content = overlay;
            }
            else
            {
                Content = content;
            }
            // Headers have explicit sizes; override Frame's default stretch alignment.
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }

        // ─── IPointerHandler (via Frame) ─────────────────────────────────────

        public void OnPointerReleased(PointerEventArgs e)
        {
            // Do not activate when a drag was in progress (the drop handles that).
            var app = UIApplication.Current;
            if (app != null && app.EventManager.DragDrop.IsDragging)
                return;

            if (_owner.ShowTabCloseButtons && IsCloseButtonHit(e.Position))
            {
                _owner.RemoveTab(_tabIndex);
                return;
            }

            if (_tabIndex != _owner.SelectedIndex)
                _owner.SelectedIndex = _tabIndex;
        }

        public void OnPointerEntered(PointerEventArgs e)
        {
            bool isSelected = _owner.SelectedIndex == _tabIndex;
            if (!isSelected && IsEnabled)
            {
                base.Background = _owner.TabHoverBackground;
                MarkNeedsPaint();
            }
        }

        public void OnPointerExited(PointerEventArgs e)
        {
            bool isSelected = _owner.SelectedIndex == _tabIndex;
            base.Background = isSelected ? _owner.TabActiveBackground : _owner.TabBackground;
            MarkNeedsPaint();
        }

        // ─── Render ──────────────────────────────────────────────────────────

        protected override void OnAfterRender(IRenderer renderer)
        {
            bool isSelected = _owner.SelectedIndex == _tabIndex;
            if (isSelected || IsDragging)
            {
                // Accent bar at the top edge (same style as DraggableTabButton).
                renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, 2, _owner.TabAccentColor);
            }

            if (IsDropTargetActive)
            {
                renderer.DrawRectOutline(
                    ComputedX, ComputedY, ComputedWidth, ComputedHeight,
                    2, _owner.TabDropIndicatorColor);
            }

        }

        // ─── IDraggable ──────────────────────────────────────────────────────

        public DragData? OnDragStart(float mouseX, float mouseY)
        {
            if (!_owner.EnableTabReorder) return null;
            var title = _tabIndex < _owner._tabs.Count
                ? _owner._tabs[_tabIndex].Title
                : string.Empty;
            return new DragData("tab", _tabIndex, this)
                .WithAllowedEffects(DragDropEffect.Move)
                .WithMetadata("title", title);
        }

        public void OnDragging(float mouseX, float mouseY)
        {
            if (_owner.EnableTabReorder && IsDragging)
                _owner.HandleDragScroll(mouseX, mouseY);
        }

        public void OnDragEnd(bool wasDropped) => MarkNeedsPaint();

        // ─── IDropTarget ─────────────────────────────────────────────────────

        public bool CanAcceptDataType(string dataType)
            => _owner.EnableTabReorder && dataType == "tab";

        public DropConstraints? Constraints
            => _owner.EnableTabReorder ? _constraints : null;

        public DragDropEffect? AllowedEffects
            => _owner.EnableTabReorder ? DragDropEffect.Move : (DragDropEffect?)null;

        public bool OnDragEnter(DragData dragData)
        {
            if (!_owner.EnableTabReorder) return false;
            if (dragData.Data is not int draggedIndex || draggedIndex == _tabIndex) return false;
            IsDropTargetActive = true;
            UpdateDragHighlight(true);
            MarkNeedsPaint();
            return true;
        }

        public void OnDragOver(DragData dragData, float mouseX, float mouseY)
        {
            if (_owner.EnableTabReorder) _owner.HandleDragScroll(mouseX, mouseY);
        }

        public void OnDragLeave(DragData dragData)
        {
            IsDropTargetActive = false;
            UpdateDragHighlight(false);
            MarkNeedsPaint();
        }

        public bool OnDrop(DragData dragData, float mouseX, float mouseY)
        {
            if (!_owner.EnableTabReorder) return false;
            IsDropTargetActive = false;
            UpdateDragHighlight(false);
            MarkNeedsPaint();
            if (dragData.Data is not int draggedIndex || draggedIndex == _tabIndex) return false;
            float relX = mouseX - ComputedX;
            int targetIndex = relX < ComputedWidth / 2 ? _tabIndex : _tabIndex + 1;
            if (draggedIndex < targetIndex) targetIndex--;
            _owner.ReorderTab(draggedIndex, targetIndex);
            return true;
        }

        private void UpdateDragHighlight(bool isActive)
        {
            if (isActive && _owner.SelectedIndex != _tabIndex)
            {
                base.Background = _owner.TabHoverBackground;
                return;
            }

            bool isSelected = _owner.SelectedIndex == _tabIndex;
            base.Background = isSelected ? _owner.TabActiveBackground : _owner.TabBackground;
        }

        private bool IsCloseButtonHit(System.Numerics.Vector2 position)
        {
            if (!_owner.ShowTabCloseButtons)
                return false;

            if (_closeGlyph != null)
            {
                return position.X >= _closeGlyph.ComputedX && position.X <= _closeGlyph.ComputedX + _closeGlyph.ComputedWidth &&
                       position.Y >= _closeGlyph.ComputedY && position.Y <= _closeGlyph.ComputedY + _closeGlyph.ComputedHeight;
            }

            var bounds = GetCloseButtonBounds();
            return position.X >= bounds.x && position.X <= bounds.x + bounds.size &&
                   position.Y >= bounds.y && position.Y <= bounds.y + bounds.size;
        }

        private (float x, float y, float size) GetCloseButtonBounds()
        {
            float size = _owner.TabCloseButtonHitSize;
            float x = ComputedX + ComputedWidth - size - CloseButtonOffset;
            float y = ComputedY + (ComputedHeight - size) / 2;
            return (x, y, size);
        }
    }

    private sealed class TabCloseGlyph : Rayo.Core.View<TabCloseGlyph>
    {
        private readonly TabControl _owner;
        private readonly int _tabIndex;
        private static readonly IconData CloseIcon = Icons.Close;

        public TabCloseGlyph(TabControl owner, int tabIndex)
        {
            _owner = owner;
            _tabIndex = tabIndex;
            IsInputTransparent = true;
        }

        public override void Measure(float availableWidth, float availableHeight)
        {
            DesiredWidth = _owner.TabCloseButtonHitSize;
            DesiredHeight = _owner.TabCloseButtonHitSize;
        }

        public override void Render(IRenderer renderer)
        {
            var wrapper = Parent?.Parent as CustomTabHeaderWrapper;
            bool isHovered = wrapper?.IsHovered == true;
            bool isSelected = _owner.SelectedIndex == _tabIndex;
            var color = isHovered || isSelected
                ? _owner.TabCloseButtonHoverColor
                : _owner.TabCloseButtonColor;

            float iconSize = Math.Min(_owner.TabCloseButtonSize, Math.Min(ComputedWidth, ComputedHeight));
            float iconX = ComputedX + (ComputedWidth - iconSize) / 2;
            float iconY = ComputedY + (ComputedHeight - iconSize) / 2;
            RenderCloseIcon(renderer, iconX, iconY, iconSize, color);
        }

        private static void RenderCloseIcon(IRenderer renderer, float x, float y, float size, Color color)
        {
            float scaleX = size / CloseIcon.ViewBoxWidth;
            float scaleY = size / CloseIcon.ViewBoxHeight;
            float scale = Math.Min(scaleX, scaleY);

            float offsetX = (size - (CloseIcon.ViewBoxWidth * scale)) / 2;
            float offsetY = (size - (CloseIcon.ViewBoxHeight * scale)) / 2;

            float renderX = x + offsetX;
            float renderY = y + offsetY;

            foreach (var command in CloseIcon.Commands)
            {
                command.Draw(renderer, renderX, renderY, scale, color);
            }
        }
    }

    // =========================================================================
    // DISPOSABLE HELPER
    // =========================================================================

    /// <summary>
    /// Minimal <see cref="IDisposable"/> that executes an action on <see cref="Dispose"/>.
    /// Used to unsubscribe event handlers registered via <see cref="VisualElement.RegisterDisposable"/>.
    /// </summary>
    private sealed class ActionDisposable : IDisposable
    {
        private Action? _action;
        public ActionDisposable(Action action) => _action = action;
        public void Dispose() { _action?.Invoke(); _action = null; }
    }
}

/// <summary>
/// Custom ScrollView that disables built-in drag scrolling to avoid conflict with tab dragging.
/// </summary>
internal class TabHeadersScrollView : ScrollView, IInputHandler
{
    // Explicitly implement IInputHandler to override ScrollView's implementation
    // and disable drag scrolling behavior.
    bool IInputHandler.HandleInput(InputEventArgs args)
    {
        return false;
    }
}

/// <summary>
/// ✅ Botón especializado para scroll de tabs.
/// Hereda de Button para aprovechar la funcionalidad existente.
/// </summary>
internal class TabScrollButton : IconButton, IInputHandler, IPointerHandler
{
    private readonly Action _clickAction;
    private readonly System.Numerics.Vector2 _gradientStart;
    private readonly System.Numerics.Vector2 _gradientEnd;
    private readonly TabControl _owner;

    public TabScrollButton(TabControl owner, IconData iconData, System.Numerics.Vector2 gradientStart, System.Numerics.Vector2 gradientEnd, Action onClick)
    {
        _clickAction = onClick;
        _owner = owner;
        _gradientStart = gradientStart;
        _gradientEnd = gradientEnd;
        IconData = iconData;
        Width = 30;
        Height = 40;
        BorderRadius = new CornerRadius(0);
        Padding = new Thickness(0);

        // Default colors
        Background = new Color(37, 37, 38);
        HoverBackground = new Color(60, 60, 60);
        PressedBackground = new Color(30, 30, 30);
        IconColor = Color.White;
        IconSize = 16;

        UpdateStateColors(Background);
        Tapped += _ => _clickAction();
    }

    public bool CanHandleInput => true;

    bool IInputHandler.HandleInput(InputEventArgs args)
    {
        return false;
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _owner.SetHeaderInputSuppressed(true);
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _owner.SetHeaderInputSuppressed(false);
    }

    public new Color Background
    {
        get => base.Background.PrimaryColor;
        set
        {
            if (!base.Background.PrimaryColor.Equals(value))
            {
                UpdateStateColors(value);
            }
        }
    }

    private void UpdateStateColors(Color baseColor)
    {
        var hoverColor = new Color(
            Math.Min(1f, baseColor.R * 1.5f),
            Math.Min(1f, baseColor.G * 1.5f),
            Math.Min(1f, baseColor.B * 1.5f),
            baseColor.A);

        var pressedColor = new Color(
            baseColor.R * 0.7f,
            baseColor.G * 0.7f,
            baseColor.B * 0.7f,
            baseColor.A);

        base.Background = CreateGradientBrush(baseColor);
        HoverBackground = CreateGradientBrush(hoverColor);
        PressedBackground = CreateGradientBrush(pressedColor);

        MarkNeedsPaint();
    }

    private Rayo.Rendering.Brushes.LinearGradientBrush CreateGradientBrush(Color baseColor)
    {
        var edgeColor = new Color(
            Math.Min(1f, baseColor.R * 1.2f),
            Math.Min(1f, baseColor.G * 1.2f),
            Math.Min(1f, baseColor.B * 1.2f),
            baseColor.A * 0.4f);

        var innerColor = new Color(
            baseColor.R * 0.8f,
            baseColor.G * 0.8f,
            baseColor.B * 0.8f,
            baseColor.A);

        return new Rayo.Rendering.Brushes.LinearGradientBrush(innerColor, edgeColor)
        {
            StartPoint = _gradientStart,
            EndPoint = _gradientEnd
        };
    }

}

/// <summary>
/// Botón de tab con capacidad de arrastre usando el sistema universal de Drag & Drop.
/// ✅ REFACTORIZADO: Hereda de Button para mejor manejo de eventos.
/// </summary>
internal class DraggableTabButton : Button, IDraggable, IDropTarget, IInputHandler
{
    private readonly int _tabIndex;
    private readonly string _tabTitle;
    private readonly TabControl _owner;
    private const float CloseButtonOffset = 5f;
    private static readonly IconData CloseIcon = Icons.Close;
    private readonly DropConstraints _constraints = new DropConstraints()
        .AcceptType("tab")
        .WithEffects(DragDropEffect.Move);

    // IDraggable
    public bool IsDragging { get; set; }

    // IDropTarget
    public bool IsDropTargetActive { get; set; }

    public DraggableTabButton(int tabIndex, string tabTitle, TabControl owner)
    {
        _tabIndex = tabIndex;
        _tabTitle = tabTitle;
        _owner = owner;

        // Button properties
        Text = tabTitle;
        Width = 150;
        Height = 40;
        BorderRadius = new CornerRadius(0);
        Padding = _owner.ShowTabCloseButtons ? new Thickness(10, 0, 30, 0) : new Thickness(10, 0, 10, 0);
        FontSize = 14;
        TextColor = Color.White;

        UpdatePressedBackground(Background);
    }

    public bool CanHandleInput => true;

    public bool HandleInput(InputEventArgs args)
    {
        if (!IsVisible) return false;

        switch (args.EventType)
        {
            case InputEventType.MouseDown:
                IsPressed = true;
                MarkNeedsPaint();
                return true;

            case InputEventType.MouseDrag:
                if (!_owner.EnableTabReorder)
                    return false;

                if (IsPressed)
                {
                    var app = UIApplication.Current;
                    if (app != null)
                    {
                        var dd = app.EventManager.DragDrop;

                        // Try to start drag if not started
                        if (!dd.IsDragging && dd.CurrentDraggable == null)
                        {
                            dd.TryStartDrag(args.Position.X, args.Position.Y);
                        }

                        // Drive the drag process manually since we captured the input
                        if (dd.CurrentDraggable != null)
                        {
                            dd.ProcessMouseMove(args.Position.X, args.Position.Y);

                            if (dd.IsDragging)
                            {
                                IsPressed = false; // Cancel press visual
                                MarkNeedsPaint();
                                return true;
                            }
                        }
                    }
                }
                return true;

            case InputEventType.MouseUp:
                if (IsPressed)
                {
                    IsPressed = false;
                    MarkNeedsPaint();

                    if (_owner.ShowTabCloseButtons && IsCloseButtonHit(args.Position))
                    {
                        _owner.RemoveTab(_tabIndex);
                        return true;
                    }

                    // Only click if not dragging
                    var app = UIApplication.Current;
                    if (app == null || !app.EventManager.DragDrop.IsDragging)
                    {
                    // Create and invoke the Tapped event using object initialization
                        var tapArgs = new Rayo.Core.Input.Gestures.TapGestureEventArgs
                        {
                            Position = args.Position,
                            TapCount = 1,
                            PointerType = Rayo.Core.Input.PointerType.Mouse,
                            Timestamp = DateTime.UtcNow
                        };
                        // Invoke the protected Button event
                        OnTapGesture(tapArgs);
                    }
                    return true;
                }
                break;
        }
        return false;
    }

    public void OnFocusGained() { }
    public void OnFocusLost() { }

    // Helper method to invoke the Tapped event from this derived class
    private void OnTapGesture(TapGestureEventArgs args)
    {
        // Since we can't invoke Tapped directly (it's a Button event),
        // get subscribed handlers using reflection or simply
        // invoke the handlers from the OnTap method we added
        var field = typeof(Button).GetField("Tapped", 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic);
        
        if (field != null)
        {
            var eventDelegate = field.GetValue(this) as Action<TapGestureEventArgs>;
            eventDelegate?.Invoke(args);
        }
    }

    [PaintProperty]
    public new Color Background
    {
        get => base.Background.PrimaryColor;
        set
        {
            if (!base.Background.PrimaryColor.Equals(value))
            {
                base.Background = value;
                UpdatePressedBackground(value);
            }
        }
    }

    private void UpdatePressedBackground(Color baseColor)
    {
        PressedBackground = new Color(
            baseColor.R * 0.8f,
            baseColor.G * 0.8f,
            baseColor.B * 0.8f,
            baseColor.A);
        MarkNeedsPaint();
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);
    }

    public override void Render(IRenderer renderer)
    {
        // Let Button render background and text
        base.Render(renderer);

        // Determine whether this tab is selected
        bool isSelected = _owner.SelectedIndex == _tabIndex;

        // Accent bar at top edge for active tab or while dragging.
        if (isSelected || IsDragging)
        {
            float accentHeight = 2;
            renderer.DrawRect(
                ComputedX,
                ComputedY,
                ComputedWidth,
                accentHeight,
                _owner.TabAccentColor
            );
        }

        // Border if it is an active drop target
        if (IsDropTargetActive)
        {
            renderer.DrawRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight,
            2, _owner.TabDropIndicatorColor);
        }

        if (_owner.ShowTabCloseButtons)
        {
            var bounds = GetCloseButtonBounds();
            var color = IsHovered ? _owner.TabCloseButtonHoverColor : _owner.TabCloseButtonColor;
            float iconSize = Math.Min(_owner.TabCloseButtonSize, bounds.size);
            float iconX = bounds.x + (bounds.size - iconSize) / 2;
            float iconY = bounds.y + (bounds.size - iconSize) / 2;
            RenderCloseIcon(renderer, iconX, iconY, iconSize, color);
        }

        // TODO: Button doesn't have Children - needs architectural refactoring
        /*
        foreach (var child in Children.ToArray())
        {
            if (child.IsVisible)
                child.Render(renderer);
        }
        */
    }

    private (float x, float y, float size) GetCloseButtonBounds()
    {
        float size = _owner.TabCloseButtonHitSize;
        float x = ComputedX + ComputedWidth - size - CloseButtonOffset;
        float y = ComputedY + (ComputedHeight - size) / 2;
        return (x, y, size);
    }

    private bool IsCloseButtonHit(System.Numerics.Vector2 position)
    {
        if (!_owner.ShowTabCloseButtons)
            return false;

        var bounds = GetCloseButtonBounds();
        return position.X >= bounds.x && position.X <= bounds.x + bounds.size &&
               position.Y >= bounds.y && position.Y <= bounds.y + bounds.size;
    }

    private static void RenderCloseIcon(IRenderer renderer, float x, float y, float size, Color color)
    {
        float scaleX = size / CloseIcon.ViewBoxWidth;
        float scaleY = size / CloseIcon.ViewBoxHeight;
        float scale = Math.Min(scaleX, scaleY);

        float offsetX = (size - (CloseIcon.ViewBoxWidth * scale)) / 2;
        float offsetY = (size - (CloseIcon.ViewBoxHeight * scale)) / 2;

        float renderX = x + offsetX;
        float renderY = y + offsetY;

        foreach (var command in CloseIcon.Commands)
        {
            command.Draw(renderer, renderX, renderY, scale, color);
        }
    }

    // IDraggable
    public DragData? OnDragStart(float mouseX, float mouseY)
    {
        if (!_owner.EnableTabReorder)
            return null;

        return new DragData("tab", _tabIndex, this)
            .WithAllowedEffects(DragDropEffect.Move)
            .WithMetadata("title", _tabTitle);
    }

    public void OnDragging(float mouseX, float mouseY)
    {
        if (!_owner.EnableTabReorder)
            return;

        if (IsDragging)
        {
            _owner.HandleDragScroll(mouseX, mouseY);
            MarkNeedsPaint();
        }
    }

    public void OnDragEnd(bool wasDropped)
    {
        if (!_owner.EnableTabReorder)
            return;

        MarkNeedsPaint();
    }

    // IDropTarget
    public bool CanAcceptDataType(string dataType)
        => _owner.EnableTabReorder && dataType == "tab";

    public DropConstraints? Constraints => _owner.EnableTabReorder ? _constraints : null;

    public DragDropEffect? AllowedEffects => _owner.EnableTabReorder ? DragDropEffect.Move : (DragDropEffect?)null;

    public bool OnDragEnter(DragData dragData)
    {
        if (!_owner.EnableTabReorder)
            return false;

        var draggedIndexObj = dragData.Data;
        if (draggedIndexObj is not int draggedIndex || draggedIndex == _tabIndex)
            return false;

        IsDropTargetActive = true;
        UpdateDragHighlight(true);
        MarkNeedsPaint();
        return true;
    }

    public void OnDragOver(DragData dragData, float mouseX, float mouseY)
    {
        if (!_owner.EnableTabReorder)
            return;

        _owner.HandleDragScroll(mouseX, mouseY);
    }

    public void OnDragLeave(DragData dragData)
    {
        if (!_owner.EnableTabReorder)
            return;

        IsDropTargetActive = false;
        UpdateDragHighlight(false);
        MarkNeedsPaint();
    }

    public bool OnDrop(DragData dragData, float mouseX, float mouseY)
    {
        if (!_owner.EnableTabReorder)
            return false;

        IsDropTargetActive = false;
        UpdateDragHighlight(false);
        MarkNeedsPaint();

        var draggedIndexObj = dragData.Data;
        if (draggedIndexObj is not int draggedIndex || draggedIndex == _tabIndex)
            return false;

        float relativeX = mouseX - ComputedX;
        int targetIndex = relativeX < (ComputedWidth / 2) ? _tabIndex : _tabIndex + 1;

        if (draggedIndex < targetIndex)
            targetIndex--;

        _owner.ReorderTab(draggedIndex, targetIndex);
        return true;
    }

    private void UpdateDragHighlight(bool isActive)
    {
        if (isActive && _owner.SelectedIndex != _tabIndex)
        {
            base.Background = _owner.TabHoverBackground;
            return;
        }

        bool isSelected = _owner.SelectedIndex == _tabIndex;
        base.Background = isSelected ? _owner.TabActiveBackground : _owner.TabBackground;
    }
}

