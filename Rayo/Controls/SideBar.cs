namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// Represents an item in the SideBar navigation
/// </summary>
public class SideBarItem
{
    public string Text { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public Action? Selected { get; set; }
    public bool IsEnabled { get; set; } = true;
    public object? Tag { get; set; }

    public SideBarItem() { }

    public SideBarItem(string text, string icon = "", string? key = null)
    {
        Text = text;
        Icon = icon;
        Key = key ?? text;
    }

    public SideBarItem(string text, string icon, Action onSelected)
    {
        Text = text;
        Icon = icon;
        Key = text;
        Selected = onSelected;
    }
}

/// <summary>
/// SideBar - A fixed navigation sidebar with collapsible support.
/// </summary>
public class SideBar : Rayo.Core.CompositeView<SideBar>
{
    private Frame? _container;
    private VStack? _itemsContainer;
    private VStack? _headerContainer;
    private VStack? _footerContainer;
    private readonly List<SideBarItem> _items = new();

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    #region ExpandedWidth
    [LayoutProperty]
    public float ExpandedWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateWidth);
    } = 240;
    #endregion

    #region CollapsedWidth
    public float CollapsedWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateWidth);
    } = 60;
    #endregion

    #region ItemHeight
    public float ItemHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 44;
    #endregion

    #region ItemSpacing
    public float ItemSpacing
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value) && _itemsContainer != null)
            {
                _itemsContainer.Spacing(value);
            }
        }
    } = 4;
    #endregion

    #region Background
    [PaintProperty]
    public new Brush Background
    {
        get => base.Background;
        set
        {
            base.Background = value;
            _container?.Background(value);
        }
    }
    #endregion

    #region ItemBackground
    public Rendering.Brushes.Brush ItemBackground
    {
        get => field;
        set
        {
            field = value;
            RebuildItems();
        }
    } = Color.Transparent;
    #endregion

    #region ItemHoverColor
    public Brush ItemHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(45, 48, 58);
    #endregion

    #region ItemSelectedColor
    public Brush ItemSelectedColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(59, 130, 246);
    #endregion

    #region ItemTextColor
    public Brush ItemTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(180, 185, 195);
    #endregion

    #region ItemSelectedTextColor
    public Brush ItemSelectedTextColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.White;
    #endregion

    #region ItemIconColor
    public Brush ItemIconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Color(140, 145, 160);
    #endregion

    #region ItemSelectedIconColor
    public Brush ItemSelectedIconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.White;
    #endregion

    #region BorderColor
    public Brush BorderColor
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                _container?.BorderColor(value.PrimaryColor);
                MarkNeedsPaint();
            }
        }
    } = new Color(50, 55, 65);
    #endregion

    #region BorderWidth
    public float BorderWidth
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                _container?.BorderWidth(value);
                MarkNeedsPaint();
            }
        }
    } = 1;
    #endregion

    #region ItemBorderRadius
    public float ItemBorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 8;
    #endregion

    #region ItemPadding
    public Thickness ItemPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Thickness(12, 10, 12, 10);
    #endregion

    #region IconSize
    public float IconSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 18;
    #endregion

    #region FontSize
    public float FontSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 14;
    #endregion


    #region SelectedKey
    public string SelectedKey
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                SelectionChanged?.Invoke(field);
                RebuildItems();
            }
        }
    } = string.Empty;
    #endregion

    #region IsCollapsed
    public bool IsCollapsed
    {
        get => field;
        set
        {
            if (this.SetProperty(ref field, value))
            {
                CollapsedChanged?.Invoke(field);
                UpdateWidth();
                RebuildItems();
            }
        }
    }
    #endregion

    // =========================================================================
    // EVENTS
    // =========================================================================

    public event Action<string>? SelectionChanged;
    public event Action<bool>? CollapsedChanged;

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    public SideBar()
    {
        Background = new Color(25, 27, 35);
        Width = ExpandedWidth;
        BuildComponents();
    }

    private void BuildComponents()
    {
        _headerContainer = new VStack()
            .Spacing(8)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        _itemsContainer = new VStack()
            .Spacing(ItemSpacing)
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        _footerContainer = new VStack()
            .Spacing(8)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var mainLayout = new VStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                _headerContainer,
                new ScrollView()
                    .Content(_itemsContainer)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                _footerContainer
            );

        _container = new Frame()
            .Background(Background)
            .BorderColor(BorderColor.PrimaryColor)
            .BorderWidth(BorderWidth)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(mainLayout);

        AddChild(_container);
    }


    // Fluent API manual methods

    public SideBar ItemColors(Color background, Color hover, Color selected)
    {
        ItemBackground = background;
        ItemHoverColor = hover;
        ItemSelectedColor = selected;
        return this;
    }

    public SideBar TextColors(Color normal, Color selected)
    {
        ItemTextColor = normal;
        ItemSelectedTextColor = selected;
        return this;
    }

    public SideBar IconColors(Color normal, Color selected)
    {
        ItemIconColor = normal;
        ItemSelectedIconColor = selected;
        return this;
    }


    public SideBar AddItem(SideBarItem item)
    {
        _items.Add(item);
        RebuildItems();
        return this;
    }

    public SideBar AddItem(string text, string icon = "", Action? onSelected = null)
    {
        var item = new SideBarItem(text, icon) { Selected = onSelected };
        return AddItem(item);
    }

    public SideBar AddItems(params SideBarItem[] items)
    {
        _items.AddRange(items);
        RebuildItems();
        return this;
    }

    public SideBar Items(IEnumerable<SideBarItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        RebuildItems();
        return this;
    }

    public SideBar ClearItems()
    {
        _items.Clear();
        RebuildItems();
        return this;
    }

    public SideBar Header(VisualElement header)
    {
        if (_headerContainer != null)
        {
            _headerContainer.ClearChildren();
            _headerContainer.AddChild(header);
        }
        return this;
    }

    public SideBar Footer(VisualElement footer)
    {
        if (_footerContainer != null)
        {
            _footerContainer.ClearChildren();
            _footerContainer.AddChild(footer);
        }
        return this;
    }

    public SideBar AddCollapseToggle()
    {
        var toggleButton = new IconButton()
            .IconData(IsCollapsed ? Icons.ChevronRight : Icons.ChevronLeft)
            .Background(Color.Transparent)
            .HoverBackground(ItemHoverColor.PrimaryColor)
            .BorderWidth(0)
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Right);

        toggleButton.OnTapped(() =>
        {
            IsCollapsed = !IsCollapsed;
            toggleButton.IconData(IsCollapsed ? Icons.ChevronRight : Icons.ChevronLeft);
        });

        if (_headerContainer != null)
        {
            _headerContainer.Padding(new Thickness(8));
            _headerContainer.AddChild(toggleButton);
        }

        return this;
    }

    private void UpdateWidth()
    {
        Width = IsCollapsed ? CollapsedWidth : ExpandedWidth;
        MarkNeedsLayout();
    }

    private void RebuildItems()
    {
        if (_itemsContainer == null) return;

        _itemsContainer.ClearChildren();

        foreach (var item in _items)
        {
            var itemButton = CreateItemButton(item);
            _itemsContainer.AddChild(itemButton);
        }

        MarkNeedsLayout();
    }

    private VisualElement CreateItemButton(SideBarItem item)
    {
        bool isSelected = item.Key == SelectedKey;

        var bgColor = isSelected ? ItemSelectedColor : ItemBackground;
        var textColor = isSelected ? ItemSelectedTextColor : ItemTextColor;
        var iconColor = isSelected ? ItemSelectedIconColor : ItemIconColor;

        if (IsCollapsed)
        {
            // Collapsed mode: show only icon
            var iconText = new Label()
                .Text(item.Icon)
                .Foreground(iconColor.PrimaryColor)
                .FontSize(IconSize)
                .SetInputTransparent(true);

            HStack iconStack = new HStack();
            iconStack.Alignment(Alignment.Center);
            iconStack.HorizontalAlignment(HorizontalAlignment.Stretch);
            iconStack.VerticalAlignment(VerticalAlignment.Stretch);
            iconStack.SetInputTransparent(true);
            iconStack.AddChild(iconText);

            var button = new Frame()
                .Background(bgColor)
                .Height(ItemHeight)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .SetInputTransparent(true)
                .BorderRadius(ItemBorderRadius)
                .Content(iconStack);

            return new SideBarItemButton(this, item, bgColor, ItemHoverColor.PrimaryColor)
                .Height(ItemHeight)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Content(button);
        }
        else
        {
            // Expanded mode: show icon and text
            HStack content = new HStack();
            content.Spacing(12);
            content.Alignment(Alignment.Center);
            content.VerticalAlignment(VerticalAlignment.Center);
            content.SetInputTransparent(true);

            if (!string.IsNullOrEmpty(item.Icon))
            {
                content.AddChild(
                    new Label()
                        .Text(item.Icon)
                        .Foreground(iconColor.PrimaryColor)
                        .FontSize(IconSize)
                        .SetInputTransparent(true)
                );
            }

            content.AddChild(
                new Label()
                    .Text(item.Text)
                    .Foreground(textColor.PrimaryColor)
                    .FontSize(FontSize)
                    .SetInputTransparent(true)
            );

            var button = new Frame()
                .Background(bgColor)
                .Padding(ItemPadding)
                .Height(ItemHeight)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .SetInputTransparent(true)
                .BorderRadius(ItemBorderRadius)
                .Content(content);

            return new SideBarItemButton(this, item, bgColor, ItemHoverColor.PrimaryColor)
                .Height(ItemHeight)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Content(button);
        }
    }

    internal void SelectItem(SideBarItem item)
    {
        if (!item.IsEnabled) return;

        SelectedKey = item.Key;
        item.Selected?.Invoke();
    }

    public override void Measure(float availableWidth, float availableHeight)
    {
        float w = IsCollapsed ? CollapsedWidth : ExpandedWidth;

        if (_container != null)
        {
            _container.Measure(w, availableHeight);
        }

        DesiredWidth = w;
        DesiredHeight = availableHeight;
    }

    public override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float w = IsCollapsed ? CollapsedWidth : ExpandedWidth;

        if (_container != null)
        {
            _container.Arrange(x, y, w, height);
        }
    }

    public override void Render(IRenderer renderer)
    {
        // Render all children (the container Frame)
        foreach (var child in Children.ToArray())
        {
            if (child.IsVisible)
            {
                child.Render(renderer);
            }
        }
    }
}

