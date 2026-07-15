using Rayo.Controls;

namespace Rayo.DevTool;

/// <summary>
/// DevTool-specific glyphs that are not part of Rayo's public SVG icon catalog.
/// </summary>
internal static class DevToolIcons
{
    public static IconData ArrowUp => new IconData("arrowUp")
        .AddPath([(7f, 14f), (12f, 9f), (17f, 14f)], 2.5f);

    public static IconData ArrowDown => new IconData("arrowDown")
        .AddPath([(7f, 10f), (12f, 15f), (17f, 10f)], 2.5f);

    public static IconData Target => new IconData("target")
        .AddCircle(12f, 12f, 9f, false, 2f)
        .AddCircle(12f, 12f, 6f, false, 2f)
        .AddCircle(12f, 12f, 3f, false, 2f);

    public static IconData Sun => new IconData("sun")
        .AddCircle(12f, 12f, 5f, false, 2f)
        .AddLine(12f, 1f, 12f, 5f, 2f)
        .AddLine(12f, 19f, 12f, 23f, 2f)
        .AddLine(1f, 12f, 5f, 12f, 2f)
        .AddLine(19f, 12f, 23f, 12f, 2f);

    public static IconData Moon => new IconData("moon")
        .AddPath([(17f, 3f), (14f, 4f), (11.5f, 6f), (10f, 9f),
                  (10f, 12.5f), (11.5f, 15.5f), (14f, 18f), (17f, 19f)], 2f);
}
