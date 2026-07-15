namespace Rayo.Controls;

/// <summary>
/// Factory for the SVG icon assets embedded in the Rayo assembly.
/// Add SVG files under <c>Assets/Icons</c>; their file name becomes the icon key.
/// </summary>
public static class IconAssets
{
    private const string ResourcePrefix = "Rayo.Assets.Icons.";

    /// <summary>
    /// Gets an SVG icon from the built-in asset catalog.
    /// For example, <c>FromName("delete")</c> resolves
    /// <c>Assets/Icons/delete.svg</c>.
    /// </summary>
    public static ImageSource FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Icon name cannot be empty.", nameof(name));

        return new EmbeddedResourceImageSource(
            typeof(IconAssets).Assembly,
            $"{ResourcePrefix}{name}.svg");
    }
}
