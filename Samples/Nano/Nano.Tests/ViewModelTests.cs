using Nano.ViewModels;
using Nano.Views.ProjectAssetStore;
using Nano.Views.ProjectAssetStore.Components;
using Rayo.Rendering;
using Xunit;

namespace Nano.Tests;

public sealed class ViewModelTests
{
    [Fact]
    public void Project_explorer_view_model_owns_navigation_and_view_mode()
    {
        var store = new FakeProjectAssetStore();
        var viewModel = new ProjectAssetExplorerViewModel(store);

        viewModel.NavigateTo("scripts");
        viewModel.SetViewMode(AssetViewMode.Grid);

        Assert.Equal("scripts", viewModel.CurrentDirectory.Value);
        Assert.Equal(AssetViewMode.Grid, viewModel.ViewMode.Value);
        Assert.Equal("main.lua", Assert.Single(viewModel.Assets).Name);

        viewModel.NavigateUp();

        Assert.Equal(string.Empty, viewModel.CurrentDirectory.Value);
    }

    [Fact]
    public void Home_view_model_reuses_and_reindexes_document_tabs()
    {
        var viewModel = new HomeViewModel();

        var first = viewModel.OpenTextAsset("a.lua", "a", _ => { });
        var second = viewModel.OpenTextAsset("b.lua", "b", _ => { });
        var existing = viewModel.OpenTextAsset("a.lua", "ignored", _ => { });

        Assert.Equal(2, first.TabIndex);
        Assert.Equal(3, second.TabIndex);
        Assert.False(existing.IsNew);
        Assert.Equal(2, existing.TabIndex);

        Assert.True(viewModel.CloseTextAsset(2));
        Assert.Equal(
            2,
            viewModel.OpenTextAsset("b.lua", "ignored", _ => { }).TabIndex);
    }

    [Fact]
    public void Sprite_editor_view_model_manages_frames_and_history()
    {
        var viewModel = new SpriteEditorViewModel();
        var red = new Color(220, 40, 40);
        var blue = new Color(40, 80, 220);

        viewModel.CurrentFrame.Pixels[0, 0] = red;
        viewModel.RecordCurrentFrameState();
        viewModel.CurrentFrame.Pixels[0, 0] = blue;
        viewModel.RecordCurrentFrameState();

        Assert.True(viewModel.Undo());
        Assert.Equal(red, viewModel.CurrentFrame.Pixels[0, 0]);

        var initialCount = viewModel.Frames.Count;
        viewModel.CloneFrame(0);

        Assert.Equal(initialCount + 1, viewModel.Frames.Count);
        Assert.Equal(1, viewModel.SelectedFrameIndex.Value);
    }

    private sealed class FakeProjectAssetStore : IProjectAssetStore
    {
        private readonly Dictionary<string, string> _files =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["scripts/main.lua"] = "print('Nano')"
            };

        public string ArchivePath => "Test.nn";

        public IReadOnlyList<VirtualAsset> GetChildren(string directory)
        {
            if (directory == "scripts")
            {
                return [new VirtualAsset("scripts/main.lua", "main.lua", false)];
            }

            return [new VirtualAsset("scripts", "scripts", true)];
        }

        public void CreateDirectory(string parentDirectory, string name)
        {
        }

        public string ReadText(string path) => _files[path];

        public void WriteText(string path, string text) => _files[path] = text;

        public bool IsTextFile(string path) =>
            Path.GetExtension(path).Equals(
                ".lua",
                StringComparison.OrdinalIgnoreCase);
    }
}
