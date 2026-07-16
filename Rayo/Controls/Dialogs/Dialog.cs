namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;

/// <summary>
/// A modal dialog overlay.
/// </summary>
public class Dialog : Component
{
    #region Title
    private readonly string _title;
    public string Title
    {
        get => _title;
    }
    #endregion

    #region Message
    private readonly string _message;
    public string Message
    {
        get => _message;
    }
    #endregion

    #region Closed
    public event Action? Closed;
    #endregion

    #region Canceled
    public event Action? Canceled;
    #endregion

    #region Content
    private readonly VisualElement? _content;
    public new VisualElement? Content
    {
        get => _content;
    }
    #endregion

    private readonly bool _showCancelButton;
    private readonly bool _showCloseButton;
    private readonly string _okText;
    private readonly string _cancelText;
    private readonly Func<bool>? _validate;

    #region BorderRadius
    private readonly int _borderRadius = 8;
    public int BorderRadius
    {
        get => _borderRadius;
    }
    #endregion

    public Dialog(string title, string message, Action onClose)
        : this(title, message, onClose, null, false)
    {
    }

    public Dialog(
        string title,
        string message,
        Action? onClose,
        Action? onCancel,
        bool showCancelButton = true,
        Func<bool>? validate = null,
        string okText = "OK",
        string cancelText = "Cancel",
        bool showCloseButton = true)
    {
        _title = title;
        _message = message;
        _showCancelButton = showCancelButton;
        _showCloseButton = showCloseButton;
        _okText = okText;
        _cancelText = cancelText;
        _validate = validate;
        if (onClose != null) Closed += onClose;
        if (onCancel != null) Canceled += onCancel;
    }

    public Dialog(string title, VisualElement content, Action onClose)
        : this(title, content, onClose, null, false)
    {
    }

    public Dialog(
        string title,
        VisualElement content,
        Action? onClose,
        Action? onCancel,
        bool showCancelButton = true,
        Func<bool>? validate = null,
        string okText = "OK",
        string cancelText = "Cancel",
        bool showCloseButton = true)
    {
        _title = title;
        _content = content;
        _message = "";
        _showCancelButton = showCancelButton;
        _showCloseButton = showCloseButton;
        _okText = okText;
        _cancelText = cancelText;
        _validate = validate;
        if (onClose != null) Closed += onClose;
        if (onCancel != null) Canceled += onCancel;
    }

    public override VisualElement Build()
    {
        // Overlay background (semi-transparent black)
        var dialogBox = new Frame();
        dialogBox.Width(420);
        dialogBox.Background = new SolidColorBrush(EffectiveTheme.Colors.Surface);
        dialogBox.BorderRadius(_borderRadius);
        dialogBox.BorderThickness = 1;
        dialogBox.Padding(new Thickness(0));
        dialogBox.HorizontalAlignment(HorizontalAlignment.Center);
        dialogBox.VerticalAlignment(VerticalAlignment.Center);

        Frame buttonSection = new Frame();
        buttonSection.Background(EffectiveTheme.Colors.SurfaceHover);
        buttonSection.Padding(new Thickness(24, 16, 24, 16));
        buttonSection.BorderRadius(new CornerRadius(0, 0, _borderRadius, _borderRadius));
        var buttons = new HStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Alignment(Alignment.End)
            .JustifyContent(JustifyContent.End)
            .Spacing(10);

        if (_showCancelButton)
        {
            buttons.Children(CreateCancelButton(), CreateOKButton());
        }
        else
        {
            buttons.Children(CreateOKButton());
        }

        buttonSection.Content(buttons);

        var mainStack = new VStack()
            .Spacing(0)
            .Children(
                CreateTitleFrame(),
                CreateContentFrame(),
                buttonSection
            );

        dialogBox.Content(mainStack);

        Frame overlay = new Frame();
        overlay.Background(new Color(0, 0, 0, 150));
        overlay.HorizontalAlignment(HorizontalAlignment.Stretch);
        overlay.VerticalAlignment(VerticalAlignment.Stretch);
        overlay.Content(dialogBox);

        return overlay;
    }

    private Frame CreateTitleFrame()
    {
        var titleLabel = new Label(_title)
            .FontSize(16)
            .Foreground(EffectiveTheme.Colors.OnSurface)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center);

