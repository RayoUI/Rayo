namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.DevTools;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Collections.Generic;
using IRenderer = Rayo.Rendering.IRenderer;
using Rayo.Styling;

/// <summary>
/// Lightweight list item - simpler than Button for better performance.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
public class ListViewItem : Frame, IPointerHandler
{
    private Action? _onTap;
    private System.Numerics.Vector2 _pressPosition;
    private const float TapThreshold = 15f;
    private const float TouchTapThreshold = 10f;

    // Internal pressed state management
    private bool _isPressed;
    private bool _isTouchPress;

    private Brush _normalBackground = Color.Transparent;
    private Brush _hoverBackground = Color.Transparent;
    private Brush _pressedBackground = Color.Transparent;

    [PaintProperty]
    public Brush NormalBackground
    {
        get => _normalBackground;
        set => this.SetProperty(ref _normalBackground, value, () =>
        {
            _normalBackground = value;
            Background = value;
        });
    }

    [PaintProperty]
    public Brush HoverBackground
    {
        get => _hoverBackground;
        set => this.SetProperty(ref _hoverBackground, value);
    }

    [PaintProperty]
    public Brush PressedBackground
    {
        get => _pressedBackground;
        set => this.SetProperty(ref _pressedBackground, value);
    }

    public ListViewItem OnTap(Action handler)
    {
        _onTap = handler;
        return this;
    }

    public void OnPointerEntered(PointerEventArgs e)
    {
        if (e.PointerType == PointerType.Mouse && !_isPressed)
        {
            Background = _hoverBackground;
        }
    }

    public void OnPointerExited(PointerEventArgs e)
    {
        if (e.PointerType == PointerType.Mouse && !_isPressed)
        {
            Background = _normalBackground;
        }
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        // Update pressed state based on whether pointer is inside bounds
        if (_isPressed)
        {
            if (_isTouchPress)
            {
                var touchDelta = e.Position - _pressPosition;
                float touchDistance = MathF.Sqrt(touchDelta.X * touchDelta.X + touchDelta.Y * touchDelta.Y);
                if (touchDistance >= TouchTapThreshold)
                {
                    _isPressed = false;
                    _isTouchPress = false;
                    Background = _normalBackground;
                }

                return;
            }

            bool isInside = IsPointInside(e.Position);
            var targetBg = isInside
                ? (_pressedBackground.PrimaryColor.A > 0 ? _pressedBackground : _hoverBackground)
                : _normalBackground;
            Background = targetBg;
        }
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        _isPressed = true;
        _isTouchPress = e.PointerType == PointerType.Touch;
        _pressPosition = e.Position;
        if (!_isTouchPress)
        {
            Background = _pressedBackground.PrimaryColor.A > 0 ? _pressedBackground : _hoverBackground;
        }
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (_isPressed)
        {
            // Only invoke tap if:
            // 1. Release is inside the element bounds
            // 2. Distance from press is small (not a drag)
            bool isInside = IsPointInside(e.Position);
            var delta = e.Position - _pressPosition;
            float distance = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
            float threshold = _isTouchPress ? TouchTapThreshold : TapThreshold;

            if (isInside && distance < threshold)
            {
                _onTap?.Invoke();
            }
        }
        _isPressed = false;
        _isTouchPress = false;
        Background = _normalBackground;
    }

    private bool IsPointInside(System.Numerics.Vector2 point)
    {
        return point.X >= ComputedX &&
               point.X <= ComputedX + ComputedWidth &&
               point.Y >= ComputedY &&
               point.Y <= ComputedY + ComputedHeight;
    }
}

