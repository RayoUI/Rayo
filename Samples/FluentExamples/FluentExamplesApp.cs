using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Interfaces;
using Rayo.Core.Platform;
using Rayo.DevTools;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace FluentExamples;

/// <summary>
/// Comprehensive examples of monadic fluent extensions
/// </summary>
public class FluentExamplesApp : UserControl
{
    private readonly Signal<bool> isActive = new(true);
    private readonly Signal<bool> isEnabled = new(true);
    private readonly Signal<bool> isVisible = new(true);
    private readonly Signal<string?> username = new("John Doe");
    private readonly Signal<int> counter = new(0);
    private readonly Signal<Color> themeColor = new(Color.Blue);

    private int _tapCount = 0;

    public override VisualElement Build()
    {
        return new ScrollView()
            .Content(
                new VStack()
                .Background(new Color(35, 35, 40))
                .Padding(new Thickness(20))
                .Spacing(15)
                .Children(
                    CreateConditionalOperationsSection(),
                    CreateNullSafetySection(),
                    CreateMappingTransformationSection(),
                    CreateReactiveBindingSection(),
                    CreateCollectionOperationsSection(),
                    CreateStateValidationSection()
                )
            );
    }

    #region 1. Conditional Operations Examples

    private VisualElement CreateConditionalOperationsSection()
    {
        return new VStack()
            .Spacing(10)
            .Children(
                new Label()
                    .Text("1. CONDITIONAL OPERATIONS")
                    .FontSize(18)
                    .Foreground(Color.Yellow),

                // When/Unless with static condition
                new Button()
                    .Text("Static Conditional")
                    .Height(40)
                    .When(PlatformDetector.IsWindows, 
                        b => b.Background(Color.Green).Text("Windows Platform"),
                        b => b.Background(Color.Blue).Text("Other Platform"))
                    .Unless(PlatformDetector.IsMobile, 
                        b => b.Width(200)),

                // When with a reactive signal
                new Frame()
                    .Height(60)
                    .BorderWidth(2)
                    .BorderRadius(8)
                    .When(isActive,
                        f => f.Background(Color.Red).BorderColor(new Color(139, 0, 0)),
                        f => f.Background(Color.Gray).BorderColor(Color.DarkGray))
                    .Content(
                        new Label()
                            .Text("Reactive Status")
                            .When(isActive, 
                                l => l.Foreground(Color.White).Text("🟢 ACTIVE"), 
                                l => l.Foreground(Color.LightGray).Text("⚫ INACTIVE"))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),

                // WhenAll - static multiple conditions
                new Button()
                    .Text("Static WhenAll")
                    .Height(40)
                    .WhenAll(
                        b => b.Background(Color.Green).TextColor(Color.White),
                        isActive.Value, isEnabled.Value, isVisible.Value)
                    .OnTapped(() => isActive.Value = !isActive.Value),

                // WhenAll - REACTIVE multiple conditions (NEW!)
                new Frame()
                    .Height(60)
                    .BorderWidth(2)
                    .BorderRadius(8)
                    .WhenAll(
                        f => f.Background(Color.Green).BorderColor(new Color(0, 200, 0)),
                        f => f.Background(Color.Red).BorderColor(new Color(200, 0, 0)),
                        isActive, isEnabled, isVisible)
                    .Content(
                        new Label()
                            .Text("Reactive WhenAll Monitor")
                            .WhenAll(
                                l => l.Text("✓ All Active").Foreground(Color.White),
                                l => l.Text("✗ Some Inactive").Foreground(Color.LightGray),
                                isActive, isEnabled, isVisible)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),

                // Controls to toggle states
                new HStack()
                    .Spacing(10)
                    .Children(
                        new Button()
                            .Text("Toggle Active")
                            .Height(35)
                            .Background(Color.Blue)
                            .OnTapped(() => isActive.Value = !isActive.Value),
                        new Button()
                            .Text("Toggle Enabled")
                            .Height(35)
                            .Background(Color.Blue)
                            .OnTapped(() => isEnabled.Value = !isEnabled.Value),
                        new Button()
                            .Text("Toggle Visible")
                            .Height(35)
                            .Background(Color.Blue)
                            .OnTapped(() => isVisible.Value = !isVisible.Value)
                    ),

                // WhenAny - static version
                new Label()
                    .Text("Static WhenAny Check")
                    .WhenAny(
                        l => l.Foreground(Color.Orange).Text("⚠️ Some condition is true"),
                        !isActive.Value, !isEnabled.Value)
                    .Unless(isActive.Value || isEnabled.Value, 
                        l => l.Foreground(Color.Red).Text("❌ All disabled")),

                // WhenAny - REACTIVE version (NEW!)
                new Label()
                    .Text("Reactive WhenAny Monitor")
                    .WhenAny(
                        l => l.Text("⚠️ At least one is active").Foreground(Color.Orange),
                        l => l.Text("❌ All are inactive").Foreground(Color.Red),
                        isActive, isEnabled, isVisible)
            );
    }

