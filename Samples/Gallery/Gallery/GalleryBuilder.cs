using Gallery;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Input;
using Rayo.Core.Interfaces;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using Rayo.Styling;
using Gallery.Pages;
using Rayo;

namespace Gallery;

/// <summary>
/// Simple tappable navigation item - lighter than Button.
/// Uses IPointerHandler for simple tap detection.
/// </summary>
internal sealed class PaletteFrame : Frame
{
    private readonly Func<ColorScheme, Color> _colorSelector;
    private readonly Func<ColorScheme, Color>? _borderSelector;

    public PaletteFrame(
        Func<ColorScheme, Color> colorSelector,
        Func<ColorScheme, Color>? borderSelector = null)
    {
        _colorSelector = colorSelector;
        _borderSelector = borderSelector;
        InitializeTheme();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        SetThemeValue(nameof(Background), (Brush)_colorSelector(theme.Colors), value => Background = value);
        if (_borderSelector != null)
            SetThemeValue(nameof(BorderBrush), (Brush)_borderSelector(theme.Colors), value => BorderBrush = value);
    }
}

internal sealed class PaletteLabel : Label
{
    private readonly Func<ColorScheme, Color> _colorSelector;

    public PaletteLabel(string text, Func<ColorScheme, Color> colorSelector) : base(text)
    {
        _colorSelector = colorSelector;
        ResetThemeValues();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        if (_colorSelector == null)
            return;

        SetThemeValue(nameof(Foreground), (Brush)_colorSelector(theme.Colors), value => Foreground = value);
    }

}

internal sealed class PaletteBorder : Border
{
    private readonly Func<ColorScheme, Color>? _backgroundSelector;
    private readonly Func<ColorScheme, Color> _borderSelector;

    public PaletteBorder(
        Func<ColorScheme, Color> borderSelector,
        Func<ColorScheme, Color>? backgroundSelector = null)
    {
        _borderSelector = borderSelector;
        _backgroundSelector = backgroundSelector;
        ResetThemeValues();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        base.OnThemeApplied(theme);
        if (_borderSelector == null)
            return;

        SetThemeValue(nameof(BorderBrush), (Brush)_borderSelector(theme.Colors), value => BorderBrush = value);
        if (_backgroundSelector != null)
            SetThemeValue(nameof(Background), (Brush)_backgroundSelector(theme.Colors), value => Background = value);
    }
}

internal sealed class PaletteIcon : Icon
{
    private readonly Func<ColorScheme, Color> _colorSelector;

    public PaletteIcon(IconData icon, Func<ColorScheme, Color> colorSelector)
        : base(icon)
    {
        _colorSelector = colorSelector;
        ResetThemeValues();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        base.OnThemeApplied(theme);
        if (_colorSelector != null)
            SetThemeValue(nameof(Color), (Brush)_colorSelector(theme.Colors), value => Color = value);
    }
}

internal sealed class PaletteBadge : Badge
{
    private readonly Func<ColorScheme, Color> _backgroundSelector;
    private readonly Func<ColorScheme, Color> _textSelector;

    public PaletteBadge(
        string text,
        Func<ColorScheme, Color> backgroundSelector,
        Func<ColorScheme, Color> textSelector)
        : base(text)
    {
        _backgroundSelector = backgroundSelector;
        _textSelector = textSelector;
        ResetThemeValues();
    }

    public PaletteBadge(
        int count,
        Func<ColorScheme, Color> backgroundSelector,
        Func<ColorScheme, Color> textSelector)
        : base(count)
    {
        _backgroundSelector = backgroundSelector;
        _textSelector = textSelector;
        ResetThemeValues();
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        base.OnThemeApplied(theme);
        if (_backgroundSelector == null || _textSelector == null)
            return;

        var background = _backgroundSelector(theme.Colors);
        SetThemeValue(nameof(Background), (Brush)background, value => Background = value);
        SetThemeValue(nameof(BorderBrush), (Brush)background, value => BorderBrush = value);
        SetThemeValue(nameof(TextColor), (Brush)_textSelector(theme.Colors), value => TextColor = value);
    }
}

