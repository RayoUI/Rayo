using Gallery;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Gallery.Pages;
using Rayo;

namespace Gallery;

/// <summary>
/// Simple tappable navigation item - lighter than Button.
/// Uses IPointerHandler for simple tap detection.
/// </summary>
internal class NavItem : Frame, IPointerHandler
{
    private Action? _onTap;
    private bool _isPressed;
    private System.Numerics.Vector2 _pressPosition;
    private const float TapThreshold = 15f;

    public void OnPointerEntered(PointerEventArgs e) { }
    public void OnPointerExited(PointerEventArgs e) { }

    public void OnPointerMoved(PointerEventArgs e)
    {
        // Cancel press if pointer moves outside bounds (allows scroll to work)
        if (_isPressed && !IsPointInside(e.Position))
        {
            _isPressed = false;
        }
    }

    public void OnPointerPressed(PointerEventArgs e)
    {
        _isPressed = true;
        _pressPosition = e.Position;
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (_isPressed)
        {
            // Only invoke tap if release is inside bounds and distance is small
            bool isInside = IsPointInside(e.Position);
            var delta = e.Position - _pressPosition;
            float distance = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

            if (isInside && distance < TapThreshold)
            {
                _onTap?.Invoke();
            }
        }
        _isPressed = false;
    }

    public NavItem OnTap(Action handler)
    {
        _onTap = handler;
        return this;
    }

    private bool IsPointInside(System.Numerics.Vector2 point)
    {
        return point.X >= ComputedX &&
               point.X <= ComputedX + ComputedWidth &&
               point.Y >= ComputedY &&
               point.Y <= ComputedY + ComputedHeight;
    }
}

/// <summary>
/// Component Gallery - Showcases all Rayo components.
/// Uses sidebar on desktop, drawer on mobile for responsive navigation.
/// </summary>
public class GalleryBuilder : UserControl
{
    private readonly Signal<string> _currentPage = new("Button");
    private Drawer? _navigationDrawer;

    // Breakpoint for switching between mobile and desktop layout
    private const float MobileBreakpoint = 600f;

    public override VisualElement Build()
    {
        // Use platform detection or could use responsive width
        bool useMobileLayout = PlatformDetector.IsMobile;

        if (useMobileLayout)
        {
            return BuildMobileLayout();
        }
        else
        {
            return BuildDesktopLayout();
        }
    }

    // =========================================================================
    // DESKTOP LAYOUT - Fixed Sidebar
    // =========================================================================