    #endregion

    #region 2. Null Safety (Maybe Monad) Examples

    private VisualElement CreateNullSafetySection()
    {
        string? nullableText = "Valid Text";
        string? nullText = null;

        return new VStack()
            .Spacing(10)
            .Children(
                new Label()
                    .Text("2. NULL SAFETY (Maybe Monad)")
                    .FontSize(18)
                    .Foreground(Color.Yellow),

                // WhenNotNull with static value
                new Label()
                    .Text("Waiting for username...")
                    .WhenNotNull(nullableText, (l, text) => l.Text($"Hello, {text}!"))
                    .Foreground(new Color(144, 238, 144)),

                // Entry with reactive binding for username input
                new VStack()
                    .Spacing(5)
                    .Children(
                        new Label().Text("Enter Username:").Foreground(Color.LightGray),
                        new Entry()
                            .Placeholder("Type your name...")
                            .Height(40)
                            .Background(new Color(50, 50, 55))
                            .TextColor(Color.White)
                            .OnTextChanged(newText => username.Value = string.IsNullOrEmpty(newText) ? null : newText)
                    ),

                // WhenNotNull with a reactive signal
                new Label()
                    .Text("No user")
                    .WhenNotNull(username, (l, name) => l.Text($"👤 User: {name.Value}"))
                    .Foreground(Color.Cyan),

                // WhenNull - handle null case
                new Label()
                    .WhenNull(nullText, l => l.Text("❌ No data available").Foreground(Color.Red))
                    .WhenNotNull(nullText, (l, text) => l.Text(text).Foreground(Color.Green)),

                // OrElse - provide default
                new Frame()
                    .Height(50)
                    .Background(Color.DarkGray)
                    .OrElse(new Frame().Background(Color.Red)),

                new Button()
                    .Text("Clear Username")
                    .Height(40)
                    .Background(Color.Orange)
                    .OnTapped(() => username.Value = username.Value == null ? "John Doe" : null)
            );
    }

    #endregion

    #region 3. Mapping and Transformation (Functor) Examples

    private VisualElement CreateMappingTransformationSection()
    {
        return new VStack()
            .Spacing(10)
            .Children(
                new Label()
                    .Text("3. MAPPING & TRANSFORMATION")
                    .FontSize(18)
                    .Foreground(Color.Yellow),

                // Tap - side effects without breaking flow
                new Button()
                    .Text("Increment Counter")
                    .Height(40)
                    .Background(Color.Purple)
                    .Tap(b => System.Console.WriteLine($"Button tapped at {System.DateTime.Now}"))
                    .OnTapped(() => 
                    {
                        counter.Value++;
                        _tapCount++;
                    }),

                // Apply - multiple actions in sequence
                new Label()
                    .Text("Multi-styled Label")
                    .Apply(
                        l => l.FontSize(16),
                        l => l.Foreground(Color.Cyan),
                        l => l.HorizontalAlignment(HorizontalAlignment.Center),
                        l => l.Padding(new Thickness(10))
                    ),

                // Pipe - transformation pipeline
                new Frame()
                    .Pipe(
                        f => f.Background(new Color(0, 0, 139)),
                        f => f.BorderRadius(10),
                        f => f.Padding(new Thickness(15)),
                        f => f.Height(80)
                    )
                    .Content(
                        new Label()
                            .Text("Piped Frame Content")
                            .Foreground(Color.White)
                    ),

                // Chain - fluent configuration
                new Button()
                    .Chain(b =>
                    {
                        b.Text($"Taps: {_tapCount}");
                        b.Background(_tapCount > 5 ? Color.Red : Color.Blue);
                        b.Height(40);
                    })
            );
    }

