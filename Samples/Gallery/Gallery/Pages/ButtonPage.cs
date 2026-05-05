using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using System.Net;
using static Rayo.Core.UIHelpers;
using GradientStop = Rayo.Rendering.Brushes.GradientStop;

namespace Gallery.Pages;

public class ButtonPage : UserControl
{
    public override VisualElement Build()
    {
        var clickCount = new Signal<int>(0);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Button", "Interactive button component with various styles and states"),

                Helper.CreateExampleSection("Basic Buttons",
                    new HStack()
                        .Spacing(12)
                        .Children(
                            new Button()
                                .Text("Default")
                                .Width(100)
                                .Height(40),

                            new Button()
                                .Text("Primary")
                                .Width(100)
                                .Height(40)
                                .Background(ColorDefault.Primary),

                            new Button()
                                .Text("Success")
                                .Width(100)
                                .Height(40)
                                .Background(ColorDefault.Success),

                            new Button()
                                .Text("Warning")
                                .Width(100)
                                .Height(40)
                                .Background(ColorDefault.Warning)
                                .TextColor(new Color(30, 30, 30)),

                            new Button()
                                .Text("Danger")
                                .Width(100)
                                .Height(40)
                                .Background(ColorDefault.Danger)
                        )
                ),

                Helper.CreateExampleSection("Button Sizes",
                    new HStack()
                        .Spacing(12)
                        .Alignment(Alignment.End)
                        .Children(
                            new Button()
                                .Text("Small")
                                .Width(80)
                                .Height(32)
                                .FontSize(12)
                                .Background(ColorDefault.Primary),

                            new Button()
                                .Text("Medium")
                                .Width(100)
                                .Height(40)
                                .FontSize(14)
                                .Background(ColorDefault.Primary),

                            new Button()
                                .Text("Large")
                                .Width(120)
                                .Height(48)
                                .FontSize(16)
                                .Background(ColorDefault.Primary),

                            new Button()
                                .Text("Extra Large")
                                .Width(150)
                                .Height(56)
                                .FontSize(18)
                                .Background(ColorDefault.Primary)
                        )
                ),

                Helper.CreateExampleSection("Text Alignment",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            new Button()
                                .Text("Left Aligned")
                                .Width(200)
                                .Height(40)
                                .TextAlignment(HorizontalAlignment.Left)
                                .Background(new Color(60, 63, 73)),

                            new Button()
                                .Text("Center Aligned")
                                .Width(200)
                                .Height(40)
                                .TextAlignment(HorizontalAlignment.Center)
                                .Background(new Color(60, 63, 73)),

                            new Button()
                                .Text("Right Aligned")
                                .Width(200)
                                .Height(40)
                                .TextAlignment(HorizontalAlignment.Right)
                                .Background(new Color(60, 63, 73))
                        )
                ),

