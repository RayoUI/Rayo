using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class PathPickerPage : UserControl
{
    public override VisualElement Build()
    {
        var filePath = new Signal<string>("No file selected");
        var folderPath = new Signal<string>("No folder selected");
        var dialogPath = new Signal<string>("No path selected");

        var filePicker = new FilePicker
        {
            Width = 360,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        filePicker.PathChanged += path => filePath.Value = path;

        var folderPicker = new FolderPicker
        {
            Width = 360,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        folderPicker.PathChanged += path => folderPath.Value = path;

        var openDialogButton = new Button
        {
            Text = "Choose path",
            Width = 150,
            Background = ColorDefault.Primary,
            HoverBackground = ColorDefault.Info,
            BorderRadius = new CornerRadius(8)
        };
        openDialogButton.OnTapped(() =>
        {
            PathPicker.ShowDialog(
                PathPickerMode.FileOrFolder,
                path => dialogPath.Value = path,
                configure: picker =>
                {
                    picker.DialogTitle = "Select project asset";
                });
        });

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("PathPicker", "File and folder selection with a reusable modal picker"),

                Helper.CreateExampleSection("FilePicker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            filePicker,
                            new Label()
                                .Text(filePath)
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("FolderPicker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            folderPicker,
                            new Label()
                                .Text(folderPath)
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("Dialog Picker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            openDialogButton,
                            new Label()
                                .Text(dialogPath)
                                .Foreground(ColorDefault.Info)
                        )
                )
            );
    }
}
