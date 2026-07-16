using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.SpriteEditor;

internal static class SpriteDimensionsDialog
{
    public static void Show(Action<int, int> create)
    {
        var width = new EntryNumber(16).Placeholder("Width").Height(38);
        var height = new EntryNumber(16).Placeholder("Height").Height(38);
        var content = new VStack()
            .Spacing(8)
            .Children(
                new Label("Set the pixel dimensions for this sprite (1–256).")
                    .FontSize(13)
                    .Foreground(new Color(148, 163, 184)),
                new HStack()
                    .Spacing(8)
                    .Children(width, height));

        Dialog.Show(
            "Sprite dimensions",
            content,
            showCancelButton: true,
            onAccepted: () => create(int.Parse(width.Text), int.Parse(height.Text)),
            validate: () =>
                int.TryParse(width.Text, out var parsedWidth) &&
                int.TryParse(height.Text, out var parsedHeight) &&
                parsedWidth is >= 1 and <= 256 &&
                parsedHeight is >= 1 and <= 256,
            okText: "Create",
            cancelText: "Cancel",
            initialFocus: width);
    }
}
