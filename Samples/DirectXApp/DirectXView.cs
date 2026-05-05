namespace DirectXApp;

using Rayo.Animation;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Numerics;

/// <summary>
/// A Rayo leaf control that embeds a live GPU-rendered 3D scene via Direct3D 11.
/// <para>
/// <see cref="DirectXScene3D"/> owns its own D3D11 device and renders the cube to an
/// off-screen RGBA8 texture. Each animation tick the pixels are read back and uploaded
/// to the UI renderer via <see cref="IRenderer.CreateTextureFromPixels"/>, making the
/// control compatible with any renderer backend (SkiaSharp, OpenGL, Vulkan).
/// </para>
/// </summary>
public class DirectXView : View<DirectXView>, IFrameAnimation
{
    private DirectXScene3D? _scene;
    private bool            _animRegistered;

    // Latest rendered frame — updated in Tick(), blitted in Render()
    private byte[]? _cachedFrame;
    private int     _cachedW, _cachedH;

    // FPS throttle state
    private float _timeSinceRender;
    private float _fps;

    // Pre-mount backing for CubeColor
    private Vector3 _cubeColorVec = new(0.3f, 0.6f, 1.0f);

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
    /// Maximum GPU render rate in frames per second. Set to 0 for unlimited. Default is 60.
    /// </summary>
    public int MaxFps
    {
        get => field;
        set => this.SetProperty(ref field, Math.Max(0, value));
    } = 60;
    #endregion

    #region CubeColor
    public Brush CubeColor
    {
        get => new Color(_cubeColorVec.X, _cubeColorVec.Y, _cubeColorVec.Z);
        set
        {
            var c = value.PrimaryColor;
            var v = new Vector3(c.R, c.G, c.B);
            if (_cubeColorVec == v) return;
            _cubeColorVec = v;
            if (_scene != null) _scene.CubeColor = v;
        }
    }
    #endregion

    #region Rotation
    public new Vector3 Rotation
    {
        get => _scene?.Rotation ?? Vector3.Zero;
        set { if (_scene != null) _scene.Rotation = value; }
    }
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
        _scene = new DirectXScene3D
        {
            AnimationSpeed = AnimationSpeed,
            Animate        = Animate,
            CubeColor      = _cubeColorVec,
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

        // Always advance the animation clock at full rate regardless of frame cap.
        _scene.Tick(deltaTime);

        _timeSinceRender += deltaTime;

        // Throttle GPU renders to MaxFps (0 = unlimited).
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
                string label = $"D3D11  {_fps:F0} FPS";
                float  pad   = 6f;
                float  fs    = 12f;
                var    size  = renderer.MeasureText(label, fs);
                float  bx    = ComputedX + ComputedWidth  - size.X - pad * 2 - 4;
                float  by    = ComputedY + pad;
                renderer.DrawRoundedRect(bx, by, size.X + pad * 2, size.Y + pad, 4,
                    new Color(0, 0, 0, 160));
                renderer.DrawText(label, bx + pad, by + pad * 0.5f,
                    new Color(100, 180, 255), fs);
            }

            return;
        }

        // No frame yet — loading indicator
        float cx = ComputedX + ComputedWidth  * 0.5f;
        float cy = ComputedY + ComputedHeight * 0.5f;
        renderer.DrawText("Initializing D3D11 scene...", cx - 105, cy - 8,
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
