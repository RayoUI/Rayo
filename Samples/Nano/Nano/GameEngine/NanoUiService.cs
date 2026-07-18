namespace Nano.GameEngine;

/// <summary>
/// Engine-owned immediate-mode UI. It only emits Nano rendering commands and has no host UI dependency.
/// </summary>
internal sealed class NanoUiService(NanoGameInputState input, List<NanoGameCommand> commands)
{
    private readonly List<UiHitRegion> _hitRegions = [];
    private readonly List<UiLayout> _layouts = [];
    private readonly Dictionary<string, GameColor> _theme = CreateDefaultTheme();
    private string? _activeWidget;
    private int _nextLayout;

    public void BeginFrame()
    {
        _hitRegions.Clear();
        _layouts.Clear();
        _nextLayout = 0;
    }

    public void EndFrame()
    {
        input.SetUiHitRegions(_hitRegions);
        if (input.PointerReleased)
            _activeWidget = null;
        input.FinishUiFrame();
    }

    public void Panel(float x, float y, float width, float height, string? title)
    {
        commands.Add(new RectCommand(x + 3, y + 4, width, height, Color("shadow")));
        commands.Add(new RectCommand(x, y, width, height, Color("panel")));
        commands.Add(new OutlineRectCommand(x, y, width, height, 1, Color("border")));
        _hitRegions.Add(new UiHitRegion(x, y, width, height));
        if (!string.IsNullOrWhiteSpace(title))
            Label(title, x + 10, y + 9, 2, Color("text"));
    }

    public void Label(string text, float x, float y, int scale, GameColor? color = null)
    {
        scale = Math.Clamp(scale, 1, 6);
        var resolvedColor = color ?? Color("text");
        if (!text.Contains('\r') && !text.Contains('\n'))
        {
            commands.Add(new TextCommand(text, x, y, scale, resolvedColor));
            return;
        }

        var lineY = y;
        var lineStart = 0;
        for (var index = 0; index <= text.Length; index++)
        {
            if (index < text.Length && text[index] != '\n')
                continue;

            var length = index - lineStart;
            if (length > 0 && text[lineStart + length - 1] == '\r')
                length--;
            commands.Add(new TextCommand(text.Substring(lineStart, length), x, lineY, scale, resolvedColor));
            lineY += NanoBitmapFont.MeasureHeight(scale) + scale * 2;
            lineStart = index + 1;
        }
    }

    public bool Button(string id, string text, float x, float y, float width, float height)
    {
        RegisterInteractive(x, y, width, height);
        var hit = Hit(x, y, width, height);
        Activate(id, hit);
        var held = input.PointerDown && _activeWidget == id;
        var clicked = input.PointerReleased && _activeWidget == id && hit;
        commands.Add(new RectCommand(x, y + (held ? 2 : 0), width, height - (held ? 2 : 0),
            held ? Color("button_active") : hit ? Color("button_hover") : Color("button")));
        commands.Add(new OutlineRectCommand(x, y + (held ? 2 : 0), width, height - (held ? 2 : 0), 1, Color("border")));
        var scale = Math.Max(1, Math.Min(3, (int)(height / 12)));
        var textX = x + (width - NanoBitmapFont.MeasureWidth(text, scale)) * 0.5f;
        var textY = y + (height - NanoBitmapFont.MeasureHeight(scale)) * 0.5f + (held ? 2 : 0);
        Label(text, textX, textY, scale, Color("button_text"));
        return clicked;
    }

    public void Progress(float x, float y, float width, float height, float value)
    {
        var normalized = Math.Clamp(value, 0, 1);
        commands.Add(new RectCommand(x, y, width, height, Color("track")));
        commands.Add(new RectCommand(x + 2, y + 2, Math.Max(0, (width - 4) * normalized), Math.Max(0, height - 4), Color("accent")));
        commands.Add(new OutlineRectCommand(x, y, width, height, 1, Color("border")));
    }

