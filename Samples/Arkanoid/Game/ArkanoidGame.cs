namespace Arkanoid.Game;

using Arkanoid.Engine;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// All Arkanoid game state and logic: physics, collision, level loading, lives, score.
/// Pure C# — no Rayo dependencies except <see cref="IRenderer"/> used by game objects.
/// </summary>
public class ArkanoidGame
{
    // ── Constants ─────────────────────────────────────────────────────────────
    private const float BrickMargin  = 4f;
    private const float BrickOffsetY = 60f;
    private const int   BrickCols    = 10;
    private const int   BrickRows    = 6;

    // ── World ──────────────────────────────────────────────────────────────────
    public  GameWorld World       { get; } = new();
    public  float     WorldWidth  { get; private set; }
    public  float     WorldHeight { get; private set; }

    // ── Actors ────────────────────────────────────────────────────────────────
    public  Paddle?   Paddle  { get; private set; }
    public  Ball?     Ball    { get; private set; }

    // ── Game state ─────────────────────────────────────────────────────────────
    public int     Score    { get; private set; }
    public int     Lives    { get; private set; } = 3;
    public int     Level    { get; private set; } = 1;
    public GamePhase Phase  { get; private set; } = GamePhase.WaitingToStart;

    public bool IsRunning  => Phase == GamePhase.Playing;
    public bool IsGameOver => Phase == GamePhase.GameOver;
    public bool IsWon      => Phase == GamePhase.Won;

    private readonly Random _rng = new();

    // ── Input ─────────────────────────────────────────────────────────────────
    private bool _keyLeft;
    private bool _keyRight;
    private bool _keyLaunch;

