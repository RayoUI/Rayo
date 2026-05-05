using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;

namespace ExampleTabControl;

internal class Soft : IUIBuilder
{
    public VisualElement Build() =>
        new ScrollView()
            .Margin(new Thickness(50))
            .Content(
                new Button()
                    .Width(200)
                    .Height(50)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Id("SoftButton")
                    .OnTapped(() =>
                    {
                        Dialog.Show(
                            "About Rayo Notepad",
                            "Version 1.0\n\nBuilt with Rayo Framework\n.NET 10 + OpenGL");
                    })
            );
}
