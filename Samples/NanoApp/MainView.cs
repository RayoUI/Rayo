using NanoApp.Pages;
using Rayo.Controls;
using Rayo.Core;

namespace NanoApp;

public sealed class MainView : Component
{
    public override VisualElement Build()
    {
        return new TabControl()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .AddTab("Scene", new SceneEditorPage())
            .AddTab("Sprite Editor", new SpriteEditorPage(new()))
            .AddTab("Behavior Editor", new BehaviorEditorPage());
    }
}
