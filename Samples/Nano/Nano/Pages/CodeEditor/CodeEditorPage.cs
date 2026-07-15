using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages.CodeEditor;

public sealed class CodeEditorPage : Component
{
    public override VisualElement Build()
    {
        var page = new Grid()
            .Rows(GridLength.Auto, GridLength.Auto, GridLength.Star)
            .RowSpacing(10)
            .Padding(new Thickness(12))
            .Columns(GridLength.Star)
            .Background(new Color(20, 27, 40));

        page
            .AddChild(
                new Label("Lua editor")
                    .FontSize(18)
                    .Foreground(new Color(241, 245, 249)),
                0,
                0)
            .AddChild(
                new Label("Syntax highlighting and line numbers. Languages are provided through ICodeLanguage.")
                    .FontSize(13)
                    .Foreground(new Color(148, 163, 184)),
                1,
                0)
            .AddChild(
                new CodeEditor(DefaultLua, new LuaCodeLanguage())
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch),
                2,
                0);

        return page;
    }

    private const string DefaultLua = "-- Sprite update loop\nlocal speed = 120\nlocal sprite = { x = 24, y = 16 }\n\nfunction update(deltaTime)\n    sprite.x = sprite.x + speed * deltaTime\n    print(\"Sprite position\", sprite.x)\nend";
}
