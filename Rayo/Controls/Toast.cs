namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using System.Collections.Generic;

/// <summary>
/// Toast notification type
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

internal sealed class HeadlessToastManager : Rayo.Animation.IFrameAnimation
{
    private readonly List<ToastNotification> _toasts = new();
    private VStack? _toastContainer;
    private ToastPosition _position = ToastPosition.TopRight;
    private float _spacing = 12;
    private float _marginFromEdge = 20;
    private bool _isRegistered;

    public ToastPosition Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            _position = value;
            UpdateContainerPosition();
        }
    }

    public void Show(string message, ToastType type = ToastType.Info, float duration = 3f)
    {
        EnsureContainer();

        ToastNotification? toastRef = null;
        var toast = new ToastNotification(message, type, duration, () => { if (toastRef != null) RemoveToast(toastRef); });
        toastRef = toast;
        _toasts.Add(toast);

        if (_toastContainer != null)
        {
            _toastContainer.AddChild(toast);
            UpdateContainerPosition();

            if (_toastContainer.Parent == null)
            {
                Rayo.Core.OverlayManager.AddOverlay(_toastContainer);
            }
        }

        RegisterTicker();
    }

    public void Tick(float deltaTime)
    {
        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            _toasts[i].Update(deltaTime);

            if (_toasts[i].ShouldDismiss)
            {
                RemoveToast(_toasts[i]);
            }
        }
    }

    private void EnsureContainer()
    {
        if (_toastContainer != null)
        {
            return;
        }

        _toastContainer = new VStack()
            .Spacing(_spacing)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top);
    }

    private void RegisterTicker()
    {
        if (_isRegistered)
        {
            return;
        }

        Rayo.Animation.FrameAnimationTicker.Register(this);
        _isRegistered = true;
    }

    private void UnregisterTicker()
    {
        if (!_isRegistered)
        {
            return;
        }

        Rayo.Animation.FrameAnimationTicker.Unregister(this);
        _isRegistered = false;
    }

    private void RemoveToast(ToastNotification toast)
    {
        _toasts.Remove(toast);

        if (_toastContainer != null)
        {
            _toastContainer.RemoveChild(toast);

            if (_toasts.Count == 0)
            {
                Rayo.Core.OverlayManager.RemoveOverlay(_toastContainer);
                UnregisterTicker();
            }
            else
            {
                UpdateContainerPosition();
            }
        }
    }

    private void UpdateContainerPosition()
    {
        if (_toastContainer == null || _toasts.Count == 0)
        {
            return;
        }

        _toastContainer.Measure(float.PositiveInfinity, float.PositiveInfinity);

        float containerWidth = _toastContainer.DesiredWidth;
        float containerHeight = _toastContainer.DesiredHeight;

        float windowWidth = Rayo.Core.OverlayManager.WindowWidth;
        float windowHeight = Rayo.Core.OverlayManager.WindowHeight;

        float x = 0;
        float y = 0;

        switch (_position)
        {
            case ToastPosition.TopLeft:
                x = _marginFromEdge;
                y = _marginFromEdge;
                break;

            case ToastPosition.TopCenter:
                x = (windowWidth - containerWidth) / 2;
                y = _marginFromEdge;
                break;

            case ToastPosition.TopRight:
                x = windowWidth - containerWidth - _marginFromEdge;
                y = _marginFromEdge;
                break;

            case ToastPosition.BottomLeft:
                x = _marginFromEdge;
                y = windowHeight - containerHeight - _marginFromEdge;
                break;

            case ToastPosition.BottomCenter:
                x = (windowWidth - containerWidth) / 2;
                y = windowHeight - containerHeight - _marginFromEdge;
                break;

            case ToastPosition.BottomRight:
                x = windowWidth - containerWidth - _marginFromEdge;
                y = windowHeight - containerHeight - _marginFromEdge;
                break;
        }

        _toastContainer.X = x;
        _toastContainer.Y = y;
        _toastContainer.Arrange(x, y, containerWidth, containerHeight);
    }
}

public static class ToastService
{
    private static HeadlessToastManager? _headlessManager;

