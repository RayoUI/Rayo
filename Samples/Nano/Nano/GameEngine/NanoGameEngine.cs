using Lua;
using Lua.Standard;
using Nano.Views.ProjectAssetStore;

namespace Nano.GameEngine;

/// <summary>
/// Runs the asset store's root main.lua and exposes Nano's 2D and virtual-file APIs.
/// </summary>
internal sealed class NanoGameEngine : IDisposable
{
    private readonly IProjectAssetStore _projectStore;
    private readonly NanoGameInputState _input;
    private readonly List<NanoGameCommand> _commands = [];
    private readonly LuaState _state;
    private readonly LuaTable _nano = new();
    private readonly LuaTable _inputApi = new();
    private readonly LuaTable _timeApi = new();
    private readonly NanoAudioService _audio;
    private readonly NanoNetworkService _network = new();
    private readonly NanoPhysicsService _physics = new();
    private readonly NanoUiService _ui;
    private double _elapsed;
    private double _fpsWindowElapsed;
    private int _fpsWindowFrames;
    private double _smoothedFps = 60;
    private bool _previousA;
    private bool _previousB;
    private bool _disposed;

    public NanoGameEngine(
        IProjectAssetStore projectStore,
        NanoGameInputState? input = null)
    {
        _projectStore = projectStore;
        _input = input ?? new NanoGameInputState();
        _ui = new NanoUiService(_input, _commands);
        _audio = new NanoAudioService(path => _projectStore.ReadBytes(SafePath(path, allowEmpty: false)));
        _state = LuaState.Create();
        _state.OpenStandardLibraries();
        RegisterApi();

        try
        {
            // Keep modules inside scripts/ and route every load through the .nn archive.
            _state.DoStringAsync(ModuleBootstrap)
                .GetAwaiter()
                .GetResult();
            _state.DoStringAsync(_projectStore.ReadText("main.lua"))
                .GetAwaiter()
                .GetResult();
        }
        catch (LuaCompileException exception)
        {
            Error = $"main.lua compile error: {exception.Message}";
        }
        catch (LuaRuntimeException exception)
        {
            Error = $"main.lua error: {exception.Message}";
        }
        catch (Exception exception)
        {
            Error = $"Unable to start main.lua: {exception.Message}";
        }
    }

    public IReadOnlyList<NanoGameCommand> Commands => _commands;

    public string? Error { get; private set; }

    internal EngineResourceCounts ResourceCounts => new(
        _physics.WorldCount,
        _physics.BodyCount,
        _audio.ActivePlaybackCount,
        _network.RequestCount);

    internal bool IsPreloading => _audio.IsWarmUpPending;

    public void RunFrame(float deltaTime, int width, int height)
    {
        _commands.Clear();
        _audio.Update();
        _nano["width"] = width;
        _nano["height"] = height;
        _nano["delta_time"] = deltaTime;
        _elapsed += deltaTime;
        _timeApi["elapsed"] = _elapsed;
        UpdateTimeMetrics(deltaTime);
        UpdateInputApi();
        _ui.BeginFrame();

        if (Error is not null)
        {
            _ui.EndFrame();
            return;
        }

        try
        {
            CallIfDefined("update", deltaTime);
            CallIfDefined("draw");
            _previousA = _input.A;
            _previousB = _input.B;
        }
        catch (LuaRuntimeException exception)
        {
            Error = $"main.lua runtime error: {exception.Message}";
        }
        catch (Exception exception)
        {
            Error = $"Game loop error: {exception.Message}";
        }
        finally
        {
            _ui.EndFrame();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            CallIfDefined("shutdown");
        }
        catch
        {
            // Continue releasing native and managed resources even if user shutdown code fails.
        }

        _physics.Clear();
        _audio.Dispose();
        _network.Dispose();
        _commands.Clear();
        _input.Reset();
        if (_state is IDisposable disposable)
            disposable.Dispose();
    }

