using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using GradientStop = Rayo.Rendering.Brushes.GradientStop;

namespace Gallery.Pages;

public class IconButtonPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("IconButton", "Interactive icon button with unified pointer events and gesture support"),

                Helper.CreateExampleSection("Basic Icon Buttons",
                    new HStack()
                        .Spacing(10)
                        .Children(
                            new IconButton(Icons.Home)
                                .IconColor(Color.White)
                                .OnTapped(() => Console.WriteLine("Home clicked")),

                            new IconButton(Icons.Settings)
                                .IconColor(Color.White)
                                .OnTapped(() => Console.WriteLine("Settings clicked")),

                            new IconButton(Icons.Search)
                                .IconColor(Color.White)
                                .OnTapped(() => Console.WriteLine("Search clicked")),

                            new IconButton(Icons.Refresh)
                                .IconColor(Color.White)
                                .OnTapped(() => Console.WriteLine("Refresh clicked"))
                        )
                ),

                Helper.CreateExampleSection("Custom Colored Buttons",
                    new HStack()
                        .Spacing(10)
                        .Children(
                            new IconButton(Icons.Heart)
                                .IconColor(Color.White)
                                .Background(new Color(220, 50, 80))
                                .HoverBackground(new Color(240, 80, 100))
                                .PressedBackground(new Color(180, 30, 60))
                                .OnTapped(() => Console.WriteLine("Heart clicked")),

                            new IconButton(Icons.Star)
                                .IconColor(Color.White)
                                .Background(new Color(255, 193, 7))
                                .HoverBackground(new Color(255, 213, 79))
                                .PressedBackground(new Color(255, 179, 0))
                                .OnTapped(() => Console.WriteLine("Star clicked")),

                            new IconButton(Icons.Warning)
                                .IconColor(Color.White)
                                .Background(new Color(76, 175, 80))
                                .HoverBackground(new Color(102, 187, 106))
                                .PressedBackground(new Color(56, 142, 60))
                                .OnTapped(() => Console.WriteLine("Warning clicked")),

                            new IconButton(Icons.Notification)
                                .IconColor(Color.White)
                                .Background(new Color(156, 39, 176))
                                .HoverBackground(new Color(171, 71, 188))
                                .PressedBackground(new Color(123, 31, 162))
                                .OnTapped(() => Console.WriteLine("Notification clicked"))
                        )
                ),

                Helper.CreateExampleSection("Different Sizes",
                    new HStack()
                        .Spacing(10)
                        .Alignment(Alignment.End)
                        .Children(
                            new IconButton(Icons.Add)
                                .IconSize(16)
                                .IconColor(Color.White)
                                .Size(36)
                                .Background(new Color(70, 130, 180))
                                .OnTapped(() => Console.WriteLine("Small add")),

                            new IconButton(Icons.Add)
                                .IconSize(24)
                                .IconColor(Color.White)
                                .Background(new Color(70, 130, 180))
                                .OnTapped(() => Console.WriteLine("Medium add")),

                            new IconButton(Icons.Add)
                                .IconSize(32)
                                .IconColor(Color.White)
                                .Size(64)
                                .Background(new Color(70, 130, 180))
                                .OnTapped(() => Console.WriteLine("Large add"))
                        )
                ),

                Helper.CreateExampleSection("Outlined Buttons (with Borders)",
                    new HStack()
                        .Spacing(10)
                        .Children(
                            new IconButton(Icons.Download)
                                .IconColor(new Color(70, 130, 180))
                                .Background(Color.Transparent)
                                .HoverBackground(new Color(70, 130, 180, 0.1f))
                                .PressedBackground(new Color(70, 130, 180, 0.2f))
                                .BorderWidth(2)
                                .BorderColor(new Color(70, 130, 180))
                                .OnTapped(() => Console.WriteLine("Download clicked")),

                            new IconButton(Icons.Upload)
                                .IconColor(new Color(76, 175, 80))
                                .Background(Color.Transparent)
                                .HoverBackground(new Color(76, 175, 80, 0.1f))
                                .PressedBackground(new Color(76, 175, 80, 0.2f))
                                .BorderWidth(2)
                                .BorderColor(new Color(76, 175, 80))
                                .OnTapped(() => Console.WriteLine("Upload clicked")),

                            new IconButton(Icons.Delete)
                                .IconColor(new Color(244, 67, 54))
                                .Background(Color.Transparent)
                                .HoverBackground(new Color(244, 67, 54, 0.1f))
                                .PressedBackground(new Color(244, 67, 54, 0.2f))
                                .BorderWidth(2)
                                .BorderColor(new Color(244, 67, 54))
                                .OnTapped(() => Console.WriteLine("Delete clicked")),

                            new IconButton(Icons.Lock)
                                .IconColor(new Color(33, 150, 243))
                                .Background(Color.Transparent)
                                .HoverBackground(new Color(33, 150, 243, 0.1f))
                                .PressedBackground(new Color(33, 150, 243, 0.2f))
                                .BorderWidth(2)
                                .BorderColor(new Color(33, 150, 243))
                                .OnTapped(() => Console.WriteLine("Lock clicked"))
                        )
                ),

                CreateGradientButtonsSection(),

                CreateCircularButtonsSection(),

                CreateActionButtonsSection(),

                CreateIconToolbarSection()
            );
    }

    private VisualElement CreateGradientButtonsSection()
    {
        // Linear gradient: horizontal sunset (fire icon)
        var fireBtn = new IconButton(Icons.Star)
            .IconColor(Color.White)
            .Background(LinearGradientBrush.Horizontal(
                new Color(255, 80, 0),
                new Color(255, 200, 0)))
            .HoverBackground(LinearGradientBrush.Horizontal(
                new Color(255, 110, 20),
                new Color(255, 220, 40)))
            .PressedBackground(LinearGradientBrush.Horizontal(
                new Color(200, 50, 0),
                new Color(220, 160, 0)))
            .OnTapped(() => Console.WriteLine("Fire gradient clicked"));

        // Linear gradient: vertical ocean (download icon)
        var oceanBtn = new IconButton(Icons.Download)
            .IconColor(Color.White)
            .Background(LinearGradientBrush.Vertical(
                new Color(0, 180, 255),
                new Color(0, 80, 200)))
            .HoverBackground(LinearGradientBrush.Vertical(
                new Color(30, 200, 255),
                new Color(20, 100, 220)))
            .PressedBackground(LinearGradientBrush.Vertical(
                new Color(0, 140, 200),
                new Color(0, 50, 160)))
            .OnTapped(() => Console.WriteLine("Ocean gradient clicked"));

        // Linear gradient: diagonal purple-pink (heart icon)
        var purpleBtn = new IconButton(Icons.Heart)
            .IconColor(Color.White)
            .Background(LinearGradientBrush.Diagonal(
                new Color(150, 0, 255),
                new Color(255, 0, 150)))
            .HoverBackground(LinearGradientBrush.Diagonal(
                new Color(170, 30, 255),
                new Color(255, 30, 170)))
            .PressedBackground(LinearGradientBrush.Diagonal(
                new Color(110, 0, 200),
                new Color(200, 0, 110)))
            .OnTapped(() => Console.WriteLine("Purple gradient clicked"));

        // Multi-stop linear gradient: rainbow (notification icon)
        var rainbowBtn = new IconButton(Icons.Notification)
            .IconColor(Color.White)
            .Background(new LinearGradientBrush(
                new GradientStop(new Color(255, 0, 0), 0f),
                new GradientStop(new Color(255, 165, 0), 0.33f),
                new GradientStop(new Color(0, 200, 100), 0.66f),
                new GradientStop(new Color(0, 100, 255), 1f))
            { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) })
            .HoverBackground(new LinearGradientBrush(
                new GradientStop(new Color(255, 50, 50), 0f),
                new GradientStop(new Color(255, 185, 30), 0.33f),
                new GradientStop(new Color(30, 220, 120), 0.66f),
                new GradientStop(new Color(30, 130, 255), 1f))
            { StartPoint = new(0, 0.5f), EndPoint = new(1, 0.5f) })
            .OnTapped(() => Console.WriteLine("Rainbow gradient clicked"));

        // Radial gradient: glowing center (settings icon)
        var glowBtn = new IconButton(Icons.Settings)
            .IconColor(Color.White)
            .Background(new RadialGradientBrush(
                new Color(255, 220, 80),
                new Color(200, 100, 0)))
            .HoverBackground(new RadialGradientBrush(
                new Color(255, 240, 120),
                new Color(220, 120, 20)))
            .PressedBackground(new RadialGradientBrush(
                new Color(200, 170, 40),
                new Color(150, 70, 0)))
            .OnTapped(() => Console.WriteLine("Glow gradient clicked"));

        // Radial gradient: spotlight off-center (lock icon)
        var spotBtn = new IconButton(Icons.Lock)
            .IconColor(Color.White)
            .Background(RadialGradientBrush.Spotlight(
                new Color(120, 220, 255),
                new Color(0, 60, 120),
                new System.Numerics.Vector2(0.3f, 0.3f)))
            .HoverBackground(RadialGradientBrush.Spotlight(
                new Color(160, 240, 255),
                new Color(0, 80, 150),
                new System.Numerics.Vector2(0.3f, 0.3f)))
            .OnTapped(() => Console.WriteLine("Spotlight gradient clicked"));

        return Helper.CreateExampleSection("Gradient Brushes",
            new HStack()
                .Spacing(12)
                .Children(fireBtn, oceanBtn, purpleBtn, rainbowBtn, glowBtn, spotBtn)
        );
    }

    private VisualElement CreateCircularButtonsSection()
    {
        var playButton = new IconButton(Icons.Play);
        playButton.IconColor(Color.White);
        playButton.Background(new Color(76, 175, 80));
        playButton.HoverBackground(new Color(102, 187, 106));
        playButton.PressedBackground(new Color(56, 142, 60));
        playButton.Size(50);
        playButton.BorderRadius(new CornerRadius(25));
        playButton.OnTapped(() => Console.WriteLine("Play clicked"));

        var pauseButton = new IconButton(Icons.Pause);
        pauseButton.IconColor(Color.White);
        pauseButton.Background(new Color(255, 152, 0));
        pauseButton.HoverBackground(new Color(255, 167, 38));
        pauseButton.PressedBackground(new Color(245, 124, 0));
        pauseButton.Size(50);
        pauseButton.BorderRadius(new CornerRadius(25));
        pauseButton.OnTapped(() => Console.WriteLine("Pause clicked"));

        var stopButton = new IconButton(Icons.Stop);
        stopButton.IconColor(Color.White);
        stopButton.Background(new Color(244, 67, 54));
        stopButton.HoverBackground(new Color(239, 83, 80));
        stopButton.PressedBackground(new Color(211, 47, 47));
        stopButton.Size(50);
        stopButton.BorderRadius(new CornerRadius(25));
        stopButton.OnTapped(() => Console.WriteLine("Stop clicked"));

        var refreshButton = new IconButton(Icons.Refresh);
        refreshButton.IconColor(Color.White);
        refreshButton.Background(new Color(103, 58, 183));
        refreshButton.HoverBackground(new Color(126, 87, 194));
        refreshButton.PressedBackground(new Color(81, 45, 168));
        refreshButton.Size(50);
        refreshButton.BorderRadius(new CornerRadius(25));
        refreshButton.OnTapped(() => Console.WriteLine("Refresh clicked"));

        return Helper.CreateExampleSection("Circular Buttons",
            new HStack()
                .Spacing(12)
                .Children(playButton, pauseButton, stopButton, refreshButton)
        );
    }

    private VisualElement CreateActionButtonsSection()
    {
        var saveButton = new IconButton(Icons.Save);
        saveButton.IconColor(Color.White);
        saveButton.Background(new Color(76, 175, 80));
        saveButton.HoverBackground(new Color(102, 187, 106));
        saveButton.PressedBackground(new Color(56, 142, 60));
        saveButton.OnTapped(() => Console.WriteLine("Save clicked"));

        var editButton = new IconButton(Icons.Edit);
        editButton.IconColor(Color.White);
        editButton.Background(new Color(70, 130, 180));
        editButton.HoverBackground(new Color(100, 150, 200));
        editButton.PressedBackground(new Color(50, 100, 150));
        editButton.OnTapped(() => Console.WriteLine("Edit clicked"));

        var deleteButton = new IconButton(Icons.Delete);
        deleteButton.IconColor(Color.White);
        deleteButton.Background(new Color(244, 67, 54));
        deleteButton.HoverBackground(new Color(239, 83, 80));
        deleteButton.PressedBackground(new Color(211, 47, 47));
        deleteButton.OnTapped(() => Console.WriteLine("Delete clicked"));

        var closeButton = new IconButton(Icons.Close);
        closeButton.IconColor(Color.White);
        closeButton.Background(new Color(96, 125, 139));
        closeButton.HoverBackground(new Color(120, 144, 156));
        closeButton.PressedBackground(new Color(69, 90, 100));
        closeButton.OnTapped(() => Console.WriteLine("Close clicked"));

        return Helper.CreateExampleSection("Action Buttons",
            new HStack()
                .Spacing(10)
                .Children(saveButton, editButton, deleteButton, closeButton)
        );
    }

    private VisualElement CreateIconToolbarSection()
    {
        var backButton = new IconButton(Icons.ArrowLeft);
        backButton.IconColor(Color.White);
        backButton.Background(Color.Transparent);
        backButton.HoverBackground(new Color(255, 255, 255, 0.1f));
        backButton.PressedBackground(new Color(255, 255, 255, 0.2f));
        backButton.Size(36);
        backButton.OnTapped(() => Console.WriteLine("Back"));

        var forwardButton = new IconButton(Icons.ArrowRight);
        forwardButton.IconColor(Color.White);
        forwardButton.Background(Color.Transparent);
        forwardButton.HoverBackground(new Color(255, 255, 255, 0.1f));
        forwardButton.PressedBackground(new Color(255, 255, 255, 0.2f));
        forwardButton.Size(36);
        forwardButton.OnTapped(() => Console.WriteLine("Forward"));

        var refreshButton = new IconButton(Icons.Refresh);
        refreshButton.IconColor(Color.White);
        refreshButton.Background(Color.Transparent);
        refreshButton.HoverBackground(new Color(255, 255, 255, 0.1f));
        refreshButton.PressedBackground(new Color(255, 255, 255, 0.2f));
        refreshButton.Size(36);
        refreshButton.OnTapped(() => Console.WriteLine("Refresh"));

        var divider = new Frame();
        divider.Width(1);
        divider.Height(24);
        divider.Background(new Color(255, 255, 255, 0.2f));
        divider.Margin(new Thickness(4, 0));

        var volumeButton = new IconButton(Icons.VolumeUp);
        volumeButton.IconColor(Color.White);
        volumeButton.Background(Color.Transparent);
        volumeButton.HoverBackground(new Color(255, 255, 255, 0.1f));
        volumeButton.PressedBackground(new Color(255, 255, 255, 0.2f));
        volumeButton.Size(36);
        volumeButton.OnTapped(() => Console.WriteLine("Volume Up"));

        var muteButton = new IconButton(Icons.VolumeMute);
        muteButton.IconColor(Color.White);
        muteButton.Background(Color.Transparent);
        muteButton.HoverBackground(new Color(255, 255, 255, 0.1f));
        muteButton.PressedBackground(new Color(255, 255, 255, 0.2f));
        muteButton.Size(36);
        muteButton.OnTapped(() => Console.WriteLine("Mute"));

        var settingsButton = new IconButton(Icons.Settings);
        settingsButton.IconColor(Color.White);
        settingsButton.Background(Color.Transparent);
        settingsButton.HoverBackground(new Color(255, 255, 255, 0.1f));
        settingsButton.PressedBackground(new Color(255, 255, 255, 0.2f));
        settingsButton.Size(36);
        settingsButton.OnTapped(() => Console.WriteLine("Settings"));

        return Helper.CreateExampleSection("Icon Toolbar",
            new HStack()
                .Spacing(4)
                .Padding(new Thickness(8))
                .Background(new Color(40, 40, 45))
                .BorderRadius(new CornerRadius(6))
                .Children(backButton, forwardButton, refreshButton, divider, volumeButton, muteButton, settingsButton)
        );
    }
}