    public static void ShowToast(string message, ToastType type = ToastType.Info, float duration = 3f)
    {
        var app = UIApplication.Current;
        if (app != null)
        {
            app.ShowToast(message, type, duration);
            return;
        }

        _headlessManager ??= new HeadlessToastManager();
        _headlessManager.Show(message, type, duration);
    }

    public static void ShowInfo(string message, float duration = 3f)
    {
        ShowToast(message, ToastType.Info, duration);
    }

    public static void ShowSuccess(string message, float duration = 3f)
    {
        ShowToast(message, ToastType.Success, duration);
    }

    public static void ShowWarning(string message, float duration = 3f)
    {
        ShowToast(message, ToastType.Warning, duration);
    }

    public static void ShowError(string message, float duration = 3f)
    {
        ShowToast(message, ToastType.Error, duration);
    }
}

/// <summary>
/// Toast notification position
/// </summary>
public enum ToastPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>
/// Individual toast notification
/// </summary>
internal class ToastNotification : Frame
{
    private readonly DateTime _startTime;
    private readonly float _duration;
    private readonly Action? _onDismiss;

    public bool ShouldDismiss => (DateTime.UtcNow - _startTime).TotalSeconds >= _duration;

    public ToastNotification()
    {
        VerticalAlignment = VerticalAlignment.Top;
        HorizontalAlignment = HorizontalAlignment.Left;
    }

    public ToastNotification(string message, ToastType type, float duration, Action? onDismiss): this()
    {
        _startTime = DateTime.UtcNow;
        _duration = duration;
        _onDismiss = onDismiss;

        // Set colors based on type
        var (bgColor, iconText, textColor) = type switch
        {
            ToastType.Info => (new Color((byte)33, (byte)150, (byte)243), "ℹ", Color.White),
            ToastType.Success => (new Color((byte)76, (byte)175, (byte)80), "✓", Color.White),
            ToastType.Warning => (new Color((byte)255, (byte)152, (byte)0), "⚠", Color.White),
            ToastType.Error => (new Color((byte)244, (byte)67, (byte)54), "✕", Color.White),
            _ => (new Color((byte)100, (byte)100, (byte)100), "ℹ", Color.White)
        };


        this.Background(bgColor)
            .Padding(new Thickness(16, 12, 16, 12))
            .Width(300);
        BorderRadius = new CornerRadius(8);

        // Content
        var content = new HStack()
            .Spacing(12)
            .Alignment(Alignment.Center)
            .Children(
                new Label()
                    .Text(iconText)
                    .FontSize(20)
                    .Foreground(textColor),
                new Label()
                    .Text(message)
                    .FontSize(14)
                    .Foreground(textColor)
            );

        this.Content(content);
    }

    public void Update(float deltaTime)
    {
        // Lifetime tracked via DateTime
    }

    public void Dismiss()
    {
        _onDismiss?.Invoke();
    }
}

