namespace MonoGameSample;

using Rayo.Animation;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;

/// <summary>
/// A Rayo leaf control that embeds a live MonoGame scene.
/// <para>
/// On <see cref="OnMounted"/> the control spins up a <see cref="MonoGameScene"/> on a
/// background thread. Each animation tick it polls for a new rendered frame and, when one
/// arrives, uses <see cref="IRenderer.CreateTextureFromPixels"/> to blit it – meaning the
/// control works with <em>both</em> the OpenGL and SkiaSharp renderer backends.
/// </para>
/// </summary>
public class MonoGameView : View<MonoGameView>, IFrameAnimation
{
    private MonoGameScene? _scene;
    private Thread?        _gameThread;
    private bool           _animRegistered;
    private bool           _sceneStarted;   // true once the background thread has been launched

    // Latest frame and FPS received from the MonoGame thread
    private byte[]? _cachedFrame;
    private float   _cachedFps;

    // ── Reactive properties ────────────────────────────────────────────────────

    #region AnimSpeed
    /// <summary>Animation playback multiplier (0.1 – 4). Default is 1.</summary>
    public float AnimSpeed
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_scene != null) _scene.AnimSpeed = value;
        });
    } = 1.0f;
    #endregion

    #region BallCount
    /// <summary>Number of bouncing balls rendered by MonoGame (1 – 30). Default is 5.</summary>
    public int BallCount
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_scene != null) _scene.BallCount = value;
        });
    } = 5;
    #endregion

    #region MaxFps
    /// <summary>
    /// Maximum render rate of the embedded MonoGame scene in frames per second.
    /// Set to 0 to remove the cap. Default is 60.
    /// </summary>
    public int MaxFps
    {
        get => field;
        set => this.SetProperty(ref field, value, () =>
        {
            if (_scene != null) _scene.MaxFps = value;
        });
    } = 60;
    #endregion

    // ── Layout ────────────────────────────────────────────────────────────────

    public override void Measure(float availableWidth, float availableHeight)
    {
        float w = HorizontalAlignment == HorizontalAlignment.Stretch || Width <= 0
            ? (availableWidth  > 0 && !float.IsInfinity(availableWidth)  ? availableWidth  : 400f)
            : Width;

        float h = VerticalAlignment == VerticalAlignment.Stretch || Height <= 0
            ? (availableHeight > 0 && !float.IsInfinity(availableHeight) ? availableHeight : 300f)
            : Height;

        DesiredWidth  = w;
        DesiredHeight = h;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnMounted()
    {
        base.OnMounted();
        // Create the scene object but defer thread start to the first Tick().
        // This guarantees GLFW / Silk.NET is fully initialised before SDL2 tries
        // to create its own OpenGL context, avoiding the "Unable to retrieve
        // OpenGL version" race condition.
        _scene = new MonoGameScene { AnimSpeed = AnimSpeed, BallCount = BallCount, MaxFps = MaxFps };
        RegisterAnimation();
    }

    protected override void OnUnmounted()
    {
        base.OnUnmounted();
        UnregisterAnimation();
        _scene?.Exit();
    }

    private void LaunchMonoGame()
    {
        if (_scene == null || _sceneStarted) return;
        _sceneStarted = true;

        _gameThread = new Thread(() =>
        {
            // Short pause so the GLFW context has fully settled on the main thread
            // before SDL2 attempts to create its own OpenGL context.
            Thread.Sleep(200);
            try
            {
                _scene.Run();
            }
            catch (Exception ex)
            {
                // Surface the exception so Render() can display it
                _scene.LastError = ex.GetType().Name + ": " + ex.Message;
                Console.Error.WriteLine($"[MonoGameView] Scene failed: {ex}");
            }
        })
        {
            IsBackground = true,
            Name         = "MonoGameScene",
        };

        // STA is required on Windows for SDL2 window creation + COM subsystems
        _gameThread.TrySetApartmentState(ApartmentState.STA);

        _gameThread.Start();
    }

    private void RegisterAnimation()
    {
        if (_animRegistered) return;
        FrameAnimationTicker.Register(this);
        _animRegistered = true;
    }

    private void UnregisterAnimation()
    {
        if (!_animRegistered) return;
        FrameAnimationTicker.Unregister(this);
        _animRegistered = false;
    }

    // ── IFrameAnimation ───────────────────────────────────────────────────────

    void IFrameAnimation.Tick(float deltaTime)
    {
        // First tick: launch MonoGame now that Rayo + GLFW are fully up.
        if (!_sceneStarted)
        {
            LaunchMonoGame();
            return;
        }

        if (_scene == null) return;

        var frame = _scene.TryGetFrame();
        if (frame != null && !ReferenceEquals(frame, _cachedFrame))
        {
            _cachedFrame = frame;
            _cachedFps   = _scene.Fps;
            MarkNeedsPaint();
        }
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, Background);

        if (_cachedFrame != null)
        {
            // Happy path: blit the latest MonoGame frame.
            // CreateTextureFromPixels accepts raw RGBA bytes and works with both
            // the OpenGL and the SkiaSharp backends.
            using var texture = renderer.CreateTextureFromPixels(
                _cachedFrame,
                MonoGameScene.RenderWidth,
                MonoGameScene.RenderHeight);

            renderer.DrawTexture(texture, ComputedX, ComputedY, ComputedWidth, ComputedHeight);

            // FPS badge – top-right corner, drawn on top of the MonoGame frame
            if (_cachedFps > 0)
            {
                string label   = $"MonoGame  {_cachedFps:F0} FPS";
                float  padding = 6f;
                float  fontSize = 12f;
                var    size    = renderer.MeasureText(label, fontSize);
                float  bx      = ComputedX + ComputedWidth  - size.X - padding * 2 - 4;
                float  by      = ComputedY + padding;
                renderer.DrawRoundedRect(bx, by, size.X + padding * 2, size.Y + padding, 4, new Color(0, 0, 0, 160));
                renderer.DrawText(label, bx + padding, by + padding * 0.5f, new Color(120, 230, 120), fontSize);
            }

            return;
        }

        // No frame yet – show a loading or error overlay.
        var errorMsg = _scene?.LastError;
        float cx = ComputedX + ComputedWidth  * 0.5f;
        float cy = ComputedY + ComputedHeight * 0.5f;

        if (errorMsg != null)
        {
            // Error state
            renderer.DrawText("⚠ MonoGame error:", cx - 120, cy - 22, new Color(255, 100, 80), 13);
            renderer.DrawText(errorMsg,            cx - 120, cy + 2,  new Color(200, 80,  60), 11);
        }
        else
        {
            // Still loading
            renderer.DrawText("⏳ Starting MonoGame…", cx - 90, cy - 8, new Color(160, 164, 200), 13);
        }
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public new void Dispose()
    {
        base.Dispose();
        UnregisterAnimation();
        _scene?.Exit();
    }
}
