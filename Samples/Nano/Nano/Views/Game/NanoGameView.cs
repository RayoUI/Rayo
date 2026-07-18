using Nano.Views.ProjectAssetStore;
using Nano.GameEngine;
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
    private ITexture? _frameTexture;
    private long _frameVersion;
    private long _uploadedFrameVersion = -1;

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
        _frameTexture?.Dispose();
        _frameTexture = null;
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
                _frameVersion++;
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
            if (_frameTexture is null)
            {
                _frameTexture = renderer.CreateDynamicTextureFromPixels(_frame, _frameWidth, _frameHeight);
                _uploadedFrameVersion = _frameVersion;
            }
            else if (_uploadedFrameVersion != _frameVersion)
            {
                _frameTexture = renderer.UpdateDynamicTexturePixels(
                    _frameTexture,
                    _frame,
                    _frameWidth,
                    _frameHeight);
                _uploadedFrameVersion = _frameVersion;
            }
            renderer.DrawTexture(_frameTexture, ComputedX, ComputedY, ComputedWidth, ComputedHeight);
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
                case OutlineRectCommand rect:
                    renderer.DrawLine(ComputedX + rect.X, ComputedY + rect.Y, ComputedX + rect.X + rect.Width, ComputedY + rect.Y, rect.Thickness, ToColor(rect.Color));
                    renderer.DrawLine(ComputedX + rect.X + rect.Width, ComputedY + rect.Y, ComputedX + rect.X + rect.Width, ComputedY + rect.Y + rect.Height, rect.Thickness, ToColor(rect.Color));
                    renderer.DrawLine(ComputedX + rect.X + rect.Width, ComputedY + rect.Y + rect.Height, ComputedX + rect.X, ComputedY + rect.Y + rect.Height, rect.Thickness, ToColor(rect.Color));
                    renderer.DrawLine(ComputedX + rect.X, ComputedY + rect.Y + rect.Height, ComputedX + rect.X, ComputedY + rect.Y, rect.Thickness, ToColor(rect.Color));
                    break;
                case OutlineCircleCommand circle:
                    const int segments = 48;
                    for (var index = 0; index < segments; index++)
                    {
                        var a = index * Math.Tau / segments;
                        var b = (index + 1) * Math.Tau / segments;
                        renderer.DrawLine(
                            ComputedX + circle.CenterX + (float)Math.Cos(a) * circle.Radius,
                            ComputedY + circle.CenterY + (float)Math.Sin(a) * circle.Radius,
                            ComputedX + circle.CenterX + (float)Math.Cos(b) * circle.Radius,
                            ComputedY + circle.CenterY + (float)Math.Sin(b) * circle.Radius,
                            circle.Thickness,
                            ToColor(circle.Color));
                    }
                    break;
                case TextCommand text:
                    DrawBitmapText(renderer, text);
                    break;
            }
        }
    }

    private void DrawBitmapText(IRenderer renderer, TextCommand command)
    {
        var scale = Math.Max(1, command.Scale);
        var cursorX = ComputedX + command.X;
        foreach (var character in command.Text)
        {
            var rows = NanoBitmapFont.Rows(character);
            if (rows is not null)
            {
                for (var row = 0; row < NanoBitmapFont.GlyphHeight; row++)
                {
                    for (var column = 0; column < NanoBitmapFont.GlyphWidth; column++)
                    {
                        if (rows[row][column] == '1')
                            renderer.DrawRect(
                                cursorX + column * scale,
                                ComputedY + command.Y + row * scale,
                                scale,
                                scale,
                                ToColor(command.Color));
                    }
                }
            }
            cursorX += NanoBitmapFont.Advance * scale;
        }
    }

    private static Color ToColor(GameColor color) =>
        new(color.R, color.G, color.B, color.A);
}
