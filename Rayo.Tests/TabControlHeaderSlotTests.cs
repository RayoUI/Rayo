using Rayo.Controls;
using Rayo.Core;
using Rayo.Rendering;
using System.Numerics;

namespace Rayo.Tests;

public sealed class TabControlHeaderSlotTests
{
    [Fact]
    public void Horizontal_header_keeps_start_and_end_slots_fixed()
    {
        var start = new ProbeElement(36, 24);
        var end = new ProbeElement(44, 24);
        var tabControl = new TabControl
        {
            Width = 600,
            Height = 300
        }
            .HeaderStart(() => start)
            .HeaderEnd(() => end)
            .AddTab("One", new ProbeElement(100, 100))
            .AddTab("Two", new ProbeElement(100, 100));

        Layout(tabControl, 600, 300);

        Assert.InRange(start.ComputedX, 0, 0.1f);
        Assert.InRange(start.ComputedWidth, 35.9f, 36.1f);
        Assert.InRange(end.ComputedX, 555.9f, 556.1f);
        Assert.InRange(end.ComputedWidth, 43.9f, 44.1f);
    }

    [Fact]
    public void Vertical_header_places_start_above_and_end_below_tabs()
    {
        var start = new ProbeElement(24, 36);
        var end = new ProbeElement(24, 44);
        var tabControl = new TabControl
        {
            Width = 500,
            Height = 400,
            Position = TabPosition.Left
        }
            .HeaderStart(() => start)
            .HeaderEnd(() => end)
            .AddTab("One", new ProbeElement(100, 100));

        Layout(tabControl, 500, 400);

        Assert.InRange(start.ComputedY, 0, 0.1f);
        Assert.InRange(end.ComputedY, 355.9f, 356.1f);
    }

    [Fact]
    public void Rebuilding_layout_can_reuse_slot_and_selected_content_instances()
    {
        var start = new ProbeElement(36, 24);
        var end = new ProbeElement(44, 24);
        var content = new ProbeElement(100, 100);
        var tabControl = new TabControl
        {
            Width = 600,
            Height = 300
        }
            .HeaderStart(() => start)
            .HeaderEnd(() => end)
            .AddTab("One", content);

        Layout(tabControl, 600, 300);

        tabControl.Position = TabPosition.Bottom;
        Layout(tabControl, 600, 300);

        Assert.NotNull(start.Parent);
        Assert.NotNull(end.Parent);
        Assert.NotNull(content.Parent);
    }

    [Fact]
    public void Hidden_horizontal_scrollbar_still_allows_content_drag()
    {
        var scroll = new ScrollView
        {
            Width = 100,
            Height = 40,
            Orientation = ScrollOrientation.Horizontal,
            ShowHorizontalScrollbar = false,
            ShowVerticalScrollbar = false
        };
        scroll.Content(new ProbeElement(300, 40));
        Layout(scroll, 100, 40);
        var offsetChanges = 0;
        scroll.ScrollOffsetChanged += () => offsetChanges++;

        scroll.HandleInput(new InputEventArgs
        {
            EventType = InputEventType.MouseDown,
            Position = new Vector2(80, 20)
        });
        scroll.HandleInput(new InputEventArgs
        {
            EventType = InputEventType.MouseDrag,
            Position = new Vector2(30, 20)
        });

        Assert.True(scroll.HorizontalScrollOffset > 0);
        Assert.True(offsetChanges > 0);
    }

    private static void Layout(VisualElement root, float width, float height)
    {
        var tree = new UITree();
        tree.SetRoot(root);
        tree.Update(width, height);
    }

    private sealed class ProbeElement : View<ProbeElement>
    {
        public ProbeElement(float width, float height)
        {
            Width = width;
            Height = height;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }

        protected override void Measure(float availableWidth, float availableHeight)
        {
            DesiredWidth = Width;
            DesiredHeight = Height;
        }

        public override void Render(IRenderer renderer)
        {
        }
    }
}
