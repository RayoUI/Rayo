using Silk.NET.Windowing;

namespace Rayo.Core.Platform;

/// <summary>
/// Maps Rayo decoration / resize settings to Silk.NET <see cref="WindowBorder"/>.
/// </summary>
internal static class WindowBorderMapper
{
    public static WindowBorder ToSilkBorder(SystemDecorations decorations, bool canResize) =>
        decorations switch
        {
            SystemDecorations.None => WindowBorder.Hidden,
            SystemDecorations.BorderOnly => WindowBorder.Fixed,
            _ => canResize ? WindowBorder.Resizable : WindowBorder.Fixed
        };
}
