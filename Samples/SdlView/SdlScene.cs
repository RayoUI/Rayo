namespace SdlApp;

using Silk.NET.Maths;
using Silk.NET.SDL;

/// <summary>
/// Self-contained SDL2 rendering scene that draws a spinning geometric mandala
/// to an off-screen SDL texture and exposes the result as a raw RGBA byte array.
/// <para>
/// Owns its own SDL instance, hidden window, and renderer — completely independent
/// of Rayo's rendering stack. Composited into the UI via
/// <see cref="Rayo.Rendering.IRenderer.CreateTextureFromPixels"/>.
/// </para>
/// </summary>
public sealed unsafe class SdlScene : IDisposable
{
    private readonly Sdl _sdl;
    private Window*      _window;
    private Renderer*    _renderer;
    private Texture*     _target;
    private int          _targetW, _targetH;

    // Last dimensions reported by the view (used for bounce bounds in Tick)
    private int _sceneW = 400, _sceneH = 400;

    // Animation clock
    private float _time;

    // ── Pixel format ──────────────────────────────────────────────────────────
    // SDL_PIXELFORMAT_ABGR8888 = 376840196
    // On little-endian (x86/x64): memory layout is R,G,B,A — matches
    // IRenderer.CreateTextureFromPixels which expects RGBA bytes.
    private const uint PixFmt = 376840196u;

    // ── Configurable properties ───────────────────────────────────────────────

    public float     AnimationSpeed { get; set; } = 1.0f;
    public bool      Animate        { get; set; } = true;

    private int _rings = 6;
    public int Rings
    {
        get => _rings;
        set => _rings = Math.Clamp(value, 2, 14);
    }

    private int _segments = 8;
    public int Segments
    {
        get => _segments;
        set => _segments = Math.Clamp(value, 3, 20);
    }

    public SdlColorMode ColorMode { get; set; } = SdlColorMode.Rainbow;

    // ── Construction ─────────────────────────────────────────────────────────

