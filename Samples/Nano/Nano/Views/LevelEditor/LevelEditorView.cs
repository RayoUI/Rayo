using Nano.ViewModels;
using Nano.Views.LevelEditor.Components;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.LevelEditor;

public sealed class LevelEditorView : ViewBase<LevelEditorViewModel>
{
    private readonly LevelCanvas _canvas;

    public LevelEditorView()
    {
        SetViewModel(new LevelEditorViewModel());
        _canvas = new LevelCanvas(ViewModel);
    }

    public override VisualElement Build() =>
        new Grid()
            .Rows(GridLength.Star, GridLength.Pixels(132))
            .Columns(GridLength.Star)
            .Background(new Color(12, 16, 24))
            .AddChild(_canvas, 0, 0)
            .AddChild(new LevelAssetPalette(ViewModel), 1, 0);
}