/// <summary>
/// Lista de elementos con scroll automático
/// </summary>
public class ListView<T> : Rayo.Core.CompositeView<ListView<T>>, IInputHandler, IScrollable, IDragScrollable
{
    #region Properties

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => field;
        set => this.SetProperty(ref field, value);
    } = Color.Transparent;
    #endregion

    #region ItemBackground
    public Brush ItemBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemHoverBackground
    public Brush ItemHoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemSelectedBackground
    public Brush ItemSelectedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemTextColor
    public Brush ItemTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemSelectedTextColor
    public Brush ItemSelectedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemHeight
    [LayoutProperty]
    public float ItemHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 32;
    #endregion

    #region ItemSpacing
    [LayoutProperty]
    public float ItemSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 4;
    #endregion

    #region DisplayFunc

    [Rayo.Reactivity.NotFluent]
    public Func<T, string> DisplayFunc
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = item => item?.ToString() ?? "";
    #endregion

    #region ItemFactory
    [Rayo.Reactivity.NotFluent]
    public Func<VisualElement>? ItemFactory
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    }
    #endregion

    #region ItemBinder
    [Rayo.Reactivity.NotFluent]
    public Action<VisualElement, T, int, bool>? ItemBinder
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    }
    #endregion

    #region SelectedIndex
    public int SelectedIndex
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                _itemsPanel?.RefreshVisibleItems();
                EnsureSelectedItemVisible();
                if (SelectedItem is T selectedItem)
                    ItemSelected?.Invoke(selectedItem, value);
            }
        }
    } = -1;
    #endregion

    #region SelectedItem
    [Rayo.Reactivity.NotFluent]
    public T? SelectedItem
    {
        get => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : default;
    }
    #endregion

    #region Items
    public IList<T> Items
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    }
    #endregion

    #endregion

    #region Fields

    private ScrollView _scrollView;
    private VirtualizedListPanel<T> _itemsPanel;

    #endregion

    #region Events

    public event Action<T, int>? ItemSelected;

    #endregion

    #region Input/Scroll Delegation

    public bool CanHandleInput => _scrollView.CanHandleInput;

    public bool HandleInput(InputEventArgs args)
    {
        return _scrollView.HandleInput(args);
    }

    public void OnFocusGained()
    {
        _scrollView.OnFocusGained();
    }

    public void OnFocusLost()
    {
        _scrollView.OnFocusLost();
    }

    public float ContentHeight => _scrollView.ContentHeight;

    public float ContentWidth => _scrollView.ContentWidth;

    public float VerticalScrollOffset
    {
        get => _scrollView.VerticalScrollOffset;
        set => _scrollView.VerticalScrollOffset = value;
    }

    public void Scroll(float deltaY)
    {
        _scrollView.Scroll(deltaY);
    }

    public void RefreshVisibleItems()
    {
        _itemsPanel?.RefreshVisibleItems();
    }

    public bool IsDragPending => _scrollView.IsDragPending;

    public void StartDragPending()
    {
        _scrollView.StartDragPending();
    }

    public void CancelDragPending()
    {
        _scrollView.CancelDragPending();
    }

    #endregion

    #region Constructor

    public ListView()
    {
        InitializeTheme();
        Items = new List<T>();
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _scrollView = new ScrollView();
        _itemsPanel = new VirtualizedListPanel<T>(_scrollView);
        _scrollView.Content(_itemsPanel);
        AddChild(_scrollView);
        RebuildItems();
    }

    protected override void OnThemeApplied(Theme theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(ItemBackground), (Brush)palette.Surface, value => ItemBackground = value);
        SetThemeValue(nameof(ItemHoverBackground), (Brush)palette.SurfaceHover, value => ItemHoverBackground = value);
        SetThemeValue(nameof(ItemSelectedBackground), (Brush)palette.Primary, value => ItemSelectedBackground = value);
        SetThemeValue(nameof(ItemTextColor), (Brush)palette.OnSurface, value => ItemTextColor = value);
        SetThemeValue(nameof(ItemSelectedTextColor), (Brush)palette.OnPrimary, value => ItemSelectedTextColor = value);
    }

    #endregion


    #region Item Management

    public ListView<T> AddItem(T item)
    {
        Items = [..Items, item];
        return this;
    }

    public ListView<T> RemoveItem(T item)
    {
        Items = Items.Where(i => !EqualityComparer<T>.Default.Equals(i, item)).ToList();
        return this;
    }

    public ListView<T> ClearItems()
    {
        Items = [];
        return this;
    }

    #endregion

    #region Private Methods

    private void RebuildItems()
    {
        if (_itemsPanel == null)
        {
            return;
        }

        _itemsPanel.Configure(
            Items,
            ItemHeight,
            ItemSpacing,
            CreateListItem,
            BindListItem);

        InvalidateMeasure();
    }

    private VisualElement CreateListItem()
    {
        return ItemFactory?.Invoke() ?? new RecyclableListViewItem();
    }

    private void BindListItem(VisualElement element, int index)
    {
        var item = Items[index];
        var isSelected = index == SelectedIndex;

        if (ItemBinder != null)
        {
            ItemBinder(element, item, index, isSelected);
            return;
        }

        if (element is not RecyclableListViewItem listItem)
            return;

        var itemBg = isSelected ? ItemSelectedBackground : ItemBackground;
        var hoverBg = isSelected ? ItemSelectedBackground : ItemHoverBackground;

        listItem.Bind(
            DisplayFunc(item),
            itemBg,
            hoverBg,
            isSelected ? ItemSelectedTextColor : ItemTextColor,
            ItemHeight,
            () => SelectedIndex = index);
    }

    private void EnsureSelectedItemVisible()
    {
        if (_scrollView == null || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        float itemExtent = Math.Max(1, ItemHeight + ItemSpacing);
        float itemY = SelectedIndex * itemExtent;
        _scrollView.EnsureRectVisible(0, itemY, 1, Math.Max(1, ItemHeight));
    }

    #endregion

    #region Layout Overrides

    protected override void Measure(float availableWidth, float availableHeight)
    {
        // Measure ScrollView with available space minus our padding
        _scrollView.MeasureUpdate(
            availableWidth - Padding.Horizontal,
            availableHeight - Padding.Vertical
        );

        // Calculate desired size including our padding
        if (Width > 0)
        {
            DesiredWidth = Width;
        }
        else
        {
            DesiredWidth = _scrollView.DesiredWidth + Padding.Horizontal;
        }

        if (Height > 0)
        {
            DesiredHeight = Height;
        }
        else
        {
            DesiredHeight = _scrollView.DesiredHeight + Padding.Vertical;
        }

        OnMeasured(DesiredWidth, DesiredHeight);
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Arrange ScrollView inside our padding area
        float contentX = x + Padding.Left;
        float contentY = y + Padding.Top;
        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        _scrollView.ArrangeUpdate(contentX, contentY, contentWidth, contentHeight);
    }

    public override void Render(IRenderer renderer)
    {
        // El scrollview y sus hijos se renderizan automáticamente
    }

    #endregion
}

