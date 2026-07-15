namespace Rayo.Controls;

/// <summary>
/// Built-in icons backed by SVG assets embedded in the Rayo assembly.
/// </summary>
public static class Icons
{
    public static IconData Add => Svg("add");
    public static IconData ArrowBack => Svg("arrowBack", "arrow-back");
    public static IconData ArrowDownward => Svg("arrowDownward", "arrow-downward");
    public static IconData ArrowForward => Svg("arrowForward", "arrow-forward");
    public static IconData ArrowUpward => Svg("arrowUpward", "arrow-upward");
    public static IconData Broom => Svg("broom", "broom");
    public static IconData Brush => Svg("brush", "brush");
    public static IconData Calendar => Svg("calendar");
    public static IconData Camera => Svg("camera");
    public static IconData Check => Svg("check");
    public static IconData ChevronDown => Svg("chevronDown", "chevron-down");
    public static IconData ChevronLeft => Svg("chevronLeft", "chevron-left");
    public static IconData ChevronRight => Svg("chevronRight", "chevron-right");
    public static IconData ChevronUp => Svg("chevronUp", "chevron-up");
    public static IconData Clock => Svg("clock");
    public static IconData Close => Svg("close");
    public static IconData Connector => Svg("connector", "connector");
    public static IconData Delete => Svg("delete");
    public static IconData Download => Svg("download");
    public static IconData Edit => Svg("edit");
    public static IconData Ellipse => Svg("ellipse", "ellipse");
    public static IconData Eraser => Svg("eraser");
    public static IconData Error => Svg("error");
    public static IconData Email => Svg("email");
    public static IconData File => Svg("file");
    public static IconData FillBucket => Svg("fillBucket", "fill-bucket");
    public static IconData Folder => Svg("folder");
    public static IconData Heart => Svg("heart");
    public static IconData Home => Svg("home");
    public static IconData Image => Svg("image");
    public static IconData Info => Svg("info");
    public static IconData Line => Svg("line", "line");
    public static IconData Lock => Svg("lock");
    public static IconData Menu => Svg("menu");
    public static IconData MoreVert => Svg("moreVert", "more-vert");
    public static IconData Moon => Svg("moon");
    public static IconData Move => Svg("move", "move");
    public static IconData NewFile => Svg("newFile", "new-file");
    public static IconData Notification => Svg("notification");
    public static IconData Person => Svg("person");
    public static IconData Picker => Svg("picker");
    public static IconData Play => Svg("play");
    public static IconData Pause => Svg("pause");
    public static IconData Rectangle => Svg("rectangle", "rectangle");
    public static IconData Refresh => Svg("refresh");
    public static IconData Remove => Svg("remove");
    public static IconData Save => Svg("save");
    public static IconData Search => Svg("search");
    public static IconData Settings => Svg("settings");
    public static IconData Star => Svg("star");
    public static IconData Stop => Svg("stop");
    public static IconData Sun => Svg("sun");
    public static IconData Unlock => Svg("unlock");
    public static IconData Upload => Svg("upload");
    public static IconData VolumeUp => Svg("volumeUp", "volume-up");
    public static IconData VolumeOff => Svg("volumeOff", "volume-off");
    public static IconData Warning => Svg("warning");
    public static IconData Undo => Svg("undo");
    public static IconData Redo => Svg("redo");

    private static IconData Svg(string name, string? assetName = null) =>
        new IconData(name).UseImageSource(IconAssets.FromName(assetName ?? ToKebabCase(name)));

    private static string ToKebabCase(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 4);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
                result.Append('-');

            result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString();
    }
}
