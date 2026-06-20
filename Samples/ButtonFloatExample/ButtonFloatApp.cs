using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace ButtonFloatExample;

public class ButtonFloatApp : UserControl
{
    private readonly SignalList<string> _items;
    private readonly Signal<int> _createdCount;
    private VStack _list = null!;

    public ButtonFloatApp()
    {
        _items = UseSignalList<string>();
        _createdCount = UseSignal(4);

        _items.Add("Review invoice approval");
        _items.Add("Send product screenshots");
        _items.Add("Prepare release checklist");
        _items.Add("Follow up with design");
    }

    protected override void OnInit()
    {
        UseSubscription(_items, () => UIUpdateQueue.EnqueueUIUpdate(RebuildList));
    }

    public override VisualElement Build()
    {
        var header = new Frame()
            .Background(new Color(24, 31, 42))
            .Padding(new Thickness(20, 18))
            .VerticalAlignment(VerticalAlignment.Top)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new VStack()
                    .Spacing(6)
                    .Children(
                        new Label("ButtonFloat")
                            .FontSize(28)
                            .Foreground(Color.White),
                        new Label("Tap the floating button to add a new item")
                            .FontSize(13)
                            .Foreground(new Color(148, 163, 184))
                    )
            );

        _list = new VStack()
            .Spacing(12)
            .Padding(new Thickness(0, 0, 0, 16))
            .VerticalAlignment(VerticalAlignment.Top);

        RebuildList();

        return new Grid()
            .Background(new Color(15, 23, 42))
            .Padding(new Thickness(16))
            .Rows(GridLength.Auto, GridLength.Star)
            .Columns(GridLength.Star)
            .AddChild(header, 0, 0)
            .AddChild(
                new ScrollView()
                    .Background(new Color(15, 23, 42))
                    .Content(_list),
                1,
                0)
            .AddChild(
                new ButtonFloat(Icons.Add)
                    .Dock(ButtonFloatPlacement.BottomRight, 18)
                    .OnTapped(AddItem),
                1,
                0);
    }

    private void AddItem()
    {
        _createdCount.Value++;
        _items.Insert(0, $"New floating item {_createdCount.Value}");
    }

    private void RebuildList()
    {
        if (_list is null)
            return;

        _list.ClearChildren();

        foreach (var item in _items)
        {
            _list.AddChild(CreateItemCard(item));
        }
    }

    private VisualElement CreateItemCard(string title)
    {
        return new Frame()
            .Background(new Color(30, 41, 59))
            .BorderRadius(10)
            .Padding(new Thickness(14))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .Spacing(12)
                    .Children(
                        new Frame()
                            .Size(36)
                            .BorderRadius(18)
                            .Background(new Color(59, 130, 246, 0.25f))
                            .Content(
                                new Icon(Icons.Check)
                                    .Color(new Color(96, 165, 250))
                                    .Size(18)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)
                            ),
                        new VStack()
                            .Spacing(3)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Children(
                                new Label(title)
                                    .FontSize(15)
                                    .Foreground(new Color(226, 232, 240)),
                                new Label("Created from the sample app")
                                    .FontSize(12)
                                    .Foreground(new Color(148, 163, 184))
                            )
                    )
            );
    }
}
