using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class ComboBoxPage : UserControl
{
    public override VisualElement Build()
    {
        var selectedItem = new Signal<string>("None");

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("ComboBox", "Dropdown selection component"),

                Helper.CreateExampleSection("Basic ComboBox",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new ComboBox()
                                .AddItems("Option 1", "Option 2", "Option 3", "Option 4", "Option 5")
                                .Placeholder("Select an option...")
                                .OnSelectionChanged(index => selectedItem.Value = $"Selected index: {index}"),

                            new Label()
                                .Text(selectedItem)
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("ComboBox with Many Items",
                    new ComboBox()
                        .AddItems("January", "February", "March", "April", "May", "June",
                                 "July", "August", "September", "October", "November", "December")
                        .Placeholder("Select a month...")
                        .SelectedIndex(0)
                )
            );
    }
}
