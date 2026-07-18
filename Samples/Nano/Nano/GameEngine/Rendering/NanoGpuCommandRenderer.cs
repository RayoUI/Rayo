using Rayo.Rendering;

namespace Nano.GameEngine.Rendering;

/// <summary>
/// Translates engine-owned draw commands directly to the host GPU renderer.
/// No framebuffer is read back to the CPU and no full-frame texture is uploaded.
/// </summary>
internal static class NanoGpuCommandRenderer
{
    public static void Render(
        IRenderer renderer,
        IReadOnlyList<NanoGameCommand> commands,
        float originX,
        float originY,
        float width,
        float height)
    {
        renderer.PushScissor(originX, originY, width, height);
        try
        {
            foreach (var command in commands)
            {
                switch (command)
                {
                    case ClearCommand clear:
                        renderer.DrawRect(originX, originY, width, height, ToColor(clear.Color));
                        break;
                    case RectCommand rect:
                        renderer.DrawRect(originX + rect.X, originY + rect.Y, rect.Width, rect.Height, ToColor(rect.Color));
                        break;
                    case LineCommand line:
                        renderer.DrawLine(
                            originX + line.X1,
                            originY + line.Y1,
                            originX + line.X2,
                            originY + line.Y2,
                            1,
                            ToColor(line.Color));
                        break;
                    case CircleCommand circle:
                        renderer.DrawCircle(
                            originX + circle.CenterX,
                            originY + circle.CenterY,
                            circle.Radius,
                            ToColor(circle.Color));
                        break;
                    case OutlineRectCommand rect:
                        renderer.DrawRectOutline(
                            originX + rect.X,
                            originY + rect.Y,
                            rect.Width,
                            rect.Height,
                            rect.Thickness,
                            ToColor(rect.Color));
                        break;
                    case OutlineCircleCommand circle:
                        renderer.DrawCircleOutline(
                            originX + circle.CenterX,
                            originY + circle.CenterY,
                            circle.Radius,
                            circle.Thickness,
                            ToColor(circle.Color));
                        break;
                    case TextCommand text:
                        // The host renderer uses its GPU glyph atlas/batching path. The
                        // multiplier preserves the compact dimensions of Nano's 5x7 metrics.
                        renderer.DrawText(
                            text.Text,
                            originX + text.X,
                            originY + text.Y,
                            ToColor(text.Color),
                            Math.Max(1, text.Scale) * 9f);
                        break;
                }
            }
        }
        finally
        {
            renderer.PopScissor();
        }
    }

    private static Color ToColor(GameColor color) =>
        new(color.R, color.G, color.B, color.A);
}
