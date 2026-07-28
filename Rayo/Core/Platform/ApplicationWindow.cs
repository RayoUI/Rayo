using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Rayo.Core.Platform;

/// <summary>
/// Default <see cref="IApplicationWindow"/> implementation backed by <see cref="UIApplication"/>.
/// </summary>
internal sealed class ApplicationWindow : IApplicationWindow
{
    private readonly Rayo.Core.UIApplication _app;
    private readonly AndroidApplicationWindow _android;
    private readonly iOSApplicationWindow _ios;

    internal ApplicationWindow(Rayo.Core.UIApplication app)
    {
        _app = app;
        _android = new AndroidApplicationWindow(app);
        _ios = new iOSApplicationWindow(app);
    }

    public IAndroidApplicationWindow Android => _android;

    public IiOSApplicationWindow iOS => _ios;

    public SafeAreaInsets SafeArea => Rayo.Core.Platform.SafeArea.Current;

    public string Title
    {
        get => _app.NativeWindow?.Title ?? _app.WindowConfigurationInternal.Title;
        set
        {
            if (_app.NativeWindow != null)
            {
                _app.NativeWindow.Title = value;
            }

            _app.WindowConfigurationInternal.Title = value;
        }
    }

    public WindowState State
    {
        get
        {
            if (_app.NativeWindow == null)
            {
                return _app.WindowConfigurationInternal.WindowState;
            }

            return _app.NativeWindow.WindowState switch
            {
                Silk.NET.Windowing.WindowState.Maximized => WindowState.Maximized,
                Silk.NET.Windowing.WindowState.Minimized => WindowState.Minimized,
                Silk.NET.Windowing.WindowState.Fullscreen => WindowState.FullScreen,
                _ => WindowState.Normal
            };
        }
        set
        {
            if (_app.NativeWindow != null)
            {
                _app.NativeWindow.WindowState = value switch
                {
                    WindowState.Maximized => Silk.NET.Windowing.WindowState.Maximized,
                    WindowState.Minimized => Silk.NET.Windowing.WindowState.Minimized,
                    WindowState.FullScreen => Silk.NET.Windowing.WindowState.Fullscreen,
                    _ => Silk.NET.Windowing.WindowState.Normal
                };
            }

            _app.WindowConfigurationInternal.WindowState = value;
        }
    }

    public bool Topmost
    {
        get => _app.NativeWindow?.TopMost ?? _app.WindowConfigurationInternal.Topmost;
        set
        {
            if (_app.NativeWindow != null)
            {
                _app.NativeWindow.TopMost = value;
            }

            _app.WindowConfigurationInternal.Topmost = value;
        }
    }

    public bool CanResize
    {
        get => _app.WindowConfigurationInternal.CanResize;
        set
        {
            _app.WindowConfigurationInternal.CanResize = value;
            ApplyWindowBorder();
        }
    }

    public SystemDecorations SystemDecorations
    {
        get => _app.WindowConfigurationInternal.SystemDecorations;
        set
        {
            _app.WindowConfigurationInternal.SystemDecorations = value;
            ApplyWindowBorder();
        }
    }

    public int Width
    {
        get => _app.NativeWindow?.Size.X
            ?? (_app.WindowWidth > 0 ? (int)_app.WindowWidth : _app.WindowConfigurationInternal.Width);
        set => SetSize(value, Height);
    }

    public int Height
    {
        get => _app.NativeWindow?.Size.Y
            ?? (_app.WindowHeight > 0 ? (int)_app.WindowHeight : _app.WindowConfigurationInternal.Height);
        set => SetSize(Width, value);
    }

    public int X
    {
        get => _app.NativeWindow?.Position.X ?? _app.WindowConfigurationInternal.X ?? 0;
        set => SetPosition(value, Y);
    }

    public int Y
    {
        get => _app.NativeWindow?.Position.Y ?? _app.WindowConfigurationInternal.Y ?? 0;
        set => SetPosition(X, value);
    }

    public bool VSync
    {
        get => _app.EnableVSync;
        set => _app.EnableVSync = value;
    }

    public bool IsVisible
    {
        get => _app.NativeWindow?.IsVisible ?? true;
        set
        {
            if (_app.NativeWindow != null)
            {
                _app.NativeWindow.IsVisible = value;
            }
        }
    }

    public void SetSize(int width, int height)
    {
        if (_app.NativeWindow != null)
        {
            _app.NativeWindow.Size = new Vector2D<int>(width, height);
        }

        _app.WindowConfigurationInternal.Width = width;
        _app.WindowConfigurationInternal.Height = height;
    }

    public void SetPosition(int x, int y)
    {
        if (_app.NativeWindow != null)
        {
            _app.NativeWindow.Position = new Vector2D<int>(x, y);
        }

        _app.WindowConfigurationInternal.X = x;
        _app.WindowConfigurationInternal.Y = y;
    }

    public void Center() => _app.CenterWindowInternal();

    public void SetIcon(WindowIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        _app.WindowConfigurationInternal.Icon = icon;
        _app.ApplyWindowIconInternal();
    }

    private void ApplyWindowBorder()
    {
        if (_app.NativeWindow == null)
        {
            return;
        }

        var config = _app.WindowConfigurationInternal;
        _app.NativeWindow.WindowBorder = WindowBorderMapper.ToSilkBorder(
            config.SystemDecorations,
            config.CanResize);
    }
}
