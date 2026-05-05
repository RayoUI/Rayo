using Rayo;
using Rayo.Rendering;

namespace PaintApp.Controls;

/// <summary>
/// Internal CPU rasterizer for the PaintApp flood-fill pipeline.
/// All methods operate on a flat RGBA byte buffer (4 bytes per pixel, row-major).
/// </summary>
internal static class CpuRasterizer
{
    /// <summary>
    /// Dispatches a single <see cref="DrawCommand"/> into the CPU buffer using canvas-local coordinates.
    /// <paramref name="ox"/> and <paramref name="oy"/> are subtracted from each command's position
    /// so that 0-based canvas coordinates map correctly into the buffer.
    /// </summary>
    public static void Rasterize(byte[] buf, int bw, int bh, float ox, float oy, DrawCommand cmd)
    {
        switch (cmd)
        {
            case LineCmd lc:
                Line(buf, bw, bh, lc.X1 - ox, lc.Y1 - oy, lc.X2 - ox, lc.Y2 - oy, lc.Size, lc.C);
                break;

            case CircleCmd cc:
                Circle(buf, bw, bh, cc.X - ox, cc.Y - oy, cc.R, cc.C);
                break;

            case RectCmd rc:
                if (rc.Filled)
                    FillRect(buf, bw, bh, rc.X - ox, rc.Y - oy, rc.W, rc.H, rc.C);
                else
                    StrokeRect(buf, bw, bh, rc.X - ox, rc.Y - oy, rc.W, rc.H, rc.Size, rc.C);
                break;

            case EllipseCmd ec:
                if (ec.Filled)
                    FillEllipse(buf, bw, bh, ec.Cx - ox, ec.Cy - oy, ec.Rx, ec.Ry, ec.C);
                else
                    StrokeEllipse(buf, bw, bh, ec.Cx - ox, ec.Cy - oy, ec.Rx, ec.Ry, ec.Size, ec.C);
                break;

            case TextureFillCmd tfc:
                // Merge the stored pixel mask into the CPU buffer (non-transparent pixels win).
                int cw = Math.Min(bw, tfc.W), ch = Math.Min(bh, tfc.H);
                for (int py = 0; py < ch; py++)
                    for (int px = 0; px < cw; px++)
                    {
                        int si = (py * tfc.W + px) * 4;
                        if (tfc.Pixels[si + 3] > 0)
                        {
                            int di = (py * bw + px) * 4;
                            buf[di]     = tfc.Pixels[si];
                            buf[di + 1] = tfc.Pixels[si + 1];
                            buf[di + 2] = tfc.Pixels[si + 2];
                            buf[di + 3] = 255;
                        }
                    }
                break;
        }
    }

    public static void Pixel(byte[] buf, int bw, int bh, int x, int y, Color c)
    {
        if ((uint)x >= (uint)bw || (uint)y >= (uint)bh) return;
        int i = (y * bw + x) * 4;
        buf[i]     = (byte)(c.R * 255 + 0.5f);
        buf[i + 1] = (byte)(c.G * 255 + 0.5f);
        buf[i + 2] = (byte)(c.B * 255 + 0.5f);
        buf[i + 3] = (byte)(c.A * 255 + 0.5f);
    }

    public static void Circle(byte[] buf, int bw, int bh, float cx, float cy, float r, Color c)
    {
        int ir = (int)(r + 0.5f);
        int icx = (int)cx, icy = (int)cy;
        int r2 = ir * ir;
        for (int dy = -ir; dy <= ir; dy++)
            for (int dx = -ir; dx <= ir; dx++)
                if (dx * dx + dy * dy <= r2)
                    Pixel(buf, bw, bh, icx + dx, icy + dy, c);
    }

    public static void Line(byte[] buf, int bw, int bh,
                             float x1, float y1, float x2, float y2, float thickness, Color c)
    {
        float dx = x2 - x1, dy = y2 - y1;
        int steps = Math.Max(1, (int)(MathF.Sqrt(dx * dx + dy * dy) + 0.5f));
        float r = thickness / 2f;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Circle(buf, bw, bh, x1 + t * dx, y1 + t * dy, r, c);
        }
    }

    public static void FillRect(byte[] buf, int bw, int bh, float x, float y, float w, float h, Color c)
    {
        int x0 = (int)x, y0 = (int)y, x1 = (int)(x + w), y1 = (int)(y + h);
        for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
                Pixel(buf, bw, bh, px, py, c);
    }

    public static void StrokeRect(byte[] buf, int bw, int bh,
                                   float x, float y, float w, float h, float t, Color c)
    {
        FillRect(buf, bw, bh, x,         y,         w, t, c);   // top
        FillRect(buf, bw, bh, x,         y + h - t, w, t, c);   // bottom
        FillRect(buf, bw, bh, x,         y,         t, h, c);   // left
        FillRect(buf, bw, bh, x + w - t, y,         t, h, c);   // right
    }

    public static void FillEllipse(byte[] buf, int bw, int bh,
                                    float cx, float cy, float rx, float ry, Color c)
    {
        if (rx < 0.5f || ry < 0.5f) return;
        int y0 = (int)(cy - ry), y1 = (int)(cy + ry + 0.5f);
        for (int py = y0; py <= y1; py++)
        {
            float ny = (py - cy) / ry;
            if (ny < -1f || ny > 1f) continue;
            float halfW = rx * MathF.Sqrt(1f - ny * ny);
            int x0 = (int)(cx - halfW), x1 = (int)(cx + halfW + 0.5f);
            for (int px = x0; px <= x1; px++)
                Pixel(buf, bw, bh, px, py, c);
        }
    }

    public static void StrokeEllipse(byte[] buf, int bw, int bh,
                                      float cx, float cy, float rx, float ry,
                                      float thickness, Color c)
    {
        if (rx < 0.5f || ry < 0.5f) return;
        float r = Math.Max(1f, thickness / 2f);
        int steps = Math.Max(64, (int)(2f * MathF.PI * Math.Max(rx, ry)));
        for (int i = 0; i < steps; i++)
        {
            float angle = 2f * MathF.PI * i / steps;
            Circle(buf, bw, bh,
                   cx + rx * MathF.Cos(angle),
                   cy + ry * MathF.Sin(angle), r, c);
        }
    }
}
