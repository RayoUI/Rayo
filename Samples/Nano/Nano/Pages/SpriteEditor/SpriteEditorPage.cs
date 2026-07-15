using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages.SpriteEditor;

public sealed class SpriteEditorPage : Component
{
    private readonly SpriteCanvas _canvas = new();
    private readonly List<SpriteFrame> _frames = [new(), new(), new()];
    private readonly SpritePalette _palette = new();
    private readonly SpriteFrameViewer _frameViewer;
    private int _selectedFrameIndex;

    public SpriteEditorPage()
    {
        _frameViewer = new SpriteFrameViewer(
            _frames,
            () => _selectedFrameIndex,
            SelectFrame,
            AddFrame,
            CloneFrame,
            DeleteFrame);

        _canvas.Frame = _frames[0];
        _canvas.FrameChanged += _frameViewer.RefreshPreviews;
        _canvas.ColorPicked += _palette.SelectColor;
        _palette.ColorSelected += color => _canvas.SelectedColor = color;
    }

    public override VisualElement Build()
    {
        var canvasLayer = new Grid().Rows(GridLength.Star).Columns(GridLength.Star);
        canvasLayer.AddChild(
            new Frame()
                .Background(new Color(38, 48, 64))
                .Content(_canvas),
            0,
            0);

        var frameViewer = _frameViewer.Build();
        frameViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
        frameViewer.VerticalAlignment = VerticalAlignment.Top;
        canvasLayer.AddChild(frameViewer, 0, 0);

        return new Grid()
            .Rows(GridLength.Star, GridLength.Auto, GridLength.Auto)
            .Columns(GridLength.Star)
            .AddChild(canvasLayer, 0, 0)
            .AddChild(_palette.Build(), 1, 0)
            .AddChild(new SpriteToolPicker(_canvas).Build(), 2, 0);
    }

    private void AddFrame()
    {
        _frames.Add(new SpriteFrame());
        _frameViewer.RefreshFrames();
        SelectFrame(_frames.Count - 1);
    }

    private void CloneFrame(int index)
    {
        _frames.Insert(index + 1, _frames[index].Clone());
        _frameViewer.RefreshFrames();
        SelectFrame(index + 1);
    }

    private void DeleteFrame(int index)
    {
        if (_frames.Count == 1)
        {
            _canvas.Clear();
            return;
        }

        _frames.RemoveAt(index);
        _frameViewer.RefreshFrames();
        SelectFrame(Math.Min(index, _frames.Count - 1));
    }

    private void SelectFrame(int index)
    {
        _selectedFrameIndex = index;
        _canvas.Frame = _frames[index];
        _frameViewer.RefreshSelection();
    }
}
