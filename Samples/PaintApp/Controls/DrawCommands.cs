using Rayo;
using Rayo.Rendering;

namespace PaintApp.Controls;

// ── Tool enum ─────────────────────────────────────────────────────────────────

public enum PaintTool { Pencil, Brush, Eraser, Line, Rectangle, Ellipse, Fill }

// ── Draw command hierarchy ────────────────────────────────────────────────────

public abstract record DrawCommand;

/// <summary>Draws a thick line segment (continuous strokes).</summary>
public record LineCmd(float X1, float Y1, float X2, float Y2, Color C, float Size) : DrawCommand;

/// <summary>Draws a filled circle (round line caps / brush dots).</summary>
public record CircleCmd(float X, float Y, float R, Color C) : DrawCommand;

/// <summary>Draws a rectangle — filled or outlined.</summary>
public record RectCmd(float X, float Y, float W, float H, Color C, float Size, bool Filled) : DrawCommand;

/// <summary>Draws an ellipse centred at (Cx, Cy) — filled or outlined.</summary>
public record EllipseCmd(float Cx, float Cy, float Rx, float Ry, Color C, float Size, bool Filled) : DrawCommand;

/// <summary>
/// Stores a pixel-level flood-fill result.
/// Filled pixels carry the draw color (opaque); unfilled pixels are transparent so
/// underlying strokes show through correctly.
/// The GPU texture is created lazily on the first call to Render().
/// </summary>
public record class TextureFillCmd : DrawCommand
{
    public byte[] Pixels  { get; }
    public int    W       { get; }
    public int    H       { get; }
    public float  CanvasX { get; }
    public float  CanvasY { get; }
    public float  CanvasW { get; }
    public float  CanvasH { get; }

    // Lazily created GPU texture; created once in ExecuteCommand, then cached.
    internal ITexture? Texture;

    public TextureFillCmd(byte[] pixels, int w, int h,
                          float canvasX, float canvasY, float canvasW, float canvasH)
    {
        Pixels  = pixels;
        W       = w;
        H       = h;
        CanvasX = canvasX;
        CanvasY = canvasY;
        CanvasW = canvasW;
        CanvasH = canvasH;
    }
}
