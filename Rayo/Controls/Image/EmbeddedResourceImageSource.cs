using System.Reflection;

namespace Rayo.Controls;

/// <summary>
/// Image source backed by an embedded assembly resource.
/// </summary>
public sealed class EmbeddedResourceImageSource : ImageSource
{
    private readonly Assembly _assembly;
    private readonly string _resourceName;

    public EmbeddedResourceImageSource(Assembly assembly, string resourceName)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _resourceName = string.IsNullOrWhiteSpace(resourceName)
            ? throw new ArgumentException("Resource name cannot be empty.", nameof(resourceName))
            : resourceName;
    }

    public override string GetCacheKey() => $"resource://{_assembly.FullName}/{_resourceName}";

    public override Task<Stream?> GetStreamAsync()
    {
        IsLoading = true;
        OnLoadingStateChanged();

        try
        {
            var stream = _assembly.GetManifestResourceStream(_resourceName);
            IsLoaded = stream != null;
            Error = stream == null ? $"Embedded resource not found: {_resourceName}" : null;
            return Task.FromResult(stream);
        }
        finally
        {
            IsLoading = false;
            OnLoadingStateChanged();
        }
    }
}
