using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal static class NewFolderDialog
{
    public static void Show(
        Func<string, bool> createDirectory,
        Func<string, bool> validateDirectoryName)
    {
        var folderName = new Entry()
            .Placeholder("Folder name")
            .Height(38);
        var content = new VStack()
            .Spacing(8)
            .Children(
                new Label("Create a folder in the current location.")
                    .FontSize(13)
                    .Foreground(new Color(148, 163, 184)),
                folderName);

        Dialog.Show(
            "New folder",
            content,
            showCancelButton: true,
            onAccepted: () =>
            {
                if (!createDirectory(folderName.Text))
                {
                    ToastService.ShowInfo("Enter a valid folder name.");
                }
            },
            validate: () => validateDirectoryName(folderName.Text),
            okText: "Create",
            cancelText: "Cancel",
            initialFocus: folderName);
    }
}
