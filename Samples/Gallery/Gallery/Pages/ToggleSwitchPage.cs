using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class ToggleSwitchPage : UserControl
{
    public override VisualElement Build()
    {
        var isEnabled = new Signal<bool>(false);
        var notifications = new Signal<bool>(true);
        var darkMode = new Signal<bool>(false);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ToggleSwitch", "ON/OFF toggle button"),

                Helper.CreateExampleSection("Basic Toggle",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new HStack()
                                .Spacing(15)
                                .Alignment(Alignment.Start)
                                .Children(
                                    new ToggleSwitch()
                                        .IsOn(isEnabled.Value)
                                        .OnToggled(value => isEnabled.Value = value),
                                    new Label()
                                        .Text(isEnabled.Map(v => v ? "Enabled" : "Disabled"))
                                        .VerticalAlignment(VerticalAlignment.Center)
                                        .Foreground(ColorDefault.Info)
                                )
                        )
                ),

                Helper.CreateExampleSection("Settings Example",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new HStack()
                                .Spacing(15)
                                .Children(
                                    new ToggleSwitch()
                                        .IsOn(notifications.Value)
                                        .OnToggled(value => notifications.Value = value),
                                    new Label("Enable Notifications")
                                        .VerticalAlignment(VerticalAlignment.Center)
                                ),

                            new HStack()
                                .Spacing(15)
                                .Children(
                                    new ToggleSwitch()
                                        .IsOn(darkMode.Value)
                                        .OnToggled(value => darkMode.Value = value)
                                        .OnColor(new Color(100, 100, 255)),
                                    new Label("Dark Mode")
                                        .VerticalAlignment(VerticalAlignment.Center)
                                )
                        )
                )
            );
    }
}
