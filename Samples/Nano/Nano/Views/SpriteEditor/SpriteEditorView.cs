using Nano.Views.SpriteEditor.Components;
using Nano.ViewModels;
using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.SpriteEditor;

public sealed class SpriteEditorView : ViewBase<SpriteEditorViewModel>
{
    private readonly SpriteCanvas _canvas = new();
    private readonly SpritePalette _palette;
    private SpriteFrameViewer? _frameViewer;
    private readonly Action<string>? _save;

    public SpriteEditorView()
        : this(new SpriteEditorViewModel(), null)
    {
    }

    public SpriteEditorView(SpriteAssetDocument document, Action<string> save)
        : this(new SpriteEditorViewModel(document), save)
    {
    }

    private SpriteEditorView(SpriteEditorViewModel viewModel, Action<string>? save)
    {
        _save = save;
        SetViewModel(viewModel);
        _palette = new SpritePalette(ViewModel.Palette);
        _frameViewer = new SpriteFrameViewer(
            ViewModel.Frames,
            () => ViewModel.SelectedFrameIndex.Value,
            SelectFrame,
            AddFrame,
            CloneFrame,
            DeleteFrame);

        _canvas.Frame = ViewModel.CurrentFrame;
        _canvas.FrameChanged += _frameViewer.RefreshPreviews;
        _canvas.EditCommitted += () =>
        {
            ViewModel.RecordCurrentFrameState();
            Save();
        };
        _canvas.ColorPicked += _palette.SelectColor;
        _palette.ColorSelected += color => _canvas.SelectedColor = color;
    }

    public override VisualElement Build()
    {
        var canvasLayer = new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Star);
        canvasLayer.AddChild(
            new Frame()
                .Background(new Color(38, 48, 64))
                .Content(_canvas),
            0,
            0);

        var frameViewer = _frameViewer!.Build();
        frameViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
        frameViewer.VerticalAlignment = VerticalAlignment.Top;
        canvasLayer.AddChild(frameViewer, 0, 0);

        return new Grid()
            .Rows(GridLength.Star, GridLength.Auto, GridLength.Auto)
            .Columns(GridLength.Star)
            .AddChild(canvasLayer, 0, 0)
            .AddChild(_palette.Build(), 1, 0)
            .AddChild(
                new SpriteToolPicker(_canvas, Undo, Redo).Build(),
                2,
                0);
    }

    private void AddFrame()
    {
        ViewModel.AddFrame();
        _frameViewer!.RefreshFrames();
        _frameViewer.ScrollToEnd();
        ApplySelectedFrame();
        Save();
    }

    private void CloneFrame(int index)
    {
        ViewModel.CloneFrame(index);
        _frameViewer!.RefreshFrames();
        ApplySelectedFrame();
        Save();
    }

    private void DeleteFrame(int index)
    {
        ViewModel.DeleteFrame(index);
        _frameViewer!.RefreshFrames();
        ApplySelectedFrame();
        Save();
    }

    private void SelectFrame(int index)
    {
        ViewModel.SelectFrame(index);
        ApplySelectedFrame();
    }

    private void Undo()
    {
        if (ViewModel.Undo())
        {
            _canvas.Frame = ViewModel.CurrentFrame;
        }
    }

    private void Redo()
    {
        if (ViewModel.Redo())
        {
            _canvas.Frame = ViewModel.CurrentFrame;
        }
    }

    private void ApplySelectedFrame()
    {
        _canvas.Frame = ViewModel.CurrentFrame;
        _frameViewer!.RefreshSelection();
        _frameViewer.RefreshPreviews();
    }

    private void Save() => _save?.Invoke(ViewModel.ToDocument().Serialize());
}
