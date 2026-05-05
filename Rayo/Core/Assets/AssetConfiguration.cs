namespace Rayo.Core.Assets;

/// <summary>
/// Fluent builder for configuring application assets.
/// Similar to MAUI's MauiAppBuilder configuration pattern.
/// </summary>
public class AssetConfiguration
{
    private readonly AssetManager _assetManager;

    internal AssetConfiguration(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    /// <summary>
    /// Adds a search path for locating assets.
    /// Paths are searched in order of registration (first registered = highest priority).
    /// </summary>
    /// <param name="path">The directory path to search for assets</param>
    /// <returns>The configuration instance for chaining</returns>
    public AssetConfiguration AddSearchPath(string path)
    {
        _assetManager.AddSearchPath(path);
        return this;
    }

    /// <summary>
    /// Registers a font file with an alias for use throughout the application.
    /// </summary>
    /// <param name="path">Path to the font file (TTF, OTF)</param>
    /// <param name="alias">The alias to use when referencing this font (e.g., "Roboto", "Icons")</param>
    /// <param name="defaultSize">Default font size when not specified</param>
    /// <returns>The configuration instance for chaining</returns>
    /// <example>
    /// <code>
    /// app.ConfigureAssets(assets =>
    /// {
    ///     assets.AddFont("Assets/Fonts/Roboto-Regular.ttf", "Roboto");
    ///     assets.AddFont("Assets/Fonts/Lineicons.ttf", "Icons", 24);
    /// });
    /// </code>
    /// </example>
    public AssetConfiguration AddFont(string path, string alias, float defaultSize = 14f)
    {
        _assetManager.RegisterFont(path, alias, defaultSize);
        return this;
    }

    /// <summary>
    /// Registers an image file with an alias for use throughout the application.
    /// </summary>
    /// <param name="path">Path to the image file (PNG, JPG, etc.)</param>
    /// <param name="alias">The alias to use when referencing this image</param>
    /// <returns>The configuration instance for chaining</returns>
    /// <example>
    /// <code>
    /// app.ConfigureAssets(assets =>
    /// {
    ///     assets.AddImage("Assets/Images/logo.png", "Logo");
    ///     assets.AddImage("Assets/Images/avatar.png", "DefaultAvatar");
    /// });
    /// </code>
    /// </example>
    public AssetConfiguration AddImage(string path, string alias)
    {
        _assetManager.RegisterImage(path, alias);
        return this;
    }

    /// <summary>
    /// Sets the default font family for the application.
    /// This font will be used when no specific font is specified for text elements.
    /// </summary>
    /// <param name="alias">The alias of a previously registered font</param>
    /// <returns>The configuration instance for chaining</returns>
    /// <example>
    /// <code>
    /// app.ConfigureAssets(assets =>
    /// {
    ///     assets.AddFont("Assets/Fonts/Roboto-Regular.ttf", "Roboto");
    ///     assets.SetDefaultFont("Roboto");
    /// });
    /// </code>
    /// </example>
    public AssetConfiguration SetDefaultFont(string alias)
    {
        _assetManager.SetDefaultFont(alias);
        return this;
    }

    /// <summary>
    /// Configures fonts using a dedicated builder.
    /// </summary>
    /// <param name="configure">Action to configure fonts</param>
    /// <returns>The configuration instance for chaining</returns>
    /// <example>
    /// <code>
    /// app.ConfigureAssets(assets =>
    /// {
    ///     assets.ConfigureFonts(fonts =>
    ///     {
    ///         fonts.AddFont("Roboto-Regular.ttf", "Roboto");
    ///         fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
    ///         fonts.SetDefault("Roboto");
    ///     });
    /// });
    /// </code>
    /// </example>
    public AssetConfiguration ConfigureFonts(Action<FontConfigurationBuilder> configure)
    {
        var builder = new FontConfigurationBuilder(this);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Configures images using a dedicated builder.
    /// </summary>
    /// <param name="configure">Action to configure images</param>
    /// <returns>The configuration instance for chaining</returns>
    public AssetConfiguration ConfigureImages(Action<ImageConfigurationBuilder> configure)
    {
        var builder = new ImageConfigurationBuilder(this);
        configure(builder);
        return this;
    }
}

/// <summary>
/// Builder for configuring fonts specifically.
/// </summary>
public class FontConfigurationBuilder
{
    private readonly AssetConfiguration _parent;

    internal FontConfigurationBuilder(AssetConfiguration parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Adds a font with the specified alias.
    /// </summary>
    public FontConfigurationBuilder AddFont(string path, string alias, float defaultSize = 14f)
    {
        _parent.AddFont(path, alias, defaultSize);
        return this;
    }

    /// <summary>
    /// Sets the default font family.
    /// </summary>
    public FontConfigurationBuilder SetDefault(string alias)
    {
        _parent.SetDefaultFont(alias);
        return this;
    }
}

/// <summary>
/// Builder for configuring images specifically.
/// </summary>
public class ImageConfigurationBuilder
{
    private readonly AssetConfiguration _parent;

    internal ImageConfigurationBuilder(AssetConfiguration parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Adds an image with the specified alias.
    /// </summary>
    public ImageConfigurationBuilder AddImage(string path, string alias)
    {
        _parent.AddImage(path, alias);
        return this;
    }
}