/// <summary>
/// Toast notification manager - handles displaying and managing toast notifications
/// </summary>
public class ToastManager
{
    private readonly UIApplication _app;
    private readonly List<ToastNotification> _toasts = new();
    private VStack? _toastContainer;
    private ToastPosition _position = ToastPosition.TopRight;
    public ToastPosition Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            _position = value;
            UpdateContainerPosition();
        }
    }
    private float _spacing = 12;
    private float _marginFromEdge = 20;

    public ToastManager(UIApplication app)
    {
        _app = app;
        _app.Updated += Update;
        CreateContainer();
    }

    private void CreateContainer()
    {
        _toastContainer = new VStack()
            .Spacing(_spacing)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top);
    }

    public void Show(string message, ToastType type = ToastType.Info, float duration = 3f)
    {
        ToastNotification? toastRef = null;
        var toast = new ToastNotification(message, type, duration, () => { if (toastRef != null) RemoveToast(toastRef); });
        toastRef = toast;
        _toasts.Add(toast);

        if (_toastContainer != null)
        {
            _toastContainer.AddChild(toast);
            UpdateContainerPosition();

            // Add to overlay if not already added
            if (_toastContainer.Parent == null)
            {
                _app.AddOverlay(_toastContainer);
            }
        }
    }

    public void Info(string message, float duration = 3f)
    {
        Show(message, ToastType.Info, duration);
    }

    public void Success(string message, float duration = 3f)
    {
        Show(message, ToastType.Success, duration);
    }

    public void Warning(string message, float duration = 3f)
    {
        Show(message, ToastType.Warning, duration);
    }

    public void Error(string message, float duration = 3f)
    {
        Show(message, ToastType.Error, duration);
    }

    public void Update(float deltaTime)
    {
        // Update all toasts
        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            _toasts[i].Update(deltaTime);

            if (_toasts[i].ShouldDismiss)
            {
                RemoveToast(_toasts[i]);
            }
        }
    }

    private void RemoveToast(ToastNotification toast)
    {
        _toasts.Remove(toast);

        if (_toastContainer != null)
        {
            _toastContainer.RemoveChild(toast);

            // Remove container from overlay if no toasts left
            if (_toasts.Count == 0)
            {
                _app.RemoveOverlay(_toastContainer);
            }
            else
            {
                UpdateContainerPosition();
            }
        }
    }

    private void UpdateContainerPosition()
    {
        if (_toastContainer == null || _toasts.Count == 0) return;

        // Measure container
        _toastContainer.Measure(float.PositiveInfinity, float.PositiveInfinity);

        float containerWidth = _toastContainer.DesiredWidth;
        float containerHeight = _toastContainer.DesiredHeight;

        // Calculate position based on ToastPosition
        float x = 0, y = 0;

        switch (_position)
        {
            case ToastPosition.TopLeft:
                x = _marginFromEdge;
                y = _marginFromEdge;
                break;

            case ToastPosition.TopCenter:
                x = (_app.Window.Width - containerWidth) / 2;
                y = _marginFromEdge;
                break;

            case ToastPosition.TopRight:
                x = _app.Window.Width - containerWidth - _marginFromEdge;
                y = _marginFromEdge;
                break;

            case ToastPosition.BottomLeft:
                x = _marginFromEdge;
                y = _app.Window.Height - containerHeight - _marginFromEdge;
                break;

            case ToastPosition.BottomCenter:
                x = (_app.Window.Width - containerWidth) / 2;
                y = _app.Window.Height - containerHeight - _marginFromEdge;
                break;

            case ToastPosition.BottomRight:
                x = _app.Window.Width - containerWidth - _marginFromEdge;
                y = _app.Window.Height - containerHeight - _marginFromEdge;
                break;
        }

        // Set position properties so re-layout uses them
        _toastContainer.X = x;
        _toastContainer.Y = y;
        _toastContainer.Arrange(x, y, containerWidth, containerHeight);
    }

    public void Clear()
    {
        _toasts.Clear();
        _toastContainer?.ClearChildren();

        if (_toastContainer != null)
        {
            _app.RemoveOverlay(_toastContainer);
        }
    }
}

/// <summary>
/// Extension methods for UIApplication to easily show toasts
/// </summary>
public static class ToastExtensions
{
    private static Dictionary<UIApplication, ToastManager> _managers = new();

    public static ToastManager GetToastManager(this UIApplication app)
    {
        if (!_managers.ContainsKey(app))
        {
            _managers[app] = new ToastManager(app);
        }
        return _managers[app];
    }

    public static void ShowToast(this UIApplication app, string message, ToastType type = ToastType.Info, float duration = 3f)
    {
        app.GetToastManager().Show(message, type, duration);
    }

    public static void ShowInfo(this UIApplication app, string message, float duration = 3f)
    {
        app.GetToastManager().Info(message, duration);
    }

    public static void ShowSuccess(this UIApplication app, string message, float duration = 3f)
    {
        app.GetToastManager().Success(message, duration);
    }

    public static void ShowWarning(this UIApplication app, string message, float duration = 3f)
    {
        app.GetToastManager().Warning(message, duration);
    }

    public static void ShowError(this UIApplication app, string message, float duration = 3f)
    {
        app.GetToastManager().Error(message, duration);
    }
}
