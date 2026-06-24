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
        var openImagePath = new Signal<string>("No image selected");
        var folderPath = new Signal<string>("No folder selected");
        var dialogPath = new Signal<string>("No path selected");
        var savePath = new Signal<string>("No save path selected");
        var strictSavePath = new Signal<string>("No strict save path selected");

        var picturesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var imagePicker = new FilePicker
        {
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Left,
            DialogTitle = "Open image",
            DefaultDirectory = picturesDirectory,
            SupportedFileExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp"]
        };
        imagePicker.PathChanged += path => openImagePath.Value = path;

        var folderPicker = new FolderPicker
        {
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Left,
            DialogTitle = "Select output folder",
            DefaultDirectory = documentsDirectory
        };
        folderPicker.PathChanged += path => folderPath.Value = path;

        var chooseAssetButton = CreateActionButton("Choose asset", 150);
        chooseAssetButton.OnTapped(() =>
        {
            PathPicker.ShowDialog(
                PathPickerMode.FileOrFolder,
                path => dialogPath.Value = path,
                configure: picker =>
                {
                    picker.DialogTitle = "Select project asset";
                    picker.DefaultDirectory = documentsDirectory;
                    picker.SupportedFileExtensions = [".png", ".jpg", ".json", ".txt"];
                });
        });

        var saveButton = CreateActionButton("Save as", 120);
        saveButton.OnTapped(() =>
        {
            SaveFilePicker.ShowDialog(
                path => savePath.Value = path,
                configure: picker =>
                {
                    picker.DialogTitle = "Save image";
                    picker.DefaultDirectory = picturesDirectory;
                    picker.DefaultFileName = "gallery-export.png";
                    picker.SupportedFileExtensions = [".png"];
                    picker.SaveConflictBehavior = SaveFileConflictBehavior.Overwrite;
                });
        });

        var strictSaveButton = CreateActionButton("Save new", 120);
        strictSaveButton.OnTapped(() =>
        {
            SaveFilePicker.ShowDialog(
                path => strictSavePath.Value = path,
                configure: picker =>
                {
                    picker.DialogTitle = "Save without overwrite";
                    picker.DefaultDirectory = documentsDirectory;
                    picker.DefaultFileName = "new-document.txt";
                    picker.SupportedFileExtensions = [".txt"];
                    picker.SaveConflictBehavior = SaveFileConflictBehavior.Reject;
                });
        });

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("PathPicker", "Open, folder, file-or-folder, and save-file selection"),

                Helper.CreateExampleSection("Open File With Supported Extensions",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            imagePicker,
                            CreateResultLabel(openImagePath)
                        )
                ),

                Helper.CreateExampleSection("Folder Picker With Default Directory",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            folderPicker,
                            CreateResultLabel(folderPath)
                        )
                ),

                Helper.CreateExampleSection("Modal File Or Folder Picker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            chooseAssetButton,
                            CreateResultLabel(dialogPath)
                        )
                ),

                Helper.CreateExampleSection("Save File Picker",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new HStack()
                                .Spacing(10)
                                .Children(saveButton, strictSaveButton),
                            CreateResultLabel(savePath),
                            CreateResultLabel(strictSavePath)
                        )
                )
            );
    }

    private static Button CreateActionButton(string text, float width) =>
        new Button
        {
            Text = text,
            Width = width,
            Background = ColorDefault.Primary,
            HoverBackground = ColorDefault.Info,
            BorderRadius = new CornerRadius(8)
        };

    private static Label CreateResultLabel(Signal<string> text) =>
        new Label()
            .Text(text)
            .Foreground(ColorDefault.Info);
}
