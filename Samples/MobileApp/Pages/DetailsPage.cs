using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace MobileApp.Pages;

public class DetailsPage : Component
{
    public override VisualElement Build()
    {
        return new ScrollView()
            .Content(
                new VStack()
                    .Spacing(16)
                    .Padding(new Thickness(20))
                    .Children(
                        new Frame()
                            .Background(Color.White)
                            .BorderRadius(14)
                            .Padding(new Thickness(20))
                            .Content(
                                new VStack()
                                    .Spacing(10)
                                    .Children(
                                        new Label("Router")
                                            .FontSize(24)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("The drawer changes the current route and swaps the page hosted in the content frame.")
                                            .FontSize(14)
                                            .LineHeight(1.25f)
                                            .Foreground(new Color(91, 103, 122))
                                    )),
                        new Frame()
                            .Background(Color.White)
                            .BorderRadius(14)
                            .Padding(new Thickness(20))
                            .Content(
                                new VStack()
                                    .Spacing(10)
                                    .Children(
                                        new Label("Template checklist")
                                            .FontSize(18)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("Shared project\nDesktop host\nAndroid host\nDrawer navigation\nCounter state\nTwo routed pages")
                                            .FontSize(14)
                                            .LineHeight(1.35f)
                                            .Foreground(new Color(91, 103, 122))
                                    ))
                    ));
    }
}
