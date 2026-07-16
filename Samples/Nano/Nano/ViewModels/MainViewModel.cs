using Nano.Views.ProjectAssetStore;
using Rayo.Core;
using Rayo.Reactivity;

namespace Nano.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public MainViewModel()
        : this(new NanoProjectStore())
    {
    }

    internal MainViewModel(IProjectAssetStore projectStore)
    {
        ProjectStore = projectStore;
    }

    public IProjectAssetStore ProjectStore { get; }

    public Signal<string> Title { get; } = new("Nano");

    public Signal<bool> CanGoBack { get; } = new(false);
}
