namespace Arkanoid.Game;

using Arkanoid.Engine;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// Player-controlled paddle. Moves horizontally clamped to world bounds.
/// </summary>
public class Paddle : GameObject
{
    public float Speed { get; set; } = 480f;

    /// <summary>-1 = left, +1 = right, 0 = stationary. Set each frame by the game.</summary>
    public float MoveDirection { get; set; }

    public static Paddle Create(float worldWidth, float worldHeight) => new()
    {
        Width  = 100f,
        Height = 14f,
        Speed  = 480f,
        Position = new Vector2(worldWidth / 2f - 50f, worldHeight - 34f)
    };

    public void ClampToWorld(float worldWidth)
    {
        float x = Math.Clamp(Position.X, 0f, worldWidth - Width);
        Position = new Vector2(x, Position.Y);
    }

    public override void Update(float deltaTime)
    {
        if (MoveDirection == 0) return;
        Position = new Vector2(Position.X + MoveDirection * Speed * deltaTime, Position.Y);
    }

    public override void Render(IRenderer renderer, float ox, float oy)
    {
        float rx = ox + Position.X;
        float ry = oy + Position.Y;
        var body = new Color(100, 200, 255);

        // Shadow
        renderer.DrawRoundedRect(rx + 2, ry + 3, Width, Height, 6f, new Color(0, 0, 0, 60));
        // Body
        renderer.DrawRoundedRect(rx, ry, Width, Height, 6f, body);
        // Shine
        renderer.DrawRoundedRect(rx + 5, ry + 2, Width - 10, 3, 2f, new Color(255, 255, 255, 90));
    }
}
