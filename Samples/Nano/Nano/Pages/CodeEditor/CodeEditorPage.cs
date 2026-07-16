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
        return new CodeEditor(DefaultLua, new LuaCodeLanguage())
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
    }

    private const string DefaultLua = "-- Sprite update loop\nlocal speed = 120\nlocal sprite = { x = 24, y = 16 }\n\nfunction update(deltaTime)\n    sprite.x = sprite.x + speed * deltaTime\n    print(\"Sprite position\", sprite.x)\nend";
}
