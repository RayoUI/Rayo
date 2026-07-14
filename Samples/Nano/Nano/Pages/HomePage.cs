using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Nano.Pages.SpriteEditor;

namespace Nano.Pages;

public class HomePage : Component
{
    public override VisualElement Build()
    {
        return new TabControl()
            .AddTab("Inicio", new SpriteEditorPage())
            .AddTab("Explorar", CreateTabContent("Contenido de la segunda pestana."))
            .AddTab("Ajustes", CreateTabContent("Contenido de la tercera pestana."));
    }

    private static VisualElement CreateTabContent(string text)
    {
        return new VStack()
            .Padding(new Thickness(20))
            .Children(new Label(text).FontSize(16));
    }
}