                Helper.CreateExampleSection("Icon Buttons (FontFamily)",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new Label("Use Frame with Label for icon-only buttons")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160)),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateIconButton("\uEA1C", ColorDefault.Danger),      // Heart
                                    CreateIconButton("\uEA1D", ColorDefault.Warning),     // Star
                                    CreateIconButton("\uEA44", ColorDefault.Primary),     // Home
                                    CreateIconButton("\uEA5E", ColorDefault.Success),     // Search
                                    CreateIconButton("\uEA5F", ColorDefault.Info),        // Settings
                                    CreateIconButton("\uEA54", new Color(168, 85, 247)),  // User
                                    CreateIconButton("\uEBB3", new Color(34, 197, 94)),   // Atom
                                    CreateIconButton("\uEAA4", new Color(239, 68, 68))    // Trash
                                )
                        )
                ),

                Helper.CreateExampleSection("Text + Icon Buttons",
                    new VStack()
                        .Spacing(16)
                        .Children(
                            new Label("Combine icons with text using HStack inside Frame")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160)),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateTextIconButton("\uEA1C", "Like", ColorDefault.Danger),
                                    CreateTextIconButton("\uEA1D", "Favorite", ColorDefault.Warning),
                                    CreateTextIconButton("\uEA5E", "Search", ColorDefault.Primary),
                                    CreateTextIconButton("\uEBBA", "Done", ColorDefault.Success)
                                ),

                            new Label("Icon on right side")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160)),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateTextIconButtonRight("Next", "\uEAD4", ColorDefault.Primary),
                                    CreateTextIconButtonRight("Download", "\uEA26", ColorDefault.Success),
                                    CreateTextIconButtonRight("Share", "\uEB9B", ColorDefault.Info)
                                )
                        )
                ),

                Helper.CreateExampleSection("Outlined Buttons",
                    new HStack()
                        .Spacing(12)
                        .Children(
                            CreateOutlinedButton("Primary", ColorDefault.Primary),
                            CreateOutlinedButton("Success", ColorDefault.Success),
                            CreateOutlinedButton("Warning", ColorDefault.Warning),
                            CreateOutlinedButton("Danger", ColorDefault.Danger)
                        )
                ),

                CreateBorderRadiusSection(),

                CreateGradientButtonsSection(),

                Helper.CreateExampleSection("Interactive Example",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label()
                                .Text(clickCount.Map(c => $"Click count: {c}"))
                                .FontSize(16)
                                .Foreground(ColorDefault.Info),

                            new HStack()
                                .Spacing(10)
                                .Children(
                                    new Button()
                                        .Text("+ Increment")
                                        .Width(120)
                                        .Height(40)
                                        .Background(ColorDefault.Success)
                                        .OnTapped(() => clickCount.Value++),

                                    new Button()
                                        .Text("- Decrement")
                                        .Width(120)
                                        .Height(40)
                                        .Background(ColorDefault.Warning)
                                        .TextColor(new Color(30, 30, 30))
                                        .OnTapped(() => { if (clickCount.Value > 0) clickCount.Value--; }),

                                    new Button()
                                        .Text("Reset")
                                        .Width(100)
                                        .Height(40)
                                        .Background(ColorDefault.Danger)
                                        .OnTapped(() => clickCount.Value = 0)
                                )
                        )
                ),

                Helper.CreateExampleSection("Code Example",
                    new Frame()
                        .Background(new Color(30, 33, 42))
                        .BorderRadius(8)
                        .Padding(new Thickness(12))
                        .Content(
                            new VStack()
                                .Spacing(4)
                                .Children(
                                    new Label("// Basic button")
                                        .FontSize(11)
                                        .Foreground(new Color(106, 153, 85)),
                                    new Label("new Button().Text(\"Click Me\").Background(ColorDefault.Primary)")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("// Button with icon (using Frame + HStack)")
                                        .FontSize(11)
                                        .Foreground(new Color(106, 153, 85)),
                                    new Label("new Frame().Children(new HStack().Children(")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    new Label(\"\\uEA1C\").FontFamily(\"Lineicons\"),")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254)),
                                    new Label("    new Label(\"Like\")))")
                                        .FontSize(11)
                                        .Foreground(new Color(156, 220, 254))
                                )
                        )
                )
            );
    }

    private Frame CreateIconButton(string icon, Color bgColor)
    {
        var label = new Label(icon);
        label.FontFamily("Lineicons");
        label.FontSize(20);
        label.Foreground(Color.White);
        label.HorizontalAlignment(HorizontalAlignment.Center);
        label.VerticalAlignment(VerticalAlignment.Center);

        var Frame = new Frame();
        Frame.Size(new Size(44, 44));
        Frame.Background(bgColor);
        Frame.BorderRadius(8);
        Frame.Content(label);
        return Frame;
    }

    private Frame CreateTextIconButton(string icon, string text, Color bgColor)
    {
        var iconLabel = new Label(icon);
        iconLabel.FontFamily("Lineicons");
        iconLabel.FontSize(16);
        iconLabel.Foreground(Color.White);

        var textLabel = new Label(text);
        textLabel.FontSize(14);
        textLabel.Foreground(Color.White);

        var hstack = new HStack();
        hstack.Spacing(8);
        hstack.Alignment(Alignment.Center);
        hstack.Children(iconLabel, textLabel);

        var Frame = new Frame();
        Frame.Background(bgColor);
        Frame.BorderRadius(8);
        Frame.Padding(new Thickness(14, 10));
        Frame.Content(hstack);
        return Frame;
    }

    private Frame CreateTextIconButtonRight(string text, string icon, Color bgColor)
    {
        var textLabel = new Label(text);
        textLabel.FontSize(14);
        textLabel.Foreground(Color.White);

        var iconLabel = new Label(icon);
        iconLabel.FontFamily("Lineicons");
        iconLabel.FontSize(16);
        iconLabel.Foreground(Color.White);

        var hstack = new HStack();
        hstack.Spacing(8);
        hstack.Alignment(Alignment.Center);
        hstack.Children(textLabel, iconLabel);

        var Frame = new Frame();
        Frame.Background(bgColor);
        Frame.BorderRadius(8);
        Frame.Padding(new Thickness(14, 10));
        Frame.Content(hstack);
        return Frame;
    }

    private VisualElement CreateBorderRadiusSection()
    {
        var btn1 = new Button();
        btn1.Text("Square");
        btn1.Width(100);
        btn1.Height(40);
        btn1.BorderRadius(0);
        btn1.Background(ColorDefault.Primary);

        var btn2 = new Button();
        btn2.Text("Rounded");
        btn2.Width(100);
        btn2.Height(40);
        btn2.BorderRadius(8);
        btn2.Background(ColorDefault.Primary);

        var btn3 = new Button();
        btn3.Text("More Rounded");
        btn3.Width(120);
        btn3.Height(40);
        btn3.BorderRadius(16);
        btn3.Background(ColorDefault.Primary);

        var btn4 = new Button();
        btn4.Text("Pill");
        btn4.Width(100);
        btn4.Height(40);
        btn4.BorderRadius(20);
        btn4.Background(ColorDefault.Primary);

        return Helper.CreateExampleSection("Button with Border Radius",
            new HStack()
                .Spacing(12)
                .Children(btn1, btn2, btn3, btn4)
        );
    }

    private VisualElement CreateGradientButtonsSection()
    {
        // Row 1: Linear gradients — horizontal direction
        var sunsetBtn = new Button()
            .Text("Sunset")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(LinearGradientBrush.Horizontal(
                new Color(255, 94, 58), new Color(255, 195, 0)))
            .HoverBackground(LinearGradientBrush.Horizontal(
                new Color(255, 114, 78), new Color(255, 215, 30)))
            .PressedBackground(LinearGradientBrush.Horizontal(
                new Color(210, 60, 20), new Color(210, 155, 0)))
            .BorderWidth(0);

        var oceanBtn = new Button()
            .Text("Ocean")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(LinearGradientBrush.Horizontal(
                new Color(0, 198, 255), new Color(0, 114, 255)))
            .HoverBackground(LinearGradientBrush.Horizontal(
                new Color(30, 218, 255), new Color(30, 134, 255)))
            .PressedBackground(LinearGradientBrush.Horizontal(
                new Color(0, 158, 205), new Color(0, 80, 200)))
            .BorderWidth(0);

        var forestBtn = new Button()
            .Text("Forest")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(LinearGradientBrush.Horizontal(
                new Color(86, 204, 101), new Color(29, 142, 73)))
            .HoverBackground(LinearGradientBrush.Horizontal(
                new Color(106, 220, 120), new Color(49, 162, 93)))
            .PressedBackground(LinearGradientBrush.Horizontal(
                new Color(56, 164, 71), new Color(10, 110, 50)))
            .BorderWidth(0);

        // Row 2: Linear gradients — vertical direction
        var skyBtn = new Button()
            .Text("Sky")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(LinearGradientBrush.Vertical(
                new Color(135, 206, 250), new Color(30, 100, 200)))
            .HoverBackground(LinearGradientBrush.Vertical(
                new Color(155, 220, 255), new Color(50, 120, 220)))
            .PressedBackground(LinearGradientBrush.Vertical(
                new Color(100, 170, 210), new Color(10, 70, 160)))
            .BorderWidth(0)
            .TextColor(new Color(10, 30, 80));

        var auroraBrush = new LinearGradientBrush(
            new GradientStop(new Color(100, 0, 200), 0f),
            new GradientStop(new Color(0, 180, 255), 0.5f),
            new GradientStop(new Color(0, 230, 160), 1f))
        { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) };

        var auroraHoverBrush = new LinearGradientBrush(
            new GradientStop(new Color(130, 30, 230), 0f),
            new GradientStop(new Color(30, 210, 255), 0.5f),
            new GradientStop(new Color(30, 255, 190), 1f))
        { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) };

        var auroraBtn = new Button()
            .Text("Aurora")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(auroraBrush)
            .HoverBackground(auroraHoverBrush)
            .BorderWidth(0);

        // Row 3: Diagonal & radial
        var neonBtn = new Button()
            .Text("Neon")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(LinearGradientBrush.Diagonal(
                new Color(255, 0, 128), new Color(80, 0, 255)))
            .HoverBackground(LinearGradientBrush.Diagonal(
                new Color(255, 40, 160), new Color(110, 30, 255)))
            .PressedBackground(LinearGradientBrush.Diagonal(
                new Color(200, 0, 90), new Color(50, 0, 200)))
            .BorderWidth(0);

        var glowBtn = new Button()
            .Text("Glow")
            .Width(120).Height(42)
            .BorderRadius(8)
            .Background(new RadialGradientBrush(
                new Color(255, 230, 100), new Color(200, 80, 0)))
            .HoverBackground(new RadialGradientBrush(
                new Color(255, 245, 140), new Color(220, 100, 20)))
            .PressedBackground(new RadialGradientBrush(
                new Color(200, 175, 50), new Color(150, 50, 0)))
            .BorderWidth(0)
            .TextColor(new Color(80, 30, 0));

        var cosmicBtn = new Button()
            .Text("Cosmic")
            .Width(120).Height(42)
            .BorderRadius(20)
            .Background(new LinearGradientBrush(
                new GradientStop(new Color(30, 0, 60), 0f),
                new GradientStop(new Color(120, 0, 180), 0.4f),
                new GradientStop(new Color(255, 60, 120), 1f))
            { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) })
            .HoverBackground(new LinearGradientBrush(
                new GradientStop(new Color(50, 10, 90), 0f),
                new GradientStop(new Color(150, 20, 210), 0.4f),
                new GradientStop(new Color(255, 90, 150), 1f))
            { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) })
            .BorderWidth(0);

        return Helper.CreateExampleSection("Gradient Brushes",
            new VStack()
                .Spacing(12)
                .Children(
                    new HStack()
                        .Spacing(12)
                        .Children(sunsetBtn, oceanBtn, forestBtn),
                    new HStack()
                        .Spacing(12)
                        .Children(skyBtn, auroraBtn, neonBtn),
                    new HStack()
                        .Spacing(12)
                        .Children(glowBtn, cosmicBtn)
                )
        );
    }

    private Frame CreateOutlinedButton(string text, Color borderColor)
    {
        var Frame = new Frame();
        Frame.Background(Color.Transparent);
        Frame.BorderRadius(8);
        Frame.BorderWidth(2);
        Frame.BorderColor(borderColor);
        Frame.Padding(new Thickness(16, 10));
        Frame.Content(
            new Label(text)
                .FontSize(14)
                .Foreground(borderColor)
                .HorizontalAlignment(HorizontalAlignment.Center)
        );
        return Frame;
    }
}
