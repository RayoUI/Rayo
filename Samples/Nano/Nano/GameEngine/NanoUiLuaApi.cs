using Lua;

namespace Nano.GameEngine;

/// <summary>Lua binding for the engine-owned immediate-mode UI toolkit.</summary>
internal sealed class NanoUiLuaApi(NanoUiService ui)
{
    public LuaTable CreateTable() => new()
    {
        ["panel"] = Function(context =>
        {
            ui.Panel(
                Number(context, 0), Number(context, 1), Number(context, 2), Number(context, 3),
                context.ArgumentCount > 4 ? context.GetArgument<string>(4) : null);
            return 0;
        }),
        ["label"] = Function(context =>
        {
            var scale = context.ArgumentCount > 3 ? Integer(context, 3) : 2;
            var color = context.ArgumentCount > 6 ? ReadColor(context, 4) : (GameColor?)null;
            ui.Label(context.GetArgument<string>(0), Number(context, 1), Number(context, 2), scale, color);
            return 0;
        }),
        ["button"] = Function(context =>
        {
            context.Return(ui.Button(
                context.GetArgument<string>(0), context.GetArgument<string>(1),
                Number(context, 2), Number(context, 3), Number(context, 4), Number(context, 5)));
            return 1;
        }),
        ["progress"] = Function(context =>
        {
            ui.Progress(Number(context, 0), Number(context, 1), Number(context, 2), Number(context, 3), Number(context, 4));
            return 0;
        }),
        ["slider"] = Function(context =>
        {
            context.Return(ui.Slider(
                context.GetArgument<string>(0), Number(context, 1), Number(context, 2), Number(context, 3), Number(context, 4),
                Number(context, 5), OptionalNumber(context, 6, 0), OptionalNumber(context, 7, 1)));
            return 1;
        }),
        ["checkbox"] = Function(context =>
        {
            var result = ui.Checkbox(
                context.GetArgument<string>(0), context.GetArgument<string>(1),
                Number(context, 2), Number(context, 3), context.GetArgument<bool>(4));
            context.Return(new LuaTable { ["value"] = result.Value, ["changed"] = result.Changed });
            return 1;
        }),
        ["separator"] = Function(context =>
        {
            ui.Separator(Number(context, 0), Number(context, 1), Number(context, 2));
            return 0;
        }),
        ["vstack"] = Function(context =>
        {
            context.Return(ui.VerticalLayout(
                Number(context, 0), Number(context, 1), Number(context, 2), OptionalNumber(context, 3, 8)));
            return 1;
        }),
        ["next"] = Function(context =>
        {
            var bounds = ui.Next(Integer(context, 0), Number(context, 1));
            var result = context.ArgumentCount > 2
                ? context.GetArgument<LuaTable>(2)
                : new LuaTable();
            SetBounds(result, bounds);
            context.Return(result);
            return 1;
        }),
        ["measure"] = Function(context =>
        {
            var text = context.GetArgument<string>(0);
            var scale = context.ArgumentCount > 1 ? Integer(context, 1) : 2;
            var result = context.ArgumentCount > 2
                ? context.GetArgument<LuaTable>(2)
                : new LuaTable();
            result["width"] = NanoBitmapFont.MeasureWidth(text, scale);
            result["height"] = NanoBitmapFont.MeasureHeight(scale);
            context.Return(result);
            return 1;
        }),
        ["theme"] = Function(context =>
        {
            ui.SetThemeColor(context.GetArgument<string>(0), ReadColor(context, 1));
            return 0;
        }),
        ["reset_theme"] = Function(_ => { ui.ResetTheme(); return 0; })
    };

    private static void SetBounds(LuaTable result, UiHitRegion bounds)
    {
        result["x"] = bounds.X;
        result["y"] = bounds.Y;
        result["width"] = bounds.Width;
        result["height"] = bounds.Height;
    }

    private static float OptionalNumber(LuaFunctionExecutionContext context, int index, float fallback) =>
        context.ArgumentCount > index ? Number(context, index) : fallback;

    private static float Number(LuaFunctionExecutionContext context, int index) =>
        (float)context.GetArgument<double>(index);

    private static int Integer(LuaFunctionExecutionContext context, int index) =>
        (int)context.GetArgument<double>(index);

    private static GameColor ReadColor(LuaFunctionExecutionContext context, int startIndex) => new(
        Byte(context, startIndex), Byte(context, startIndex + 1), Byte(context, startIndex + 2),
        context.ArgumentCount > startIndex + 3 ? Byte(context, startIndex + 3) : (byte)255);

    private static byte Byte(LuaFunctionExecutionContext context, int index) =>
        (byte)Math.Clamp((int)Math.Round(context.GetArgument<double>(index)), 0, 255);

    private static LuaFunction Function(Func<LuaFunctionExecutionContext, int> callback) =>
        new((context, _) => new ValueTask<int>(callback(context)));
}
