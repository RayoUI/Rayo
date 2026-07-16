using Nano.Views.CodeEditor.Components;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.CodeEditor;

public sealed class CodeEditorView : Component
{
    public override VisualElement Build()
    {
        return new CodeEdit(DefaultLua, new LuaCodeLanguage())
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch);
    }

    private const string DefaultLua = "-- Sprite update loop\nlocal speed = 120\nlocal sprite = { x = 24, y = 16 }\n\nfunction update(deltaTime)\n    sprite.x = sprite.x + speed * deltaTime\n    print(\"Sprite position\", sprite.x)\nend";
}
