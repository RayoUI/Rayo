using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages;

public class SettingsPage : Component
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
                                        new Label("Settings")
                                            .FontSize(24)
                                            .Foreground(new Color(25, 39, 62)),
                                        new Label("Use this page for app preferences, notification settings, and mobile configuration.")
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
                                        new Checkbox()
                                            .Label("Enable notifications")
                                            .LabelColor(new Color(45, 55, 72))
                                            .IsChecked(true),
                                        new Checkbox()
                                            .Label("Use compact mode")
                                            .LabelColor(new Color(45, 55, 72)),
                                        new Checkbox()
                                            .Label("Sync over Wi-Fi only")
                                            .LabelColor(new Color(45, 55, 72))
                                            .IsChecked(true)
                                    ))
                    ));
    }
}
