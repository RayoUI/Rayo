namespace Rayo.Styling;

/// <summary>
/// Controls whether a <see cref="StyleSheet"/> penetrates into nested
/// <see cref="Rayo.Core.UserControl"/> subtrees.
/// </summary>
public enum StyleScope
{
    /// <summary>
    /// Styles are applied to the entire subtree, including nested UserControl elements.
    /// This is the default — equivalent to normal CSS cascading.
    /// </summary>
    Global,

    /// <summary>
    /// Styles stop at nested UserControl boundaries.
    /// Each nested component is responsible for styling its own content.
    /// Equivalent to CSS Shadow DOM scoping.
    /// </summary>
    Local,
}
