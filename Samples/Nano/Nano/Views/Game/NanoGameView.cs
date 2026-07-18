using Nano.Views.ProjectAssetStore;
using Nano.GameEngine;
using Nano.GameEngine.Rendering;
using Rayo.Animation;
using Rayo.Core;
using Rayo.Rendering;

namespace Nano.Views.Game;

/// <summary>Hosts the Lua game loop and renders its commands on the host GPU.</summary>
internal sealed class NanoGameView(
    IProjectAssetStore projectStore,
    NanoGameInputState input)
    : View<NanoGameView>, IFrameAnimation
{
    private NanoGameEngine? _engine;
    private bool _animationRegistered;
    private string? _hostError;

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
        }
        catch (Exception exception)
        {
            _hostError = $"Unable to run the game: {exception.Message}";
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

        var gpuUnavailable = renderer is IGpuRendererStatus { IsGpuAccelerated: false };
        if (gpuUnavailable)
        {
            _hostError = "GPU rendering is required but no hardware GPU surface is available.";
        }
        else if (_engine is not null)
            NanoGpuCommandRenderer.Render(
                renderer,
                _engine.Commands,
                ComputedX,
                ComputedY,
                ComputedWidth,
                ComputedHeight);

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

}