internal sealed class VirtualizedListPanel<T> : CompositeView<VirtualizedListPanel<T>>
{
    private readonly ScrollView _ownerScrollView;
    private IList<T> _items = Array.Empty<T>();
    private float _itemHeight;
    private float _itemSpacing;
    private Func<VisualElement>? _itemFactory;
    private Action<VisualElement, int>? _itemBinder;
    private readonly Dictionary<int, VisualElement> _activeChildren = new();
    private readonly Stack<VisualElement> _recycledChildren = new();
    private readonly List<VisualElement> _orderedChildrenBuffer = new();
    private int _firstMaterializedIndex = -1;
    private int _lastMaterializedIndex = -1;
    private int _version;
    private int _materializedVersion = -1;
    private const int OverscanItems = 2;

    public VirtualizedListPanel(ScrollView ownerScrollView)
    {
        _ownerScrollView = ownerScrollView;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public void Configure(
        IList<T> items,
        float itemHeight,
        float itemSpacing,
        Func<VisualElement> itemFactory,
        Action<VisualElement, int> itemBinder)
    {
        float previousContentHeight = GetTotalContentHeight();
        _items = items ?? Array.Empty<T>();
        _itemHeight = itemHeight;
        _itemSpacing = itemSpacing;
        _itemFactory = itemFactory;
        _itemBinder = itemBinder;
        _version++;
        _firstMaterializedIndex = -1;
        _lastMaterializedIndex = -1;

        float nextContentHeight = GetTotalContentHeight();
        if (previousContentHeight != nextContentHeight)
            InvalidateMeasure();
        else
            InvalidateArrange();
    }

    public void RefreshVisibleItems()
    {
        if (_itemBinder == null || _activeChildren.Count == 0)
            return;

        foreach (var active in _activeChildren)
        {
            if ((uint)active.Key >= (uint)_items.Count)
                continue;

            _itemBinder(active.Value, active.Key);
        }

        InvalidateArrange();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = float.IsInfinity(availableWidth) || availableWidth <= 0 ? Width : availableWidth;
        DesiredHeight = GetTotalContentHeight();
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        if (_itemFactory == null || _itemBinder == null || _items.Count == 0)
        {
            ClearMaterializedChildren();
            return;
        }

        float itemExtent = GetItemExtent();
        if (itemExtent <= 0)
        {
            ClearMaterializedChildren();
            return;
        }

        float viewportHeight = Math.Max(0, _ownerScrollView.ComputedHeight - _ownerScrollView.Padding.Vertical);
        float scrollOffset = _ownerScrollView.VerticalScrollOffset;

        int firstVisible = Math.Max(0, (int)MathF.Floor(scrollOffset / itemExtent) - OverscanItems);
        int visibleCount = Math.Max(1, (int)MathF.Ceiling(viewportHeight / itemExtent) + OverscanItems * 2);
        int lastVisible = Math.Min(_items.Count - 1, firstVisible + visibleCount - 1);

        if (firstVisible != _firstMaterializedIndex ||
            lastVisible != _lastMaterializedIndex ||
            _materializedVersion != _version)
        {
            MaterializeRange(firstVisible, lastVisible);
        }

        for (int localIndex = 0; localIndex < Children.Count; localIndex++)
        {
            var child = Children[localIndex];
            int itemIndex = _firstMaterializedIndex + localIndex;
            float itemY = y + itemIndex * itemExtent;
            float childHeight = Math.Max(0, _itemHeight);
            child.ArrangeUpdate(x, itemY, width, childHeight);
        }
    }

    public override void Render(IRenderer renderer)
    {
    }

    private void MaterializeRange(int firstIndex, int lastIndex)
    {
        bool requiresRebind = _materializedVersion != _version;
        bool rangeUnchanged = firstIndex == _firstMaterializedIndex && lastIndex == _lastMaterializedIndex;

        if (!requiresRebind &&
            !rangeUnchanged &&
            _firstMaterializedIndex != -1 &&
            TryUpdateRangeInPlace(firstIndex, lastIndex))
        {
            _firstMaterializedIndex = firstIndex;
            _lastMaterializedIndex = lastIndex;
            _materializedVersion = _version;
            return;
        }

        _orderedChildrenBuffer.Clear();

        foreach (var active in _activeChildren.ToArray())
        {
            if (active.Key >= firstIndex && active.Key <= lastIndex)
                continue;

            active.Value.Parent = null;
            _activeChildren.Remove(active.Key);
            _recycledChildren.Push(active.Value);
            PerformanceTracker.RecordVirtualizedRecycled();
        }

        for (int index = firstIndex; index <= lastIndex; index++)
        {
            bool isNewChild = false;

            if (!_activeChildren.TryGetValue(index, out var child))
            {
                bool reused = _recycledChildren.Count > 0;
                child = reused ? _recycledChildren.Pop() : _itemFactory!();
                _activeChildren[index] = child;
                isNewChild = true;
                if (reused)
                    PerformanceTracker.RecordVirtualizedReused();
                else
                    PerformanceTracker.RecordVirtualizedCreated();
            }

            if (isNewChild || requiresRebind)
            {
                _itemBinder!(child, index);
                PerformanceTracker.RecordVirtualizedRebound();
            }

            _orderedChildrenBuffer.Add(child);
        }

        if (!rangeUnchanged)
            Children = [.. _orderedChildrenBuffer];

        _firstMaterializedIndex = firstIndex;
        _lastMaterializedIndex = lastIndex;
        _materializedVersion = _version;
    }

    private bool TryUpdateRangeInPlace(int firstIndex, int lastIndex)
    {
        if (Children.Count == 0)
            return false;

        int oldFirst = _firstMaterializedIndex;
        int oldLast = _lastMaterializedIndex;
        if (oldFirst == -1 || oldLast == -1)
            return false;

        int overlapFirst = Math.Max(firstIndex, oldFirst);
        int overlapLast = Math.Min(lastIndex, oldLast);
        if (overlapFirst > overlapLast)
            return false;

        bool structureChanged = false;

        while (_firstMaterializedIndex < firstIndex && Children.Count > 0)
        {
            var removed = Children[0];
            Children.RemoveAt(0);
            removed.Parent = null;
            _activeChildren.Remove(_firstMaterializedIndex);
            _recycledChildren.Push(removed);
            PerformanceTracker.RecordVirtualizedRecycled();
            _firstMaterializedIndex++;
            structureChanged = true;
        }

        while (_lastMaterializedIndex > lastIndex && Children.Count > 0)
        {
            var removed = Children[^1];
            Children.RemoveAt(Children.Count - 1);
            removed.Parent = null;
            _activeChildren.Remove(_lastMaterializedIndex);
            _recycledChildren.Push(removed);
            PerformanceTracker.RecordVirtualizedRecycled();
            _lastMaterializedIndex--;
            structureChanged = true;
        }

        for (int index = oldFirst - 1; index >= firstIndex; index--)
        {
            bool reused = _recycledChildren.Count > 0;
            var child = reused ? _recycledChildren.Pop() : _itemFactory!();
            _activeChildren[index] = child;
            if (child.Parent != this)
                child.Parent = this;
            _itemBinder!(child, index);
            if (reused)
                PerformanceTracker.RecordVirtualizedReused();
            else
                PerformanceTracker.RecordVirtualizedCreated();
            PerformanceTracker.RecordVirtualizedRebound();
            Children.Insert(0, child);
            structureChanged = true;
        }

        for (int index = oldLast + 1; index <= lastIndex; index++)
        {
            bool reused = _recycledChildren.Count > 0;
            var child = reused ? _recycledChildren.Pop() : _itemFactory!();
            _activeChildren[index] = child;
            if (child.Parent != this)
                child.Parent = this;
            _itemBinder!(child, index);
            if (reused)
                PerformanceTracker.RecordVirtualizedReused();
            else
                PerformanceTracker.RecordVirtualizedCreated();
            PerformanceTracker.RecordVirtualizedRebound();
            Children.Add(child);
            structureChanged = true;
        }

        if (structureChanged)
            RaiseTreeStructureChanged(this);

        return true;
    }

    private void ClearMaterializedChildren()
    {
        if (Children.Count == 0 && _firstMaterializedIndex == -1 && _lastMaterializedIndex == -1)
            return;

        foreach (var child in Children)
        {
            child.Parent = null;
            _recycledChildren.Push(child);
        }

        Children = [];
        _activeChildren.Clear();
        _firstMaterializedIndex = -1;
        _lastMaterializedIndex = -1;
        _materializedVersion = _version;
    }

    private float GetItemExtent()
    {
        return Math.Max(1, _itemHeight + _itemSpacing);
    }

    private float GetTotalContentHeight()
    {
        if (_items.Count == 0)
            return 0;

        return _items.Count * _itemHeight + Math.Max(0, _items.Count - 1) * _itemSpacing;
    }
}

internal sealed class RecyclableListViewItem : ListViewItem
{
    private readonly Label _label;

