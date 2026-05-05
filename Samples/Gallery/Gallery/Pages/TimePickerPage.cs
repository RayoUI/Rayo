using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using System;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class TimePickerPage : UserControl
{
    public override VisualElement Build()
    {
        var selectedTime = new Signal<TimeSpan>(new TimeSpan(12, 0, 0));
        var dialogTime = new Signal<TimeSpan>(new TimeSpan(9, 30, 0));

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
                    new Label("Stand-up reminder")
                        .FontSize(12)
                        .Foreground(ColorDefault.Secondary),
                    new Label()
                        .FontSize(16)
                        .Foreground(Color.White)
                        .Text(dialogTime.Map(time => FormatDialogTime(time)))
                )
        );

        void OpenTimeDialog()
        {
            TimePicker.ShowDialog(
                dialogTime.Value,
                time => dialogTime.Value = time,
                configure: picker => picker.TimeFormat("hh:mm tt"));
        }

        var scheduleButton = new Button();
        scheduleButton.Text("Schedule time");
        scheduleButton.Width(150);
        scheduleButton.Background(ColorDefault.Primary);
        scheduleButton.HoverBackground(ColorDefault.Info);
        scheduleButton.BorderRadius(8);
        scheduleButton.OnTapped(OpenTimeDialog);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("TimePicker", "Time selection with hour/minute/AM-PM"),

                Helper.CreateExampleSection("Basic TimePicker (12-hour format)",
                    new VStack()
                        .Spacing(15)
                        .Children(
                            new TimePicker()
                                .Width(200)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .OnTimeChanged(time => selectedTime.Value = time),

                            new Label()
                                .Text(selectedTime.Map(t => $"Selected: {t:hh\\:mm}"))
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("24-hour Format",
                    new TimePicker()
                        .Width(180)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .TimeFormat("HH:mm")
                ),

                Helper.CreateExampleSection("With Minute Increment",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Label()
                                .Text("15-minute increments:")
                                .Foreground(Color.White),

                            new TimePicker()
                                .Width(200)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .MinuteIncrement(15)
                        )
                ),

                Helper.CreateExampleSection("Custom Colors",
                    new TimePicker()
                        .Width(200)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .HeaderColor(new Color(76, 175, 80))
                        .SelectedColor(new Color(76, 175, 80))
                ),

                Helper.CreateExampleSection("Dialog Picker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Reuse the ColorPicker flow to collect times inside modal cards, keeping layouts uncluttered.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            dialogPreview,
                            scheduleButton
                        )
                )
            );
    }

    private static string FormatDialogTime(TimeSpan time)
    {
        return DateTime.Today.Add(time).ToString("hh:mm tt");
    }
}
