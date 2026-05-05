namespace Rayo.Layout;

/// <summary>
/// Alineación de elementos en el eje transversal (perpendicular al eje principal)
/// </summary>
public enum Alignment
{
    /// <summary>Alinear al inicio (izquierda o arriba)</summary>
    Start,

    /// <summary>Alinear al centro</summary>
    Center,

    /// <summary>Alinear al final (derecha o abajo)</summary>
    End,

    /// <summary>Estirar para llenar el espacio disponible</summary>
    Stretch
}

/// <summary>
/// Distribución de elementos en el eje principal
/// </summary>
public enum JustifyContent
{
    /// <summary>Elementos al inicio, sin espacio entre ellos</summary>
    Start,

    /// <summary>Elementos centrados</summary>
    Center,

    /// <summary>Elementos al final</summary>
    End,

    /// <summary>Espacio uniforme entre elementos</summary>
    SpaceBetween,

    /// <summary>Espacio uniforme alrededor de elementos</summary>
    SpaceAround,

    /// <summary>Espacio uniforme entre y alrededor</summary>
    SpaceEvenly
}

/// <summary>
/// Orientación del layout
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// Dirección del flujo de elementos en Flex
/// </summary>
public enum FlexDirection
{
    /// <summary>Elementos en fila de izquierda a derecha</summary>
    Row,

    /// <summary>Elementos en fila de derecha a izquierda</summary>
    RowReverse,

    /// <summary>Elementos en columna de arriba a abajo</summary>
    Column,

    /// <summary>Elementos en columna de abajo a arriba</summary>
    ColumnReverse
}

/// <summary>
/// Comportamiento de wrap (ajuste de línea) en Flex
/// </summary>
public enum FlexWrap
{
    /// <summary>No ajustar, mantener elementos en una sola línea</summary>
    NoWrap,

    /// <summary>Ajustar a múltiples líneas</summary>
    Wrap,

    /// <summary>Ajustar a múltiples líneas en orden inverso</summary>
    WrapReverse
}

/// <summary>
/// Alineación de líneas múltiples en Flex
/// </summary>
public enum FlexAlignContent
{
    /// <summary>Líneas al inicio</summary>
    Start,

    /// <summary>Líneas centradas</summary>
    Center,

    /// <summary>Líneas al final</summary>
    End,

    /// <summary>Espacio entre líneas</summary>
    SpaceBetween,

    /// <summary>Espacio alrededor de líneas</summary>
    SpaceAround,

    /// <summary>Líneas estiradas</summary>
    Stretch
}