internal sealed class ThemeToggleButton : ButtonIcon
{
    public ThemeToggleButton()
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        base.OnThemeApplied(theme);
        IconData = theme.Brightness == ThemeBrightness.Dark ? Icons.Sun : Icons.Moon;
        SetThemeValue(nameof(IconColor), (Brush)theme.Colors.OnSurface, value => IconColor = value);
        SetThemeValue(nameof(Background), (Brush)Color.Transparent, value => Background = value);
        SetThemeValue(nameof(HoverBackground), (Brush)theme.Colors.SurfacePressed, value => HoverBackground = value);
        SetThemeValue(nameof(PressedBackground), (Brush)theme.Colors.Border, value => PressedBackground = value);
    }
}

internal class NavItem : Frame, IPointerHandler
{
    private Action? _onTap;
    private Label? _label;
    private bool _isSelected;
    private bool _isPressed;
    private System.Numerics.Vector2 _pressPosition;
    private const float TapThreshold = 15f;

    public NavItem()
    {
        InitializeTheme();
    }

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

    public NavItem Configure(Label label, bool isSelected)
    {
        _label = label;
        base.Content = label;
        SetSelected(isSelected);
        return this;
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        ApplyAppearance((UIApplication.Current?.ActiveTheme ?? RayoThemes.Light));
    }

    protected override void OnThemeApplied(ThemeData theme)
    {
        ApplyAppearance(theme);
    }

    private void ApplyAppearance(ThemeData theme)
    {
        var palette = theme.Colors;
        Background = _isSelected ? palette.Primary.WithAlpha(0.22f) : Color.Transparent;
        if (_label != null)
            _label.Foreground = _isSelected ? palette.Primary : palette.OnSurface;
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
public class GalleryBuilder : Component
{
    private readonly Signal<string> _currentPage;
    private Drawer? _navigationDrawer;

    // Breakpoint for switching between mobile and desktop layout
    private const float MobileBreakpoint = 600f;

    public GalleryBuilder()
    {
        _currentPage = UseSignal("Button");
    }

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
                new PaletteFrame(colors => colors.Border)
                    .Width(1)
                    .VerticalAlignment(VerticalAlignment.Stretch),

                // Main Content Area - will stretch to fill remaining space
                BuildMainContent()
            );
    }

