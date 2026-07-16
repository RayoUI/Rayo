using System.Numerics;
using Rayo.Animation;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Layout;
using Rayo.Rendering;

namespace Rayo.Tests;

public sealed class DrawerDragDropTests
{
    [Fact]
    public void Content_factory_creates_a_fresh_tree_when_drawer_reopens()
    {
        var tree = new UITree();
        var drawer = new Drawer
        {
            AnimationDuration = 0
        };
        var createdContent = new List<VisualElement>();
        drawer.ContentFactory(() =>
        {
            var content = new TestOverlay();
            createdContent.Add(content);
            return content;
        });

        tree.SetRoot(new VStack().Children(drawer));
        tree.InitializeEventManager(null);
        Drawer.UITree(tree);
        tree.Update(320, 480);

        drawer.Open();
        FrameAnimationTicker.Tick(1);
        drawer.Close();
        FrameAnimationTicker.Tick(1);
        drawer.Open();

        Assert.Equal(2, createdContent.Count);
        Assert.NotSame(createdContent[0], createdContent[1]);

        drawer.Close();
        FrameAnimationTicker.Tick(1);
    }

    [Fact]
    public void Native_overlay_blocking_tracks_the_complete_overlay_stack()
    {
        var tree = new UITree();
        var firstBlocker = new TestNativeOverlayBlocker();
        var secondBlocker = new TestNativeOverlayBlocker();
        var passiveOverlay = new TestOverlay();
        var changes = new List<bool>();
        tree.NativeOverlayBlockingChanged += changes.Add;

        tree.AddOverlay(passiveOverlay);
        tree.AddOverlay(firstBlocker);
        tree.AddOverlay(secondBlocker);
        tree.RemoveOverlay(firstBlocker);

        Assert.True(tree.AreNativeOverlaysBlocked);
        Assert.Equal([true], changes);

        tree.RemoveOverlay(secondBlocker);

        Assert.False(tree.AreNativeOverlaysBlocked);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void Touch_drag_can_drop_from_open_drawer_overlay_into_main_tree()
    {
        var tree = new UITree();
        var target = new TestDropTarget();
        var root = new Grid()
            .Rows(GridLength.Star)
            .Columns(GridLength.Star)
            .AddChild(target, 0, 0);

        tree.SetRoot(root);
        tree.InitializeEventManager(null);
        Drawer.UITree(tree);
        tree.Update(800, 600);

        var drawer = new Drawer
        {
            Position = DrawerPosition.Left,
            DrawerWidth = 250,
            AnimationDuration = 0,
            ShowOverlay = false
        };
        var draggable = new TestDraggable();
        drawer.Content(new VStack().Children(draggable));
        drawer.Open();
        FrameAnimationTicker.Tick(1);
        tree.Update(800, 600);

        var start = new Vector2(
            draggable.ComputedX + draggable.ComputedWidth / 2,
            draggable.ComputedY + draggable.ComputedHeight / 2);
        var end = new Vector2(500, 300);
        var eventManager = tree.EventManager!;

        eventManager.ProcessTouchDown(PointerEventArgs.FromTouch(1, start, 1));
        eventManager.ProcessTouchMove(PointerEventArgs.FromTouch(1, end, 1));
        FrameAnimationTicker.Tick(1);
        tree.Update(800, 600);
        eventManager.ProcessTouchUp(PointerEventArgs.FromTouch(1, end, 1));

        Assert.Equal(1, target.DropCount);
        Assert.Single(tree.Overlays);
    }

    [Fact]
    public void Touch_scroll_cancels_a_delayed_drag_candidate()
    {
        var tree = new UITree();
        var draggable = new TestDraggable
        {
            TouchDragStartDelay = TimeSpan.FromMilliseconds(300)
        };
        var scroll = new ScrollView()
            .Content(
                new VStack().Children(
                    draggable,
                    new Frame().Height(900)));

        tree.SetRoot(scroll);
        tree.InitializeEventManager(null);
        tree.Update(320, 480);

        var start = new Vector2(
            draggable.ComputedX + draggable.ComputedWidth / 2,
            draggable.ComputedY + draggable.ComputedHeight / 2);
        var end = start - new Vector2(0, 40);
        var eventManager = tree.EventManager!;

        eventManager.ProcessTouchDown(PointerEventArgs.FromTouch(1, start, 1));
        eventManager.ProcessTouchMove(PointerEventArgs.FromTouch(1, end, 1));

        Assert.Null(eventManager.DragDrop.CurrentDraggable);
        Assert.Equal(0, draggable.DragStartCount);
    }

    private sealed class TestDraggable : View<TestDraggable>, IDraggable
    {
        public TestDraggable()
        {
            Width = 180;
            Height = 70;
        }

        public bool IsDragging { get; set; }
        public float DragThreshold => 3;
        public TimeSpan TouchDragStartDelay { get; init; }
        public int DragStartCount { get; private set; }

        public DragData? OnDragStart(float mouseX, float mouseY)
        {
            DragStartCount++;
            return new DragData("test", "entity", this)
                .WithAllowedEffects(DragDropEffect.Copy);
        }

        public void OnDragging(float mouseX, float mouseY)
        {
        }

        public void OnDragEnd(bool wasDropped)
        {
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

    private sealed class TestOverlay : View<TestOverlay>
    {
        public override void Render(IRenderer renderer)
        {
        }
    }

    private sealed class TestNativeOverlayBlocker : View<TestNativeOverlayBlocker>, INativeOverlayPolicy
    {
        public bool BlocksNativeOverlays => true;

        public override void Render(IRenderer renderer)
        {
        }
    }

    private sealed class TestDropTarget : View<TestDropTarget>, IDropTarget
    {
        public int DropCount { get; private set; }
        public bool IsDropTargetActive { get; set; }
        public DropConstraints? Constraints => null;
        public DragDropEffect? AllowedEffects => DragDropEffect.Copy;

        public bool CanAcceptDataType(string dataType) => dataType == "test";
        public bool OnDragEnter(DragData dragData) => true;
        public void OnDragOver(DragData dragData, float mouseX, float mouseY)
        {
        }

        public void OnDragLeave(DragData dragData)
        {
        }

        public bool OnDrop(DragData dragData, float mouseX, float mouseY)
        {
            DropCount++;
            return true;
        }

        protected override void Measure(float availableWidth, float availableHeight)
        {
            DesiredWidth = availableWidth;
            DesiredHeight = availableHeight;
        }

        public override void Render(IRenderer renderer)
        {
        }
    }

}
