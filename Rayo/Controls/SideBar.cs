namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

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
public class SideBar : BorderCompositeView<SideBar>
{
    private Frame? _container;
    private VStack? _itemsContainer;
    private VStack? _headerContainer;
    private VStack? _footerContainer;
    private ButtonIcon? _collapseToggleButton;
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
    [LayoutProperty]
    public float CollapsedWidth
    {
        get => field;
        set => this.SetProperty(ref field, value, UpdateWidth);
    } = 60;
    #endregion

    #region ItemHeight
    [LayoutProperty]
    public float ItemHeight
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 44;
    #endregion

    #region ItemSpacing
    [LayoutProperty]
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
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemHoverColor
    public Brush ItemHoverColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemSelectedColor
    public Brush ItemSelectedColor
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

    #region ItemIconColor
    public Brush ItemIconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemSelectedIconColor
    public Brush ItemSelectedIconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = Color.Transparent;
    #endregion

    #region ItemBorderRadius
    public float ItemBorderRadius
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 8;
    #endregion

    #region ItemPadding
    [LayoutProperty]
    public Thickness ItemPadding
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = new Thickness(12, 10, 12, 10);
    #endregion

    #region IconSize
    [LayoutProperty]
    public float IconSize
    {
        get => field;
        set => this.SetProperty(ref field, value, RebuildItems);
    } = 18;
    #endregion

