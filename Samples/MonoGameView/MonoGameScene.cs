using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameSample;

/// <summary>
/// MonoGame <see cref="Game"/> subclass that renders a bouncing-balls scene to an
/// off-screen <see cref="RenderTarget2D"/>. Designed to run on a background thread.
/// <para>
/// Pixel data is shared thread-safely with <see cref="MonoGameView"/> via
/// <see cref="TryGetFrame"/> so Rayo can blit it each frame.
/// </para>
/// </summary>
internal sealed class MonoGameScene : Game
{
    public const int RenderWidth  = 800;
    public const int RenderHeight = 500;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch?    _spriteBatch;
    private RenderTarget2D? _renderTarget;
    private Texture2D?      _ballTexture;

    // Ball physics state
    private readonly record struct Ball(
        Vector2 Position,
        Vector2 Velocity,
        Color   Color,
        float   Radius);

    private Ball[]    _balls = [];
    private readonly Random _rng = new(42);

    // Pre-allocated staging buffer reused every frame to avoid per-frame Color[] GC
    private readonly Color[] _colorStage = new Color[RenderWidth * RenderHeight];

    // Thread-safe pixel sharing
    private byte[]?      _latestFrame;
    private readonly object _frameLock = new();

    // Last initialization/runtime error (null when healthy)
    private string? _lastError;

    // Properties written from the Rayo thread (volatile for single-write visibility)
    private volatile float _animSpeed   = 1.0f;
    private volatile int   _targetCount = 5;

