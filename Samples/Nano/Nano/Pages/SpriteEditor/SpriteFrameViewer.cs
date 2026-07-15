using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Pages.SpriteEditor;

public sealed class SpriteFrameViewer : Component
{
    private const float MenuWidth = 174f;
    private const float MenuHeight = 52f;
    private readonly IReadOnlyList<SpriteFrame> _frames;
    private readonly Func<int> _selectedIndex;
    private readonly Action<int> _selectFrame;
    private readonly Action _addFrame;
    private readonly Action<int> _cloneFrame;
    private readonly Action<int> _deleteFrame;
    private readonly HStack _items = new();
    private readonly List<SpriteFramePreview> _previews = [];
    private bool _isOpen;
    private VisualElement? _content;
    private ButtonIcon? _handle;
    private FrameOptionsPopup? _optionsMenu;

    public SpriteFrameViewer(IReadOnlyList<SpriteFrame> frames, Func<int> selectedIndex, Action<int> selectFrame, Action addFrame, Action<int> cloneFrame, Action<int> deleteFrame)
    {
        _frames = frames;
        _selectedIndex = selectedIndex;
        _selectFrame = selectFrame;
        _addFrame = addFrame;
        _cloneFrame = cloneFrame;
        _deleteFrame = deleteFrame;
    }

    public override VisualElement Build()
    {
        _content = new ScrollView().Orientation(ScrollOrientation.Horizontal).Height(82).Content(_items);
        _content.IsVisible = _isOpen;
        _handle = new ButtonIcon(_isOpen ? Icons.ChevronDown : Icons.ChevronUp)
            .Size(36)
            .Variant(ButtonVariant.Secondary)
            .OnTapped(Toggle);
        RefreshFrames();
        return new VStack().Spacing(0).Children(
            _content,
            new HStack().HorizontalAlignment(HorizontalAlignment.Right).Padding(new Thickness(0, 0, 8, 0)).Children(_handle));
    }

    public void RefreshFrames()
    {
        _items.Spacing(10).Padding(new Thickness(4));
        _items.ClearChildren();
        _previews.Clear();
        for (var index = 0; index < _frames.Count; index++)
        {
            var frameIndex = index;
            var preview = new SpriteFramePreview(_frames[index], index + 1, index == _selectedIndex())
            {
                Selected = () => _selectFrame(frameIndex)
            };
            preview.OptionsRequested = () => ShowOptions(frameIndex, preview);
            _previews.Add(preview);
            _items.AddChild(preview);
        }
        _items.AddChild(new ButtonIcon(Icons.Add).Size(76).Variant(ButtonVariant.Secondary).OnTapped(_addFrame));
    }

    public void RefreshSelection()
    {
        for (var index = 0; index < _previews.Count; index++)
            _previews[index].SetSelected(index == _selectedIndex());
    }

    public void RefreshPreviews()
    {
        foreach (var preview in _previews)
            preview.Refresh();
    }

    private void Toggle()
    {
        _isOpen = !_isOpen;
        if (_content is not null) _content.IsVisible = _isOpen;
        if (_handle is not null) _handle.IconData = _isOpen ? Icons.ChevronDown : Icons.ChevronUp;
    }

    private void ShowOptions(int index, SpriteFramePreview source)
    {
        CloseOptions();
        VisualElement root = source;
        while (root.Parent is not null) root = root.Parent;
        float margin = 4f;
        float left = root.ComputedX, top = root.ComputedY;
        float right = left + root.ComputedWidth, bottom = top + root.ComputedHeight;
        float x = right - (source.ComputedX + source.ComputedWidth) >= source.ComputedX - left
            ? source.ComputedX : source.ComputedX + source.ComputedWidth - MenuWidth;
        float y = bottom - (source.ComputedY + source.ComputedHeight) >= source.ComputedY - top
            ? source.ComputedY + source.ComputedHeight + margin : source.ComputedY - MenuHeight - margin;
        x = Math.Clamp(x, left + margin, MathF.Max(left + margin, right - MenuWidth - margin));
        y = Math.Clamp(y, top + margin, MathF.Max(top + margin, bottom - MenuHeight - margin));

        var menu = new FrameOptionsPopup(CloseOptions)
        {
            X = x, Y = y, Width = MenuWidth,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Background = new Color(38, 48, 64), BorderBrush = new Color(115, 130, 150),
            BorderThickness = 1, BorderRadius = 6, Padding = new Thickness(6)
        };
        menu.Content = new HStack().Spacing(6).Children(
            new Button().Text("Duplicar").Height(36).Variant(ButtonVariant.Secondary).OnTapped(() => { CloseOptions(); _cloneFrame(index); }),
            new Button().Text("Eliminar").Height(36).Variant(ButtonVariant.Danger).OnTapped(() => { CloseOptions(); ConfirmDelete(index); }));
        _optionsMenu = menu;
        OverlayManager.AddOverlay(menu, _content);
        OverlayManager.EventManager?.RegisterGlobalPointerHandler(menu);
    }

    private void ConfirmDelete(int index) => Dialog.Show("Eliminar frame", $"¿Quieres eliminar el frame {index + 1}? Esta acción no se puede deshacer.", true, () => _deleteFrame(index), okText: "Eliminar", cancelText: "Cancelar");

    private void CloseOptions()
    {
        if (_optionsMenu is null) return;
        OverlayManager.RemoveOverlay(_optionsMenu);
        OverlayManager.EventManager?.UnregisterGlobalPointerHandler(_optionsMenu);
        _optionsMenu = null;
    }

    private sealed class FrameOptionsPopup(Action onOutsideTap) : Frame, IGlobalPointerHandler
    {
        public bool HandleGlobalPointer(System.Numerics.Vector2 position, VisualElement? hitElement)
        {
            if (ContainsWindowPoint(position)) return true;
            onOutsideTap();
            return false;
        }
    }
}