    public float Slider(string id, float x, float y, float width, float height, float value, float minimum, float maximum)
    {
        RegisterInteractive(x, y, width, height);
        var hit = Hit(x, y, width, height);
        Activate(id, hit);
        if (input.PointerDown && _activeWidget == id && maximum > minimum)
            value = minimum + Math.Clamp((input.PointerX - x) / width, 0, 1) * (maximum - minimum);

        var normalized = maximum > minimum ? Math.Clamp((value - minimum) / (maximum - minimum), 0, 1) : 0;
        var trackY = y + height * 0.5f - 3;
        commands.Add(new RectCommand(x, trackY, width, 6, Color("track")));
        commands.Add(new RectCommand(x, trackY, width * normalized, 6, Color("accent")));
        commands.Add(new CircleCommand(x + width * normalized, y + height * 0.5f, Math.Max(6, height * 0.3f),
            _activeWidget == id ? Color("button_active") : Color("button_text")));
        return value;
    }

    public (bool Value, bool Changed) Checkbox(string id, string text, float x, float y, bool value)
    {
        var scale = 2;
        var size = 20f;
        var width = size + 8 + NanoBitmapFont.MeasureWidth(text, scale);
        RegisterInteractive(x, y, width, size);
        var hit = Hit(x, y, width, size);
        Activate(id, hit);
        var changed = input.PointerReleased && _activeWidget == id && hit;
        if (changed)
            value = !value;

        commands.Add(new RectCommand(x, y, size, size, hit ? Color("button_hover") : Color("track")));
        commands.Add(new OutlineRectCommand(x, y, size, size, 1, Color("border")));
        if (value)
        {
            commands.Add(new LineCommand(x + 4, y + 10, x + 8, y + 15, Color("accent")));
            commands.Add(new LineCommand(x + 8, y + 15, x + 17, y + 4, Color("accent")));
        }
        Label(text, x + size + 8, y + 3, scale, Color("text"));
        return (value, changed);
    }

    public void Separator(float x, float y, float width) =>
        commands.Add(new RectCommand(x, y, width, 1, Color("border")));

    public int VerticalLayout(float x, float y, float width, float gap)
    {
        _layouts.Add(new UiLayout(x, y, width, Math.Max(0, gap)));
        return ++_nextLayout;
    }

    public UiHitRegion Next(int layoutHandle, float height)
    {
        var index = layoutHandle - 1;
        if ((uint)index >= (uint)_layouts.Count)
            throw new InvalidOperationException($"UI layout {layoutHandle} does not exist in this frame.");
        var layout = _layouts[index];
        var result = new UiHitRegion(layout.X, layout.NextY, layout.Width, Math.Max(0, height));
        layout.NextY += Math.Max(0, height) + layout.Gap;
        _layouts[index] = layout;
        return result;
    }

    public void SetThemeColor(string name, GameColor color)
    {
        if (!_theme.ContainsKey(name))
            throw new InvalidOperationException($"Unknown UI theme color '{name}'.");
        _theme[name] = color;
    }

    public void ResetTheme()
    {
        _theme.Clear();
        foreach (var pair in CreateDefaultTheme())
            _theme[pair.Key] = pair.Value;
    }

    private void Activate(string id, bool hit)
    {
        if (input.PointerPressed && hit)
            _activeWidget = id;
    }

    private bool Hit(float x, float y, float width, float height) =>
        input.PointerX >= x && input.PointerY >= y &&
        input.PointerX <= x + width && input.PointerY <= y + height;

    private void RegisterInteractive(float x, float y, float width, float height) =>
        _hitRegions.Add(new UiHitRegion(x, y, width, height));

    private GameColor Color(string name) => _theme[name];

    private static Dictionary<string, GameColor> CreateDefaultTheme() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["panel"] = new(18, 27, 45, 238),
        ["shadow"] = new(0, 0, 0, 100),
        ["border"] = new(104, 128, 164, 210),
        ["text"] = new(232, 240, 252),
        ["button"] = new(47, 72, 112, 245),
        ["button_hover"] = new(66, 102, 155, 250),
        ["button_active"] = new(75, 200, 140, 255),
        ["button_text"] = new(255, 255, 255),
        ["track"] = new(10, 17, 30, 235),
        ["accent"] = new(75, 200, 140, 255)
    };

    private struct UiLayout(float x, float y, float width, float gap)
    {
        public float X { get; } = x;
        public float NextY { get; set; } = y;
        public float Width { get; } = width;
        public float Gap { get; } = gap;
    }
}
