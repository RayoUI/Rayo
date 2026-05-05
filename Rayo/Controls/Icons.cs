namespace Rayo.Controls;

/// <summary>
/// Librería de iconos predefinidos inspirados en Material Design Icons.
/// Todos los iconos están definidos en un viewBox de 24x24.
/// </summary>
public static class Icons
{
    // ==================== ACCIONES ====================

    /// <summary>
    /// Icono de check/marca de verificación
    /// </summary>
    public static IconData Check => new IconData("check")
        .AddPath(new List<(float x, float y)>
        {
            (4f, 12f),
            (9f, 17f),
            (20f, 6f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de cerrar/X
    /// </summary>
    public static IconData Close => new IconData("close")
        .AddLine(6f, 6f, 18f, 18f, 2.5f)
        .AddLine(18f, 6f, 6f, 18f, 2.5f);

    /// <summary>
    /// Icono de más/agregar
    /// </summary>
    public static IconData Add => new IconData("add")
        .AddLine(12f, 5f, 12f, 19f, 2.5f)
        .AddLine(5f, 12f, 19f, 12f, 2.5f);

    /// <summary>
    /// Icono de menos/remover
    /// </summary>
    public static IconData Remove => new IconData("remove")
        .AddLine(5f, 12f, 19f, 12f, 2.5f);

    /// <summary>
    /// Icono de editar/lápiz
    /// </summary>
    public static IconData Edit => new IconData("edit")
        .AddPath(new List<(float x, float y)>
        {
            (3f, 17.25f),
            (3f, 21f),
            (6.75f, 21f),
            (17.81f, 9.94f),
            (14.06f, 6.19f),
            (3f, 17.25f)
        })
        .AddLine(14.06f, 6.19f, 17.81f, 9.94f, 2f)
        .AddLine(10.5f, 9.75f, 14.25f, 13.5f, 2f);

    /// <summary>
    /// Icono de eliminar/basurero
    /// </summary>
    public static IconData Delete => new IconData("delete")
        .AddRect(5f, 7f, 14f, 14f, filled: false, strokeWidth: 2f)
        .AddLine(8f, 4f, 16f, 4f, 2f)
        .AddLine(10f, 10f, 10f, 17f, 2f)
        .AddLine(14f, 10f, 14f, 17f, 2f);

    /// <summary>
    /// Icono de guardar/diskette
    /// </summary>
    public static IconData Save => new IconData("save")
        .AddRect(4f, 4f, 16f, 16f, filled: false, strokeWidth: 2f)
        .AddRect(7f, 4f, 10f, 7f, filled: true)
        .AddRect(7f, 13f, 10f, 3f, filled: true);

    // ==================== NAVEGACIÓN ====================

    /// <summary>
    /// Icono de menú/hamburger
    /// </summary>
    public static IconData Menu => new IconData("menu")
        .AddLine(3f, 6f, 21f, 6f, 2.5f)
        .AddLine(3f, 12f, 21f, 12f, 2.5f)
        .AddLine(3f, 18f, 21f, 18f, 2.5f);

    /// <summary>
    /// Icono de flecha arriba
    /// </summary>
    public static IconData ArrowUp => new IconData("arrowUp")
        .AddPath(new List<(float x, float y)>
        {
            (7f, 14f),
            (12f, 9f),
            (17f, 14f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de flecha abajo
    /// </summary>
    public static IconData ArrowDown => new IconData("arrowDown")
        .AddPath(new List<(float x, float y)>
        {
            (7f, 10f),
            (12f, 15f),
            (17f, 10f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de flecha izquierda
    /// </summary>
    public static IconData ArrowLeft => new IconData("arrowLeft")
        .AddPath(new List<(float x, float y)>
        {
            (14f, 7f),
            (9f, 12f),
            (14f, 17f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de flecha derecha
    /// </summary>
    public static IconData ArrowRight => new IconData("arrowRight")
        .AddPath(new List<(float x, float y)>
        {
            (10f, 7f),
            (15f, 12f),
            (10f, 17f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de flecha diagonal arriba-izquierda
    /// </summary>
    public static IconData ArrowUpLeft => new IconData("arrowUpLeft")
        .AddLine(17f, 17f, 7f, 7f, 2.5f)
        .AddLine(7f, 13f, 7f, 7f, 2.5f)
        .AddLine(13f, 7f, 7f, 7f, 2.5f);

    /// <summary>
    /// Icono de flecha diagonal arriba-derecha
    /// </summary>
    public static IconData ArrowUpRight => new IconData("arrowUpRight")
        .AddLine(7f, 17f, 17f, 7f, 2.5f)
        .AddLine(17f, 13f, 17f, 7f, 2.5f)
        .AddLine(11f, 7f, 17f, 7f, 2.5f);

    /// <summary>
    /// Icono de flecha diagonal abajo-izquierda
    /// </summary>
    public static IconData ArrowDownLeft => new IconData("arrowDownLeft")
        .AddLine(17f, 7f, 7f, 17f, 2.5f)
        .AddLine(13f, 17f, 7f, 17f, 2.5f)
        .AddLine(7f, 11f, 7f, 17f, 2.5f);

    /// <summary>
    /// Icono de flecha diagonal abajo-derecha
    /// </summary>
    public static IconData ArrowDownRight => new IconData("arrowDownRight")
        .AddLine(7f, 7f, 17f, 17f, 2.5f)
        .AddLine(11f, 17f, 17f, 17f, 2.5f)
        .AddLine(17f, 11f, 17f, 17f, 2.5f);

    /// <summary>
    /// Chevron arriba
    /// </summary>
    public static IconData ChevronUp => new IconData("chevronUp")
        .AddPath(new List<(float x, float y)>
        {
            (7f, 14f),
            (12f, 9f),
            (17f, 14f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron abajo
    /// </summary>
    public static IconData ChevronDown => new IconData("chevronDown")
        .AddPath(new List<(float x, float y)>
        {
            (7f, 10f),
            (12f, 15f),
            (17f, 10f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron izquierda
    /// </summary>
    public static IconData ChevronLeft => new IconData("chevronLeft")
        .AddPath(new List<(float x, float y)>
        {
            (14f, 7f),
            (9f, 12f),
            (14f, 17f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron derecha
    /// </summary>
    public static IconData ChevronRight => new IconData("chevronRight")
        .AddPath(new List<(float x, float y)>
        {
            (10f, 7f),
            (15f, 12f),
            (10f, 17f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron diagonal arriba-izquierda
    /// </summary>
    public static IconData ChevronUpLeft => new IconData("chevronUpLeft")
        .AddPath(new List<(float x, float y)>
        {
            (15f, 15f),
            (9f, 9f),
            (9f, 15f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron diagonal arriba-derecha
    /// </summary>
    public static IconData ChevronUpRight => new IconData("chevronUpRight")
        .AddPath(new List<(float x, float y)>
        {
            (9f, 15f),
            (15f, 9f),
            (15f, 15f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron diagonal abajo-izquierda
    /// </summary>
    public static IconData ChevronDownLeft => new IconData("chevronDownLeft")
        .AddPath(new List<(float x, float y)>
        {
            (15f, 9f),
            (9f, 15f),
            (15f, 15f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Chevron diagonal abajo-derecha
    /// </summary>
    public static IconData ChevronDownRight => new IconData("chevronDownRight")
        .AddPath(new List<(float x, float y)>
        {
            (9f, 9f),
            (15f, 15f),
            (9f, 15f)
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de home/casa
    /// </summary>
    public static IconData Home => new IconData("home")
        .AddPath(new List<(float x, float y)>
        {
            (3f, 12f),
            (12f, 3f),
            (21f, 12f),
            (21f, 21f),
            (15f, 21f),
            (15f, 15f),
            (9f, 15f),
            (9f, 21f),
            (3f, 21f),
            (3f, 12f)
        }, strokeWidth: 2f);

    // ==================== COMUNICACIÓN ====================

    /// <summary>
    /// Icono de búsqueda/lupa
    /// </summary>
    public static IconData Search => new IconData("search")
        .AddCircle(10f, 10f, 7f, filled: false, strokeWidth: 2.5f)
        .AddLine(15f, 15f, 20f, 20f, 2.5f);

    /// <summary>
    /// Icono de email/correo
    /// </summary>
    public static IconData Email => new IconData("email")
        .AddRect(3f, 5f, 18f, 14f, filled: false, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (3f, 5f),
            (12f, 13f),
            (21f, 5f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de notificación/campana
    /// </summary>
    public static IconData Notification => new IconData("notification")
        .AddPath(new List<(float x, float y)>
        {
            (6f, 8f),
            (6f, 6f),
            (12f, 2f),
            (18f, 6f),
            (18f, 15f),
            (20f, 17f),
            (4f, 17f),
            (6f, 15f),
            (6f, 8f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (10f, 20f),
            (12f, 22f),
            (14f, 20f)
        }, strokeWidth: 2f);

    // ==================== OBJETOS ====================

    /// <summary>
    /// Icono de configuración/engranaje
    /// </summary>
    public static IconData Settings => new IconData("settings")
        .AddCircle(12f, 12f, 3f, filled: false, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (12f, 1f),
            (12f, 4f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (12f, 20f),
            (12f, 23f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (4.22f, 4.22f),
            (6.34f, 6.34f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (17.66f, 17.66f),
            (19.78f, 19.78f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (1f, 12f),
            (4f, 12f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (20f, 12f),
            (23f, 12f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (4.22f, 19.78f),
            (6.34f, 17.66f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (17.66f, 6.34f),
            (19.78f, 4.22f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de estrella
    /// </summary>
    public static IconData Star => new IconData("star")
        .AddPolygon(new List<(float x, float y)>
        {
            (12f, 2f),
            (15f, 9f),
            (23f, 9f),
            (17f, 14f),
            (19f, 22f),
            (12f, 17f),
            (5f, 22f),
            (7f, 14f),
            (1f, 9f),
            (9f, 9f)
        }, filled: true);

    /// <summary>
    /// Icono de corazón
    /// </summary>
    public static IconData Heart => new IconData("heart")
        .AddPath(new List<(float x, float y)>
        {
            (12f, 21.35f),
            (10.55f, 20.03f),
            (2f, 12f),
            (2f, 8.5f),
            (7.5f, 3f),
            (12f, 5.5f),
            (16.5f, 3f),
            (22f, 8.5f),
            (22f, 12f),
            (13.45f, 20.03f),
            (12f, 21.35f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de usuario/persona
    /// </summary>
    public static IconData Person => new IconData("person")
        .AddCircle(12f, 7f, 4f, filled: false, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (5f, 21f),
            (5f, 18f),
            (12f, 14f),
            (19f, 18f),
            (19f, 21f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de información (i en círculo)
    /// </summary>
    public static IconData Info => new IconData("info")
        .AddCircle(12f, 12f, 10f, filled: false, strokeWidth: 2f)
        .AddLine(12f, 11f, 12f, 17f, 2.5f)
        .AddCircle(12f, 7.5f, 1f, filled: true);

    /// <summary>
    /// Icono de advertencia/warning (triángulo con !)
    /// </summary>
    public static IconData Warning => new IconData("warning")
        .AddPath(new List<(float x, float y)>
        {
            (12f, 2f),
            (22f, 20f),
            (2f, 20f),
            (12f, 2f)
        }, strokeWidth: 2f)
        .AddLine(12f, 8f, 12f, 13f, 2.5f)
        .AddCircle(12f, 16f, 1f, filled: true);

    /// <summary>
    /// Icono de error/X en círculo
    /// </summary>
    public static IconData Error => new IconData("error")
        .AddCircle(12f, 12f, 10f, filled: false, strokeWidth: 2f)
        .AddLine(8f, 8f, 16f, 16f, 2.5f)
        .AddLine(16f, 8f, 8f, 16f, 2.5f);

    // ==================== DOCUMENTOS ====================

    /// <summary>
    /// Icono de archivo/documento
    /// </summary>
    public static IconData File => new IconData("file")
        .AddPath(new List<(float x, float y)>
        {
            (6f, 2f),
            (6f, 22f),
            (18f, 22f),
            (18f, 8f),
            (13f, 2f),
            (6f, 2f)
        }, strokeWidth: 2f)
        .AddLine(13f, 2f, 13f, 8f, 2f)
        .AddLine(13f, 8f, 18f, 8f, 2f);

    /// <summary>
    /// Icono de carpeta/folder
    /// </summary>
    public static IconData Folder => new IconData("folder")
        .AddPath(new List<(float x, float y)>
        {
            (3f, 5f),
            (3f, 19f),
            (21f, 19f),
            (21f, 9f),
            (10f, 9f),
            (8f, 5f),
            (3f, 5f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de descargar/download
    /// </summary>
    public static IconData Download => new IconData("download")
        .AddLine(12f, 4f, 12f, 16f, 2.5f)
        .AddPath(new List<(float x, float y)>
        {
            (7f, 11f),
            (12f, 16f),
            (17f, 11f)
        }, strokeWidth: 2.5f)
        .AddLine(4f, 20f, 20f, 20f, 2.5f);

    /// <summary>
    /// Icono de subir/upload
    /// </summary>
    public static IconData Upload => new IconData("upload")
        .AddLine(12f, 16f, 12f, 4f, 2.5f)
        .AddPath(new List<(float x, float y)>
        {
            (7f, 9f),
            (12f, 4f),
            (17f, 9f)
        }, strokeWidth: 2.5f)
        .AddLine(4f, 20f, 20f, 20f, 2.5f);

    // ==================== MEDIOS ====================

    /// <summary>
    /// Icono de play/reproducir
    /// </summary>
    public static IconData Play => new IconData("play")
        .AddPolygon(new List<(float x, float y)>
        {
            (8f, 5f),
            (8f, 19f),
            (19f, 12f)
        }, filled: true);

    /// <summary>
    /// Icono de pausa
    /// </summary>
    public static IconData Pause => new IconData("pause")
        .AddRect(6f, 4f, 4f, 16f, filled: true)
        .AddRect(14f, 4f, 4f, 16f, filled: true);

    /// <summary>
    /// Icono de stop/detener
    /// </summary>
    public static IconData Stop => new IconData("stop")
        .AddRect(6f, 6f, 12f, 12f, filled: true);

    /// <summary>
    /// Icono de imagen/foto
    /// </summary>
    public static IconData Image => new IconData("image")
        .AddRect(3f, 3f, 18f, 18f, filled: false, strokeWidth: 2f)
        .AddCircle(8.5f, 8.5f, 2f, filled: true)
        .AddPath(new List<(float x, float y)>
        {
            (3f, 17f),
            (8f, 12f),
            (13f, 17f),
            (21f, 9f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de cámara
    /// </summary>
    public static IconData Camera => new IconData("camera")
        .AddPath(new List<(float x, float y)>
        {
            (3f, 7f),
            (3f, 19f),
            (21f, 19f),
            (21f, 7f),
            (17f, 7f),
            (15f, 4f),
            (9f, 4f),
            (7f, 7f),
            (3f, 7f)
        }, strokeWidth: 2f)
        .AddCircle(12f, 13f, 3f, filled: false, strokeWidth: 2f);

    // ==================== CONTROLES ====================

    /// <summary>
    /// Icono de volumen alto
    /// </summary>
    public static IconData VolumeUp => new IconData("volumeUp")
        .AddPolygon(new List<(float x, float y)>
        {
            (11f, 5f),
            (6f, 9f),
            (3f, 9f),
            (3f, 15f),
            (6f, 15f),
            (11f, 19f)
        }, filled: true)
        .AddPath(new List<(float x, float y)>
        {
            (15f, 9f),
            (17f, 12f),
            (15f, 15f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (18f, 6f),
            (21f, 12f),
            (18f, 18f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de volumen mute/silencio
    /// </summary>
    public static IconData VolumeMute => new IconData("volumeMute")
        .AddPolygon(new List<(float x, float y)>
        {
            (11f, 5f),
            (6f, 9f),
            (3f, 9f),
            (3f, 15f),
            (6f, 15f),
            (11f, 19f)
        }, filled: true)
        .AddLine(17f, 9f, 21f, 15f, 2.5f)
        .AddLine(21f, 9f, 17f, 15f, 2.5f);

    /// <summary>
    /// Icono de refrescar/actualizar
    /// Arc: 270° clockwise from top (12,3.5) through right, bottom, to left (3.5,12).
    /// Center=(12,12), r=8.5. x = 12+8.5*sin(θ), y = 12-8.5*cos(θ), θ in 15° steps.
    /// Arrowhead at arc start (top) pointing clockwise (right).
    /// </summary>
    public static IconData Refresh => new IconData("refresh")
        .AddPath(new List<(float x, float y)>
        {
            (12f,    3.5f),  // θ=0   (top)
            (14.2f,  3.8f),  // θ=15
            (16.25f, 4.6f),  // θ=30
            (18f,    6f),    // θ=45
            (19.4f,  7.75f), // θ=60
            (20.2f,  9.8f),  // θ=75
            (20.5f,  12f),   // θ=90  (right)
            (20.2f,  14.2f), // θ=105
            (19.4f,  16.25f),// θ=120
            (18f,    18f),   // θ=135
            (16.25f, 19.4f), // θ=150
            (14.2f,  20.2f), // θ=165
            (12f,    20.5f), // θ=180 (bottom)
            (9.8f,   20.2f), // θ=195
            (7.75f,  19.4f), // θ=210
            (6f,     18f),   // θ=225
            (4.6f,   16.25f),// θ=240
            (3.8f,   14.2f), // θ=255
            (3.5f,   12f),   // θ=270 (left)
        }, strokeWidth: 2.5f)
        // Arrowhead at arc start (top), tip at (12,3.5) pointing left
        .AddPath(new List<(float x, float y)>
        {
            (13.8f, 2f),
            (11.3f, 4f),
            (13.8f, 6f),
        }, strokeWidth: 2.5f);

    /// <summary>
    /// Icono de bloqueo/candado cerrado
    /// </summary>
    public static IconData Lock => new IconData("lock")
        .AddRect(5f, 11f, 14f, 10f, filled: false, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (7f, 11f),
            (7f, 7f),
            (12f, 3f),
            (17f, 7f),
            (17f, 11f)
        }, strokeWidth: 2f)
        .AddCircle(12f, 16f, 1.5f, filled: true);

    /// <summary>
    /// Icono de desbloqueo/candado abierto
    /// </summary>
    public static IconData Unlock => new IconData("unlock")
        .AddRect(5f, 11f, 14f, 10f, filled: false, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (7f, 11f),
            (7f, 7f),
            (12f, 3f),
            (17f, 7f)
        }, strokeWidth: 2f)
        .AddCircle(12f, 16f, 1.5f, filled: true);

    // ==================== DESARROLLO/DEBUG ====================

    /// <summary>
    /// Icono de target/objetivo - útil para resaltar elementos
    /// </summary>
    public static IconData Target => new IconData("target")
        .AddCircle(12f, 12f, 9f, filled: false, strokeWidth: 2f)
        .AddCircle(12f, 12f, 6f, filled: false, strokeWidth: 2f)
        .AddCircle(12f, 12f, 3f, filled: false, strokeWidth: 2f)
        .AddLine(12f, 3f, 12f, 7f, 2f)
        .AddLine(12f, 17f, 12f, 21f, 2f)
        .AddLine(3f, 12f, 7f, 12f, 2f)
        .AddLine(17f, 12f, 21f, 12f, 2f);

    /// <summary>
    /// Icono de highlight/resaltar - cuadro punteado
    /// </summary>
    public static IconData Highlight => new IconData("highlight")
        .AddRect(4f, 4f, 16f, 16f, filled: false, strokeWidth: 2.5f)
        .AddLine(4f, 12f, 20f, 12f, 1.5f)
        .AddLine(12f, 4f, 12f, 20f, 1.5f)
        .AddCircle(4f, 4f, 1.5f, filled: true)
        .AddCircle(20f, 4f, 1.5f, filled: true)
        .AddCircle(4f, 20f, 1.5f, filled: true)
        .AddCircle(20f, 20f, 1.5f, filled: true);

    // ==================== NUEVOS ICONOS ====================

    /// <summary>
    /// Icono de gráfico de barras
    /// </summary>
    public static IconData BarChart => new IconData("barChart")
        .AddRect(4f, 10f, 3f, 10f, filled: true)
        .AddRect(10f, 6f, 3f, 14f, filled: true)
        .AddRect(16f, 2f, 3f, 18f, filled: true);

    /// <summary>
    /// Icono de gráfico de líneas
    /// </summary>
    public static IconData LineChart => new IconData("lineChart")
        .AddLine(4f, 16f, 10f, 10f, 2f)
        .AddLine(10f, 10f, 16f, 14f, 2f)
        .AddLine(16f, 14f, 20f, 8f, 2f);

    /// <summary>
    /// Icono de nube
    /// </summary>
    public static IconData Cloud => new IconData("cloud")
        .AddPath(new List<(float x, float y)>
        {
            (6f, 16f),
            (4f, 12f),
            (6f, 8f),
            (10f, 6f),
            (14f, 8f),
            (16f, 12f),
            (14f, 16f),
            (6f, 16f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de sol
    /// </summary>
    public static IconData Sun => new IconData("sun")
        .AddCircle(12f, 12f, 5f, filled: false, strokeWidth: 2f)
        .AddLine(12f, 1f, 12f, 5f, 2f)
        .AddLine(12f, 19f, 12f, 23f, 2f)
        .AddLine(1f, 12f, 5f, 12f, 2f)
        .AddLine(19f, 12f, 23f, 12f, 2f)
        .AddLine(4.22f, 4.22f, 6.34f, 6.34f, 2f)
        .AddLine(17.66f, 17.66f, 19.78f, 19.78f, 2f)
        .AddLine(4.22f, 19.78f, 6.34f, 17.66f, 2f)
        .AddLine(17.66f, 6.34f, 19.78f, 4.22f, 2f);

    /// <summary>
    /// Icono de luna
    /// </summary>
    public static IconData Moon => new IconData("moon")
        .AddPath(new List<(float x, float y)>
        {
            (12f, 2f),
            (15f, 2f),
            (18f, 5f),
            (18f, 9f),
            (15f, 12f),
            (12f, 12f),
            (9f, 9f),
            (9f, 5f),
            (12f, 2f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de reloj
    /// </summary>
    public static IconData Clock => new IconData("clock")
        .AddCircle(12f, 12f, 10f, filled: false, strokeWidth: 2f)
        .AddLine(12f, 12f, 12f, 7f, 2f)
        .AddLine(12f, 12f, 16f, 12f, 2f);

    /// <summary>
    /// Icono de calendario
    /// </summary>
    public static IconData Calendar => new IconData("calendar")
        .AddRect(3f, 5f, 18f, 16f, filled: false, strokeWidth: 2f)
        .AddLine(3f, 9f, 21f, 9f, 2f)
        .AddLine(7f, 2f, 7f, 5f, 2f)
        .AddLine(17f, 2f, 17f, 5f, 2f)
        .AddPath(new List<(float x, float y)>
        {
            (7f, 13f),
            (10f, 13f),
            (10f, 16f),
            (7f, 16f),
            (7f, 13f)
        }, strokeWidth: 2f);

    /// <summary>
    /// Icono de selector
    /// </summary>
    public static IconData Picker => new IconData("picker")
        .AddPath(new List<(float x, float y)>
        {
            (12f, 2f),
            (14f, 4f),
            (10f, 8f),
            (8f, 6f),
            (12f, 2f)
        }, strokeWidth: 2f)
        .AddLine(10f, 8f, 4f, 20f, 2f)
        .AddLine(4f, 20f, 8f, 16f, 2f);

    /// <summary>
    /// Icono de paleta de colores
    /// </summary>
    public static IconData ColorPalette => new IconData("colorPalette")
        .AddCircle(12f, 12f, 10f, filled: false, strokeWidth: 2f)
        .AddCircle(8f, 10f, 1.5f, filled: true)
        .AddCircle(12f, 8f, 1.5f, filled: true)
        .AddCircle(16f, 10f, 1.5f, filled: true)
        .AddCircle(12f, 16f, 2f, filled: true);

    // ==================== PAINT TOOLS ====================

    /// <summary>
    /// Brush tool icon — angled stroke with a rounded tip.
    /// </summary>
    public static IconData BrushTool => new IconData("brushTool")
        .AddLine(4f, 20f, 16f, 8f, 3f)
        .AddCircle(17.5f, 6.5f, 3f, filled: false, strokeWidth: 2f)
        .AddLine(4f, 20f, 7f, 18f, 2f);

    /// <summary>
    /// Eraser tool icon — a rectangle body with a dividing line.
    /// </summary>
    public static IconData Eraser => new IconData("eraser")
        .AddPath(new List<(float x, float y)>
        {
            (3f, 14f), (3f, 20f), (13f, 20f), (20f, 13f), (14f, 7f), (7f, 7f), (3f, 14f)
        }, strokeWidth: 2f)
        .AddLine(10f, 20f, 20f, 10f, 1.5f);

    /// <summary>
    /// Rectangle tool icon — an outlined rectangle.
    /// </summary>
    public static IconData RectangleTool => new IconData("rectangleTool")
        .AddRect(4f, 6f, 16f, 12f, filled: false, strokeWidth: 2.5f);

    /// <summary>
    /// Ellipse tool icon — an outlined circle.
    /// </summary>
    public static IconData EllipseTool => new IconData("ellipseTool")
        .AddCircle(12f, 12f, 8f, filled: false, strokeWidth: 2.5f);

    /// <summary>
    /// Line tool icon — a single diagonal line.
    /// </summary>
    public static IconData LineTool => new IconData("lineTool")
        .AddLine(4f, 20f, 20f, 4f, 2.5f);

    /// <summary>
    /// Fill (paint bucket) tool icon.
    /// </summary>
    public static IconData FillBucket => new IconData("fillBucket")
        .AddPath(new List<(float x, float y)>
        {
            (5f, 3f), (5f, 14f), (12f, 21f), (19f, 14f), (12f, 7f), (5f, 14f)
        }, strokeWidth: 2f)
        .AddLine(5f, 3f, 14f, 3f, 2f)
        .AddLine(14f, 3f, 14f, 8f, 2f)
        .AddCircle(20.5f, 19f, 2.5f, filled: true);

    /// <summary>
    /// New file icon — a document with a plus sign.
    /// </summary>
    public static IconData NewFile => new IconData("newFile")
        .AddPath(new List<(float x, float y)>
        {
            (6f, 2f), (6f, 22f), (18f, 22f), (18f, 8f), (14f, 2f), (6f, 2f)
        }, strokeWidth: 2f)
        .AddPath(new List<(float x, float y)>
        {
            (14f, 2f), (14f, 8f), (18f, 8f)
        }, strokeWidth: 2f)
        .AddLine(9f, 14f, 15f, 14f, 2f)
        .AddLine(12f, 11f, 12f, 17f, 2f);
}