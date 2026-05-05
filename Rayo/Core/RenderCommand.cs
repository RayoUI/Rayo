namespace Rayo.Core;

using Rayo.Rendering;

/// <summary>
/// Comando de renderizado inmutable que puede ser ejecutado en el render thread
/// </summary>
public abstract class RenderCommand
{
    public abstract void Execute(IRenderer renderer);
}

/// <summary>
/// Comando para dibujar un rectángulo
/// </summary>
public class DrawRectCommand : RenderCommand
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Color Color { get; set; }

    public override void Execute(IRenderer renderer)
    {
        renderer.DrawRect(X, Y, Width, Height, Color);
    }
}

/// <summary>
/// Comando para dibujar un rectángulo redondeado
/// </summary>
public class DrawRoundedRectCommand : RenderCommand
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public CornerRadius BorderRadius { get; set; }
    public Color Color { get; set; }

    public override void Execute(IRenderer renderer)
    {
        renderer.DrawRoundedRect(X, Y, Width, Height, BorderRadius.TopLeft, Color);
    }
}

/// <summary>
/// Comando para dibujar texto
/// </summary>
public class DrawTextCommand : RenderCommand
{
    public string Text { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public Color Color { get; set; }
    public float FontSize { get; set; } = 24;

    public override void Execute(IRenderer renderer)
    {
        renderer.DrawText(Text, X, Y, Color, FontSize);
    }
}

/// <summary>
/// Comando para iniciar scissor test
/// </summary>
public class PushScissorCommand : RenderCommand
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public override void Execute(IRenderer renderer)
    {
        renderer.PushScissor(X, Y, Width, Height);
    }
}

/// <summary>
/// Comando para finalizar scissor test
/// </summary>
public class PopScissorCommand : RenderCommand
{
    public override void Execute(IRenderer renderer)
    {
        renderer.PopScissor();
    }
}

/// <summary>
/// Frame inmutable con todos los comandos de renderizado
/// Puede ser leído de forma segura por el render thread
/// </summary>
public class RenderFrame
{
    public List<RenderCommand> Commands { get; }
    public uint Width { get; }
    public uint Height { get; }
    public DateTime CaptureTime { get; }

    public RenderFrame(List<RenderCommand> commands, uint width, uint height)
    {
        Commands = commands;
        Width = width;
        Height = height;
        CaptureTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Ejecuta todos los comandos en el renderer
    /// </summary>
    public void Execute(IRenderer renderer)
    {
        renderer.BeginFrame();

        foreach (var command in Commands)
        {
            command.Execute(renderer);
        }

        renderer.EndFrame();
    }
}