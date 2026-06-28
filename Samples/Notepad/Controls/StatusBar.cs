using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;

namespace Notepad.Controls;

public sealed class StatusBar(
    IReadableSignal<string> statusText,
    IReadableSignal<string> caretText) : Component
{
    public override VisualElement Build()
    {
        return new ThemeFrame(colors => colors.Primary)
            .Height(25)
            .Content(
                new HStack()
                    .Padding(new Thickness(10, 0))
                    .Alignment(Alignment.Center)
                    .JustifyContent(JustifyContent.SpaceBetween)
                    .Children(
                        new ThemeLabel(colors => colors.OnPrimary)
                            .Text(statusText)
                            .FontSize(12),
                        new ThemeLabel(colors => colors.OnPrimary)
                            .Text(caretText)
                            .FontSize(12)
                    )
            );
    }
}
