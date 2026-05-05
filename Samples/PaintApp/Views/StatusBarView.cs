using PaintApp.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace PaintApp.Views;

/// <summary>Status bar — shows the active tool name and the cursor position on the canvas.</summary>
public class StatusBarView : UserControl
{
    private readonly PaintViewState _vm;

    public StatusBarView(PaintViewState vm) => _vm = vm;

    public override VisualElement Build() =>
        new Frame()
            .Background(new Color(60, 60, 65))
            .VerticalAlignment(VerticalAlignment.Bottom)
            .Content(
                new HStack()
                    .Spacing(16)
                    .Padding(new Thickness(10, 3))
                    .Children(
                        new Label()
                            .Text(_vm.ToolText)
                            .FontSize(11)
                            .Foreground(new Color(200, 200, 200)),
                        new Label()
                            .Text(_vm.PositionText)
                            .FontSize(11)
                            .Foreground(new Color(160, 160, 160))
                    )
            );
}
