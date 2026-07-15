using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Nano.Pages.SpriteEditor;
using Nano.Pages.CodeEditor;

namespace Nano.Pages;

public class HomePage : Component
{
    private readonly SpriteEditorPage _spriteEditorPage = new();
    private readonly CodeEditorPage _codeEditorPage = new();
    private VisualElement? _spriteEditorContent;
    private VisualElement? _codeEditorContent;

    public override VisualElement Build()
    {
        return new TabControl()
            .AddTab("Inicio", _spriteEditorContent ??= _spriteEditorPage.Build())
            .AddTab("Explorar", CreateTabContent("Contenido de la segunda pestana."))
            .AddTab("Código", _codeEditorContent ??= _codeEditorPage.Build())
            .AddTab("Ajustes", CreateTabContent("Contenido de la tercera pestana."));
    }

    private static VisualElement CreateTabContent(string text)
    {
        return new VStack()
            .Padding(new Thickness(20))
            .Children(new Label(text).FontSize(16));
    }
}
