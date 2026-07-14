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
    private readonly HStack _frameList = new();
    private readonly List<SpriteFramePreview> _framePreviews = [];
    private int _selectedFrameIndex;

    public SpriteEditorPage()
    {
        _canvas.Frame = _frames[0];
        _canvas.FrameChanged += RefreshFramePreviews;
    }

    public override VisualElement Build()
    {
        return new Grid()
            .Rows(GridLength.Star, GridLength.Auto, GridLength.Auto)
            .Columns(GridLength.Star)
            .AddChild(
                new ScrollView()
                    .Content(
                        new VStack()
                            .Spacing(16)
                            .Padding(new Thickness(20))
                            .Children(
                                CreateFrameViewer(),
                                new Frame()
                                    .Height(360)
                                    .Background(new Color(38, 48, 64))
                                    .BorderRadius(12)
                                    .Content(_canvas)
                            )),
                0,
                0)
            .AddChild(CreatePalette(), 1, 0)
            .AddChild(CreateToolbar(), 2, 0);
    }

    private VisualElement CreatePalette()
    {
        var palette = new HStack().Spacing(10).Padding(new Thickness(12, 8));
        foreach (var color in new[]
                 {
                     new Color(62, 126, 214), new Color(34, 150, 94), new Color(225, 142, 38),
                     new Color(215, 72, 72), new Color(137, 87, 229), new Color(35, 39, 47)
                 })
        {
            palette.AddChild(new Button()
                .Size(40)
                .Background(color)
                .OnTapped(() => _canvas.SelectedColor = color));
        }

        return palette;
    }

    private VisualElement CreateFrameViewer()
    {
        _frameList.Spacing(10).Padding(new Thickness(4));
        RebuildFrameViewer();

        return new VStack()
            .Spacing(4)
            .Children(
                new ScrollView
                {
                    Orientation = ScrollOrientation.Horizontal,
                    ShowHorizontalScrollbar = false
                }.Height(82).Content(_frameList),
                new HStack()
                    .Spacing(6)
                    .Children(
                        new ButtonIcon(Icons.Add).Size(36).Variant(ButtonVariant.Secondary).OnTapped(AddFrame),
                        new ButtonIcon(Icons.Edit).Size(36).Variant(ButtonVariant.Secondary).OnTapped(CloneFrame),
                        new ButtonIcon(Icons.Delete).Size(36).Variant(ButtonVariant.Danger).OnTapped(DeleteFrame)
                    )
            );
    }

    private void AddFrame()
    {
        _frames.Add(new SpriteFrame());
        SelectFrame(_frames.Count - 1);
    }

    private void CloneFrame()
    {
        _frames.Insert(_selectedFrameIndex + 1, _frames[_selectedFrameIndex].Clone());
        SelectFrame(_selectedFrameIndex + 1);
    }

    private void DeleteFrame()
    {
        if (_frames.Count == 1)
        {
            _canvas.Clear();
            return;
        }

        _frames.RemoveAt(_selectedFrameIndex);
        SelectFrame(Math.Min(_selectedFrameIndex, _frames.Count - 1));
    }

    private void SelectFrame(int index)
    {
        _selectedFrameIndex = index;
        _canvas.Frame = _frames[index];
        RebuildFrameViewer();
    }

    private void RebuildFrameViewer()
    {
        _frameList.ClearChildren();
        _framePreviews.Clear();
        for (var index = 0; index < _frames.Count; index++)
        {
            var currentIndex = index;
            var preview = new SpriteFramePreview(_frames[index], index + 1, index == _selectedFrameIndex)
            {
                Selected = () => SelectFrame(currentIndex)
            };
            _framePreviews.Add(preview);
            _frameList.AddChild(preview);
        }
    }

    private void RefreshFramePreviews()
    {
        foreach (var preview in _framePreviews)
        {
            preview.Refresh();
        }
    }

    private VisualElement CreateToolbar()
    {
        return new Frame()
            .Id("Toolbar")
            .Background(new Color(230, 235, 242))
            .Padding(new Thickness(8))
            .Content(
                new HStack()
                    .Spacing(4)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        new ButtonIcon(Icons.BrushTool).Size(44).Variant(ButtonVariant.Secondary)
                            .OnTapped(() => _canvas.Tool = SpriteTool.Pencil),
                        new ButtonIcon(Icons.Eraser).Size(44).Variant(ButtonVariant.Secondary)
                            .OnTapped(() => _canvas.Tool = SpriteTool.Eraser),
                        new ButtonIcon(Icons.FillBucket).Size(44).Variant(ButtonVariant.Secondary)
                            .OnTapped(() => _canvas.Tool = SpriteTool.Fill),
                        new ButtonIcon(Icons.LineTool).Size(44).Variant(ButtonVariant.Secondary)
                            .OnTapped(() => _canvas.Tool = SpriteTool.Line),
                        new ButtonIcon(Icons.RectangleTool).Size(44).Variant(ButtonVariant.Secondary)
                            .OnTapped(() => _canvas.Tool = SpriteTool.Rectangle),
                        new ButtonIcon(Icons.EllipseTool).Size(44).Variant(ButtonVariant.Secondary)
                            .OnTapped(() => _canvas.Tool = SpriteTool.Ellipse),
                        new ButtonIcon(Icons.Delete).Size(44).Variant(ButtonVariant.Danger)
                            .OnTapped(_canvas.Clear)
                    ));
    }
}
