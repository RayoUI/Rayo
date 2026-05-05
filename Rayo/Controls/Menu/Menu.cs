namespace Rayo.Controls;

using Rayo.Core;
using Rayo.Core.Interactions;
using Rayo.Layout;
using Rayo.Rendering;

/// <summary>
/// A container for menu items, typically used in a MenuBar.
/// </summary>
public class Menu : UserControl
{
    private readonly string _title;
    private readonly List<MenuItem> _items = new();
    private bool _isOpen = false;
    private VisualElement? _overlayContent;

    // Track the currently open menu globally to close it when another opens
    private static Menu? _currentlyOpenMenu;

    static Menu()
    {
        ScrollInteractionNotifier.ScrollActivity += OnScrollActivity;
    }

    public Menu(string title)
    {
        _title = title;
    }

    private static void OnScrollActivity(VisualElement source)
    {
        CloseCurrentMenu();
    }

    /// <summary>
    /// Closes the currently open menu if any (called from outside, e.g., when clicking away)
    /// </summary>
    public static void CloseCurrentMenu()
    {
        if (_currentlyOpenMenu != null)
        {
            _currentlyOpenMenu.CloseMenu();
        }
    }

    public Menu AddItem(MenuItem item)
    {
        _items.Add(item);
        return this;
    }

    public override VisualElement Build()
    {
        var titleButton = new Button();
        titleButton.Text(_title);
        titleButton.Background(Color.Transparent);
        titleButton.TextColor(Color.White);
        titleButton.FontSize(12);
        titleButton.Padding(new Thickness(10, 5));
        titleButton.BorderRadius(0);
        titleButton.HoverBackground(new Color(60, 60, 60));
        titleButton.PressedBackground(new Color(40, 40, 40));
        titleButton.OnTapped(() => {
            if (_isOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu(titleButton);
            }
        });

        return titleButton;
    }

    private void OpenMenu(VisualElement anchor)
    {
        // Close any previously open menu before opening this one
        if (_currentlyOpenMenu != null && _currentlyOpenMenu != this)
        {
            _currentlyOpenMenu.CloseMenu();
        }

        _isOpen = true;
        _currentlyOpenMenu = this;

        // Calculate position
        float x = anchor.ComputedX;
        float y = anchor.ComputedY + anchor.ComputedHeight;

        // Create menu Frame with items
        var itemButtons = new List<VisualElement>();
        foreach (var item in _items)
        {
            // Build the button from the MenuItem
            var btn = (Button)item.Build();

            // Add an additional handler to close the menu after item is clicked
            btn.OnTapped(() => {
                CloseMenu();
            });

            // Ensure button has a minimum size and visible colors
            btn.HorizontalAlignment( HorizontalAlignment.Stretch );
            btn.Height( 30 );
            btn.Background( Color.Transparent );
            btn.TextColor( Color.White );
            btn.FontSize( 12 );  // Ensure font size is set

            itemButtons.Add(btn);
        }

        var vstack = new VStack()
            .Spacing( 0 )
            .Children( itemButtons.ToArray() );

        const float menuWidth = 180f;

        Frame menuFrame = new Frame();
        menuFrame.Background( new Color( 45, 45, 48 ) );
        menuFrame.Width( menuWidth );
        menuFrame.X( x );
        menuFrame.Y( y );
        menuFrame.BorderColor = new Color( 80, 80, 80 );
        menuFrame.BorderWidth = 1;
        menuFrame.Content(vstack);
        menuFrame.HorizontalAlignment = HorizontalAlignment.Left;
        menuFrame.VerticalAlignment = VerticalAlignment.Top;

        _overlayContent = menuFrame;

        // Add menu overlay
        Rayo.Core.OverlayManager.AddOverlay( _overlayContent );
    }

    private void CloseMenu()
    {
        if (_overlayContent != null)
        {
            Rayo.Core.OverlayManager.RemoveOverlay( _overlayContent );
            _overlayContent = null;
        }

        _isOpen = false;

        // Clear the global reference if this was the currently open menu
        if (_currentlyOpenMenu == this)
        {
            _currentlyOpenMenu = null;
        }
    }
}
