using Rayo.Core;
using Rayo.Layout;
using VisualScripting.Controls;
using VisualScripting.Models;

namespace NanoApp.Pages;

public sealed class BehaviorEditorPage : Component
{
    private readonly NodeGraph _graph = new();

    public override VisualElement Build()
    {
        var editor = new NodeEditorCanvas(_graph);
        var nodePanel = new NodeToolbar(editor)
        {
            ZIndex = 10
        };

        return new HStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(nodePanel, editor);
    }
}