    public RecyclableListViewItem()
    {
        _label = new Label();
        _label.Foreground = Color.Transparent;
        _label.Padding = new Thickness(12, 0);
        _label.TextVerticalAlignment = VerticalAlignment.Center;
        _label.HorizontalAlignment = HorizontalAlignment.Stretch;
        _label.VerticalAlignment = VerticalAlignment.Stretch;

        BorderRadius = new CornerRadius(4);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Content = _label;
    }

    public void Bind(string text, Brush normalBackground, Brush hoverBackground, Brush textColor, float height, Action onTap)
    {
        _label.Text(text);
        _label.Foreground = textColor;
        NormalBackground = normalBackground;
        HoverBackground = hoverBackground;
        PressedBackground = hoverBackground;
        Height = height;
        OnTap(onTap);
    }
}

/// <summary>
/// Extension methods for ListView to support fluent API with DisplayFunc.
/// Manual implementation to avoid conflicts with tuple types in source generator.
/// </summary>
public static class ListViewExtensions
{
    /// <summary>
    /// Sets the display function for formatting list items.
    /// </summary>
    public static ListView<T> WithDisplayFunc<T>(this ListView<T> listView, Func<T, string> displayFunc)
    {
        listView.DisplayFunc = displayFunc;
        return listView;
    }
}