    private VisualElement BuildDesktopLayout()
    {
        return new HStack()
            .Spacing(0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Children(
                // Left Sidebar (fixed width)
                BuildSidebar(),

                // Separator
                new Frame()
                    .Width(1)
                    .Background(new Color(50, 50, 55))
                    .VerticalAlignment(VerticalAlignment.Stretch),

                // Main Content Area - will stretch to fill remaining space
                BuildMainContent()
            );
    }

    private VisualElement BuildSidebar()
    {
        var header = new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(16, 12))
            .VerticalAlignment (VerticalAlignment.Top)
            .Content(
                new Label("Rayo Gallery")
                    .FontSize(16)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .HorizontalAlignment (HorizontalAlignment.Center)
                    .Foreground(Color.White)
            );

        var navScroll = new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(BuildNavigationList());

        return new Frame()
            .Width(220)
            .Background(new Color(30, 30, 35))
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new Grid()
                    .Rows(GridLength.Auto, GridLength.Star)
                    .Columns(GridLength.Star)
                    .AddChild(header, 0, 0)
                    .AddChild(navScroll, 1, 0)
            );
    }

    // =========================================================================
    // MOBILE LAYOUT - Drawer Navigation
    // =========================================================================

    private VisualElement BuildMobileLayout()
    {
        // Create the drawer (will render as overlay when opened)
        _navigationDrawer = new Drawer()
            .Position(DrawerPosition.Left)
            .DrawerWidth(280)
            .Background(new Color(30, 30, 35))
            .Content(BuildDrawerContent());

        // Use Grid with proper separation: AppBar and content in separate rows (no overlap)
        // This ensures ScrollView content in row 1 cannot interfere with AppBar events in row 0
        return new Grid()
            .Rows(GridLength.Auto, GridLength.Star)  // Row 0: AppBar (56px), Row 1: Content (remaining)
            .Columns(GridLength.Star)
            .AddChild(BuildAppBar(), 0, 0)           // AppBar occupies row 0
            .AddChild(BuildMainContent(), 1, 0);     // Content occupies row 1 (no overlap)
    }

    private VisualElement BuildAppBar()
    {
        return new Frame()
            .Background(new Color(40, 40, 45))
            .Padding(new Thickness(8, 8))
            .Height(56)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        // Hamburger menu button
                        new IconButton()
                            .IconData(Icons.Menu)
                            .Width(44)
                            .Height(44)
                            .Background(Color.Transparent)
                            .HoverBackground(new Color(60, 60, 65))
                            .BorderWidth(0)
                            .OnTapped(() =>
                            {
                                _navigationDrawer?.Open();
                            }),

                        // Title
                        new Label()
                            .Text(_currentPage)
                            .FontSize(18)
                            .Foreground(Color.White)
                    )
            );
    }

    private VisualElement BuildDrawerContent()
    {
        bool useMobileLayout = PlatformDetector.IsMobile;

        var header = new Frame()
            .Background(new Color(45, 45, 50))
            .Padding(useMobileLayout? new Thickness(0) : new Thickness(16, 20))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            //.BorderRadius(new CornerRadius(topRight: 10))
            .Content(
                new VStack()
                    .Spacing(4)
                    .Children(
                        new Label("Rayo")
                            .FontSize(20)
                            .TextHorizontalAlignment(HorizontalAlignment.Center)
                            .Foreground(Color.White),
                        new Label("Component Gallery")
                            .FontSize(12)
                            .Foreground(new Color(150, 150, 150))
                    )
            );

        var navScroll = new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(BuildNavigationList(closeDrawerOnSelect: true));

        return new Grid()
            .Rows(GridLength.Auto, GridLength.Star)
            .Columns(GridLength.Star)
            .AddChild(header, 0, 0)
            .AddChild(navScroll, 1, 0);
    }

    // =========================================================================
    // SHARED COMPONENTS
    // =========================================================================

    private VisualElement BuildNavigationList(bool closeDrawerOnSelect = false)
    {
        var navItems = new (string name, string category)[]
        {
            // Input Controls
            ("Button", "Input"),
            ("IconButton", "Input"),
            ("Checkbox", "Input"),
            ("RadioButton", "Input"),
            ("Entry", "Input"),
            ("Editor", "Input"),
            ("Slider", "Input"),
            ("ToggleSwitch", "Input"),
            ("ComboBox", "Input"),
            ("DatePicker", "Input"),
            ("TimePicker", "Input"),
            ("ColorPicker", "Input"),
            //("SearchBar", "Input"),
            ("Stepper", "Input"),
            ("GestureDetector", "Input"),

            // Display
            ("Label", "Display"),
            ("Badge", "Display"),
            ("Image", "Display"),
            ("Icon", "Display"),
            ("ProgressBar", "Display"),
            ("Loading", "Display"),
            ("Tooltip", "Display"),

            // Layout
            ("Frame", "Layout"),
            ("Card", "Layout"),
            ("Border", "Layout"),
            ("Accordion", "Layout"),
            ("TabControl", "Layout"),
            ("ScrollView", "Layout"),
            ("ListView", "Layout"),
            ("DataGrid", "Layout"),
            ("TreeView", "Layout"),
            ("Splitter", "Layout"),
            ("Absolute", "Layout"),
            ("Grid", "Layout"),
            ("Flex", "Layout"),
            ("HStack", "Layout"),
            ("VStack", "Layout"),
            ("LStack", "Layout"),

            // Navigation
            ("Menu", "Navigation"),
            ("Drawer", "Navigation"),
            ("SideBar", "Navigation"),

            // Feedback
            ("Dialog", "Feedback"),
            ("Toast", "Feedback"),

            // Graphics
            ("Shapes", "Graphics"),
            ("Brushes", "Graphics"),
            ("Shadow", "Graphics"),
            ("Animation", "Graphics"),

            // Styles
            ("Styles", "Styles"),
        };

        var container = new VStack()
            .Spacing(2)
            .Padding(new Thickness(8))
            .VerticalAlignment(VerticalAlignment.Top);

        string? currentCategory = null;

        foreach (var (name, category) in navItems)
        {
            // Category header
            if (category != currentCategory)
            {
                currentCategory = category;
                container.AddChild(
                    new Label(category.ToUpper())
                        .FontSize(10)
                        .Foreground(new Color(120, 120, 120))
                        .Padding(new Thickness(8, 12, 8, 4))
                );
            }

            container.AddChild(CreateNavItem(name, closeDrawerOnSelect));
        }

        return container;
    }

    private VisualElement CreateNavItem(string pageName, bool closeDrawerOnSelect = false)
    {
        var label = new Label(pageName)
            .FontSize(14)
            .Padding(new Thickness(12, 10))
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        // Bind text color to selection state
        _currentPage.Subscribe(p =>
        {
            label.Foreground(p == pageName ? new Color(120, 180, 255) : new Color(200, 200, 200));
        });
        label.Foreground(_currentPage.Value == pageName ? new Color(120, 180, 255) : new Color(200, 200, 200));

        // Create NavItem and configure it
        var navItem = new NavItem();
        navItem.BorderRadius = new CornerRadius(6);
        navItem.HorizontalAlignment = HorizontalAlignment.Stretch;
        navItem.Content(label);

        // Bind background to selection state
        _currentPage.Subscribe(p =>
        {
            navItem.Background(p == pageName ? new Color(59, 130, 246, 0.3f) : Color.Transparent);
        });
        navItem.Background(_currentPage.Value == pageName ? new Color(59, 130, 246, 0.3f) : Color.Transparent);

        // Handle tap
        navItem.OnTap(() =>
        {
            _currentPage.Value = pageName;
            if (closeDrawerOnSelect)
            {
                if (_navigationDrawer != null)
                {
                    _navigationDrawer.Close();
                }
                else
                {
                    Drawer.CloseCurrentDrawer();
                }
            }
        });

        return navItem;
    }

    private VisualElement BuildMainContent()
    {
        bool useMobileLayout = PlatformDetector.IsMobile;

        var contentFrame = new Frame()
            .Background(new Color(25, 25, 30))
            .Padding(useMobileLayout ? new Thickness(0) : new Thickness(16))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        // Update content when page changes
        _currentPage.Subscribe(page =>
        {
            contentFrame.Content(GetPageContent(page));
        });

        // Initial content
        contentFrame.Content(GetPageContent(_currentPage.Value));

        return contentFrame;
    }

    private VisualElement GetPageContent(string page)
    {
        var pageContent = page switch
        {
            "Button" => (VisualElement)new ButtonPage(),
            "IconButton" => new IconButtonPage(),
            "Badge" => new BadgePage(),
            "Menu" => new MenuPage(),
            "ProgressBar" => new ProgressBarPage(),
            "Dialog" => new DialogPage(),
            "Tooltip" => new TooltipPage(),
            "ComboBox" => new ComboBoxPage(),
            "Toast" => new ToastPage(),
            "DataGrid" => new DataGridPage(),
            "ToggleSwitch" => new ToggleSwitchPage(),
            "Card" => new CardPage(),
            "Image" => new ImagePage(),
            "Icon" => new IconPage(),
            "Loading" => new LoadingPage(),
            "Accordion" => new AccordionPage(),
            "DatePicker" => new DatePickerPage(),
            "TimePicker" => new TimePickerPage(),
            //"SearchBar" => new SearchBarPage(),
            "Stepper" => new StepperPage(),
            "Border" => new BorderPage(),
            "Shapes" => new ShapesPage(),
            "Brushes" => new BrushesPage(),
            "Shadow" => new ShadowPage(),
            "Styles" => new StylesPage(),
            "ColorPicker" => new ColorPickerPage(),
            "Drawer" => new DrawerPage(),
            "SideBar" => new SideBarPage(),
            "TreeView" => new TreeViewPage(),
            "Checkbox" => new CheckboxPage(),
            "Label" => new LabelPage(),
            "Entry" => new EntryPage(),
            "Editor" => new EditorPage(),
            "Frame" => new FramePage(),
            "RadioButton" => new RadioButtonPage(),
            "Slider" => new SliderPage(),
            "TabControl" => new TabControlPage(),
            "ListView" => new ListViewPage(),
            "ScrollView" => new ScrollViewPage(),
            "Absolute" => new AbsolutePage(),
            "Animation" => new AnimationPage(),
            "GestureDetector" => new GestureDetectorPage(),
            "Splitter" => new SplitterPage(),
            "Grid" => new GridPage(),
            "Flex" => new FlexPage(),
            "HStack" => new HStackPage(),
            "VStack" => new VStackPage(),
            "LStack" => new LStackPage(),
            _ => new ButtonPage()
        };

        return new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                pageContent
            );
    }
}
