namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Collections.Generic;
using IRenderer = Rayo.Rendering.IRenderer;

/// <summary>
/// Lightweight list item - simpler than Button for better performance.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
public class ListViewItem : Frame, IPointerHandler
{
    private Action? _onTap;
    private System.Numerics.Vector2 _pressPosition;
    private const float TapThreshold = 15f;

    // Internal pressed state management
    private bool _isPressed;

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
        if (!_isPressed)
        {
            Background = _hoverBackground;
        }
    }

    public void OnPointerExited(PointerEventArgs e)
    {
        if (!_isPressed)
        {
            Background = _normalBackground;
        }
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        // Update pressed state based on whether pointer is inside bounds
        if (_isPressed)
        {
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
        _pressPosition = e.Position;
        Background = _pressedBackground.PrimaryColor.A > 0 ? _pressedBackground : _hoverBackground;
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

            if (isInside && distance < TapThreshold)
            {
                _onTap?.Invoke();
            }
        }
        _isPressed = false;
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
    } = new Color(40, 40, 45);
    #endregion

    #region ItemHoverBackground
    public Brush ItemHoverBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(50, 50, 55);
    #endregion

    #region ItemSelectedBackground
    public Brush ItemSelectedBackground
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(59, 130, 246);
    #endregion

    #region ItemHeight
    public float ItemHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 32;
    #endregion

    #region ItemSpacing
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

    #region SelectedIndex
    public int SelectedIndex
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value, RebuildItems))
            {
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
    private VStack _itemsContainer;

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

    public void Scroll(float deltaY)
    {
        _scrollView.Scroll(deltaY);
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
        Items = new List<T>();
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _itemsContainer = new VStack();
        _itemsContainer.Spacing = ItemSpacing;
        _scrollView = new ScrollView();
        _scrollView.Content(_itemsContainer);
        AddChild(_scrollView);
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
        // Guard: evitar llamadas durante la construcción
        if (_itemsContainer == null)
        {
            return;
        }

        _itemsContainer.Spacing(ItemSpacing);
        _itemsContainer.ClearChildren();

        for (int i = 0; i < Items.Count; i++)
        {
            int index = i;
            var item = Items[i];
            var isSelected = i == SelectedIndex;

            var itemBg = isSelected ? ItemSelectedBackground : ItemBackground;
            var hoverBg = isSelected ? ItemSelectedBackground : ItemHoverBackground;

            var label = new Label(DisplayFunc(item));
            label.Foreground = Color.White;
            label.Padding = new Thickness(12, 0);
            label.TextVerticalAlignment = VerticalAlignment.Center;
            label.HorizontalAlignment = HorizontalAlignment.Stretch;
            label.VerticalAlignment = VerticalAlignment.Stretch;

            var listItem = new ListViewItem();
            listItem.NormalBackground = itemBg;
            listItem.HoverBackground = hoverBg;
            listItem.PressedBackground = hoverBg;
            listItem.OnTap(() =>
            {
                SelectedIndex = index;
            });
            listItem.Height(ItemHeight);
            listItem.BorderRadius = new CornerRadius(4);
            listItem.HorizontalAlignment = HorizontalAlignment.Stretch;
            listItem.Content(label);

            _itemsContainer.AddChild(listItem);
        }

        MarkNeedsLayout();
    }

    #endregion

    #region Layout Overrides

    public override void Measure(float availableWidth, float availableHeight)
    {
        // Measure ScrollView with available space minus our padding
        _scrollView.Measure(
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

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        // Arrange ScrollView inside our padding area
        float contentX = x + Padding.Left;
        float contentY = y + Padding.Top;
        float contentWidth = width - Padding.Horizontal;
        float contentHeight = height - Padding.Vertical;

        _scrollView.Arrange(contentX, contentY, contentWidth, contentHeight);
    }

    public override void Render(IRenderer renderer)
    {
        // El scrollview y sus hijos se renderizan automáticamente
    }

    #endregion
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
