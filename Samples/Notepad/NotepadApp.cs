using Notepad.Controls;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;

namespace Rayo.Example;

public class NotepadApp : Component
{
    private readonly NotepadWorkspace _workspace = new();

    public override VisualElement Build()
    {
        return new VStack()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Spacing(0)
            .Children(
                new MenuBar(_workspace),
                new TabControl()
                    .Ref(_workspace.Attach)
                    .Position(TabPosition.Top)
                    .ShowTabCloseButtons(true)
                    .CloseButtonDisplay(TabCloseButtonDisplayMode.ActiveTabOnly)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch),
                new StatusBar(_workspace.StatusText, _workspace.CaretText)
            );
    }
}
