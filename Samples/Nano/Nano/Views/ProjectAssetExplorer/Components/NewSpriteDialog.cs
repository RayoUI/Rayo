using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal static class NewSpriteDialog
{
    public static void Show(Func<string, VirtualAsset?> createSprite, Action<VirtualAsset> openSprite)
    {
        var name = new Entry()
            .Placeholder("Sprite name")
            .Height(38);
        var content = new VStack()
            .Spacing(8)
            .Children(
                new Label("A .sprite asset will be created in the current folder.")
                    .FontSize(13)
                    .Foreground(new Color(148, 163, 184)),
                name);

        Dialog.Show(
            "New sprite",
            content,
            showCancelButton: true,
            onAccepted: () =>
            {
                var asset = createSprite(name.Text);
                if (asset is null)
                {
                    ToastService.ShowInfo("Use an unused sprite name.");
                    return;
                }

                openSprite(asset);
            },
            validate: () => !string.IsNullOrWhiteSpace(name.Text),
            okText: "Create",
            cancelText: "Cancel",
            initialFocus: name);
    }
}