    private void RegisterApi()
    {
        var draw = new LuaTable
        {
            ["clear"] = Function(context =>
            {
                _commands.Add(new ClearCommand(ReadColor(context, 0)));
                return 0;
            }),
            ["rect"] = Function(context =>
            {
                _commands.Add(new RectCommand(
                    Number(context, 0),
                    Number(context, 1),
                    Number(context, 2),
                    Number(context, 3),
                    ReadColor(context, 4)));
                return 0;
            }),
            ["line"] = Function(context =>
            {
                _commands.Add(new LineCommand(
                    Number(context, 0),
                    Number(context, 1),
                    Number(context, 2),
                    Number(context, 3),
                    ReadColor(context, 4)));
                return 0;
            }),
            ["circle"] = Function(context =>
            {
                _commands.Add(new CircleCommand(
                    Number(context, 0),
                    Number(context, 1),
                    Math.Max(0, Number(context, 2)),
                    ReadColor(context, 3)));
                return 0;
            }),
            ["outline_rect"] = Function(context =>
            {
                _commands.Add(new OutlineRectCommand(
                    Number(context, 0), Number(context, 1), Number(context, 2), Number(context, 3),
                    Math.Max(1, Number(context, 4)), ReadColor(context, 5)));
                return 0;
            }),
            ["outline_circle"] = Function(context =>
            {
                _commands.Add(new OutlineCircleCommand(
                    Number(context, 0), Number(context, 1), Math.Max(0, Number(context, 2)),
                    Math.Max(1, Number(context, 3)), ReadColor(context, 4)));
                return 0;
            })
        };

        var files = new LuaTable
        {
            ["read"] = Function(context =>
            {
                context.Return(_projectStore.ReadText(SafePath(context.GetArgument<string>(0))));
                return 1;
            }),
            ["write"] = Function(context =>
            {
                _projectStore.WriteText(
                    SafePath(context.GetArgument<string>(0), allowEmpty: false),
                    context.GetArgument<string>(1));
                return 0;
            }),
            ["exists"] = Function(context =>
            {
                context.Return(Exists(SafePath(context.GetArgument<string>(0))));
                return 1;
            }),
            ["list"] = Function(context =>
            {
                var path = context.ArgumentCount == 0
                    ? string.Empty
                    : SafePath(context.GetArgument<string>(0));
                var result = new LuaTable();
                var children = _projectStore.GetChildren(path);
                for (var index = 0; index < children.Count; index++)
                    result[index + 1] = children[index].Name;
                context.Return(result);
                return 1;
            })
        };

        var audio = new LuaTable
        {
            ["preload"] = Function(context =>
            {
                try
                {
                    _audio.Preload(SafePath(context.GetArgument<string>(0), false));
                    context.Return(true);
                }
                catch
                {
                    context.Return(false);
                }
                return 1;
            }),
            ["play"] = Function(context =>
            {
                var volume = context.ArgumentCount > 1 ? Number(context, 1) : 1;
                var loop = context.ArgumentCount > 2 && context.GetArgument<bool>(2);
                try
                {
                    context.Return(_audio.Play(SafePath(context.GetArgument<string>(0), false), volume, loop));
                }
                catch
                {
                    // A game keeps running when a device/backend is temporarily unavailable.
                    context.Return(0);
                }
                return 1;
            }),
            ["stop"] = Function(context => { _audio.Stop(Integer(context, 0)); return 0; }),
            ["stop_all"] = Function(_ => { _audio.StopAll(); return 0; }),
            ["is_playing"] = Function(context =>
            {
                context.Return(_audio.IsPlaying(Integer(context, 0)));
                return 1;
            }),
            ["set_volume"] = Function(context =>
            {
                _audio.SetVolume(Integer(context, 0), Number(context, 1));
                return 0;
            })
        };

        var network = new LuaTable
        {
            ["get"] = Function(context =>
            {
                context.Return(_network.Get(context.GetArgument<string>(0)));
                return 1;
            }),
            ["status"] = Function(context => { context.Return(_network.Status(Integer(context, 0))); return 1; }),
            ["body"] = Function(context => { context.Return(_network.Body(Integer(context, 0))); return 1; }),
            ["error"] = Function(context => { context.Return(_network.Error(Integer(context, 0))); return 1; }),
            ["code"] = Function(context => { context.Return(_network.Code(Integer(context, 0))); return 1; }),
            ["cancel"] = Function(context => { _network.Cancel(Integer(context, 0)); return 0; }),
            ["release"] = Function(context => { _network.Release(Integer(context, 0)); return 0; })
        };

        var mathApi = new LuaTable
        {
            ["clamp"] = Function(context => Return(context, Math.Clamp(Number(context, 0), Number(context, 1), Number(context, 2)))),
            ["lerp"] = Function(context => Return(context, Number(context, 0) + (Number(context, 1) - Number(context, 0)) * Number(context, 2))),
            ["distance"] = Function(context => Return(context, Distance(context, 0, 1, 2, 3))),
            ["angle"] = Function(context => Return(context, Math.Atan2(Number(context, 3) - Number(context, 1), Number(context, 2) - Number(context, 0)))),
            ["move_towards"] = Function(context =>
            {
                var current = Number(context, 0);
                var target = Number(context, 1);
                var amount = Math.Abs(Number(context, 2));
                return Return(context, Math.Abs(target - current) <= amount
                    ? target
                    : current + Math.Sign(target - current) * amount);
            }),
            ["normalize"] = Function(context =>
            {
                var x = Number(context, 0);
                var y = Number(context, 1);
                var length = Math.Sqrt(x * x + y * y);
                var result = new LuaTable
                {
                    ["x"] = length > 0 ? x / length : 0,
                    ["y"] = length > 0 ? y / length : 0,
                    ["length"] = length
                };
                context.Return(result);
                return 1;
            }),
            ["deg_to_rad"] = Function(context => Return(context, Number(context, 0) * Math.PI / 180)),
            ["rad_to_deg"] = Function(context => Return(context, Number(context, 0) * 180 / Math.PI))
        };

        var geometry = new LuaTable
        {
            ["point_in_rect"] = Function(context => Return(context,
                Number(context, 0) >= Number(context, 2) && Number(context, 0) <= Number(context, 2) + Number(context, 4) &&
                Number(context, 1) >= Number(context, 3) && Number(context, 1) <= Number(context, 3) + Number(context, 5))),
            ["rects_overlap"] = Function(context => Return(context,
                Number(context, 0) < Number(context, 4) + Number(context, 6) &&
                Number(context, 0) + Number(context, 2) > Number(context, 4) &&
                Number(context, 1) < Number(context, 5) + Number(context, 7) &&
                Number(context, 1) + Number(context, 3) > Number(context, 5))),
            ["circles_overlap"] = Function(context =>
            {
                var radii = Number(context, 2) + Number(context, 5);
                return Return(context, Distance(context, 0, 1, 3, 4) <= radii);
            }),
            ["point_in_circle"] = Function(context =>
                Return(context, Distance(context, 0, 1, 2, 3) <= Number(context, 4))),
            ["circle_rect"] = Function(context =>
            {
                var circleX = Number(context, 0);
                var circleY = Number(context, 1);
                var closestX = Math.Clamp(circleX, Number(context, 3), Number(context, 3) + Number(context, 5));
                var closestY = Math.Clamp(circleY, Number(context, 4), Number(context, 4) + Number(context, 6));
                var dx = circleX - closestX;
                var dy = circleY - closestY;
                var radius = Number(context, 2);
                return Return(context, dx * dx + dy * dy <= radius * radius);
            }),
            ["lines_intersect"] = Function(context =>
            {
                var ax = Number(context, 2) - Number(context, 0);
                var ay = Number(context, 3) - Number(context, 1);
                var bx = Number(context, 6) - Number(context, 4);
                var by = Number(context, 7) - Number(context, 5);
                var denominator = ax * by - ay * bx;
                if (Math.Abs(denominator) < 0.00001f)
                    return Return(context, false);
                var cx = Number(context, 4) - Number(context, 0);
                var cy = Number(context, 5) - Number(context, 1);
                var t = (cx * by - cy * bx) / denominator;
                var u = (cx * ay - cy * ax) / denominator;
                return Return(context, t is >= 0 and <= 1 && u is >= 0 and <= 1);
            })
        };

        var stats = new LuaTable
        {
            ["physics_worlds"] = Function(context => { context.Return(_physics.WorldCount); return 1; }),
            ["physics_bodies"] = Function(context => { context.Return(_physics.BodyCount); return 1; }),
            ["audio_players"] = Function(context => { context.Return(_audio.ActivePlaybackCount); return 1; }),
            ["network_requests"] = Function(context => { context.Return(_network.RequestCount); return 1; }),
            ["draw_commands"] = Function(context => { context.Return(_commands.Count); return 1; })
        };

        _nano["draw"] = draw;
        _nano["file"] = files;
        _nano["audio"] = audio;
        _nano["net"] = network;
        _nano["math"] = mathApi;
        _nano["geom"] = geometry;
        _nano["physics"] = new NanoPhysicsLuaApi(_physics).CreateTable();
        _nano["ui"] = new NanoUiLuaApi(_ui).CreateTable();
        _nano["stats"] = stats;
        _nano["time"] = _timeApi;
        _nano["input"] = _inputApi;
        _nano["width"] = 0;
        _nano["height"] = 0;
        _nano["delta_time"] = 0;
        _state.Environment["nano"] = _nano;
    }