/// <summary>
/// Internal button for sidebar items with hover and press support.
/// Uses IPointerHandler for modern pointer event handling.
/// </summary>
internal class SideBarItemButton : Frame, Rayo.Core.Input.IPointerHandler
{
    private readonly SideBar _sideBar;
    private readonly SideBarItem _item;
    private readonly Rendering.Brushes.Brush _normalColor;
    private readonly Color _hoverColor;
    private Frame? _innerFrame;

    // Internal state management
    private bool _isPressed;

    public SideBarItemButton(SideBar sideBar, SideBarItem item, Rendering.Brushes.Brush normalColor, Color hoverColor)
    {
        _sideBar = sideBar;
        _item = item;
        _normalColor = normalColor;
        _hoverColor = hoverColor;
    }

    public new SideBarItemButton Content(VisualElement child)
    {
        base.Content = child;
        
        if (child is Frame frame)
        {
            _innerFrame = frame;
        }
        return this;
    }

    private void OnMouseEnter()
    {
        _innerFrame?.Background(_hoverColor);
        MarkNeedsPaint();
    }

    private void OnMouseLeave()
    {
        _innerFrame?.Background(_normalColor);
        MarkNeedsPaint();
    }

    // =========================================================================
    // IPOINTERHANDLER IMPLEMENTATION
    // =========================================================================

    void Rayo.Core.Input.IPointerHandler.OnPointerEntered(Rayo.Core.Input.PointerEventArgs e)
    {
        OnMouseEnter();
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerExited(Rayo.Core.Input.PointerEventArgs e)
    {
        OnMouseLeave();
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerPressed(Rayo.Core.Input.PointerEventArgs e)
    {
        _isPressed = true;
        MarkNeedsPaint();
    }

    void Rayo.Core.Input.IPointerHandler.OnPointerReleased(Rayo.Core.Input.PointerEventArgs e)
    {
        bool isInsideBounds = e.Position.X >= ComputedX && e.Position.X <= ComputedX + ComputedWidth &&
                              e.Position.Y >= ComputedY && e.Position.Y <= ComputedY + ComputedHeight;
        
        if (_isPressed && isInsideBounds)
        {
            _sideBar.SelectItem(_item);
        }
        _isPressed = false;
        MarkNeedsPaint();
    }
}
