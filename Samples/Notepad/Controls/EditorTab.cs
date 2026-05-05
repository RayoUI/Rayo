using Rayo.Controls;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Notepad.Controls;

public class EditorTab(string content = "") : UserControl
{
    protected override void OnInit()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public override VisualElement Build()
    {
        return new Editor()
            .Text(content)
            .FontSize(14)
            .FocusBackground(new Color(30, 30, 30))
            .Background(new Color(30, 30, 30))
            .TextColor(Color.White)
            .BorderWidth(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .OnTextChanged(text => content = text);
    }
}
