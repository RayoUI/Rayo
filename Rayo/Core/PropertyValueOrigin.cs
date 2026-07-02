namespace Rayo.Core;

/// <summary>Source and precedence of a visual property value.</summary>
public enum PropertyValueOrigin
{
    Default = 0,
    Theme = 1,
    Style = 2,
    Binding = 3,
    Local = 4,
}