        VisualElement titleContent = titleLabel;
        if (_showCloseButton)
        {
            var closeButton = new ButtonIcon(Icons.Close)
                .Size(32)
                .IconSize(18)
                .Background(Color.Transparent)
                .HoverBackground(EffectiveTheme.Colors.Surface)
                .PressedBackground(EffectiveTheme.Colors.SurfaceHover)
                .IconColor(EffectiveTheme.Colors.OnSurface)
                .BorderThickness(0)
                .Padding(new Thickness(7))
                .HorizontalAlignment(HorizontalAlignment.Right)
                .VerticalAlignment(VerticalAlignment.Center);

            closeButton.OnTapped(() => Canceled?.Invoke());

            titleContent = new Grid()
                .Rows(GridLength.Pixels(32))
                .Columns(GridLength.Star, GridLength.Pixels(32))
                .ColumnSpacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Center)
                .AddChild(titleLabel, 0, 0)
                .AddChild(closeButton, 0, 1);
        }

        Frame Frame = new Frame();
        Frame.Background(EffectiveTheme.Colors.SurfaceHover);
        Frame.Padding(new Thickness(24, 16, 16, 16));
        Frame.BorderRadius(new CornerRadius(_borderRadius, _borderRadius, 0, 0));
        Frame.Content(titleContent);
        return Frame;
    }

    private Frame CreateContentFrame()
    {
        VisualElement content = _content ?? new Label()
            .Text(_message)
            .FontSize(14)
            .Foreground(EffectiveTheme.Colors.OnDisabled)
            .HorizontalAlignment(HorizontalAlignment.Left);

        Frame Frame = new Frame();
        Frame.Padding(new Thickness(24, 20, 24, 20));
        Frame.Content(content);
        return Frame;
    }

    private Button CreateOKButton()
    {
        var button = new Button();
        button.Text(_okText);
        button.Width(100);
        button.Height(36);
        button.Variant(ButtonVariant.Primary);
        button.BorderRadius(4);
        button.OnTapped(() =>
        {
            if (_validate != null && !_validate())
            {
                return;
            }

            Closed?.Invoke();
        });
        return button;
    }

    private Button CreateCancelButton()
    {
        var button = new Button();
        button.Text(_cancelText);
        button.Width(100);
        button.Height(36);
        button.Variant(ButtonVariant.Secondary);
        button.BorderRadius(4);
        button.BorderThickness(0);
        button.OnTapped(() => Canceled?.Invoke());
        return button;
    }

    public static void Show(
        string title,
        string message,
        bool showCancelButton = false,
        Action? onAccepted = null,
        Action? onCanceled = null,
        Func<bool>? validate = null,
        string okText = "OK",
        string cancelText = "Cancel",
        bool showCloseButton = true,
        VisualElement? owner = null,
        VisualElement? initialFocus = null)
    {
        VisualElement? overlay = null;

        void RemoveOverlay()
        {
            if (overlay != null) Rayo.Core.OverlayManager.RemoveOverlay(overlay);
        }

        overlay = new Dialog(
            title,
            message,
            () =>
            {
                RemoveOverlay();
                onAccepted?.Invoke();
            },
            () =>
            {
                RemoveOverlay();
                onCanceled?.Invoke();
            },
            showCancelButton,
            validate,
            okText,
            cancelText,
            showCloseButton).Build(); // We need the built element (Frame)

        Rayo.Core.OverlayManager.AddOverlay(overlay, owner);
        if (initialFocus != null)
        {
            Rayo.Core.OverlayManager.EventManager?.SetFocus(initialFocus);
        }
    }

    public static void Show(
        string title,
        VisualElement content,
        bool showCancelButton = false,
        Action? onAccepted = null,
        Action? onCanceled = null,
        Func<bool>? validate = null,
        string okText = "OK",
        string cancelText = "Cancel",
        bool showCloseButton = true,
        VisualElement? owner = null,
        VisualElement? initialFocus = null)
    {
        VisualElement? overlay = null;

        void RemoveOverlay()
        {
            if (overlay != null) Rayo.Core.OverlayManager.RemoveOverlay(overlay);
        }

        overlay = new Dialog(
            title,
            content,
            () =>
            {
                RemoveOverlay();
                onAccepted?.Invoke();
            },
            () =>
            {
                RemoveOverlay();
                onCanceled?.Invoke();
            },
            showCancelButton,
            validate,
            okText,
            cancelText,
            showCloseButton).Build();

        Rayo.Core.OverlayManager.AddOverlay(overlay, owner);
        if (initialFocus != null)
        {
            Rayo.Core.OverlayManager.EventManager?.SetFocus(initialFocus);
        }
    }
}
