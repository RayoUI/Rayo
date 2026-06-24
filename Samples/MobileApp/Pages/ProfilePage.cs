using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace MobileApp.Pages;

public class ProfilePage : Component
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
                                    .Spacing(12)
                                    .Children(
                                        new Label("Profile")
                                            .FontSize(24)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("Jane Developer")
                                            .FontSize(18)
                                            .Foreground(new Color(45, 55, 72)),
                                        new Label("This page is a simple placeholder for account, identity, and user-specific mobile app content.")
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
                                    .Spacing(8)
                                    .Children(
                                        new Label("Stats")
                                            .FontSize(18)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("Projects: 4\nTasks completed: 128\nCurrent streak: 12 days")
                                            .FontSize(14)
                                            .LineHeight(1.35f)
                                            .Foreground(new Color(91, 103, 122))
                                    ))
                    ));
    }
}
