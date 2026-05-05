namespace SdlApp;

using Rayo.Animation;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;

/// <summary>
/// A Rayo leaf control that embeds a live SDL2-rendered scene (spinning mandala).
/// <para>
/// <see cref="SdlScene"/> owns its own SDL2 device stack and renders to an off-screen
/// texture each tick. Pixels are read back and composited into the UI via
/// <see cref="IRenderer.CreateTextureFromPixels"/>, making this control compatible
/// with any renderer backend (SkiaSharp, OpenGL, Vulkan).
/// </para>
/// </summary>
public class SdlView : View<SdlView>, IFrameAnimation
{
    private SdlScene? _scene;
    private bool      _animRegistered;

    // Latest rendered frame — updated in Tick(), blitted in Render()
    private byte[]? _cachedFrame;
    private int     _cachedW, _cachedH;

    // FPS throttle state
    private float _timeSinceRender;
    private float _fps;

    // ── Reactive properties ───────────────────────────────────────────────

    #region AnimationSpeed
    public float AnimationSpeed
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { if (_scene != null) _scene.AnimationSpeed = value; });
    } = 1.0f;
    #endregion

    #region Animate
    public bool Animate
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { if (_scene != null) _scene.Animate = value; });
    } = true;
    #endregion

    #region MaxFps
    /// <summary>
    /// Maximum SDL render rate in frames per second. Set to 0 for unlimited. Default is 60.
    /// </summary>
    public int MaxFps
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(0, value));
    } = 60;
    #endregion

    #region Rings
    public int Rings
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { if (_scene != null) _scene.Rings = value; });
    } = 6;
    #endregion

    #region Segments
    public int Segments
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { if (_scene != null) _scene.Segments = value; });
    } = 8;
    #endregion

    #region ColorMode
    public SdlColorMode ColorMode
    {
        get => field;
        set => this.SetProperty(ref field, value, () => { if (_scene != null) _scene.ColorMode = value; });
    } = SdlColorMode.Rainbow;
    #endregion

    // ── Layout ────────────────────────────────────────────────────────────

    public override void Measure(float availableWidth, float availableHeight)
    {
        float w = HorizontalAlignment == HorizontalAlignment.Stretch || Width <= 0
            ? (availableWidth  > 0 && !float.IsInfinity(availableWidth)  ? availableWidth  : 400f)
            : Width;

        float h = VerticalAlignment == VerticalAlignment.Stretch || Height <= 0
            ? (availableHeight > 0 && !float.IsInfinity(availableHeight) ? availableHeight : 400f)
            : Height;

        DesiredWidth  = w;
        DesiredHeight = h;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override void OnMounted()
    {
        base.OnMounted();
        _scene = new SdlScene
        {
            AnimationSpeed = AnimationSpeed,
            Animate        = Animate,
            Rings          = Rings,
            Segments       = Segments,
            ColorMode      = ColorMode,
        };
        RegisterAnimation();
    }

    protected override void OnUnmounted()
    {
        base.OnUnmounted();
        UnregisterAnimation();
        _scene?.Dispose();
        _scene = null;
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

    // ── IFrameAnimation ───────────────────────────────────────────────────

    void IFrameAnimation.Tick(float deltaTime)
    {
        if (_scene == null) return;

        // Always advance the animation clock at full rate.
        _scene.Tick(deltaTime);

        _timeSinceRender += deltaTime;

        // Throttle SDL renders to MaxFps (0 = unlimited).
        float frameInterval = MaxFps > 0 ? 1f / MaxFps : 0f;
        if (_timeSinceRender < frameInterval) return;

        int w = Math.Max(1, (int)ComputedWidth);
        int h = Math.Max(1, (int)ComputedHeight);

        _cachedFrame = _scene.RenderFrame(w, h);
        _cachedW     = w;
        _cachedH     = h;

        _fps             = _timeSinceRender > 0f ? 1f / _timeSinceRender : 0f;
        _timeSinceRender = 0f;

        MarkNeedsPaint();
    }

    // ── Rendering ─────────────────────────────────────────────────────────

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, Background);

        if (_cachedFrame != null && _cachedW > 0 && _cachedH > 0)
        {
            using var texture = renderer.CreateTextureFromPixels(_cachedFrame, _cachedW, _cachedH);
            renderer.DrawTexture(texture, ComputedX, ComputedY, ComputedWidth, ComputedHeight);

            // FPS badge — top-right corner
            if (_fps > 0f)
            {
                string label = $"SDL2  {_fps:F0} FPS";
                float  pad   = 6f;
                float  fs    = 12f;
                var    size  = renderer.MeasureText(label, fs);
                float  bx    = ComputedX + ComputedWidth  - size.X - pad * 2 - 4;
                float  by    = ComputedY + pad;
                renderer.DrawRoundedRect(bx, by, size.X + pad * 2, size.Y + pad, 4,
                    new Color(0, 0, 0, 160));
                renderer.DrawText(label, bx + pad, by + pad * 0.5f,
                    new Color(120, 230, 120), fs);
            }

            return;
        }

        // No frame yet — loading indicator
        float cx = ComputedX + ComputedWidth  * 0.5f;
        float cy = ComputedY + ComputedHeight * 0.5f;
        renderer.DrawText("Initializing SDL2 scene...", cx - 100, cy - 8,
            new Color(160, 164, 200), 13);
    }

    // ── Disposal ──────────────────────────────────────────────────────────

    public new void Dispose()
    {
        base.Dispose();
        UnregisterAnimation();
        _scene?.Dispose();
        _scene = null;
    }
}
