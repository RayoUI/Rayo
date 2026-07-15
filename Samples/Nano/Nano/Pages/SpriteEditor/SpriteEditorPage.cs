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
    private readonly Dictionary<SpriteFrame, FrameHistory> _history = [];
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
        _canvas.EditCommitted += RecordCurrentFrameState;
        _canvas.ColorPicked += _palette.SelectColor;
        _palette.ColorSelected += color => _canvas.SelectedColor = color;

        foreach (var frame in _frames)
        {
            _history.Add(frame, new FrameHistory(frame));
        }
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
            .AddChild(new SpriteToolPicker(_canvas, Undo, Redo).Build(), 2, 0);
    }

    private void AddFrame()
    {
        var frame = new SpriteFrame();
        _frames.Add(frame);
        _history.Add(frame, new FrameHistory(frame));
        _frameViewer.RefreshFrames();
        _frameViewer.ScrollToEnd();
        SelectFrame(_frames.Count - 1);
    }

    private void CloneFrame(int index)
    {
        var frame = _frames[index].Clone();
        _frames.Insert(index + 1, frame);
        _history.Add(frame, new FrameHistory(frame));
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
        _history.Remove(_canvas.Frame);
        _frameViewer.RefreshFrames();
        SelectFrame(Math.Min(index, _frames.Count - 1));
    }

    private void SelectFrame(int index)
    {
        _selectedFrameIndex = index;
        _canvas.Frame = _frames[index];
        _frameViewer.RefreshSelection();
    }

    private void RecordCurrentFrameState()
    {
        _history[_canvas.Frame].Record(_canvas.Frame);
    }

    private void Undo()
    {
        if (_history[_canvas.Frame].Undo(_canvas.Frame))
        {
            _canvas.Frame = _canvas.Frame;
        }
    }

    private void Redo()
    {
        if (_history[_canvas.Frame].Redo(_canvas.Frame))
        {
            _canvas.Frame = _canvas.Frame;
        }
    }

    private sealed class FrameHistory
    {
        private readonly List<Color[,]> _states = [];
        private int _position;

        public FrameHistory(SpriteFrame frame)
        {
            _states.Add(ClonePixels(frame));
        }

        public void Record(SpriteFrame frame)
        {
            if (_position < _states.Count - 1)
            {
                _states.RemoveRange(_position + 1, _states.Count - _position - 1);
            }

            _states.Add(ClonePixels(frame));
            _position++;
        }

        public bool Undo(SpriteFrame frame) => Restore(frame, _position - 1);

        public bool Redo(SpriteFrame frame) => Restore(frame, _position + 1);

        private bool Restore(SpriteFrame frame, int position)
        {
            if (position < 0 || position >= _states.Count)
            {
                return false;
            }

            Array.Copy(_states[position], frame.Pixels, frame.Pixels.Length);
            _position = position;
            return true;
        }

        private static Color[,] ClonePixels(SpriteFrame frame) => (Color[,])frame.Pixels.Clone();
    }
}