    private VisualElement BuildSidebar()
    {
        var header = new PaletteFrame(colors => colors.SurfaceHover)
            .Padding(new Thickness(16, 12))
            .VerticalAlignment (VerticalAlignment.Top)
            .Content(
                new HStack()
                    .Spacing(8)
                    .Alignment(Alignment.Center)
                    .JustifyContent(JustifyContent.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new PaletteLabel("Rayo Gallery", colors => colors.OnSurface)
                            .FontSize(16)
                    )
            );

        var navScroll = new ScrollView()
            .VerticalAlignment(VerticalAlignment.Stretch)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(BuildNavigationList());

        return new PaletteFrame(colors => colors.Background)
            .Width(220)
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
        return new PaletteFrame(colors => colors.SurfaceHover)
            .Padding(new Thickness(8, 8))
            .Height(56)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new Grid()
                    .Rows(GridLength.Star)
                    .Columns(GridLength.Pixels(44), GridLength.Star, GridLength.Pixels(44))
                    .ColumnSpacing(12)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .AddChild(
                        new ButtonIcon()
                            .IconData(Icons.Menu)
                            .Variant(ButtonVariant.Ghost)
                            .Width(44)
                            .Height(44)
                            .BorderThickness(0)
                            .OnTapped(() =>
                            {
                                _navigationDrawer?.Open();
                            }),
                        0,
                        0)
                    .AddChild(
                        new Label()
                            .Text(_currentPage)
                            .FontSize(18)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        0,
                        1)
                    .AddChild(BuildCompactThemeButton(), 0, 2)
            );
    }

    private VisualElement BuildDrawerContent()
    {
        var header = new PaletteFrame(colors => colors.SurfaceHover)
            .Padding(new Thickness(16, 14))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new VStack()
                    .Spacing(4)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Children(
                        new PaletteLabel("Rayo", colors => colors.OnSurface)
                            .FontSize(20),
                        new PaletteLabel("Component Gallery", colors => colors.OnDisabled)
                            .FontSize(12)
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
            ("ButtonGroup", "Input"),
            ("ButtonFloat", "Input"),
            ("ButtonIcon", "Input"),
            ("Checkbox", "Input"),
            ("RadioButton", "Input"),
            ("Entry", "Input"),
            ("EntryNumber", "Input"),
            ("Editor", "Input"),
            ("Slider", "Input"),
            ("ToggleSwitch", "Input"),
            ("ComboBox", "Input"),
            ("DatePicker", "Input"),
            ("TimePicker", "Input"),
            ("ColorPicker", "Input"),
            ("PathPicker", "Input"),
            //("SearchBar", "Input"),
            ("Stepper", "Input"),
            ("GestureDetector", "Input"),

            // Display
            ("Label", "Display"),
            ("Link", "Display"),
            ("Badge", "Display"),
            ("Image", "Display"),
            ("Icon", "Display"),
            ("Icons", "Display"),
            ("Carousel", "Display"),
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
            ("AnchoredPopup", "Layout"),

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
            ("Themes", "Styles"),
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
                    new PaletteLabel(category.ToUpper(), colors => colors.OnDisabled)
                        .FontSize(10)
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

        // Create NavItem and configure it
        var navItem = new NavItem().Configure(label, _currentPage.Value == pageName);
        navItem.BorderRadius = new CornerRadius(6);
        navItem.HorizontalAlignment = HorizontalAlignment.Stretch;

        UseSubscription(_currentPage, p =>
        {
            navItem.SetSelected(p == pageName);
        });

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

        var contentFrame = new PaletteFrame(colors => colors.Background)
            .Padding(useMobileLayout ? new Thickness(0) : new Thickness(16))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var pageHost = new GalleryPageHost(GetPageContent);

        // Update content when page changes
        UseSubscription(_currentPage, page =>
        {
            pageHost.Show(page);
        });

        // Initial content
        pageHost.Show(_currentPage.Value);
        contentFrame.Content(pageHost);

        return contentFrame;
    }

    private VisualElement GetPageContent(string page)
    {
        var pageContent = page switch
        {
            "Button" => (VisualElement)new ButtonPage(),
            "ButtonGroup" => new ButtonGroupPage(),
            "ButtonFloat" => new ButtonFloatPage(),
            "ButtonIcon" => new ButtonIconPage(),
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
            "Icons" => new IconsPage(),
            "Carousel" => new CarouselPage(),
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
            "Themes" => new ThemePage(ApplyTheme),
            "ColorPicker" => new ColorPickerPage(),
            "PathPicker" => new PathPickerPage(),
            "AnchoredPopup" => new AnchoredPopupPage(),
            "Drawer" => new DrawerPage(),
            "SideBar" => new SideBarPage(),
            "TreeView" => new TreeViewPage(),
            "Checkbox" => new CheckboxPage(),
            "Label" => new LabelPage(),
            "Link" => new LinkPage(),
            "Entry" => new EntryPage(),
            "EntryNumber" => new EntryNumberPage(),
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

    private VisualElement BuildCompactThemeButton()
    {
        return new ThemeToggleButton()
            .Variant(ButtonVariant.Ghost)
            .Size(40)
            .IconSize(18)
            .OnTapped(ToggleTheme);
    }

    private void ToggleTheme()
    {
        var current = UIApplication.Current?.ActiveTheme ?? RayoThemes.Light;
        ApplyTheme(
            current.Brightness == ThemeBrightness.Dark
                ? RayoThemes.Light
                : RayoThemes.Dark);
    }

    private void ApplyTheme(ThemeData theme)
    {
        RayoThemes.UseTheme(theme);
    }
}
