namespace Rayo.Hosting.Android;

public sealed class AndroidVirtualKeyboardService : Rayo.Core.Platform.IVirtualKeyboardService, IDisposable
{
    private readonly global::Android.Views.View _view;
    private readonly global::Android.Content.Context _context;
    private global::Android.Views.ViewGroup? _accessoryRoot;
    private global::Android.Widget.HorizontalScrollView? _accessoryView;
    private global::Android.Views.ViewTreeObserver? _viewTreeObserver;
    private Rayo.Core.UITree? _overlayTree;
    private IReadOnlyList<Rayo.Core.Platform.VirtualKeyboardAccessoryKey> _accessoryKeys = [];
    private long _inputSession;
    private bool _nativeOverlaysBlocked;
    private bool _restoreAfterResume;
    private bool _disposed;

    public AndroidVirtualKeyboardService(global::Android.Views.View view, global::Android.Content.Context context)
    {
        _view = view;
        _context = context;
    }

    public void AttachOverlayTree(Rayo.Core.UITree tree)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ReferenceEquals(_overlayTree, tree))
        {
            return;
        }

        DetachOverlayTree();
        _overlayTree = tree;
        _nativeOverlaysBlocked = tree.AreNativeOverlaysBlocked;
        tree.NativeOverlayBlockingChanged += OnNativeOverlayBlockingChanged;
        _view.Post(UpdateAccessoryVisibility);
    }

    public void Show(IReadOnlyList<Rayo.Core.Platform.VirtualKeyboardAccessoryKey> accessoryKeys)
    {
        if (_disposed)
        {
            return;
        }

        var session = ++_inputSession;
        _accessoryKeys = accessoryKeys;
        _restoreAfterResume = false;
        var imm = _context.GetSystemService(global::Android.Content.Context.InputMethodService)
            as global::Android.Views.InputMethods.InputMethodManager;
        if (imm == null)
        {
            PostRemoveAccessoryBar(session);
            return;
        }

        _view.Post(() =>
        {
            if (_disposed || session != _inputSession)
            {
                return;
            }

            _view.RequestFocus();
            imm.ShowSoftInput(_view, global::Android.Views.InputMethods.ShowFlags.Implicit);
            ShowAccessoryBar(accessoryKeys, session);
        });
    }

    public void Hide()
    {
        if (_disposed)
        {
            return;
        }

        var session = ++_inputSession;
        _accessoryKeys = [];
        _restoreAfterResume = false;
        var imm = _context.GetSystemService(global::Android.Content.Context.InputMethodService)
            as global::Android.Views.InputMethods.InputMethodManager;
        if (imm == null)
        {
            PostRemoveAccessoryBar(session);
            return;
        }

        _view.Post(() =>
        {
            if (_disposed || session != _inputSession)
            {
                return;
            }

            RemoveAccessoryBar();
            imm.HideSoftInputFromWindow(_view.WindowToken, global::Android.Views.InputMethods.HideSoftInputFlags.None);
        });
    }

    public void NotifyAppPaused()
    {
        if (_disposed)
            return;
        _restoreAfterResume |= IsKeyboardVisible();
    }

    public void RestoreAfterResume(Rayo.Core.Platform.IVirtualKeyboardOptions? options)
    {
        if (_disposed || !_restoreAfterResume)
            return;
        if (options is not { ShouldShowVirtualKeyboard: true })
        {
            _restoreAfterResume = false;
            return;
        }

        var keys = options.KeyboardAccessoryKeys;
        var session = ++_inputSession;
        _accessoryKeys = keys;
        _view.Post(() => TryRestoreKeyboard(keys, session));
    }

    private void ShowAccessoryBar(
        IReadOnlyList<Rayo.Core.Platform.VirtualKeyboardAccessoryKey> keys,
        long session)
    {
        if (_disposed || session != _inputSession)
        {
            return;
        }

        RemoveAccessoryBar();
        if (keys.Count == 0 || _view.RootView is not global::Android.Views.ViewGroup root)
        {
            return;
        }

        var row = new global::Android.Widget.LinearLayout(_context)
        {
            Orientation = global::Android.Widget.Orientation.Horizontal
        };
        row.SetGravity(global::Android.Views.GravityFlags.CenterVertical);
        row.SetPadding(Dp(4), 0, Dp(4), 0);

        foreach (var key in keys)
        {
            var keyView = new global::Android.Widget.TextView(_context)
            {
                Text = key.Label,
                Gravity = global::Android.Views.GravityFlags.Center,
                Focusable = false,
                FocusableInTouchMode = false,
                Clickable = true,
                TextSize = 18
            };
            keyView.SetTextColor(global::Android.Graphics.Color.White);
            keyView.SetBackgroundColor(global::Android.Graphics.Color.Rgb(51, 65, 85));
            keyView.Click += (_, _) => InsertText(key.Text);

            var keyParameters = new global::Android.Widget.LinearLayout.LayoutParams(Dp(key.Label == "Tab" ? 58 : 42), Dp(40))
            {
                LeftMargin = Dp(2),
                RightMargin = Dp(2)
            };
            row.AddView(keyView, keyParameters);
        }

        var scroll = new global::Android.Widget.HorizontalScrollView(_context)
        {
            FillViewport = false,
            HorizontalScrollBarEnabled = false,
            Focusable = false,
            FocusableInTouchMode = false,
            OverScrollMode = global::Android.Views.OverScrollMode.Never
        };
        scroll.SetBackgroundColor(global::Android.Graphics.Color.Rgb(30, 41, 59));
        scroll.AddView(row, new global::Android.Widget.HorizontalScrollView.LayoutParams(
            global::Android.Views.ViewGroup.LayoutParams.WrapContent,
            global::Android.Views.ViewGroup.LayoutParams.MatchParent));

        var layoutParameters = new global::Android.Widget.FrameLayout.LayoutParams(
            global::Android.Views.ViewGroup.LayoutParams.MatchParent,
            Dp(48),
            global::Android.Views.GravityFlags.Bottom);
        root.AddView(scroll, layoutParameters);

        _accessoryRoot = root;
        _accessoryView = scroll;
        UpdateAccessoryVisibility();
        var observer = _view.ViewTreeObserver;
        if (observer != null)
        {
            _viewTreeObserver = observer;
            observer.GlobalLayout += OnGlobalLayout;
        }
        UpdateAccessoryPosition();
        ScheduleAccessoryPositionUpdate(session, 100);
        ScheduleAccessoryPositionUpdate(session, 250);
        ScheduleAccessoryPositionUpdate(session, 500);
    }

    private void RemoveAccessoryBar()
    {
        if (_viewTreeObserver?.IsAlive == true)
        {
            _viewTreeObserver.GlobalLayout -= OnGlobalLayout;
        }
        _viewTreeObserver = null;

        if (_accessoryView != null)
        {
            (_accessoryView.Parent as global::Android.Views.ViewGroup)?.RemoveView(_accessoryView);
            _accessoryView.Dispose();
        }

        _accessoryView = null;
        _accessoryRoot = null;
    }

    private void PostRemoveAccessoryBar(long session)
    {
        _view.Post(() =>
        {
            if (!_disposed && session == _inputSession)
            {
                RemoveAccessoryBar();
            }
        });
    }

    private void OnGlobalLayout(object? sender, EventArgs args) => UpdateAccessoryPosition();

    private void OnNativeOverlayBlockingChanged(bool isBlocked)
    {
        if (_disposed)
        {
            return;
        }

        _nativeOverlaysBlocked = isBlocked;
        _view.Post(UpdateAccessoryVisibility);
    }

    private void UpdateAccessoryVisibility()
    {
        if (_disposed || _accessoryView == null)
        {
            return;
        }

        _accessoryView.Visibility = _nativeOverlaysBlocked
            ? global::Android.Views.ViewStates.Invisible
            : global::Android.Views.ViewStates.Visible;
    }

    private void DetachOverlayTree()
    {
        if (_overlayTree != null)
        {
            _overlayTree.NativeOverlayBlockingChanged -= OnNativeOverlayBlockingChanged;
            _overlayTree = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DetachOverlayTree();
        _view.Post(RemoveAccessoryBar);
        GC.SuppressFinalize(this);
    }

    private void UpdateAccessoryPosition()
    {
        if (_disposed || _accessoryView == null || _accessoryRoot == null ||
            _accessoryView.LayoutParameters is not global::Android.Widget.FrameLayout.LayoutParams parameters)
        {
            return;
        }

        var visibleFrame = new global::Android.Graphics.Rect();
        _view.GetWindowVisibleDisplayFrame(visibleFrame);
        int rootHeight = _view.RootView?.Height ?? visibleFrame.Bottom;
        int keyboardInset = Math.Max(0, rootHeight - visibleFrame.Bottom);
        if (parameters.BottomMargin != keyboardInset)
        {
            parameters.BottomMargin = keyboardInset;
            _accessoryView.LayoutParameters = parameters;
        }
    }

    private void ScheduleAccessoryPositionUpdate(long session, long delayMilliseconds)
    {
        _view.PostDelayed(() =>
        {
            if (!_disposed && session == _inputSession)
            {
                UpdateAccessoryPosition();
            }
        }, delayMilliseconds);
    }

    private bool IsKeyboardVisible()
    {
        if (_view.RootView is not { Height: > 0 } root)
            return false;
        var visibleFrame = new global::Android.Graphics.Rect();
        _view.GetWindowVisibleDisplayFrame(visibleFrame);
        return root.Height - visibleFrame.Bottom > Dp(100);
    }

    private void TryRestoreKeyboard(
        IReadOnlyList<Rayo.Core.Platform.VirtualKeyboardAccessoryKey> keys,
        long session)
    {
        if (_disposed || session != _inputSession || !_restoreAfterResume || !_view.HasWindowFocus)
            return;

        var imm = _context.GetSystemService(global::Android.Content.Context.InputMethodService)
            as global::Android.Views.InputMethods.InputMethodManager;
        if (imm == null)
            return;

        _restoreAfterResume = false;
        _accessoryKeys = keys;
        _view.RequestFocus();
        imm.RestartInput(_view);
        imm.ShowSoftInput(_view, global::Android.Views.InputMethods.ShowFlags.Implicit);
        ShowAccessoryBar(keys, session);

        // The Activity can report focus slightly before the IME accepts requests.
        _view.PostDelayed(() => RetryShowKeyboard(imm, session), 100);
        _view.PostDelayed(() => RetryShowKeyboard(imm, session), 300);
    }

    private void RetryShowKeyboard(
        global::Android.Views.InputMethods.InputMethodManager imm,
        long session)
    {
        if (!_disposed && session == _inputSession && _view.HasWindowFocus && !IsKeyboardVisible())
        {
            _view.RequestFocus();
            imm.RestartInput(_view);
            imm.ShowSoftInput(_view, global::Android.Views.InputMethods.ShowFlags.Implicit);
        }
    }

    private void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Rayo.Reactivity.UIUpdateQueue.EnqueueUIUpdate(() =>
        {
            var eventManager = Rayo.Core.OverlayManager.EventManager;
            if (eventManager == null)
            {
                return;
            }

            foreach (char character in text)
            {
                eventManager.ProcessTextInput(character);
            }
        });
    }

    private int Dp(int value) => (int)MathF.Round(value * _context.Resources!.DisplayMetrics!.Density);
}
