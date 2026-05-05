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
        try
        {
            TextCopy.ClipboardService.SetText(text);
        }
        catch
        {
            // Si falla, usar buffer interno
            _clipboardBuffer = text;
        }
    }

    /// <summary>
    /// Obtiene texto del clipboard
    /// </summary>
    public static string GetText()
    {
        try
        {
            return TextCopy.ClipboardService.GetText() ?? string.Empty;
        }
        catch
        {
            // Si falla, usar buffer interno
            return _clipboardBuffer;
        }
    }
}