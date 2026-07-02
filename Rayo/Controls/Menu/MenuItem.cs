namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;
using Rayo.Styling;

public readonly record struct MenuItemIconOptions(IconData Icon, Brush Color, float? Size = null, float? Spacing = null);

/// <summary>
/// An action, checked option, or nested submenu entry within a <see cref="Menu"/>.
/// </summary>
public class MenuItem : Component
{
    private readonly string _text;
    private readonly Action? _onClick;
    private readonly List<MenuItem> _items = [];
    private Func<bool>? _isChecked;

    [LayoutProperty]
    public HorizontalAlignment TextAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = HorizontalAlignment.Left;

    public IconData? IconData
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    }

    public Brush IconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = Color.Transparent;

    public float IconSize
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = 14f;

    public float IconSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = 8f;

    public MenuItemIconOptions? IconOptions
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (value.HasValue)
            {
                var options = value.Value;
                IconData = options.Icon;
                IconColor = options.Color;
                if (options.Size.HasValue) IconSize = options.Size.Value;
                if (options.Spacing.HasValue) IconSpacing = options.Spacing.Value;
            }
            else
            {
                IconData = null;
                IconColor = Color.Transparent;
            }

            Rebuild();
        });
    }

    internal string Text => _text;
    internal IReadOnlyList<MenuItem> Items => _items;
    internal bool HasSubmenu => _items.Count > 0;
    internal bool IsChecked => _isChecked?.Invoke() == true;

    public MenuItem(string text, Action? onClick = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _text = text;
        _onClick = onClick;
    }

    /// <summary>Adds a child entry and turns this item into a submenu.</summary>
    public MenuItem AddItem(MenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
        return this;
    }

    /// <summary>Displays a check icon while the supplied state evaluates to true.</summary>
    public MenuItem CheckedWhen(Func<bool> isChecked)
    {
        _isChecked = isChecked ?? throw new ArgumentNullException(nameof(isChecked));
        return this;
    }

    internal void Invoke() => _onClick?.Invoke();

    public override VisualElement Build() =>
        BuildEntry(
            onHovered: null,
            onActivated: (item, _) =>
            {
                if (!item.HasSubmenu)
                    item.Invoke();
            });

    internal VisualElement BuildEntry(
        Action<MenuItem, VisualElement>? onHovered,
        Action<MenuItem, VisualElement>? onActivated)
    {
        return new MenuEntryView(this, onHovered, onActivated);
    }
}

/// <summary>
/// Interactive menu row with reserved icon and submenu columns.
/// </summary>
internal sealed class MenuEntryView : Frame, IPointerHandler
{
    private readonly MenuItem _item;
    private readonly Action<MenuItem, VisualElement>? _onHovered;
    private readonly Action<MenuItem, VisualElement>? _onActivated;
    private bool _isHovered;
    private bool _isPressed;

    public MenuEntryView(
        MenuItem item,
        Action<MenuItem, VisualElement>? onHovered,
        Action<MenuItem, VisualElement>? onActivated)
    {
        _item = item;
        _onHovered = onHovered;
        _onActivated = onActivated;

        Height = 30;
        Padding = new Thickness(7, 0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;

        var leading = BuildLeadingIcon(item);
        var label = new Label(item.Text)
            .FontSize(12)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .SetInputTransparent(true);
        var trailing = BuildTrailingIcon(item);

        Content = new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Pixels(22), GridLength.Star, GridLength.Pixels(18))
            .ColumnSpacing(item.IconSpacing)
            .SetInputTransparent(true)
            .AddChild(leading, 0, 0)
            .AddChild(label, 0, 1)
            .AddChild(trailing, 0, 2);

        InitializeTheme();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        var background = _isPressed
            ? theme.Colors.SurfacePressed
            : _isHovered
                ? theme.Colors.SurfaceHover
                : Color.Transparent;

        SetThemeValue(nameof(Background), (Brush)background, value => Background = value);
    }

    public void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        ApplyCurrentTheme();
        _onHovered?.Invoke(_item, this);
    }

    public void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        ApplyCurrentTheme();
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        _isPressed = true;
        ApplyCurrentTheme();
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (!_isPressed)
            return;

        _isPressed = false;
        ApplyCurrentTheme();
        _onActivated?.Invoke(_item, this);
    }

    private void ApplyCurrentTheme() =>
        OnThemeApplied(EffectiveTheme);

    private static VisualElement BuildLeadingIcon(MenuItem item)
    {
        IconData? iconData = item.IsChecked ? Icons.Check : item.IconData;
        if (iconData == null)
            return new Frame().Background(Color.Transparent).SetInputTransparent(true);

        var icon = new Icon(iconData)
            .Size(item.IconSize)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .SetInputTransparent(true);

        if (!item.IsChecked && item.IconColor.PrimaryColor.A > 0)
            icon.Color = item.IconColor;

        return icon;
    }

    private static VisualElement BuildTrailingIcon(MenuItem item)
    {
        if (!item.HasSubmenu)
            return new Frame().Background(Color.Transparent).SetInputTransparent(true);

        return new Icon(Icons.ChevronRight)
            .Size(12)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .SetInputTransparent(true);
    }
}