    #endregion

    #region 4. Reactive Binding Examples

    private VisualElement CreateReactiveBindingSection()
    {
        return new VStack()
            .Spacing(10)
            .Children(
                new Label()
                    .Text("4. REACTIVE BINDING")
                    .FontSize(18)
                    .Foreground(Color.Yellow),

                // Bind - signal to action
                new Label()
                    .Text("Counter: 0")
                    .Bind(counter, (l, value) => l.Text($"Counter: {value}"))
                    .FontSize(20)
                    .Foreground(new Color(0, 255, 0)),

                // Entry with validation example
                new VStack()
                    .Spacing(5)
                    .Children(
                        new Label().Text("Enter number (reactive validation):").Foreground(Color.LightGray),
                        new Entry()
                            .Placeholder("Enter a number...")
                            .Height(40)
                            .Background(new Color(50, 50, 55))
                            .TextColor(Color.White)
                            .OnTextChanged(newText => 
                            {
                                if (int.TryParse(newText, out int value))
                                {
                                    counter.Value = value;
                                }
                            }),
                        new Label()
                            .Bind(counter, (l, value) => 
                            {
                                l.Text(value > 10 ? "⚠️ High value!" : value > 0 ? "✓ Valid" : "Enter number");
                                l.Foreground(value > 10 ? Color.Orange : value > 0 ? Color.Green : Color.Gray);
                            })
                    ),

                // Observe - signal with transformation
                new Frame()
                    .Height(60)
                    .BorderWidth(2)
                    .Observe(counter, (f, value) => 
                        f.Background(value > 5 ? Color.Red : Color.Green)
                         .BorderColor(value > 10 ? Color.Yellow : Color.Gray))
                    .Content(
                        new Label()
                            .Text("Reactive Frame")
                            .Bind(counter, (l, v) => l.Text($"Count: {v}"))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                    ),

                // React - react to changes (no initial execution in Bind variant)
                new Label()
                    .Text("Watching...")
                    .React(themeColor, (l, color) => 
                        l.Foreground(color).Text($"Theme: {color}")),

                new Button()
                    .Text("Change Theme")
                    .Height(40)
                    .OnTapped(() => 
                    {
                        var colors = new[] { Color.Red, Color.Blue, Color.Green, Color.Purple, Color.Orange };
                        var random = new System.Random();
                        themeColor.Value = colors[random.Next(colors.Length)];
                    })
            );
    }

    #endregion

    #region 5. Collection Operations Examples

    private VisualElement CreateCollectionOperationsSection()
    {
        var children = new VisualElement[]
        {
            new Label().Text("Child 1").Foreground(Color.Cyan),
            new Label().Text("Child 2").Foreground(new Color(0, 255, 0)),
            new Button().Text("Child Button").Height(30),
            new Label().Text("Child 3").Foreground(Color.Yellow)
        };

        return new VStack()
            .Spacing(10)
            .Children(
                new Label()
                    .Text("5. COLLECTION OPERATIONS")
                    .FontSize(18)
                    .Foreground(Color.Yellow),

                // WithChildren - add multiple children to a VStack
                new VStack()
                    .Background(Color.DarkGray)
                    .Padding(new Thickness(10))
                    .BorderRadius(8)
                    .WithChildren(children),

                // ForEachChild - iterate over children
                new VStack()
                    .Spacing(5)
                    .WithChildren(new VisualElement[]
                    {
                        new Label().Text("Item A"),
                        new Label().Text("Item B"),
                        new Button().Text("Button X").Height(30),
                        new Label().Text("Item C")
                    })
                    .ForEachChild(child => 
                    {
                        if (child is Label label)
                            label.Foreground(Color.Orange);
                    }),

                // ForEachChildOfType - typed iteration
                new VStack()
                    .Spacing(5)
                    .WithChildren(new VisualElement[]
                    {
                        new Button().Text("Button 1").Height(30),
                        new Label().Text("Label 1"),
                        new Button().Text("Button 2").Height(30)
                    })
                    .ForEachChildOfType<VStack, Button>(btn => 
                        btn.Background(Color.Purple).TextColor(Color.White)),

                // WithChildrenWhen - conditional children
                new VStack()
                    .Spacing(5)
                    .WithChildrenWhen(isEnabled.Value,
                        new Label().Text("✓ Feature Enabled").Foreground(Color.Green),
                        new Button().Text("Use Feature").Height(30)
                    )
                    .WithChildrenWhen(!isEnabled.Value,
                        new Label().Text("✗ Feature Disabled").Foreground(Color.Red)
                    )
            );
    }

