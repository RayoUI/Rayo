using Nano.ViewModels;
using Nano.Views.ProjectAssetStore;
using Nano.Views.ProjectAssetStore.Components;
using Nano.Views.SpriteEditor;
using Rayo.Rendering;
using Xunit;

namespace Nano.Tests;

public sealed class ViewModelTests
{
    [Fact]
    public void Sprite_asset_document_round_trips_canvas_frames_and_animations()
    {
        var document = SpriteAssetDocument.CreateBlank(24, 12);
        document.Frames.Add(SpriteFrameDocument.FromFrame(document.Frames[0].ToFrame(24, 12)));
        document.Animations[0].FrameIndices = [0, 1];

        var restored = SpriteAssetDocument.Deserialize(document.Serialize());
        restored.Validate();

        Assert.Equal(24, restored.Width);
        Assert.Equal(12, restored.Height);
        Assert.Equal(2, restored.Frames.Count);
        Assert.Equal([0, 1], restored.Animations[0].FrameIndices);
        Assert.Equal(24 * 12 * 4, restored.Frames[0].Pixels.Length);
        Assert.All(
            restored.Frames[0].ToFrame(24, 12).Pixels.Cast<Color>(),
            color => Assert.Equal(0f, color.A));
    }

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

        public VirtualAsset CreateSprite(string parentDirectory, string name)
        {
            var fileName = name.EndsWith(".sprite", StringComparison.OrdinalIgnoreCase)
                ? name
                : $"{name}.sprite";
            var path = string.IsNullOrEmpty(parentDirectory)
                ? fileName
                : $"{parentDirectory}/{fileName}";
            _files[path] = string.Empty;
            return new VirtualAsset(path, fileName, false);
        }

        public string ReadText(string path) => _files[path];

        public void WriteText(string path, string text) => _files[path] = text;

        public bool IsTextFile(string path) =>
            Path.GetExtension(path).Equals(
                ".lua",
                StringComparison.OrdinalIgnoreCase);

        public bool IsSpriteFile(string path) =>
            path.EndsWith(".sprite", StringComparison.OrdinalIgnoreCase);
    }
}
