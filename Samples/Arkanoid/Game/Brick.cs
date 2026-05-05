namespace Arkanoid.Game;

using Arkanoid.Engine;
using Rayo.Rendering;
using System.Numerics;

/// <summary>
/// A single breakable brick. Supports multi-hit with a flash effect on each impact.
/// </summary>
public class Brick : GameObject
{
    public Color Color       { get; set; } = new Color(220, 80, 80);
    public int   ScoreValue  { get; set; } = 10;
    public int   HitPoints   { get; private set; } = 1;

    private float _flashTimer;

    public static Brick Create(float x, float y, float w, float h, Color color,
                               int hitPoints = 1, int score = 10) => new()
    {
        Position   = new Vector2(x, y),
        Width      = w,
        Height     = h,
        Color      = color,
        ScoreValue = score,
        HitPoints  = hitPoints
    };

    /// <summary>Applies one hit. Returns true when destroyed.</summary>
    public bool Hit()
    {
        HitPoints--;
        _flashTimer = 0.12f;
        if (HitPoints <= 0) { IsActive = false; return true; }
        return false;
    }

    public override void Update(float deltaTime)
    {
        if (_flashTimer > 0) _flashTimer -= deltaTime;
    }

    public override void Render(IRenderer renderer, float ox, float oy)
    {
        float rx = ox + Position.X;
        float ry = oy + Position.Y;

        Color c = _flashTimer > 0
            ? new Color(
                (byte)Math.Min(255, Color.R + 90),
                (byte)Math.Min(255, Color.G + 90),
                (byte)Math.Min(255, Color.B + 90))
            : Color;

        // Shadow
        renderer.DrawRoundedRect(rx + 2, ry + 2, Width, Height, 4f, new Color(0, 0, 0, 60));
        // Body
        renderer.DrawRoundedRect(rx, ry, Width, Height, 4f, c);
        // Shine
        renderer.DrawRoundedRect(rx + 4, ry + 2, Width - 8, 3f, 1.5f, new Color(255, 255, 255, 80));

        // Remaining-hit dots
        for (int i = 0; i < HitPoints - 1; i++)
            renderer.DrawCircle(rx + Width - 8 - i * 7f, ry + Height / 2f, 2.5f,
                new Color(255, 255, 255, 210));
    }
}
