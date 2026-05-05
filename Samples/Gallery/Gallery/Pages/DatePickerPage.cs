using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class DatePickerPage : UserControl
{
    public override VisualElement Build()
    {
        var selectedDate = new Signal<DateTime>(DateTime.Today);
        var dialogDate = new Signal<DateTime>(DateTime.Today.AddDays(3));

        var dialogPreview = new Frame();
        dialogPreview.Height(90);
        dialogPreview.HorizontalAlignment(HorizontalAlignment.Stretch);
        dialogPreview.BorderRadius(12);
        dialogPreview.Background(new Color(38, 41, 52));
        dialogPreview.BorderWidth(1);
        dialogPreview.BorderColor(new Color(255, 255, 255, 25));
        dialogPreview.Padding(new Thickness(16));
        dialogPreview.Content(
            new VStack()
                .Spacing(4)
                .Children(
                    new Label("Next review")
                        .FontSize(12)
                        .Foreground(ColorDefault.Secondary),
                    new Label()
                        .FontSize(16)
                        .Foreground(Color.White)
                        .Text(dialogDate.Map(date => date.ToString("dddd, MMMM d")))
                )
        );

        void OpenDateDialog()
        {
            DatePicker.ShowDialog(
                dialogDate.Value,
                date => dialogDate.Value = date,
                configure: picker => picker.DateFormat("dddd, MMMM d"));
        }

        var openDialogButton = new Button();
        openDialogButton.Text("Choose date");
        openDialogButton.Width(150);
        openDialogButton.Background(ColorDefault.Primary);
        openDialogButton.HoverBackground(ColorDefault.Info);
        openDialogButton.BorderRadius(8);
        openDialogButton.OnTapped(OpenDateDialog);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("DatePicker", "Calendar-based date selection"),

                Helper.CreateExampleSection("Basic DatePicker",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new DatePicker()
                                .Width(240)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .SelectedDate(DateTime.Today)
                                .OnDateChanged(date => selectedDate.Value = date),

                            new Label()
                                .Text(selectedDate.Map(d => $"Selected: {d:yyyy-MM-dd}"))
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("Custom Format",
                    new DatePicker()
                        .Width(240)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .DateFormat("MMMM dd, yyyy")
                        .SelectedDate(DateTime.Today)
                ),

                Helper.CreateExampleSection("Dialog Picker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Mirror the ColorPicker UX by opening the DatePicker inside a modal overlay for compact pages.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            dialogPreview,
                            openDialogButton
                        )
                )
            );
    }

}
