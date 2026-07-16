using Nano.Views.SpriteEditor.Components;
using Nano.Views.SpriteEditor;
using Rayo.Core;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace Nano.ViewModels;

public sealed class SpriteEditorViewModel : ViewModelBase
{
    private readonly Dictionary<SpriteFrame, FrameHistory> _history = [];

    public SpriteEditorViewModel()
        : this(SpriteAssetDocument.CreateBlank(16, 16))
    {
    }

    public SpriteEditorViewModel(SpriteAssetDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Validate();
        Width = document.Width;
        Height = document.Height;
        Palette = document.Palette.Select(color => color.ToColor()).ToList();
        Animations = document.Animations
            .Select(animation => new SpriteAnimationDocument
            {
                Name = animation.Name,
                Loop = animation.Loop,
                Speed = animation.Speed,
                FrameIndices = [.. animation.FrameIndices]
            })
            .ToList();
        Frames = document.Frames.Select(frame => frame.ToFrame(Width, Height)).ToList();
        SelectedFrameIndex = UseSignal(0);
        foreach (var frame in Frames)
        {
            _history.Add(frame, new FrameHistory(frame));
        }
    }

    public List<SpriteFrame> Frames { get; }

    public int Width { get; }
    public int Height { get; }
    public List<Color> Palette { get; }
    public List<SpriteAnimationDocument> Animations { get; }

    public Signal<int> SelectedFrameIndex { get; }

    public SpriteFrame CurrentFrame => Frames[SelectedFrameIndex.Value];

    public int AddFrame()
    {
        var frame = new SpriteFrame(Width, Height);
        Frames.Add(frame);
        _history.Add(frame, new FrameHistory(frame));
        SelectedFrameIndex.Value = Frames.Count - 1;
        return SelectedFrameIndex.Value;
    }

    public int CloneFrame(int index)
    {
        var frame = Frames[index].Clone();
        Frames.Insert(index + 1, frame);
        _history.Add(frame, new FrameHistory(frame));
        SelectedFrameIndex.Value = index + 1;
        return SelectedFrameIndex.Value;
    }

    public int DeleteFrame(int index)
    {
        if (Frames.Count == 1)
        {
            var blank = new SpriteFrame(Width, Height);
            Array.Copy(blank.Pixels, Frames[0].Pixels, blank.Pixels.Length);
            _history[Frames[0]] = new FrameHistory(Frames[0]);
            SelectedFrameIndex.Value = 0;
            return 0;
        }

        var removed = Frames[index];
        Frames.RemoveAt(index);
        _history.Remove(removed);
        SelectedFrameIndex.Value = Math.Min(index, Frames.Count - 1);
        return SelectedFrameIndex.Value;
    }

    public void SelectFrame(int index)
    {
        if (index < 0 || index >= Frames.Count)
            return;

        SelectedFrameIndex.Value = index;
    }

    public void RecordCurrentFrameState() =>
        _history[CurrentFrame].Record(CurrentFrame);

    public bool Undo() => _history[CurrentFrame].Undo(CurrentFrame);

    public bool Redo() => _history[CurrentFrame].Redo(CurrentFrame);

    public SpriteAssetDocument ToDocument() => new()
    {
        Width = Width,
        Height = Height,
        Palette = Palette.Select(SpriteColor.FromColor).ToList(),
        Frames = Frames.Select(SpriteFrameDocument.FromFrame).ToList(),
        Animations = Animations
            .Select(animation => new SpriteAnimationDocument
            {
                Name = animation.Name,
                Loop = animation.Loop,
                Speed = animation.Speed,
                FrameIndices = [.. animation.FrameIndices]
            })
            .ToList()
    };

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
                _states.RemoveRange(
                    _position + 1,
                    _states.Count - _position - 1);
            }

            _states.Add(ClonePixels(frame));
            _position++;
        }

        public bool Undo(SpriteFrame frame) => Restore(frame, _position - 1);

        public bool Redo(SpriteFrame frame) => Restore(frame, _position + 1);

        private bool Restore(SpriteFrame frame, int position)
        {
            if (position < 0 || position >= _states.Count)
                return false;

            Array.Copy(_states[position], frame.Pixels, frame.Pixels.Length);
            _position = position;
            return true;
        }

        private static Color[,] ClonePixels(SpriteFrame frame) =>
            (Color[,])frame.Pixels.Clone();
    }
}
