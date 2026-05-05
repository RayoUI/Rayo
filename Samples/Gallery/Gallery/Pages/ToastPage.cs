using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class ToastPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Toast", "Non-blocking notifications"),

                Helper.CreateExampleSection("Toast Types",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Button()
                                .Text("Info Toast")
                                .Background(ColorDefault.Info)
                                .OnTapped(() =>
                                    Rayo.Controls.ToastService.ShowInfo("This is an information message", 1)),

                            new Button()
                                .Text("Success Toast")
                                .Background(ColorDefault.Success)
                                .OnTapped(() =>
                                    Rayo.Controls.ToastService.ShowSuccess("Operation completed successfully!")),

                            new Button()
                                .Text("Warning Toast")
                                .Background(ColorDefault.Warning)
                                .OnTapped(() =>
                                    Rayo.Controls.ToastService.ShowWarning("Warning: Check your input")),

                            new Button()
                                .Text("Error Toast")
                                .Background(ColorDefault.Danger)
                                .OnTapped(() =>
                                    Rayo.Controls.ToastService.ShowError("An error occurred during operation"))
                        )
                ),

                new Label("Note: Toast notifications require a configured overlay host")
                    .FontSize(12)
                    .Foreground(ColorDefault.Secondary)
            );
    }
}
