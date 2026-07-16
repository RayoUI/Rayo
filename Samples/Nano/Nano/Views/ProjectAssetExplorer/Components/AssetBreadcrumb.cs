using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Layout;
using Rayo.Rendering;

namespace Nano.Views.ProjectAssetStore.Components;

internal sealed class AssetBreadcrumb : Component
{
    private const float MaximumSegmentWidth = 112;
    private readonly string _currentDirectory;
    private readonly Action<string> _navigateTo;

    public AssetBreadcrumb(string currentDirectory, Action<string> navigateTo)
    {
        _currentDirectory = currentDirectory;
        _navigateTo = navigateTo;
    }

    public override VisualElement Build()
    {
        var segments = new List<VisualElement>();
        var currentPath = string.Empty;
        foreach (var segment in _currentDirectory.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
            var targetPath = currentPath;
            segments.Add(CreateSeparator());
            segments.Add(new BreadcrumbSegment(
                segment,
                MaximumSegmentWidth,
                () => _navigateTo(targetPath)));
        }

        return new HStack()
            .Spacing(2)
            .Height(34)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .Children(
                new ButtonIcon(Icons.Home)
                    .Size(34)
                    .IconSize(18)
                    .IconColor(Color.White)
                    .Variant(ButtonVariant.Ghost)
                    .OnTapped(() => _navigateTo(string.Empty)),
                new ScrollView
                {
                    Orientation = ScrollOrientation.Horizontal,
                    ShowHorizontalScrollbar = false,
                    ShowVerticalScrollbar = false
                }
                .Height(34)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Center)
                .Content(
                    new HStack()
                        .Spacing(2)
                        .Height(34)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Children(segments.ToArray())));
    }

    private static Label CreateSeparator() =>
        new Label("/")
            .FontSize(13)
            .Foreground(new Color(100, 116, 139))
            .VerticalAlignment(VerticalAlignment.Center);

    private sealed class BreadcrumbSegment : Frame, IPointerHandler
    {
        private static readonly Color HoverBackground = new(45, 55, 72);
        private readonly Action _onTapped;
        private bool _isTapPending;

        public BreadcrumbSegment(string text, float maximumWidth, Action onTapped)
        {
            _onTapped = onTapped;
            Height = 28;
            MaxWidth = maximumWidth;
            Padding = new Thickness(8, 0);
            Background = Color.Transparent;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Center;
            Content = new Label()
                .Text(text)
                .FontSize(14)
                .Foreground(new Color(191, 219, 254))
                .TextTrimming(TextTrimming.CharacterEllipsis)
                .TextHorizontalAlignment(HorizontalAlignment.Left)
                .TextVerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch);
        }

        public void OnPointerEntered(PointerEventArgs args)
        {
            if (args.PointerType == PointerType.Mouse)
            {
                Background = HoverBackground;
            }
        }

        public void OnPointerExited(PointerEventArgs args)
        {
            if (args.PointerType == PointerType.Mouse)
            {
                Background = Color.Transparent;
            }
        }

        public void OnPointerPressed(PointerEventArgs args)
        {
            _isTapPending = args.Button == 0;
        }

        public void OnPointerReleased(PointerEventArgs args)
        {
            if (!_isTapPending)
            {
                return;
            }

            _isTapPending = false;
            _onTapped();
        }

        public void OnPointerCanceled(PointerEventArgs args)
        {
            _isTapPending = false;
            Background = Color.Transparent;
        }
    }
}
