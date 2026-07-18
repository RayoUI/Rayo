using Silk.NET.Maths;
using Silk.NET.SDL;

namespace Nano.Views.Game;

/// <summary>SDL2 off-screen renderer used by the Nano game page.</summary>
internal sealed unsafe class NanoSdlScene : IDisposable
{
    // SDL_PIXELFORMAT_ABGR8888 has an RGBA byte layout on little-endian targets.
    private const uint PixelFormat = 376840196u;

    private readonly Sdl _sdl;
    private Window* _window;
    private Renderer* _renderer;
    private Texture* _target;
    private Surface* _surface;
    private readonly bool _usesMemorySurface;
    private int _targetWidth;
    private int _targetHeight;

    public NanoSdlScene(bool forceMemorySurface = false)
    {
        _sdl = Sdl.GetApi();

        // AndroidPlatformHost owns the Android Activity, so SDL cannot initialize
        // its window/video backend through SDLActivity. Use SDL's software renderer
        // over an in-memory surface instead.
        if (forceMemorySurface || OperatingSystem.IsAndroid())
        {
            _usesMemorySurface = true;
            if (_sdl.Init(0) != 0)
                throw new InvalidOperationException("SDL initialization failed.");
            return;
        }

        if (_sdl.Init(Sdl.InitVideo) != 0)
            throw new InvalidOperationException("SDL_Init(SDL_INIT_VIDEO) failed.");

        _window = _sdl.CreateWindow("nano-game-offscreen", 0, 0, 1, 1, (uint)WindowFlags.Hidden);
        if (_window == null)
            throw new InvalidOperationException("SDL_CreateWindow failed.");

        _renderer = _sdl.CreateRenderer(
            _window,
            -1,
            (uint)(RendererFlags.Accelerated | RendererFlags.Targettexture));
        if (_renderer == null)
        {
            _renderer = _sdl.CreateRenderer(
                _window,
                -1,
                (uint)(RendererFlags.Software | RendererFlags.Targettexture));
        }

        if (_renderer == null)
            throw new InvalidOperationException("SDL_CreateRenderer failed.");
    }

    public byte[] RenderFrame(int width, int height, IReadOnlyList<NanoGameCommand> commands)
    {
        EnsureTarget(width, height);
        if (!_usesMemorySurface)
            _sdl.SetRenderTarget(_renderer, _target);
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        SetColor(new GameColor(7, 10, 16));
        _sdl.RenderClear(_renderer);

        foreach (var command in commands)
        {
            switch (command)
            {
                case ClearCommand clear:
                    SetColor(clear.Color);
                    _sdl.RenderClear(_renderer);
                    break;
                case RectCommand rect:
                    DrawRect(rect);
                    break;
                case LineCommand line:
                    SetColor(line.Color);
                    _sdl.RenderDrawLine(
                        _renderer,
                        (int)line.X1,
                        (int)line.Y1,
                        (int)line.X2,
                        (int)line.Y2);
                    break;
                case CircleCommand circle:
                    DrawCircle(circle);
                    break;
            }
        }

        var pixels = new byte[width * height * 4];
        fixed (byte* pointer = pixels)
        {
            var result = _sdl.RenderReadPixels(
                _renderer,
                (Rectangle<int>*)null,
                PixelFormat,
                pointer,
                width * 4);
            if (result != 0)
                throw new InvalidOperationException("SDL_RenderReadPixels failed.");
        }

        if (!_usesMemorySurface)
            _sdl.SetRenderTarget(_renderer, (Texture*)null);
        return pixels;
    }

    public void Dispose()
    {
        if (_target != null)
        {
            _sdl.DestroyTexture(_target);
            _target = null;
        }

        if (_renderer != null)
        {
            _sdl.DestroyRenderer(_renderer);
            _renderer = null;
        }

        if (_surface != null)
        {
            _sdl.FreeSurface(_surface);
            _surface = null;
        }

        if (_window != null)
        {
            _sdl.DestroyWindow(_window);
            _window = null;
        }

        _sdl.Quit();
        _sdl.Dispose();
    }

    private void EnsureTarget(int width, int height)
    {
        if (_usesMemorySurface)
        {
            EnsureMemorySurface(width, height);
            return;
        }

        if (_target != null && _targetWidth == width && _targetHeight == height)
            return;

        if (_target != null)
            _sdl.DestroyTexture(_target);

        _target = _sdl.CreateTexture(
            _renderer,
            PixelFormat,
            (int)TextureAccess.Target,
            width,
            height);
        if (_target == null)
            throw new InvalidOperationException("SDL_CreateTexture failed.");

        _targetWidth = width;
        _targetHeight = height;
    }

    private void EnsureMemorySurface(int width, int height)
    {
        if (_surface != null && _targetWidth == width && _targetHeight == height)
            return;

        if (_renderer != null)
        {
            _sdl.DestroyRenderer(_renderer);
            _renderer = null;
        }

        if (_surface != null)
        {
            _sdl.FreeSurface(_surface);
            _surface = null;
        }

        _surface = _sdl.CreateRGBSurfaceWithFormat(0, width, height, 32, PixelFormat);
        if (_surface == null)
            throw new InvalidOperationException("SDL_CreateRGBSurfaceWithFormat failed.");

        _renderer = _sdl.CreateSoftwareRenderer(_surface);
        if (_renderer == null)
            throw new InvalidOperationException("SDL_CreateSoftwareRenderer failed.");

        _targetWidth = width;
        _targetHeight = height;
    }

    private void DrawRect(RectCommand command)
    {
        SetColor(command.Color);
        var rectangle = new Rectangle<int>(
            (int)command.X,
            (int)command.Y,
            Math.Max(0, (int)command.Width),
            Math.Max(0, (int)command.Height));
        _sdl.RenderFillRect(_renderer, &rectangle);
    }

    private void DrawCircle(CircleCommand command)
    {
        SetColor(command.Color);
        var radius = Math.Max(0, (int)command.Radius);
        var centerX = (int)command.CenterX;
        var centerY = (int)command.CenterY;
        for (var y = -radius; y <= radius; y++)
        {
            var halfWidth = (int)Math.Sqrt(radius * radius - y * y);
            _sdl.RenderDrawLine(
                _renderer,
                centerX - halfWidth,
                centerY + y,
                centerX + halfWidth,
                centerY + y);
        }
    }

    private void SetColor(GameColor color) =>
        _sdl.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
}
