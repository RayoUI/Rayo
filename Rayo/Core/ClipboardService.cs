namespace Rayo.Core;

/// <summary>
/// Servicio de clipboard simple y portátil usando TextCopy
/// </summary>
public static class ClipboardService
{
    // Buffer de clipboard interno (fallback si no hay acceso al sistema)
    private static string _clipboardBuffer = string.Empty;

    /// <summary>
    /// Copia texto al clipboard
    /// </summary>
    public static void SetText(string text)
    {
        _clipboardBuffer = text ?? string.Empty;

        // TextCopy has no Android backend in this host and throws when invoked.
        // The in-process clipboard still provides reliable copy/cut/paste for
        // Rayo controls without surfacing a platform exception.
        if (Platform.PlatformDetector.IsMobile)
        {
            return;
        }

        try
        {
            TextCopy.ClipboardService.SetText(_clipboardBuffer);
        }
        catch
        {
            // Keep using the in-process clipboard fallback.
        }
    }

    /// <summary>
    /// Obtiene texto del clipboard
    /// </summary>
    public static string GetText()
    {
        if (Platform.PlatformDetector.IsMobile)
        {
            return _clipboardBuffer;
        }

        try
        {
            string systemText = TextCopy.ClipboardService.GetText() ?? string.Empty;
            return string.IsNullOrEmpty(systemText) ? _clipboardBuffer : systemText;
        }
        catch
        {
            // Si falla, usar buffer interno
            return _clipboardBuffer;
        }
    }
}
