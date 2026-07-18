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
    NanoGameInputState input,
    Action? onLoaded = null)
    : View<NanoGameView>, IFrameAnimation
{
    private NanoGameEngine? _engine;
    private Task<NanoGameEngine>? _loadTask;
    private bool _animationRegistered;
    private bool _loadingPresented;
    private bool _readyPresented;
    private bool _loaded;
    private bool _unmounted;
    private float _loadingElapsed;
    private float _progress = 0.05f;
    private string _loadingStatus = "PREPARING GAME";
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

        _unmounted = true;
        if (_engine is not null)
        {
            _engine.Dispose();
        }
        else if (_loadTask is { } pendingLoad)
        {
            _ = pendingLoad.ContinueWith(
                static task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                        task.Result.Dispose();
                    else
                        _ = task.Exception;
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }
        _engine = null;
        _loadTask = null;
        base.OnUnmounted();
    }

    void IFrameAnimation.Tick(float deltaTime)
    {
        if (_hostError is not null)
            return;

        if (!_loaded)
        {
            AdvanceLoading(deltaTime);
            MarkNeedsPaint();
            return;
        }

        var width = Math.Max(1, (int)ComputedWidth);
        var height = Math.Max(1, (int)ComputedHeight);

        var engine = _engine;
        if (engine is null)
            return;

        try
        {
            engine.RunFrame(deltaTime, width, height);
        }
        catch (Exception exception)
        {
            _hostError = $"Unable to run the game: {exception.Message}";
        }

        MarkNeedsPaint();
    }

    private void AdvanceLoading(float deltaTime)
    {
        _loadingElapsed += Math.Clamp(deltaTime, 0, 0.1f);

        // Guarantee that the loading page is painted once before any heavy work starts.
        if (!_loadingPresented)
        {
            _loadingPresented = true;
            _progress = 0.1f;
            _loadingStatus = "READING ASSETS";
            return;
        }

        if (_loadTask is null)
        {
            _progress = 0.18f;
            _loadingStatus = "LOADING LUA MODULES";
            _loadTask = Task.Run(() => new NanoGameEngine(projectStore, input));
            return;
        }

        if (!_loadTask.IsCompleted)
        {
            _progress = Math.Min(0.72f, 0.18f + _loadingElapsed * 0.22f);
            return;
        }

        if (_engine is null)
        {
            try
            {
                _engine = _loadTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _hostError = $"Unable to initialize the game engine: {exception.Message}";
                return;
            }

            if (_unmounted)
            {
                _engine.Dispose();
                _engine = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_engine.Error))
            {
                _hostError = _engine.Error;
                return;
            }
        }

        if (_engine.IsPreloading)
        {
            _progress = Math.Min(0.94f, Math.Max(_progress, 0.78f) + deltaTime * 0.08f);
            _loadingStatus = "WARMING UP AUDIO";
            return;
        }

        _progress = 1;
        _loadingStatus = "READY";
        if (!_readyPresented)
        {
            _readyPresented = true;
            return;
        }

        _loaded = true;
        onLoaded?.Invoke();
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
        else if (!_loaded)
        {
            RenderLoading(renderer);
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

    private void RenderLoading(IRenderer renderer)
    {
        var panelWidth = Math.Min(330f, Math.Max(220f, ComputedWidth - 40));
        var panelHeight = 154f;
        var panelX = ComputedX + (ComputedWidth - panelWidth) * 0.5f;
        var panelY = ComputedY + (ComputedHeight - panelHeight) * 0.42f;
        var barX = panelX + 24;
        var barY = panelY + 91;
        var barWidth = panelWidth - 48;
        var pulse = 0.55f + 0.45f * MathF.Sin(_loadingElapsed * 5f);

        renderer.DrawRoundedRect(panelX + 4, panelY + 6, panelWidth, panelHeight, 14, new Color(0, 0, 0, 90));
        renderer.DrawRoundedRect(panelX, panelY, panelWidth, panelHeight, 14, new Color(18, 27, 45, 248));
        renderer.DrawRoundedRectOutline(panelX, panelY, panelWidth, panelHeight, 14, 1, new Color(75, 200, 140, 210));
        renderer.DrawText("NANO ENGINE", panelX + 24, panelY + 22, new Color(232, 240, 252), 20);
        renderer.DrawText(_loadingStatus, panelX + 24, panelY + 57, new Color(150, 170, 205), 12);
        renderer.DrawRoundedRect(barX, barY, barWidth, 12, 6, new Color(7, 12, 22, 255));
        renderer.DrawRoundedRect(
            barX,
            barY,
            barWidth * Math.Clamp(_progress, 0, 1),
            12,
            6,
            new Color(75, 200, 140, (int)(210 + pulse * 45)));
        renderer.DrawText($"{MathF.Round(_progress * 100):0}%", barX, barY + 25, new Color(125, 225, 170), 12);
    }

}