    #endregion

    #region 6. State, Validation, and Guards Examples

    private VisualElement CreateStateValidationSection()
    {
        var emailSignal = new Signal<string>("");

        return new VStack()
            .Spacing(10)
            .Children(
                new Label()
                    .Text("6. STATE, VALIDATION & GUARDS")
                    .FontSize(18)
                    .Foreground(Color.Yellow),

                // Entry with validation example
                new VStack()
                    .Spacing(5)
                    .Children(
                        new Label().Text("Email Validation:").Foreground(Color.LightGray),
                        new Entry()
                            .Placeholder("Enter email...")
                            .Height(40)
                            .Background(new Color(50, 50, 55))
                            .TextColor(Color.White)
                            .OnTextChanged(newText => emailSignal.Value = newText)
                            .TryApply(
                                e => e.BorderWidth(1),
                                ex => System.Console.WriteLine($"Entry error: {ex.Message}")
                            ),
                        new Label()
                            .Bind(emailSignal, (l, email) => 
                            {
                                var isValid = !string.IsNullOrEmpty(email) && email.Contains("@");
                                l.Text(isValid ? "✓ Valid email" : "✗ Invalid email");
                                l.Foreground(isValid ? Color.Green : Color.Red);
                            })
                    ),

                // Guard - ensure condition before proceeding
                new Button()
                    .Text("Guarded Action")
                    .Height(40)
                    .Guard(isActive.Value, "Must be active to proceed")
                    .Background(Color.Green)
                    .OnTapped(() => System.Console.WriteLine("Guard passed!")),

                // Validate - custom validation
                new Label()
                    .Text("Valid Label")
                    .Validate(l => l.Text?.Length > 0, "Label must have text")
                    .Foreground(Color.Green),

                // TryApply - safe operation with error handling
                new Button()
                    .Text("Safe Operation")
                    .Height(40)
                    .Background(Color.Orange)
                    .TryApply(
                        b => 
                        {
                            System.Console.WriteLine("Attempting operation...");
                        },
                        ex => System.Console.WriteLine($"Error: {ex.Message}")
                    ),

                // WithState - persistent state across calls
                new Button()
                    .Text("Stateful Button")
                    .Height(40)
                    .WithState(_tapCount, (b, count) => 
                    {
                        b.Text($"Taps: {count}");
                        b.Background(count > 3 ? Color.Red : Color.Blue);
                    }),

                // Complex composition example
                new Frame()
                    .Height(100)
                    .BorderWidth(2)
                    .BorderRadius(10)
                    .When(counter.Value > 0, f => f.BorderColor(Color.Green))
                    .WhenNotNull(username.Value, (f, name) => f.Background(new Color(0, 100, 0)))
                    .Guard(true, "Frame requires initialization")
                    .Tap(f => System.Console.WriteLine("Complex frame rendered"))
                    .Content(
                        new VStack()
                            .Spacing(5)
                            .Alignment(Alignment.Center)
                            .Children(
                                new Label()
                                    .Bind(counter, (l, c) => l.Text($"Total: {c}"))
                                    .Foreground(Color.White),
                                new Label()
                                    .WhenNotNull(username.Value, (l, name) => l.Text($"By: {name}"))
                                    .Foreground(Color.LightGray)
                            )
                    )
            );
    }

    #endregion
}