    public SdlScene()
    {
        _sdl = Sdl.GetApi();

        if (_sdl.Init(Sdl.InitVideo) != 0)
            throw new InvalidOperationException("SDL_Init(SDL_INIT_VIDEO) failed.");

        // Hidden window — only needed to obtain a renderer handle.
        _window = _sdl.CreateWindow(
            "sdl-offscreen",
            100, 100, 1, 1,
            (uint)WindowFlags.Hidden);

        if (_window == null)
            throw new InvalidOperationException("SDL_CreateWindow failed.");

        // Try hardware-accelerated renderer; fall back to software.
        _renderer = _sdl.CreateRenderer(_window, -1,
            (uint)(RendererFlags.Accelerated | RendererFlags.Targettexture));

        if (_renderer == null)
        {
            _renderer = _sdl.CreateRenderer(_window, -1,
                (uint)(RendererFlags.Software | RendererFlags.Targettexture));
        }

        if (_renderer == null)
            throw new InvalidOperationException("SDL_CreateRenderer failed.");
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    public void Tick(float dt)
    {
        if (Animate) _time += dt * AnimationSpeed;
    }

    // ── Render ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the current frame to an off-screen SDL texture and returns the
    /// raw RGBA pixel data (width × height × 4 bytes).
    /// </summary>
    public byte[] RenderFrame(int w, int h)
    {
        _sceneW = w;
        _sceneH = h;
        EnsureTarget(w, h);

        _sdl.SetRenderTarget(_renderer, _target);
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        // Clear to background
        _sdl.SetRenderDrawColor(_renderer, 15, 17, 28, 255);
        _sdl.RenderClear(_renderer);

        DrawMandala(w, h);

        // Read back RGBA pixels from the current render target
        var pixels = new byte[w * h * 4];
        fixed (byte* p = pixels)
            _sdl.RenderReadPixels(_renderer, (Rectangle<int>*)null, PixFmt, p, w * 4);

        _sdl.SetRenderTarget(_renderer, (Texture*)null);
        return pixels;
    }

    // ── Mandala drawing ───────────────────────────────────────────────────────

    private void DrawMandala(int w, int h)
    {
        float cx         = w * 0.5f;
        float cy         = h * 0.5f;
        float maxRadius  = Math.Min(w, h) * 0.44f;
        int   rings      = Rings;
        int   segments   = Segments;

        for (int ring = 0; ring < rings; ring++)
        {
            float t      = (ring + 1f) / rings;
            float radius = maxRadius * t;

            // Alternate rotation direction; outer rings rotate faster
            float dir    = (ring % 2 == 0) ? 1f : -1f;
            float speed  = 0.35f + ring * 0.12f;
            float angle  = _time * dir * speed;

            var (r, g, b) = GetColor(ring, rings);

            // Polygon outline
            _sdl.SetRenderDrawColor(_renderer, r, g, b, 220);
            DrawPolygon(cx, cy, radius, segments, angle);

            // Web lines: connect matching vertices of adjacent rings
            if (ring > 0)
            {
                float prevRadius = maxRadius * ring / rings;
                float prevDir    = ((ring - 1) % 2 == 0) ? 1f : -1f;
                float prevSpeed  = 0.35f + (ring - 1) * 0.12f;
                float prevAngle  = _time * prevDir * prevSpeed;

                _sdl.SetRenderDrawColor(_renderer, r, g, b, 70);
                DrawWebLines(cx, cy, prevRadius, radius, segments, prevAngle, angle);
            }
        }

        // Spokes from center to innermost ring vertices
        {
            float innerRadius = maxRadius / rings;
            float innerAngle  = _time * 0.35f;
            var (r, g, b) = GetColor(0, rings);
            _sdl.SetRenderDrawColor(_renderer, r, g, b, 80);
            DrawSpokes(cx, cy, innerRadius, segments, innerAngle);
        }

        // Center square
        _sdl.SetRenderDrawColor(_renderer, 255, 255, 255, 200);
        var dot = new Rectangle<int>((int)cx - 4, (int)cy - 4, 8, 8);
        _sdl.RenderFillRect(_renderer, &dot);
    }

    private void DrawPolygon(float cx, float cy, float radius, int sides, float angleOffset)
    {
        float step = MathF.Tau / sides;
        for (int i = 0; i < sides; i++)
        {
            float a1 = angleOffset + i       * step;
            float a2 = angleOffset + (i + 1) * step;
            int   x1 = (int)(cx + MathF.Cos(a1) * radius);
            int   y1 = (int)(cy + MathF.Sin(a1) * radius);
            int   x2 = (int)(cx + MathF.Cos(a2) * radius);
            int   y2 = (int)(cy + MathF.Sin(a2) * radius);
            _sdl.RenderDrawLine(_renderer, x1, y1, x2, y2);
        }
    }

    private void DrawWebLines(
        float cx, float cy,
        float r1, float r2,
        int sides,
        float angle1, float angle2)
    {
        float step = MathF.Tau / sides;
        for (int i = 0; i < sides; i++)
        {
            float a1 = angle1 + i * step;
            float a2 = angle2 + i * step;
            int   x1 = (int)(cx + MathF.Cos(a1) * r1);
            int   y1 = (int)(cy + MathF.Sin(a1) * r1);
            int   x2 = (int)(cx + MathF.Cos(a2) * r2);
            int   y2 = (int)(cy + MathF.Sin(a2) * r2);
            _sdl.RenderDrawLine(_renderer, x1, y1, x2, y2);
        }
    }

    private void DrawSpokes(float cx, float cy, float radius, int sides, float angleOffset)
    {
        float step = MathF.Tau / sides;
        for (int i = 0; i < sides; i++)
        {
            float a = angleOffset + i * step;
            int   x = (int)(cx + MathF.Cos(a) * radius);
            int   y = (int)(cy + MathF.Sin(a) * radius);
            _sdl.RenderDrawLine(_renderer, (int)cx, (int)cy, x, y);
        }
    }

    // ── Color helpers ─────────────────────────────────────────────────────────

    private (byte r, byte g, byte b) GetColor(int ring, int totalRings)
    {
        float t = (float)ring / Math.Max(1, totalRings - 1);
        return ColorMode switch
        {
            SdlColorMode.Fire  => HsvToRgb(Lerp(0.00f, 0.10f, t) + (_time * 0.03f % 0.10f), 0.90f, 1.0f),
            SdlColorMode.Ocean => HsvToRgb(Lerp(0.55f, 0.68f, t) + (_time * 0.02f % 0.13f), 0.80f, 1.0f),
            SdlColorMode.Mono  => HsvToRgb(0f, 0f, Lerp(0.30f, 1.0f, t)),
            _                  => HsvToRgb((_time * 0.05f + t * 0.75f) % 1f, 0.85f, 1.0f), // Rainbow
        };
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static (byte r, byte g, byte b) HsvToRgb(float h, float s, float v)
    {
        h = ((h % 1f) + 1f) % 1f;
        float c  = v * s;
        float x  = c * (1f - MathF.Abs(h * 6f % 2f - 1f));
        float m  = v - c;
        int   hi = (int)(h * 6f) % 6;
        var (rf, gf, bf) = hi switch
        {
            0 => (c, x, 0f),
            1 => (x, c, 0f),
            2 => (0f, c, x),
            3 => (0f, x, c),
            4 => (x, 0f, c),
            _ => (c, 0f, x),
        };
        return ((byte)((rf + m) * 255), (byte)((gf + m) * 255), (byte)((bf + m) * 255));
    }

    // ── Render target management ───────────────────────────────────────────────

    private void EnsureTarget(int w, int h)
    {
        if (_target != null && _targetW == w && _targetH == h) return;

        if (_target != null) _sdl.DestroyTexture(_target);

        _target  = _sdl.CreateTexture(_renderer, PixFmt, (int)TextureAccess.Target, w, h);
        _targetW = w;
        _targetH = h;
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_target   != null) { _sdl.DestroyTexture(_target);    _target   = null; }
        if (_renderer != null) { _sdl.DestroyRenderer(_renderer); _renderer = null; }
        if (_window   != null) { _sdl.DestroyWindow(_window);     _window   = null; }
        _sdl.Quit();
        _sdl.Dispose();
    }
}

public enum SdlColorMode { Rainbow, Fire, Ocean, Mono }
