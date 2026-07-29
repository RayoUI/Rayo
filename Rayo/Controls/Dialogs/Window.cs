using CancelEventArgs = System.ComponentModel.CancelEventArgs;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Core.Input;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;

namespace Rayo.Controls;

/// <summary>
/// A floating, non-modal window that can host arbitrary content.
/// </summary>
public class Window : Component
{
    private readonly string _title;
    private readonly VisualElement _content;
    private readonly float _width;
    private readonly List<VisualElement> _headerActions = [];
    private bool _centerOnScreen;
    private HStack? _headerActionsHost;

    /// <summary>Gets or sets the window position used when it is first displayed.</summary>
    public Position InitialPosition { get; set; }

    /// <summary>Gets or sets whether the title bar displays the close button.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Gets or sets whether the user can drag the window by its title bar.</summary>
    public bool IsDragEnabled { get; set; } = true;

    /// <summary>Raised before the window is closed. Set <see cref="CancelEventArgs.Cancel"/> to keep it open.</summary>
    public event EventHandler<CancelEventArgs>? Closing;

    /// <summary>Raised after the window has been closed.</summary>
    public event EventHandler? Closed;

    public Window(string title, VisualElement content, float width = 420, float x = 32, float y = 32)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(content);

        _title = title;
        _content = content;
        _width = width;
        InitialPosition = new Position(x, y);
    }

    /// <summary>Centers the window when it is first displayed.</summary>
    public Window Centered()
    {
        _centerOnScreen = true;
        return this;
    }

    /// <summary>Adds a custom control to the right side of the title bar.</summary>
    public Window AddHeaderAction(VisualElement action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _headerActions.Add(action);
        _headerActionsHost?.AddChild(action);
        return this;
    }

    /// <summary>Adds custom controls to the right side of the title bar.</summary>
    public Window AddHeaderActions(params VisualElement[] actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        foreach (var action in actions)
        {
            AddHeaderAction(action);
        }

        return this;
    }

    /// <summary>Adds custom controls to the right side of the title bar.</summary>
    public Window HeaderActions(params VisualElement[] actions) => AddHeaderActions(actions);

    public override VisualElement Build()
    {
        var window = new WindowFrame
        {
            Width = _width,
            HorizontalAlignment = _centerOnScreen ? HorizontalAlignment.Center : HorizontalAlignment.Left,
            VerticalAlignment = _centerOnScreen ? VerticalAlignment.Center : VerticalAlignment.Top,
            Background = EffectiveTheme.Colors.Surface,
            BorderBrush = EffectiveTheme.Colors.Border,
            BorderThickness = 1,
            BorderRadius = 8,
            Padding = new Thickness(0)
        };

        if (!_centerOnScreen)
        {
            window.Position(InitialPosition.X, InitialPosition.Y);
        }

        var titleLabel = new Label(_title)
            .FontSize(16)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center);

        _headerActionsHost = new HStack()
            .Spacing(8)
            .Alignment(Alignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);
        foreach (var action in _headerActions)
        {
            _headerActionsHost.AddChild(action);
        }

        WindowCloseButton? closeButton = null;
        VisualElement titleContent = titleLabel;
        if (ShowCloseButton)
        {
            closeButton = new WindowCloseButton(Icons.Close);
            closeButton
                .Size(32)
                .IconSize(18)
                .Background(Color.Transparent)
                .BorderThickness(0)
                .Padding(new Thickness(7))
                .HorizontalAlignment(HorizontalAlignment.Right)
                .VerticalAlignment(VerticalAlignment.Center)
                .OnTapped(() => { Close(); });

            _headerActionsHost.AddChild(closeButton);
        }

        if (_headerActions.Count > 0 || ShowCloseButton)
        {
            titleContent = new Grid()
                .Rows(GridLength.Pixels(32))
                .Columns(GridLength.Star, GridLength.Auto)
                .ColumnSpacing(12)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .AddChild(titleLabel, 0, 0)
                .AddChild(_headerActionsHost, 0, 1);
        }

        var titleBar = new WindowTitleBar(window, () => IsDragEnabled)
            .Padding(new Thickness(16, 12))
            .BorderRadius(new CornerRadius(8, 8, 0, 0))
            .Content(titleContent);

        window.ConfigureThemeChrome(titleBar, titleLabel, closeButton);

        window.Content(new VStack()
            .Spacing(0)
            .Children(
                titleBar,
                new Frame()
                    .Padding(new Thickness(20))
                    .Content(_content)));

        return window;
    }

    /// <summary>
    /// Builds and shows a non-modal window in the global overlay layer.
    /// </summary>
    /// <summary>Builds and shows this window in the global overlay layer.</summary>
    public void Show(VisualElement? owner = null)
    {
        VisualElement? overlay = null;
        EventHandler? removeOverlay = null;
        removeOverlay = (_, _) =>
        {
            if (overlay is not null)
            {
                OverlayManager.RemoveOverlay(overlay);
            }

            Closed -= removeOverlay;
        };

        Closed += removeOverlay;
        overlay = Build();
        OverlayManager.AddOverlay(overlay, owner);
    }

    /// <summary>Requests that the window close. Returns <see langword="false"/> when closing is canceled.</summary>
    public bool Close()
    {
        var args = new CancelEventArgs();
        Closing?.Invoke(this, args);
        if (args.Cancel)
        {
            return false;
        }

        Closed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public static void Show(
        string title,
        VisualElement content,
        float width = 420,
        float x = 32,
        float y = 32,
        VisualElement? owner = null,
        bool centered = false)
    {
        var window = new Window(title, content, width, x, y);
        if (centered)
        {
            window.Centered();
        }

        window.Show(owner);
    }

    private sealed class WindowFrame : Frame, IPointerHandler
    {
        private Frame? _titleBar;
        private Label? _titleLabel;
        private WindowCloseButton? _closeButton;

        public void OnPointerPressed(PointerEventArgs e) => OverlayManager.BringOverlayToFront(this);

        public void OnPointerEntered(PointerEventArgs e) => UpdateCloseButtonHover(e);

        public void OnPointerMoved(PointerEventArgs e) => UpdateCloseButtonHover(e);

        public void OnPointerExited(PointerEventArgs e)
        {
            if (_closeButton is not null)
            {
                _closeButton.IsHovered = false;
                _closeButton.IsPressed = false;
            }
        }

        public void ConfigureThemeChrome(Frame titleBar, Label titleLabel, WindowCloseButton? closeButton)
        {
            _titleBar = titleBar;
            _titleLabel = titleLabel;
            _closeButton = closeButton;
            ApplyThemeChrome(EffectiveTheme);
        }

        protected override void OnThemeApplied(ThemeData theme)
        {
            base.OnThemeApplied(theme);
            ApplyThemeChrome(theme);
        }

        private void ApplyThemeChrome(ThemeData theme)
        {
            Background = theme.Colors.Surface;
            BorderBrush = theme.Colors.Border;

            if (_titleBar is not null)
            {
                _titleBar.Background = theme.Colors.SurfaceHover;
            }

            if (_titleLabel is not null)
            {
                _titleLabel.Foreground = theme.Colors.OnSurface;
            }

            if (_closeButton is not null)
            {
                _closeButton.ApplyWindowTheme(theme);
            }
        }

        private void UpdateCloseButtonHover(PointerEventArgs e)
        {
            if (_closeButton is not null)
            {
                _closeButton.IsHovered = _closeButton.ContainsWindowPoint(e.Position);
            }
        }
    }

    private sealed class WindowCloseButton(IconData icon) : ButtonIcon(icon)
    {
        public void ApplyWindowTheme(ThemeData theme)
        {
            Background = Color.Transparent;
            HoverBackground = theme.Colors.Surface;
            PressedBackground = theme.Colors.SurfaceHover;
            IconColor = theme.Colors.OnSurface;
        }

        protected override void OnThemeApplied(ThemeData theme)
        {
            base.OnThemeApplied(theme);
            ApplyWindowTheme(theme);
        }
    }

    private sealed class WindowTitleBar(Frame window, Func<bool> isDragEnabled) : Frame, IDraggable
    {
        private float _startPointerX;
        private float _startPointerY;
        private float _startWindowX;
        private float _startWindowY;

        public bool IsDragging { get; set; }

        public DragData? OnDragStart(float mouseX, float mouseY)
        {
            if (!isDragEnabled())
            {
                return null;
            }

            _startPointerX = mouseX;
            _startPointerY = mouseY;
            _startWindowX = window.ComputedX;
            _startWindowY = window.ComputedY;
            window.HorizontalAlignment = HorizontalAlignment.Left;
            window.VerticalAlignment = VerticalAlignment.Top;
            window.Position(_startWindowX, _startWindowY);

            return new DragData("window", window, this)
                .WithAllowedEffects(DragDropEffect.Move);
        }

        public void OnDragging(float mouseX, float mouseY)
        {
            window.Position(
                _startWindowX + mouseX - _startPointerX,
                _startWindowY + mouseY - _startPointerY);
        }

        public void OnDragEnd(bool wasDropped)
        {
            MarkNeedsPaint();
        }
    }
}