    private void UpdateInputApi()
    {
        _inputApi["x"] = _input.X;
        _inputApi["y"] = _input.Y;
        _inputApi["left"] = _input.Left;
        _inputApi["right"] = _input.Right;
        _inputApi["up"] = _input.Up;
        _inputApi["down"] = _input.Down;
        _inputApi["a"] = _input.A;
        _inputApi["b"] = _input.B;
        _inputApi["a_pressed"] = _input.A && !_previousA;
        _inputApi["b_pressed"] = _input.B && !_previousB;
    }

    private void UpdateTimeMetrics(float deltaTime)
    {
        _timeApi["frame_time"] = deltaTime * 1000.0;
        if (deltaTime > 0 && deltaTime < 1)
        {
            _fpsWindowElapsed += deltaTime;
            _fpsWindowFrames++;
            if (_fpsWindowElapsed >= 0.5)
            {
                _smoothedFps = _fpsWindowFrames / _fpsWindowElapsed;
                _fpsWindowElapsed = 0;
                _fpsWindowFrames = 0;
            }
        }
        _timeApi["fps"] = _smoothedFps;
    }

    private void CallIfDefined(string name, params LuaValue[] arguments)
    {
        var function = _state.Environment[name];
        if (function.Type == LuaValueType.Nil)
            return;

        _state.CallAsync(function, arguments).GetAwaiter().GetResult();
    }

