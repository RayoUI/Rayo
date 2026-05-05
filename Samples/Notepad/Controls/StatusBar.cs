using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Notepad.Controls;

public class StatusBar() : UserControl
{
    public override VisualElement Build()
    {
        return new Frame()
            .Height(24)
            .Background(new Color(0, 122, 204)) // VS Blue-ish
            .Content(
                new HStack()
                    .Padding(new Thickness(10, 0, 10, 0))
                    .Alignment(Alignment.Center)
                    .Children(
                        new Label("Ready")
                            .FontSize(12)
                            .Foreground(Color.White),
                        
                        new Frame().Width(20), // Spacer

                        new Label("Ln 1, Col 1")
                            .FontSize(12)
                            .Foreground(Color.White)
                            .HorizontalAlignment(HorizontalAlignment.Right)
                    )
            );
    }
}
