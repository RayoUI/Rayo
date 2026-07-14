using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages;

public class DetailsPage : Component
{
    private readonly Action<AppRoute> _navigate;

    public DetailsPage(Action<AppRoute> navigate)
    {
        _navigate = navigate;
    }

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
                                    .Spacing(14)
                                    .Children(
                                        new Label("Page actions")
                                            .FontSize(18)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("Pages can request navigation through the callback provided by the shell.")
                                            .FontSize(14)
                                            .LineHeight(1.25f)
                                            .Foreground(new Color(91, 103, 122)),
                                        new HStack()
                                            .Height(48)
                                            .Spacing(10)
                                            .Alignment(Alignment.Center)
                                            .Children(
                                                new Button()
                                                    .Text("Open profile")
                                                    .Size(new Size(128, 48))
                                                    .OnTapped(() => _navigate(AppRoute.Profile)),
                                                new Button()
                                                    .Text("Open settings")
                                                    .Size(new Size(136, 48))
                                                    .OnTapped(() => _navigate(AppRoute.Settings))
                                            )
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
                                        new Label("Shared project\nDesktop host\nAndroid host\nDrawer navigation\nCounter state\nRouted pages")
                                            .FontSize(14)
                                            .LineHeight(1.35f)
                                            .Foreground(new Color(91, 103, 122))
                                    ))
                    ));
    }
}