    public void SetInput(bool left, bool right, bool launch)
    {
        _keyLeft   = left;
        _keyRight  = right;
        _keyLaunch = launch;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Initialize(float worldWidth, float worldHeight)
    {
        WorldWidth  = worldWidth;
        WorldHeight = worldHeight;
        World.WorldWidth  = worldWidth;
        World.WorldHeight = worldHeight;
        StartLevel();
    }

    public void Resize(float worldWidth, float worldHeight)
    {
        WorldWidth  = worldWidth;
        WorldHeight = worldHeight;
        World.WorldWidth  = worldWidth;
        World.WorldHeight = worldHeight;

        // Keep paddle and ball proportionally positioned
        if (Paddle != null)
            Paddle.Position = new Vector2(worldWidth / 2f - Paddle.Width / 2f, worldHeight - 34f);
        if (Ball != null && Ball.IsAttached && Paddle != null)
            Ball.Position = new Vector2(Paddle.Position.X + Paddle.Width / 2f - Ball.Radius,
                                        Paddle.Position.Y - Ball.Height - 2f);
    }

    private void StartLevel()
    {
        World.Clear();

        Paddle = Paddle.Create(WorldWidth, WorldHeight);
        World.Add(Paddle);

        Ball = Ball.Create();
        AttachBallToPaddle();
        World.Add(Ball);

        LoadBricks(Level);

        // Flush pending → _objects so Render works before the first Playing tick.
        World.Update(0f);

        Phase = GamePhase.WaitingToStart;
    }

    private void AttachBallToPaddle()
    {
        if (Ball == null || Paddle == null) return;
        Ball.IsAttached = true;
        Ball.Position = new Vector2(
            Paddle.Position.X + Paddle.Width / 2f - Ball.Radius,
            Paddle.Position.Y - Ball.Height - 2f);
    }

    // ── Level layout ──────────────────────────────────────────────────────────

    private static readonly Color[] RowColors =
    [
        new Color(230, 60,  60),   // row 0 — red    (hardest, multi-hit)
        new Color(230, 140, 40),   // row 1 — orange
        new Color(220, 210, 40),   // row 2 — yellow
        new Color(60,  200, 80),   // row 3 — green
        new Color(60,  160, 220),  // row 4 — blue
        new Color(160, 80,  220),  // row 5 — purple
    ];

    private void LoadBricks(int level)
    {
        float bw = (WorldWidth - BrickMargin * (BrickCols + 1)) / BrickCols;
        float bh = 22f;

        for (int row = 0; row < BrickRows; row++)
        {
            int hitPoints  = row < 2 ? 2 : 1;          // top two rows take 2 hits
            int scoreValue = (BrickRows - row) * 10 * level;
            var color      = RowColors[row];

            for (int col = 0; col < BrickCols; col++)
            {
                float x = BrickMargin + col * (bw + BrickMargin);
                float y = BrickOffsetY + row * (bh + BrickMargin);
                World.Add(Brick.Create(x, y, bw, bh, color, hitPoints, scoreValue));
            }
        }
    }

    // ── Main update ───────────────────────────────────────────────────────────

    public void Update(float deltaTime)
    {
        if (Phase == GamePhase.GameOver || Phase == GamePhase.Won) return;

        // Launch
        if (_keyLaunch && Phase == GamePhase.WaitingToStart && Ball != null)
        {
            Ball.Launch(_rng);
            Phase = GamePhase.Playing;
        }

        // Paddle movement
        if (Paddle != null)
        {
            Paddle.MoveDirection = (_keyLeft ? -1f : 0f) + (_keyRight ? 1f : 0f);
            Paddle.Update(deltaTime);
            Paddle.ClampToWorld(WorldWidth);

            // Ball follows paddle while attached
            if (Ball != null && Ball.IsAttached)
                Ball.Position = new Vector2(
                    Paddle.Position.X + Paddle.Width / 2f - Ball.Radius,
                    Paddle.Position.Y - Ball.Height - 2f);
        }

        if (Phase != GamePhase.Playing) return;

        // Ball physics & collisions (sub-stepped for accuracy)
        const int steps = 3;
        float subDt = deltaTime / steps;
        for (int s = 0; s < steps; s++)
        {
            Ball?.Update(subDt);
            ResolveCollisions();
        }

        // Tick brick flash timers (ball/paddle are already updated above; skip them)
        foreach (var brick in World.ObjectsOfType<Brick>())
            brick.Update(deltaTime);

        // Sweep dead bricks and flush any pending additions
        World.FlushPendingAndSweep();

        // Check win
        if (!World.ObjectsOfType<Brick>().Any())
        {
            Level++;
            Phase = GamePhase.Won;
        }
    }

    private void ResolveCollisions()
    {
        if (Ball == null || Paddle == null) return;

        var b = Ball;

        // ── Wall collisions ────────────────────────────────────────────────
        if (b.Position.X <= 0)
        {
            b.Position = new Vector2(0, b.Position.Y);
            b.BounceX();
        }
        else if (b.Position.X + b.Width >= WorldWidth)
        {
            b.Position = new Vector2(WorldWidth - b.Width, b.Position.Y);
            b.BounceX();
        }

        if (b.Position.Y <= 0)
        {
            b.Position = new Vector2(b.Position.X, 0);
            b.BounceY();
        }

        // ── Ball out (bottom) ──────────────────────────────────────────────
        if (b.Position.Y > WorldHeight)
        {
            Lives--;
            if (Lives <= 0)
            {
                Phase = GamePhase.GameOver;
                return;
            }

            AttachBallToPaddle();
            Phase = GamePhase.WaitingToStart;
            return;
        }

        // ── Paddle collision ───────────────────────────────────────────────
        var ballRect   = b.Bounds;
        var paddleRect = Paddle.Bounds;

        if (ballRect.Intersects(paddleRect) && b.Velocity.Y > 0)
        {
            b.Position = new Vector2(b.Position.X, Paddle.Position.Y - b.Height);
            float fraction = (b.Position.X + b.Radius - Paddle.Position.X) / Paddle.Width;
            b.BounceOffPaddle(Math.Clamp(fraction, 0f, 1f));
        }

        // ── Brick collisions ───────────────────────────────────────────────
        foreach (var brick in World.ObjectsOfType<Brick>())
        {
            var brickRect = brick.Bounds;
            if (!ballRect.Intersects(brickRect)) continue;

            // Determine which axis to bounce on from overlap depth
            var overlap = ballRect.Overlap(brickRect);
            if (overlap.Width < overlap.Height)
                b.BounceX();
            else
                b.BounceY();

            bool destroyed = brick.Hit();
            Score += brick.ScoreValue;
            break; // one brick per sub-step
        }
    }

    // ── Restart ───────────────────────────────────────────────────────────────

    public void RestartGame()
    {
        Score = 0;
        Lives = 3;
        Level = 1;
        StartLevel();
    }

    public void NextLevel()
    {
        StartLevel();
        // Increase difficulty: slightly faster ball per level
        if (Ball != null)
            Ball.Speed = 320f + (Level - 1) * 20f;
    }
}

public enum GamePhase { WaitingToStart, Playing, GameOver, Won }
