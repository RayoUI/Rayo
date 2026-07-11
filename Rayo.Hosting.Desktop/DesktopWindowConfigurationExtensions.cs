using System.Runtime.InteropServices;
using Rayo.Core.Platform;
using Rayo.Hosting.Abstractions;
using SkiaSharp;

namespace Rayo.Hosting.Desktop;

/// <summary>
/// Desktop-specific window configuration helpers.
/// </summary>
public static class DesktopWindowConfigurationExtensions
{
    /// <summary>
    /// Sets the desktop window icon from tightly packed RGBA pixel data.
    /// </summary>
    public static IPlatformWindowConfiguration SetIcon(
        this IPlatformWindowConfiguration config,
        int width,
        int height,
        byte[] rgbaPixels)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config is DesktopWindowConfiguration desktopConfig)
        {
            desktopConfig.NativeConfiguration.Icon = new WindowIcon(width, height, rgbaPixels);
        }

        return config;
    }

    /// <summary>
    /// Loads an image file and uses it as the desktop window icon.
    /// </summary>
    public static IPlatformWindowConfiguration SetIconFromFile(
        this IPlatformWindowConfiguration config,
        string path)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (config is DesktopWindowConfiguration desktopConfig)
        {
            desktopConfig.NativeConfiguration.Icon = LoadIcon(path);
        }

        return config;
    }

    private static WindowIcon LoadIcon(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Window icon file was not found.", path);

        using var codec = SKCodec.Create(path)
            ?? throw new InvalidOperationException($"Unable to decode window icon image: {path}");

        var imageInfo = new SKImageInfo(
            codec.Info.Width,
            codec.Info.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var bitmap = new SKBitmap(imageInfo);
        var result = codec.GetPixels(imageInfo, bitmap.GetPixels());
        if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
        {
            throw new InvalidOperationException(
                $"Unable to decode window icon image '{path}'. Decoder result: {result}.");
        }

        var pixels = new byte[checked(imageInfo.Width * imageInfo.Height * 4)];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);

        return new WindowIcon(imageInfo.Width, imageInfo.Height, pixels);
    }
}
