using Lua;
using Lua.Standard;

namespace VisualScripting.Lua;

/// <summary>
/// Executes a Lua script string and captures all print() output.
/// Runs synchronously (blocking) — suitable for short scripts in a UI context.
/// </summary>
public static class LuaRunner
{
    public record RunResult(List<string> Output, string Error);

    public static RunResult Run(string luaCode)
    {
        return RunAsync(luaCode).GetAwaiter().GetResult();
    }

    public static async Task<RunResult> RunAsync(string luaCode)
    {
        var output = new List<string>();

        var state = LuaState.Create();
        state.OpenStandardLibraries();

        // Override print() to capture output instead of writing to stdout
        state.Environment["print"] = new LuaFunction((ctx, ct) =>
        {
            var parts = new List<string>();
            for (int i = 0; i < ctx.ArgumentCount; i++)
            {
                string part;
                try        { part = ctx.GetArgument<string>(i); }
                catch { try { part = ctx.GetArgument<double>(i).ToString("G"); }
                        catch { try { part = ctx.GetArgument<bool>(i) ? "true" : "false"; }
                                catch { part = "nil"; } } }
                parts.Add(part);
            }
            output.Add(string.Join("\t", parts));
            return new ValueTask<int>(0);
        });

        try
        {
            await state.DoStringAsync(luaCode);
            return new RunResult(output, null);
        }
        catch (LuaRuntimeException ex)
        {
            return new RunResult(output, ex.Message);
        }
        catch (LuaCompileException ex)
        {
            return new RunResult(output, $"Compile error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new RunResult(output, $"Error: {ex.Message}");
        }
    }
}