    public MonoGameScene()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth           = 1,
            PreferredBackBufferHeight          = 1,
            SynchronizeWithVerticalRetrace     = false,
        };
        IsMouseVisible    = false;
        IsFixedTimeStep   = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        Content.RootDirectory = "Content";
    }

    // ── Public API (called from the Rayo main thread) ─────────────────────

    public float AnimSpeed
    {
        get => _animSpeed;
        set => _animSpeed = Math.Max(0.1f, value);
    }

    public int BallCount
    {
        get => _targetCount;
        set => _targetCount = Math.Clamp(value, 1, 30);
    }

    /// <summary>
    /// Caps the MonoGame render loop. Set to 0 or less to remove the cap.
    /// Defaults to 60.
    /// </summary>
    public int MaxFps
    {
        set
        {
            if (value <= 0)
            {
                IsFixedTimeStep = false;
            }
            else
            {
                IsFixedTimeStep   = true;
                TargetElapsedTime = TimeSpan.FromSeconds(1.0 / value);
            }
        }
    }

    /// <summary>Returns the most recently completed rendered frame as an RGBA byte array,
    /// or <c>null</c> if no frame is ready yet.</summary>
    public byte[]? TryGetFrame()
    {
        lock (_frameLock)
            return _latestFrame;
    }

    /// <summary>Returns any initialization or runtime error message, or <c>null</c> when healthy.</summary>
    public string? LastError
    {
        get { lock (_frameLock) return _lastError; }
        set { lock (_frameLock) _lastError = value; }
    }

    // ── Game lifecycle ─────────────────────────────────────────────────────────

    protected override void Initialize()
    {
        // Keep the SDL window tiny and well off-screen.
        // Extreme values like -32000 can cause issues with some GPU drivers.
        Window.IsBorderless    = true;
        Window.AllowUserResizing = false;
        Window.Position        = new Point(-500, -500);
        base.Initialize();
        Console.Error.WriteLine("[MonoGameScene] Initialize() completed.");
    }

    protected override void LoadContent()
    {
        _spriteBatch  = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, RenderWidth, RenderHeight);
        _ballTexture  = CreateCircleTexture(64);
        ResetBalls(_targetCount);
        Console.Error.WriteLine("[MonoGameScene] LoadContent() completed – scene ready.");
    }

    protected override void Update(GameTime gameTime)
    {
        float dt      = (float)gameTime.ElapsedGameTime.TotalSeconds * _animSpeed;
        int   needed  = _targetCount;

        if (_balls.Length != needed)
            ResetBalls(needed);

        for (int i = 0; i < _balls.Length; i++)
        {
            var b   = _balls[i];
            var pos = b.Position + b.Velocity * dt;
            var vel = b.Velocity;

            if (pos.X - b.Radius < 0)
            {
                pos.X =  b.Radius;
                vel.X =  Math.Abs(vel.X);
            }
            else if (pos.X + b.Radius > RenderWidth)
            {
                pos.X =  RenderWidth - b.Radius;
                vel.X = -Math.Abs(vel.X);
            }

            if (pos.Y - b.Radius < 0)
            {
                pos.Y =  b.Radius;
                vel.Y =  Math.Abs(vel.Y);
            }
            else if (pos.Y + b.Radius > RenderHeight)
            {
                pos.Y =  RenderHeight - b.Radius;
                vel.Y = -Math.Abs(vel.Y);
            }

            _balls[i] = b with { Position = pos, Velocity = vel };
        }

        base.Update(gameTime);
    }

    private int   _drawCallCount;
    private double _fpsAccum;       // seconds accumulated since last FPS snapshot
    private int    _fpsFrames;      // frames counted in the current second

    /// <summary>Smoothed frames-per-second measured by the MonoGame thread.</summary>
    public volatile float Fps;

    protected override void Draw(GameTime gameTime)
    {
        _drawCallCount++;
        if (_drawCallCount == 1)
            Console.Error.WriteLine("[MonoGameScene] First Draw() – scene is alive.");

        // ── FPS counter ───────────────────────────────────────────────────────
        double dt = gameTime.ElapsedGameTime.TotalSeconds;
        _fpsAccum  += dt;
        _fpsFrames++;
        if (_fpsAccum >= 0.5)   // update twice per second for a responsive read-out
        {
            Fps        = (float)(_fpsFrames / _fpsAccum);
            _fpsAccum  = 0;
            _fpsFrames = 0;
        }

        // ── Render scene to off-screen target ─────────────────────────────────
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(18, 20, 38));
        _spriteBatch!.Begin(blendState: BlendState.NonPremultiplied);

        foreach (var b in _balls)
        {
            int size = (int)(b.Radius * 2);
            _spriteBatch.Draw(
                _ballTexture!,
                new Rectangle(
                    (int)(b.Position.X - b.Radius),
                    (int)(b.Position.Y - b.Radius),
                    size, size),
                b.Color);
        }

        _spriteBatch.End();

        // ── GPU → CPU pixel readback ──────────────────────────────────────────
        _renderTarget!.GetData(_colorStage);

        // Convert MonoGame Color (RGBA component properties) → raw RGBA bytes
        // so Rayo's CreateTextureFromPixels can consume them directly.
        var frame = new byte[RenderWidth * RenderHeight * 4];
        for (int i = 0; i < _colorStage.Length; i++)
        {
            var c = _colorStage[i];
            int j = i * 4;
            frame[j]     = c.R;
            frame[j + 1] = c.G;
            frame[j + 2] = c.B;
            frame[j + 3] = c.A;
        }

        lock (_frameLock)
            _latestFrame = frame;

        // ── Return to default backbuffer ──────────────────────────────────────
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void ResetBalls(int count)
    {
        var balls = new Ball[count];
        for (int i = 0; i < count; i++)
        {
            float r   = 15f + (float)_rng.NextDouble() * 30f;
            var   pos = new Vector2(
                r + (float)_rng.NextDouble() * (RenderWidth  - r * 2),
                r + (float)_rng.NextDouble() * (RenderHeight - r * 2));
            var   vel = new Vector2(
                (70f + (float)_rng.NextDouble() * 130f) * (_rng.NextDouble() > .5 ? 1f : -1f),
                (70f + (float)_rng.NextDouble() * 130f) * (_rng.NextDouble() > .5 ? 1f : -1f));
            var   col = new Color(
                (float)_rng.NextDouble(),
                (float)_rng.NextDouble(),
                (float)_rng.NextDouble());

            balls[i] = new Ball(pos, vel, col, r);
        }
        _balls = balls;
    }

    /// <summary>Generates a smooth circle sprite (white, alpha at edges).</summary>
    private Texture2D CreateCircleTexture(int diameter)
    {
        var   tex  = new Texture2D(GraphicsDevice, diameter, diameter);
        var   data = new Color[diameter * diameter];
        float r    = diameter / 2f;

        for (int y = 0; y < diameter; y++)
        for (int x = 0; x < diameter; x++)
        {
            float dist  = MathF.Sqrt((x - r + .5f) * (x - r + .5f) + (y - r + .5f) * (y - r + .5f));
            float alpha = Math.Clamp(r - dist, 0f, 1f);
            data[y * diameter + x] = new Color(1f, 1f, 1f, alpha);
        }

        tex.SetData(data);
        return tex;
    }
}
