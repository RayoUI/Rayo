using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Gestures.Components;
using Rayo.Gestures.Events;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class GestureDetectorPage : UserControl
{
    public override VisualElement Build()
    {
        // Signal state for demos
        var tapCount = new Signal<int>(0);
        var doubleTapCount = new Signal<int>(0);
        var longPressCount = new Signal<int>(0);
        var lastSwipe = new Signal<string>("None");
        var panOffset = new Signal<string>("X: 0, Y: 0");
        var dragBoxX = new Signal<float>(0);
        var dragBoxY = new Signal<float>(0);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("GestureDetector", "Flutter-style gesture detection for tap, double-tap, long-press, swipe, and pan"),

                // Tap Gesture
                Helper.CreateExampleSection("Tap Gesture",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(tapCount.Map(c => $"Tap count: {c}"))
                                .FontSize(14)
                                .Foreground(ColorDefault.Info),

                            new GestureDetector(
                                new Frame()
                                    .Size(new Size(200, 60))
                                    .Background(ColorDefault.Primary)
                                    .BorderRadius(8)
                                    .Content(
                                        new Label("Tap Me!")
                                            .FontSize(16)
                                            .Foreground(Color.White)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                    )
                            ).OnTap((TapEventArgs _) => tapCount.Value++)
                        )
                ),

                // Double Tap Gesture
                Helper.CreateExampleSection("Double Tap Gesture",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(doubleTapCount.Map(c => $"Double-tap count: {c}"))
                                .FontSize(14)
                                .Foreground(ColorDefault.Info),

                            new GestureDetector(
                                new Frame()
                                    .Size(new Size(200, 60))
                                    .Background(ColorDefault.Success)
                                    .BorderRadius(8)
                                    .Content(
                                        new Label("Double Tap Me!")
                                            .FontSize(16)
                                            .Foreground(Color.White)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                    )
                            ).OnDoubleTap((TapEventArgs _) => doubleTapCount.Value++)
                        )
                ),

                // Long Press Gesture
                Helper.CreateExampleSection("Long Press Gesture",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(longPressCount.Map(c => $"Long-press count: {c}"))
                                .FontSize(14)
                                .Foreground(ColorDefault.Info),

                            new Label("Hold for 500ms to trigger")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary),

                            new GestureDetector(
                                new Frame()
                                    .Size(new Size(200, 60))
                                    .Background(ColorDefault.Warning)
                                    .BorderRadius(8)
                                    .Content(
                                        new Label("Long Press Me!")
                                            .FontSize(16)
                                            .Foreground(new Color(30, 30, 30))
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                    )
                            ).OnLongPress(pos => longPressCount.Value++)
                        )
                ),

                // Swipe Gesture
                Helper.CreateExampleSection("Swipe Gesture",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(lastSwipe.Map(s => $"Last swipe: {s}"))
                                .FontSize(14)
                                .Foreground(ColorDefault.Info),

                            new Label("Swipe in any direction")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary),

                            new GestureDetector(
                                new Frame()
                                    .Size(new Size(250, 100))
                                    .Background(ColorDefault.Danger)
                                    .BorderRadius(8)
                                    .Content(
                                        new VStack()
                                            .Spacing(4)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Children(
                                                new Label("\uEAD3")
                                                    .FontFamily("Lineicons")
                                                    .FontSize(24)
                                                    .Foreground(Color.White)
                                                    .HorizontalAlignment(HorizontalAlignment.Center),
                                                new Label("Swipe Here")
                                                    .FontSize(14)
                                                    .Foreground(Color.White)
                                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                            )
                                    )
                            ).OnSwipe((direction, velocity) =>
                            {
                                lastSwipe.Value = $"{direction} (velocity: {velocity:F0})";
                            })
                        )
                ),

                // Pan Gesture
                Helper.CreateExampleSection("Pan Gesture (Drag)",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(panOffset.Map(s => $"Pan offset: {s}"))
                                .FontSize(14)
                                .Foreground(ColorDefault.Info),

                            new Label("Drag the box around")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary),

                            new Absolute()
                                .Size(new Size(300, 150))
                                .Background(new Color(50, 50, 60))
                                .BorderRadius(8)
                                .Children(
                                    CreateDraggableBox(dragBoxX, dragBoxY, panOffset)
                                )
                        )
                ),

                // Combined Gestures
                Helper.CreateExampleSection("Combined Gestures",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Single element with multiple gesture handlers")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary),

                            CreateMultiGestureDemo()
                        )
                ),

                // Code Example
                Helper.CreateExampleSection("Code Example",
                    new Frame()
                        .Background(new Color(30, 33, 42))
                        .BorderRadius(8)
                        .Padding(new Thickness(12))
                        .Content(
                            new VStack()
                                .Spacing(4)
                                .Children(
                                    new Label("// Wrap any element with GestureDetector")
                                        .FontSize(11)
                                        .Foreground(new Color(106, 153, 85)),
                                    new Label("new GestureDetector(")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    new Frame().Background(Color.Blue)")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label(")")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label(".OnTap(pos => Console.WriteLine(\"Tapped!\"))")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label(".OnDoubleTap(pos => Console.WriteLine(\"Double tap!\"))")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label(".OnLongPress(pos => Console.WriteLine(\"Long press!\"))")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label(".OnSwipe((dir, vel) => Console.WriteLine($\"Swipe {dir}\"))")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label(".OnPanUpdate(delta => MoveElement(delta));")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254))
                                )
                        )
                )
            );
    }

    private GestureDetector CreateDraggableBox(Signal<float> x, Signal<float> y, Signal<string> offset)
    {
        var box = new Frame()
            .Size(new Size(60, 60))
            .Background(new Color(168, 85, 247))
            .BorderRadius(8)
            .Content(
                new Label("\uEAD3")
                    .FontFamily("Lineicons")
                    .FontSize(24)
                    .Foreground(Color.White)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
            );

        // Bind margin to position
        x.Subscribe(newX => box.Margin(new Thickness(newX, y.Value, 0, 0)));
        y.Subscribe(newY => box.Margin(new Thickness(x.Value, newY, 0, 0)));

        return new GestureDetector(box)
            .OnPanUpdate(delta =>
            {
                // Clamp within bounds
                float newX = Math.Clamp(x.Value + delta.X, 0, 240);
                float newY = Math.Clamp(y.Value + delta.Y, 0, 90);
                //x.Value = newX;
                //y.Value = newY;
                offset.Value = $"X: {newX:F0}, Y: {newY:F0}";
                box.AbsoluteLeft(newX);
                box.AbsoluteTop(newY);
            });
    }

    private VisualElement CreateMultiGestureDemo()
    {
        var status = new Signal<string>("Waiting for gesture...");
        var bgColor = new Signal<Color>(new Color(100, 100, 120));

        var Frame = new Frame();
        Frame.Size(new Size(250, 80));
        Frame.BorderRadius(8);
        Frame.Content(
            new Label()
                .Text(status)
                .FontSize(14)
                .Foreground(Color.White)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
        );

        // Bind background color
        bgColor.Subscribe(c => Frame.Background(c));
        Frame.Background(bgColor.Value);

        return new GestureDetector(Frame)
            .OnTap((TapEventArgs _) =>
            {
                status.Value = "Single Tap!";
                bgColor.Value = ColorDefault.Primary;
            })
            .OnDoubleTap((TapEventArgs _) =>
            {
                status.Value = "Double Tap!";
                bgColor.Value = ColorDefault.Success;
            })
            .OnLongPress(pos =>
            {
                status.Value = "Long Press!";
                bgColor.Value = ColorDefault.Warning;
            })
            .OnSwipe((dir, vel) =>
            {
                status.Value = $"Swipe {dir}!";
                bgColor.Value = ColorDefault.Danger;
            });
    }
}
