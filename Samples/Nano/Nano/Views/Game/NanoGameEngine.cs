using Lua;
using Lua.Standard;
using Nano.Views.ProjectAssetStore;

namespace Nano.Views.Game;

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
    private bool _previousA;
    private bool _previousB;

    public NanoGameEngine(
        IProjectAssetStore projectStore,
        NanoGameInputState? input = null)
    {
        _projectStore = projectStore;
        _input = input ?? new NanoGameInputState();
        _state = LuaState.Create();
        _state.OpenStandardLibraries();
        RegisterApi();

        try
        {
            // File access is intentionally routed through nano.file and the .nn archive.
            _state.DoStringAsync("io=nil; os=nil; package=nil; require=nil; dofile=nil; loadfile=nil")
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

    public void RunFrame(float deltaTime, int width, int height)
    {
        _commands.Clear();
        _nano["width"] = width;
        _nano["height"] = height;
        _nano["delta_time"] = deltaTime;
        UpdateInputApi();

        if (Error is not null)
            return;

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
    }

    public void Dispose()
    {
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

        _nano["draw"] = draw;
        _nano["file"] = files;
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
}
