using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;

namespace StyleDemo.Components;

// ---------------------------------------------------------------------------
// IsolatedCard — a UserControl with StyleScope.Local.
//
// No matter what global theme is active, this component always renders with
// its own hardcoded look. It demonstrates that StyleScope.Local prevents the
// global StyleSheet from cascading into the component's internal subtree.
// ---------------------------------------------------------------------------
public class IsolatedCard : UserControl
{
    // StyleScope.Local: global styles stop at this component's boundary
    protected override StyleScope StyleScope => StyleScope.Local;

    public override VisualElement Build() =>
        new Frame()
            .Background(new Color(60, 30, 80))
            .BorderRadius(10f)
            .Padding(new Thickness(20, 16))
            .Content(
                new VStack()
                    .Spacing(8f)
                    .Children(
                        new Label("StyleScope.Local")
                            .Foreground(new Color(200, 150, 255))
                            .FontSize(13f),
                        new Label("This card ignores the global theme.")
                            .Foreground(new Color(220, 200, 240))
                            .FontSize(14f),
                        new Label("Its colours are always purple,")
                            .Foreground(new Color(180, 160, 210))
                            .FontSize(12f),
                        new Label("regardless of Dark / Light / Neon.")
                            .Foreground(new Color(180, 160, 210))
                            .FontSize(12f),
                        new Button()
                            .Text("Own Style")
                            .Background(new Color(150, 80, 220))
                            .HoverBackground(new Color(170, 100, 240))
                            .PressedBackground(new Color(120, 60, 190))
                            .TextColor(new Color(255, 255, 255))
                            .Height(36f)
                            .BorderRadius(6f)
                            .HorizontalAlignment(HorizontalAlignment.Left)
                    )
            );
}
