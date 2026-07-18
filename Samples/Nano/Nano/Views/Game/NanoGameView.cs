using Nano.Views.ProjectAssetStore;
using Rayo.Animation;
using Rayo.Core;
using Rayo.Rendering;

namespace Nano.Views.Game;

/// <summary>Hosts the Lua game loop and composites SDL's RGBA frames into Nano.</summary>
internal sealed class NanoGameView(
    IProjectAssetStore projectStore,
    NanoGameInputState input)
    : View<NanoGameView>, IFrameAnimation
{
    private NanoGameEngine? _engine;
    private NanoSdlScene? _scene;
    private byte[]? _frame;
    private int _frameWidth;
    private int _frameHeight;
    private bool _animationRegistered;
    private string? _hostError;
    private bool _usesRayoFallback;

    protected override void Measure(float availableWidth, float availableHeight)
    {
        DesiredWidth = availableWidth > 0 && !float.IsInfinity(availableWidth)
            ? availableWidth
            : 390;
        DesiredHeight = availableHeight > 0 && !float.IsInfinity(availableHeight)
            ? availableHeight
            : 720;
    }

    protected override void OnMounted()
    {
        base.OnMounted();
        try
        {
            _engine = new NanoGameEngine(projectStore, input);
        }
        catch (Exception exception)
        {
            _hostError = $"Unable to initialize the game engine: {exception.Message}";
            MarkNeedsPaint();
            return;
        }

        try
        {
            _scene = new NanoSdlScene();
        }
        catch
        {
            // Lua and the game loop can still run when the platform has no usable
            // native SDL backend. Commands are rendered by Rayo in Render().
            _scene = null;
            _usesRayoFallback = true;
        }

        FrameAnimationTicker.Register(this);
        _animationRegistered = true;
    }

    protected override void OnUnmounted()
    {
        if (_animationRegistered)
        {
            FrameAnimationTicker.Unregister(this);
            _animationRegistered = false;
        }

        _scene?.Dispose();
        _scene = null;
        _engine?.Dispose();
        _engine = null;
        base.OnUnmounted();
    }

    void IFrameAnimation.Tick(float deltaTime)
    {
        if (_engine is null)
            return;

        var width = Math.Max(1, (int)ComputedWidth);
        var height = Math.Max(1, (int)ComputedHeight);

        try
        {
            _engine.RunFrame(deltaTime, width, height);
            if (_scene is not null)
            {
                _frame = _scene.RenderFrame(width, height, _engine.Commands);
                _frameWidth = width;
                _frameHeight = height;
            }
        }
        catch
        {
            _scene?.Dispose();
            _scene = null;
            _frame = null;
            _usesRayoFallback = true;
        }

        MarkNeedsPaint();
    }

    public override void Render(IRenderer renderer)
    {
        renderer.DrawRect(
            ComputedX,
            ComputedY,
            ComputedWidth,
            ComputedHeight,
            new Color(7, 10, 16));

        if (_frame is not null && _frameWidth > 0 && _frameHeight > 0)
        {
            using var texture = renderer.CreateTextureFromPixels(_frame, _frameWidth, _frameHeight);
            renderer.DrawTexture(texture, ComputedX, ComputedY, ComputedWidth, ComputedHeight);
        }
        else if (_usesRayoFallback && _engine is not null)
        {
            RenderCommands(renderer, _engine.Commands);
        }

        var error = _hostError ?? _engine?.Error;
        if (!string.IsNullOrWhiteSpace(error))
        {
            var message = error.Length > 100 ? $"{error[..100]}..." : error;
            renderer.DrawRoundedRect(
                ComputedX + 16,
                ComputedY + 16,
                Math.Max(0, ComputedWidth - 32),
                64,
                8,
                new Color(90, 25, 31, 235));
            renderer.DrawText(
                message,
                ComputedX + 28,
                ComputedY + 38,
                new Color(255, 220, 220),
                13);
        }
    }

    private void RenderCommands(IRenderer renderer, IReadOnlyList<NanoGameCommand> commands)
    {
        foreach (var command in commands)
        {
            switch (command)
            {
                case ClearCommand clear:
                    renderer.DrawRect(
                        ComputedX,
                        ComputedY,
                        ComputedWidth,
                        ComputedHeight,
                        ToColor(clear.Color));
                    break;
                case RectCommand rect:
                    renderer.DrawRect(
                        ComputedX + rect.X,
                        ComputedY + rect.Y,
                        rect.Width,
                        rect.Height,
                        ToColor(rect.Color));
                    break;
                case LineCommand line:
                    renderer.DrawLine(
                        ComputedX + line.X1,
                        ComputedY + line.Y1,
                        ComputedX + line.X2,
                        ComputedY + line.Y2,
                        1,
                        ToColor(line.Color));
                    break;
                case CircleCommand circle:
                    renderer.DrawCircle(
                        ComputedX + circle.CenterX,
                        ComputedY + circle.CenterY,
                        circle.Radius,
                        ToColor(circle.Color));
                    break;
            }
        }
    }

    private static Color ToColor(GameColor color) =>
        new(color.R, color.G, color.B, color.A);
}
