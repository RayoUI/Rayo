using NanoApp.Pages;
using Rayo;
using Rayo.Controls;
using Rayo.Core;

namespace NanoApp;

public sealed class MainView : Component
{
    public override VisualElement Build()
    {
        var sceneEditor = new SceneEditorPage();

        return new TabControl()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HeaderContentSpacing(4)
            .HeaderStart(() =>
                new ButtonIcon(Icons.Menu)
                    .Width(36)
                    .Height(30)
                    .IconSize(18)
                    .Variant(ButtonVariant.Ghost)
                    .OnTapped(sceneEditor.OpenEntityDrawer))
            .AddTab("Scene", sceneEditor)
            .AddTab("Sprite Editor", new SpriteEditorPage(new()))
            .AddTab("Behavior Editor", new BehaviorEditorPage());
    }
}