    private bool Exists(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        var separator = path.LastIndexOf('/');
        var parent = separator < 0 ? string.Empty : path[..separator];
        var name = separator < 0 ? path : path[(separator + 1)..];
        return _projectStore.GetChildren(parent)
            .Any(asset => asset.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string SafePath(string path, bool allowEmpty = true)
    {
        var normalized = path.Trim().Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrEmpty(normalized))
        {
            if (allowEmpty)
                return string.Empty;
            throw new InvalidOperationException("An asset path is required.");
        }

        if (normalized.StartsWith('/') || normalized.Contains(':'))
            throw new InvalidOperationException("Asset paths must be relative to the project root.");

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(part => part is "." or ".."))
            throw new InvalidOperationException("Asset paths cannot leave the project root.");

        return string.Join('/', parts);
    }

    private static LuaFunction Function(Func<LuaFunctionExecutionContext, int> callback) =>
        new((context, _) => new ValueTask<int>(callback(context)));

    private static float Number(LuaFunctionExecutionContext context, int index) =>
        (float)context.GetArgument<double>(index);

    private static int Integer(LuaFunctionExecutionContext context, int index) =>
        (int)context.GetArgument<double>(index);

    private static int Return(LuaFunctionExecutionContext context, double value)
    {
        context.Return(value);
        return 1;
    }

    private static int Return(LuaFunctionExecutionContext context, bool value)
    {
        context.Return(value);
        return 1;
    }

    private static double Distance(LuaFunctionExecutionContext context, int x1, int y1, int x2, int y2)
    {
        var dx = Number(context, x2) - Number(context, x1);
        var dy = Number(context, y2) - Number(context, y1);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static GameColor ReadColor(LuaFunctionExecutionContext context, int startIndex)
    {
        var alpha = context.ArgumentCount > startIndex + 3
            ? Byte(context, startIndex + 3)
            : (byte)255;
        return new GameColor(
            Byte(context, startIndex),
            Byte(context, startIndex + 1),
            Byte(context, startIndex + 2),
            alpha);
    }

    private static byte Byte(LuaFunctionExecutionContext context, int index) =>
        (byte)Math.Clamp((int)Math.Round(context.GetArgument<double>(index)), 0, 255);

    private const string ModuleBootstrap = """
        io=nil; os=nil; package=nil; dofile=nil; loadfile=nil
        local loaded_modules = {}
        function require(name)
            if type(name) ~= "string" or name == "" then error("module name expected", 2) end
            if string.find(name, "[^%w_%.]") or string.find(name, "..", 1, true) then
                error("invalid module name", 2)
            end
            local path = "scripts/" .. string.gsub(name, "%.", "/") .. ".lua"
            if string.find(path, "..", 1, true) or string.sub(path, 1, 1) == "/" then
                error("invalid module path", 2)
            end
            if loaded_modules[path] ~= nil then return loaded_modules[path] end
            local source = nano.file.read(path)
            local chunk, message = load(source, "@" .. path, "t", _ENV)
            if not chunk then error(message, 2) end
            local module = chunk()
            if module == nil then module = true end
            loaded_modules[path] = module
            return module
        end
        nano.require = require
        """;
}

internal readonly record struct EngineResourceCounts(
    int PhysicsWorlds,
    int PhysicsBodies,
    int AudioPlayers,
    int NetworkRequests);
