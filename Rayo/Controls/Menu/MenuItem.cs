namespace Rayo.Controls;

using System;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Reactivity;

public readonly record struct MenuItemIconOptions(IconData Icon, Brush Color, float? Size = null, float? Spacing = null);

/// <summary>
/// An item within a Menu.
/// </summary>
public class MenuItem : UserControl
{
    private readonly string _text;
    private readonly Action? _onClick;

    #region TextAlignment
    [LayoutProperty]
    public HorizontalAlignment TextAlignment
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = HorizontalAlignment.Left;
    #endregion

    #region IconData
    public IconData? IconData
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    }
    #endregion

    #region IconColor
    public Brush IconColor
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = new Color(200, 205, 215);
    #endregion

    #region IconSize
    public float IconSize
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = 14f;
    #endregion

    #region IconSpacing
    public float IconSpacing
    {
        get => field;
        set => this.SetProperty(ref field, value, Rebuild);
    } = 8f;
    #endregion

    #region IconOptions
    public MenuItemIconOptions? IconOptions
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (field == value) return;

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
            }
            Rebuild();
        });
    }
    #endregion

    public MenuItem(string text, Action? onClick = null)
    {
        _text = text;
        _onClick = onClick;
    }

    public override VisualElement Build()
    {
        var button = new Button();
        button.Text(IconData == null ? _text : string.Empty);
        button.Background(Color.Transparent);
        button.TextColor(Color.White);
        button.FontSize(12);
        button.Padding(new Thickness(15, 5));
        button.HorizontalAlignment(HorizontalAlignment.Stretch);
        button.TextAlignment(TextAlignment);
        button.BorderRadius(0);
        button.HoverBackground(new Color(0, 122, 204));
        button.PressedBackground(new Color(0, 100, 180));
        button.OnTapped(() => {
            _onClick?.Invoke();
        });

        if (IconData != null)
        {
            var icon = new Icon(IconData)
                .Size(new Size(IconSize, IconSize))
                .SetInputTransparent(true);

            var label = new Label(_text)
                .FontSize(12)
                .Foreground(Color.White)
                .HorizontalAlignment(HorizontalAlignment.Left)
                .SetInputTransparent(true);

            var justify = TextAlignment switch
            {
                HorizontalAlignment.Center => JustifyContent.Center,
                HorizontalAlignment.Right => JustifyContent.End,
                _ => JustifyContent.Start
            };

            HStack content = new HStack();
            content.Spacing(IconSpacing);
            content.Alignment(Alignment.Center);
            content.JustifyContent(justify);
            content.HorizontalAlignment(HorizontalAlignment.Stretch);
            content.SetInputTransparent(true);
            content.AddChild(icon);
            content.AddChild(label);

            // Create a Frame to wrap the button content - not needed since we fixed the code
            // We can directly add content to wrapper below

            // Use the Text property to hide default button text, then wrap with our content
            button.Text(string.Empty);
            // Since we cannot manipulate button children externally, we redesign to use Text("")
            // and return a Frame that wraps both the button behavior and our custom content

            // Alternative: create a clickable Frame instead
            var wrapper = new Frame()
                .Background(Color.Transparent)
                .Padding(new Thickness(15, 5))
                .HorizontalAlignment(HorizontalAlignment.Stretch);
            wrapper.Content(content);

            // Make wrapper clickable
            var clickableWrapper = new MenuItemClickableFrame(wrapper, () => _onClick?.Invoke());
            return clickableWrapper;
        }

        return button;
    }
}

/// <summary>
/// Internal clickable Frame for MenuItem with icon support
/// </summary>
internal class MenuItemClickableFrame : Frame, Rayo.Core.Input.IPointerHandler
{
    private readonly Frame _innerFrame;
    private readonly Action _onClick;
    private bool _isPressed;

    public MenuItemClickableFrame(Frame innerFrame, Action onClick)
    {
        _innerFrame = innerFrame;
        _onClick = onClick;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        this.Content(_innerFrame);
    }

    public void OnPointerEntered(Rayo.Core.Input.PointerEventArgs e)
    {
        if (e.PointerType == Rayo.Core.Input.PointerType.Mouse)
        {
            _innerFrame.Background(new Color(0, 122, 204));
            MarkNeedsPaint();
        }
    }

    public void OnPointerExited(Rayo.Core.Input.PointerEventArgs e)
    {
        if (e.PointerType == Rayo.Core.Input.PointerType.Mouse)
        {
            _innerFrame.Background(Color.Transparent);
            _isPressed = false;
            MarkNeedsPaint();
        }
    }

    public void OnPointerPressed(Rayo.Core.Input.PointerEventArgs e)
    {
        _isPressed = true;
        _innerFrame.Background(new Color(0, 100, 180));
        MarkNeedsPaint();
    }

    public void OnPointerReleased(Rayo.Core.Input.PointerEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            _innerFrame.Background(new Color(0, 122, 204));
            MarkNeedsPaint();
            _onClick?.Invoke();
        }
    }
}
