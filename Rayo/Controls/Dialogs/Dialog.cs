namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// A modal dialog overlay.
/// </summary>
public class Dialog : UserControl
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

    #region Content
    private readonly VisualElement? _content;
    public new VisualElement? Content
    {
        get => _content;
    }
    #endregion

    #region BorderRadius
    private readonly int _borderRadius = 8;
    public new int BorderRadius
    {
        get => _borderRadius;
    }
    #endregion

    public Dialog(string title, string message, Action onClose)
    {
        _title = title;
        _message = message;
        if (onClose != null) Closed += onClose;
    }

    public Dialog(string title, VisualElement content, Action onClose)
    {
        _title = title;
        _content = content;
        _message = "";
        if (onClose != null) Closed += onClose;
    }

    public override VisualElement Build()
    {
        // Overlay background (semi-transparent black)
        var dialogBox = new Frame();
        dialogBox.Width(420);
        dialogBox.Background = new SolidColorBrush(new Color(45, 45, 48));
        dialogBox.BorderRadius(_borderRadius);
        dialogBox.BorderWidth = 1;
        dialogBox.Padding(new Thickness(0));
        dialogBox.HorizontalAlignment(HorizontalAlignment.Center);
        dialogBox.VerticalAlignment(VerticalAlignment.Center);

        Frame buttonSection = new Frame();
        buttonSection.Background(new Color(40, 40, 42));
        buttonSection.Padding(new Thickness(24, 16, 24, 16));
        buttonSection.BorderRadius(new CornerRadius(0, 0, _borderRadius, _borderRadius));
        buttonSection.Content(
            new HStack()
                .Children(CreateOKButton())
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Alignment(Alignment.End)
                .JustifyContent(JustifyContent.End)
                .Spacing(10)
        );

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
            .Foreground(Color.White)
            .HorizontalAlignment(HorizontalAlignment.Left);

        Frame Frame = new Frame();
        Frame.Background(new Color(40, 40, 42));
        Frame.Padding(new Thickness(24, 20, 24, 20));
        Frame.BorderRadius(new CornerRadius(_borderRadius, _borderRadius, 0, 0));
        Frame.Content(titleLabel);
        return Frame;
    }

    private Frame CreateContentFrame()
    {
        VisualElement content = _content ?? new Label()
            .Text(_message)
            .FontSize(14)
            .Foreground(new Color(200, 200, 200))
            .HorizontalAlignment(HorizontalAlignment.Left);

        Frame Frame = new Frame();
        Frame.Padding(new Thickness(24, 20, 24, 20));
        Frame.Content(content);
        return Frame;
    }

    private Button CreateOKButton()
    {
        var button = new Button();
        button.Text("OK");
        button.Width(100);
        button.Height(36);
        button.Background(new Color(0, 120, 215));
        button.HoverBackground(new Color(0, 140, 235));
        button.PressedBackground(new Color(0, 100, 195));
        button.BorderRadius(4);
        button.OnTapped(() => Closed?.Invoke());
        return button;
    }

    public static void Show(string title, string message)
    {
        VisualElement? overlay = null;
        
        overlay = new Dialog(title, message, () => {
            if (overlay != null) Rayo.Core.OverlayManager.RemoveOverlay(overlay);
        }).Build(); // We need the built element (Frame)

        Rayo.Core.OverlayManager.AddOverlay(overlay);
    }
}