    #region FontSize
    [LayoutProperty]
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
        InitializeTheme();
        Width = ExpandedWidth;
        BuildComponents();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        var palette = theme.Colors;
        SetThemeValue(nameof(Background), (Brush)palette.Background, value => Background = value);
        SetThemeValue(nameof(ItemBackground), (Brush)Color.Transparent, value => ItemBackground = value);
        SetThemeValue(nameof(ItemHoverColor), (Brush)palette.SurfaceHover, value => ItemHoverColor = value);
        SetThemeValue(nameof(ItemSelectedColor), (Brush)palette.Primary, value => ItemSelectedColor = value);
        SetThemeValue(nameof(ItemTextColor), (Brush)palette.OnSurface, value => ItemTextColor = value);
        SetThemeValue(nameof(ItemSelectedTextColor), (Brush)palette.OnPrimary, value => ItemSelectedTextColor = value);
        SetThemeValue(nameof(ItemIconColor), (Brush)palette.OnSurface, value => ItemIconColor = value);
        SetThemeValue(nameof(ItemSelectedIconColor), (Brush)palette.OnPrimary, value => ItemSelectedIconColor = value);
        SetThemeValue(nameof(BorderBrush), (Brush)palette.Border, value => BorderBrush = value);
        if (_collapseToggleButton != null)
        {
            ApplyCollapseToggleTheme(palette);
        }
        RefreshItemVisuals();
    }

    protected override void OnBorderBrushChanged()
    {
        base.OnBorderBrushChanged();
        _container?.BorderBrush(BorderBrush.PrimaryColor);
    }

    protected override void OnBorderThicknessChanged()
    {
        base.OnBorderThicknessChanged();
        _container?.BorderThickness(BorderThickness);
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
            .BorderBrush(BorderBrush.PrimaryColor)
            .BorderThickness(BorderThickness)
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
        var toggleButton = new ButtonIcon()
            .IconData(IsCollapsed ? Icons.ChevronRight : Icons.ChevronLeft)
            .BorderThickness(0)
            .Padding(new Thickness(8))
            .HorizontalAlignment(HorizontalAlignment.Right);

        toggleButton.OnTapped(() =>
        {
            IsCollapsed = !IsCollapsed;
            toggleButton.IconData(IsCollapsed ? Icons.ChevronRight : Icons.ChevronLeft);
        });

        if (_headerContainer != null)
        {
            _collapseToggleButton = toggleButton;
            ApplyCollapseToggleTheme(EffectiveTheme.Colors);
            _headerContainer.Padding(new Thickness(8));
            _headerContainer.AddChild(toggleButton);
        }

        return this;
    }

    private void ApplyCollapseToggleTheme(ColorScheme palette)
    {
        if (_collapseToggleButton == null)
        {
            return;
        }

        _collapseToggleButton.Background = palette.SurfaceHover;
        _collapseToggleButton.HoverBackground = palette.Primary.WithAlpha(0.16f);
        _collapseToggleButton.PressedBackground = palette.Primary.WithAlpha(0.24f);
        _collapseToggleButton.IconColor = palette.OnSurface;
        _collapseToggleButton.BorderBrush = palette.Border;
    }

    private void UpdateWidth()
    {
        Width = IsCollapsed ? CollapsedWidth : ExpandedWidth;
        InvalidateMeasure();
    }

    private void RefreshItemVisuals()
    {
        if (_itemsContainer == null)
            return;

        foreach (var itemButton in _itemsContainer.GetChildren().OfType<SideBarItemButton>())
            itemButton.RefreshVisuals();
    }

    private void RebuildItems()
    {
        if (_itemsContainer == null) return;

        _itemsContainer.ClearChildren();

        foreach (var item in _items)
        {
            var itemButton = CreateItemButton(item);
            _itemsContainer.AddChild(itemButton);
            // Adding the subtree lets Label apply its own semantic foreground.
            // Re-apply the SideBar-specific state afterwards so ItemTextColor and
            // ItemIconColor remain the final component-theme values.
            itemButton.RefreshVisuals();
        }

        InvalidateMeasure();
    }

    private SideBarItemButton CreateItemButton(SideBarItem item)
    {
        if (IsCollapsed)
        {
            // Collapsed mode: show only icon
            var iconText = new Label()
                .Text(item.Icon)
                .FontSize(IconSize)
                .Width(CollapsedWidth)
                .Height(ItemHeight)
                .TextVerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .SetInputTransparent(true);
            iconText.TextHorizontalAlignment = HorizontalAlignment.Center;

            var button = new Frame()
                .Height(ItemHeight)
                .Padding(new Thickness(0))
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .SetInputTransparent(true)
                .BorderRadius(ItemBorderRadius)
                .Content(iconText);

            var itemButton = new SideBarItemButton(
                this,
                item,
                button,
                textLabel: null,
                iconLabel: iconText)
            {
                Height = ItemHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            itemButton.Content(button);
            return itemButton;
        }
        else
        {
            // Expanded mode: show icon and text
            HStack content = new HStack();
            content.Spacing(12);
            content.Alignment(Alignment.Center);
            content.VerticalAlignment(VerticalAlignment.Center);
            content.SetInputTransparent(true);

            Label? iconLabel = null;
            if (!string.IsNullOrEmpty(item.Icon))
            {
                iconLabel = new Label()
                    .Text(item.Icon)
                    .FontSize(IconSize)
                    .SetInputTransparent(true);
                content.AddChild(iconLabel);
            }

            var textLabel = new Label()
                .Text(item.Text)
                .FontSize(FontSize)
                .SetInputTransparent(true);
            content.AddChild(textLabel);

            var button = new Frame()
                .Padding(ItemPadding)
                .Height(ItemHeight)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .SetInputTransparent(true)
                .BorderRadius(ItemBorderRadius)
                .Content(content);

            var itemButton = new SideBarItemButton(this, item, button, textLabel, iconLabel)
            {
                Height = ItemHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            itemButton.Content(button);
            return itemButton;
        }
    }

    internal void SelectItem(SideBarItem item)
    {
        if (!item.IsEnabled) return;

        SelectedKey = item.Key;
        item.Selected?.Invoke();
    }

    protected override void Measure(float availableWidth, float availableHeight)
    {
        float w = IsCollapsed ? CollapsedWidth : ExpandedWidth;

        if (_container != null)
        {
            _container.MeasureUpdate(w, availableHeight);
        }

        DesiredWidth = w;
        DesiredHeight = availableHeight;
    }

    protected override void Arrange(float x, float y, float width, float height)
    {
        base.Arrange(x, y, width, height);

        float w = IsCollapsed ? CollapsedWidth : ExpandedWidth;

        if (_container != null)
        {
            _container.ArrangeUpdate(x, y, w, height);
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
    private readonly Label? _textLabel;
    private readonly Label? _iconLabel;
    private Frame? _innerFrame;

    // Internal state management
    private bool _isPressed;
    private bool _isHovered;

    public SideBarItemButton(
        SideBar sideBar,
        SideBarItem item,
        Frame innerFrame,
        Label? textLabel,
        Label? iconLabel)
    {
        _sideBar = sideBar;
        _item = item;
        _innerFrame = innerFrame;
        _textLabel = textLabel;
        _iconLabel = iconLabel;
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
        _isHovered = true;
        RefreshVisuals();
    }

    private void OnMouseLeave()
    {
        _isHovered = false;
        _isPressed = false;
        RefreshVisuals();
    }

    internal void RefreshVisuals()
    {
        var state = ControlState.Normal;
        if (_item.Key == _sideBar.SelectedKey) state |= ControlState.Selected;
        if (_isHovered) state |= ControlState.Hovered;
        if (_isPressed) state |= ControlState.Pressed;
        if (!_item.IsEnabled) state |= ControlState.Disabled;

        if (_innerFrame != null)
        {
            _innerFrame.Background = new StateMap<Brush>(_sideBar.ItemBackground)
                .With(ControlState.Hovered, _sideBar.ItemHoverColor)
                .With(ControlState.Pressed, _sideBar.ItemHoverColor)
                .With(ControlState.Selected, _sideBar.ItemSelectedColor)
                .Resolve(state);
        }

        if (_textLabel != null)
        {
            _textLabel.Foreground = new StateMap<Brush>(_sideBar.ItemTextColor)
                .With(ControlState.Selected, _sideBar.ItemSelectedTextColor)
                .With(ControlState.Disabled, (Brush)_sideBar.EffectiveTheme.Colors.OnDisabled)
                .Resolve(state);
        }

        if (_iconLabel != null)
        {
            _iconLabel.Foreground = new StateMap<Brush>(_sideBar.ItemIconColor)
                .With(ControlState.Selected, _sideBar.ItemSelectedIconColor)
                .With(ControlState.Disabled, (Brush)_sideBar.EffectiveTheme.Colors.OnDisabled)
                .Resolve(state);
        }
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
        RefreshVisuals();
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
        RefreshVisuals();
    }
}
