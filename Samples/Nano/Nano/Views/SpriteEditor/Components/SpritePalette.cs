using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Input.Gestures;
using Rayo.Layout;
using Rayo.Rendering;
using IRenderer = Rayo.Rendering.IRenderer;

namespace Nano.Views.SpriteEditor.Components;

public sealed class SpritePalette : Component
{
    private readonly List<Color> _colors;
    private readonly HStack _items = new();
    private Color _selectedColor = new(62, 126, 214);
    private ScrollView? _scrollView;

    public event Action<Color>? ColorSelected;

    public SpritePalette(IEnumerable<Color>? colors = null)
    {
        _colors = colors?.ToList() ??
        [
            new(62, 126, 214), new(34, 150, 94), new(225, 142, 38),
            new(215, 72, 72), new(137, 87, 229), new(35, 39, 47)
        ];
        _selectedColor = _colors[0];
    }

    public override VisualElement Build()
    {
        _items.Spacing(10).Padding(new Thickness(12, 8));
        RebuildItems();
        return (_scrollView = new ScrollView())
            .Orientation(ScrollOrientation.Horizontal)
            .Height(56)
            .Content(_items);
    }

    public void SelectColor(Color color)
    {
        var colorAdded = !_colors.Contains(color);
        if (colorAdded)
            _colors.Add(color);

        _selectedColor = color;
        RebuildItems();
        if (colorAdded)
            ScrollToEnd();

        ColorSelected?.Invoke(color);
    }

    private void ScrollToEnd()
    {
        if (_scrollView is null || _scrollView.ComputedWidth <= 0 || _scrollView.ComputedHeight <= 0)
        {
            return;
        }

        _scrollView.MeasureUpdate(_scrollView.ComputedWidth, _scrollView.ComputedHeight);
        _scrollView.EnsureRectVisible(_scrollView.ContentWidth, 0, 0, 0);
    }

    private void RebuildItems()
    {
        _items.ClearChildren();
        foreach (var color in _colors)
            _items.AddChild(new PaletteColorSwatch(color, color.Equals(_selectedColor), () => SelectColor(color)));

        _items.AddChild(new ButtonIcon(Icons.Add).Size(40).Variant(ButtonVariant.Secondary).OnTapped(OpenColorPicker));
    }

    private void OpenColorPicker() => ColorPicker.ShowDialog(_selectedColor, SelectColor);

    private sealed class PaletteColorSwatch : View<PaletteColorSwatch>, IPointerHandler, IGestureRecognizerHost
    {
        private readonly Color _color;
        private readonly bool _isSelected;

        public List<IGestureRecognizer> GestureRecognizers { get; } = [];

        public PaletteColorSwatch(Color color, bool isSelected, Action onSelected)
        {
            _color = color;
            _isSelected = isSelected;
            Width = Height = 40;
            var tap = new TapRecognizer(15f, 500, 300);
            tap.TapDetected += _ => onSelected();
            GestureRecognizers.Add(tap);
        }

        protected override void Measure(float availableWidth, float availableHeight) => (DesiredWidth, DesiredHeight) = (40, 40);

        public override void Render(IRenderer renderer)
        {
            renderer.DrawRect(ComputedX, ComputedY, ComputedWidth, ComputedHeight, _color);
            renderer.DrawRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, _isSelected ? 5f : 1f,
                _isSelected ? new Color(25, 32, 42) : new Color(115, 130, 150));
            if (_isSelected)
                renderer.DrawRectOutline(ComputedX, ComputedY, ComputedWidth, ComputedHeight, 2f, Color.White);
        }

        public void OnPointerPressed(PointerEventArgs e) { }
        public void OnPointerMoved(PointerEventArgs e) { }
        public void OnPointerReleased(PointerEventArgs e) { }
        public void OnPointerEntered(PointerEventArgs e) { }
        public void OnPointerExited(PointerEventArgs e) { }
    }
}
