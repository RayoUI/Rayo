namespace Arkanoid.Game;

using Arkanoid.Engine;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// The game ball. Moves at constant speed, bounces off walls and paddle.
/// </summary>
public class Ball : GameObject
{
    public float  Radius { get; set; } = 8f;
    public float  Speed  { get; set; } = 320f;

    /// <summary>True while attached to the paddle before the first launch.</summary>
    public bool IsAttached { get; set; } = true;

    public static Ball Create() => new()
    {
        Radius = 8f,
        Speed  = 320f,
        Width  = 16f,
        Height = 16f,
        IsAttached = true
    };

    /// <summary>Launches the ball upward with a small random angle.</summary>
    public void Launch(Random rng)
    {
        IsAttached = false;
        float angle = -MathF.PI / 2f + (rng.NextSingle() - 0.5f) * (MathF.PI / 3f);
        Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Speed;
    }

    public void BounceX() => Velocity = new Vector2(-Velocity.X,  Velocity.Y);
    public void BounceY() => Velocity = new Vector2( Velocity.X, -Velocity.Y);

    /// <summary>
    /// Reflects off the paddle with angle influenced by hit position.
    /// <paramref name="hitFraction"/> is 0 (left edge) → 1 (right edge).
    /// </summary>
    public void BounceOffPaddle(float hitFraction)
    {
        float t = hitFraction * 2f - 1f;           // -1 .. +1
        float angle = t * (MathF.PI / 3f);          // ±60° from straight up
        float spd = Velocity.Length();
        Velocity = new Vector2(MathF.Sin(angle), -MathF.Abs(MathF.Cos(angle))) * spd;
    }

    public override void Update(float deltaTime)
    {
        if (IsAttached) return;
        Position += Velocity * deltaTime;
    }

    public override void Render(IRenderer renderer, float ox, float oy)
    {
        float cx = ox + Position.X + Radius;
        float cy = oy + Position.Y + Radius;
        var color = new Color(255, 240, 80);

        renderer.DrawCircle(cx, cy, Radius + 4f, new Color(255, 220, 40, 55)); // glow
        renderer.DrawCircle(cx, cy, Radius, color);
        renderer.DrawCircle(cx - Radius * 0.28f, cy - Radius * 0.32f,
            Radius * 0.32f, new Color(255, 255, 255, 170));                    // shine
    }
}
