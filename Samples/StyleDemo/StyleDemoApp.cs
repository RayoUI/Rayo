using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
using StyleDemo.Components;

namespace StyleDemo;

// ---------------------------------------------------------------------------
// StyleDemoApp — root UserControl for the style showcase.
//
// Extends UserControl (not IUIBuilder) so that:
//   • EnsureBuilt() calls StyleEngine.Apply(GlobalStyles, content) on first render
//   • OnGlobalStylesChanged re-applies the new sheet to the whole tree when
//     UseGlobalStyles() is called from the theme switcher buttons
//
// Features demonstrated:
//    1. Design tokens      — AppThemes builds StyleSheets from StyleTokens
//    2. Theme switch       — UseGlobalStyles() fires GlobalStylesChanged; all live
//                            UserControls re-apply styles instantly (no rebuild)
//    3. @extend            — btnBase shared across .primary/.secondary/.danger/.ghost
//    4. Class selector     — buttons tagged .primary, .danger, .theme-btn etc.
//    5. id selector        — active theme button highlighted via #theme-active
//    6. Pseudo-states      — When(StyleTrigger.Hover/Pressed/Disabled)
//    7. StyleScope         — IsolatedCard ignores global theme
//    8. Responsive         — When(Breakpoint.*) named tiers and When(Func<float,bool>) predicates
//    9. OS conditions      — When(PlatformType.*), When(ColorScheme.*), When(Orientation.*)
//   10. Advanced selectors — multi-class (.a.b), Not(), FirstChild(), LastChild()
//   11. Important()        — override cascade order regardless of specificity
// ---------------------------------------------------------------------------
public class StyleDemoApp : UserControl
{
    private string _activeTheme = "dark";

    public override VisualElement Build()
    {
        // Set the initial global theme.
        // EnsureBuilt() reads UIApplication.Current.GlobalStyles AFTER Build()
        // returns and applies it, so this call seeds GlobalStyles correctly.
        UIApplication.Current!.UseGlobalStyles(AppThemes.Dark());

        return new Frame()
            .Classes("app-root")
            .Background(new Color(22, 22, 30))
            .Content(
            new VStack()
                .Spacing(0f)
                .Children(
                    BuildHeader(),
                    new ScrollView()
                        .Content(
                            new VStack()
                                .Spacing(24f)
                                .Padding(new Thickness(32f, 24f))
                                .Children(
                                    BuildTokensSection(),
                                    BuildButtonVariantsSection(),
                                    BuildPseudoStatesSection(),
                                    BuildHoverItemsSection(),
                                    BuildIsolatedSection(),
                                    BuildResponsiveSection(),
                                    BuildOsConditionsSection(),
                                    BuildAdvancedSelectorsSection(),
                                    BuildImportantSection()
                                )
                        )
                        .VerticalAlignment(VerticalAlignment.Stretch)
                )
        );
    }

    // -------------------------------------------------------------------------
    // Header — theme switcher buttons
    // -------------------------------------------------------------------------
    private VisualElement BuildHeader()
    {
        Button darkBtn = null, lightBtn = null, neonBtn = null;

        void Switch(string theme)
        {
            _activeTheme = theme;

            // Update IDs BEFORE UseGlobalStyles so that when StyleEngine.Apply
            // walks the tree the correct button already has id="theme-active".
            // (StyleApplier only watches IsHovered/IsPressed/IsEnabled, not Id,
            //  so IDs changed after Apply would have no visible effect.)
            darkBtn!.Id(_activeTheme == "dark"  ? "theme-active" : null);
            lightBtn!.Id(_activeTheme == "light" ? "theme-active" : null);
            neonBtn!.Id(_activeTheme == "neon"   ? "theme-active" : null);

            var sheet = theme switch
            {
                "light" => AppThemes.Light(),
                "neon"  => AppThemes.Neon(),
                _       => AppThemes.Dark(),
            };

            UIApplication.Current!.UseGlobalStyles(sheet);
        }

        return new Frame()
            .Classes("header")
            .Background(new Color(16, 16, 24))
            .Padding(new Thickness(32f, 14f))
            .VerticalAlignment(VerticalAlignment.Top)
            .Content(
                new HStack()
                    .Spacing(12f)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Children(
                        new Label("Style Showcase")
                            .Classes("title")
                            .Foreground(new Color(230, 230, 245))
                            .FontSize(20f)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Center),

                        new Label("Theme:")
                            .Classes("muted")
                            .Foreground(new Color(130, 130, 155))
                            .FontSize(13f)
                            .VerticalAlignment(VerticalAlignment.Center),

                        // Buttons tagged .theme-btn — styled by the theme sheet.
                        // The active one also gets id="theme-active" (higher specificity
                        // than the class rule, so it overrides the background colour).
                        new Button()
                            .Text("Dark")
                            .Classes("theme-btn")
                            .Id("theme-active")   // active by default
                            .Ref(out darkBtn)
                            .OnTapped(() => Switch("dark")),

                        new Button()
                            .Text("Light")
                            .Classes("theme-btn")
                            .Ref(out lightBtn)
                            .OnTapped(() => Switch("light")),

                        new Button()
                            .Text("Neon")
                            .Classes("theme-btn")
                            .Ref(out neonBtn)
                            .OnTapped(() => Switch("neon"))
                    )
            );
    }

    // -------------------------------------------------------------------------
    // Section helper — titled card wrapper
    // -------------------------------------------------------------------------
    private static VisualElement Card(string title, string subtitle, params VisualElement[] children) =>
        new Frame()
            .Classes("card")
            .Background(new Color(34, 34, 46))
            .BorderRadius(10f)
            .Padding(new Thickness(24f, 20f))
            .Content(
                new VStack()
                    .Spacing(16f)
                    .Children(
                        new VStack()
                            .Spacing(4f)
                            .Children(
                                new Label(title)
                                    .Classes("section-title")
                                    .Foreground(new Color(230, 230, 245))
                                    .FontSize(15f),
                                new Label(subtitle)
                                    .Classes("muted")
                                    .Foreground(new Color(130, 130, 155))
                                    .FontSize(12f)
                            ),
                        new Frame()
                            .Background(new Color(60, 60, 80))
                            .Height(1f)
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        new VStack()
                            .Spacing(14f)
                            .Children(children)
                    )
            );

    // -------------------------------------------------------------------------
    // 1. Design tokens
    // -------------------------------------------------------------------------
    private VisualElement BuildTokensSection() =>
        Card(
            "Design Tokens",
            "Each theme is built from a StyleTokens dictionary — the single source of truth for colours and sizes.",
            new HStack()
                .Spacing(10f)
                .Children(
                    TokenChip("--color-primary",  new Color(99,  130, 246)),
                    TokenChip("--color-danger",   new Color(220, 60,  60)),
                    TokenChip("--color-success",  new Color(34,  197, 94)),
                    TokenChip("--bg-card",        new Color(34,  34,  46)),
                    TokenChip("--text-muted",     new Color(130, 130, 155))
                ),
            new Label("Switch themes above — all rules below update instantly via GlobalStylesChanged.")
                .Classes("muted")
                .Foreground(new Color(130, 130, 155))
                .FontSize(12f)
        );

    private static VisualElement TokenChip(string name, Color swatch) =>
        new VStack()
            .Spacing(6f)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new Frame()
                    .Background(swatch)
                    .BorderRadius(6f)
                    .Width(40f)
                    .Height(40f)
                    .HorizontalAlignment(HorizontalAlignment.Center),
                new Label(name)
                    .Foreground(new Color(130, 130, 155))
                    .FontSize(10f)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
            );

    // -------------------------------------------------------------------------
    // 2. Button variants — @extend + class selectors
    // -------------------------------------------------------------------------
    private VisualElement BuildButtonVariantsSection() =>
        Card(
            "Button Variants — @extend + Class Selectors",
            "btnBase is @extended by .primary, .secondary, .danger, .ghost — sharing height, radius and disabled state.",
            new HStack()
                .Spacing(12f)
                .Children(
                    new Button().Text("Primary") .Classes("primary") .Width(110f),
                    new Button().Text("Secondary").Classes("secondary").Width(110f),
                    new Button().Text("Danger")   .Classes("danger")  .Width(110f),
                    new Button().Text("Success")  .Classes("success") .Width(110f),
                    new Button().Text("Ghost")    .Classes("ghost")   .Width(110f)
                )
        );

    // -------------------------------------------------------------------------
    // 3. Pseudo-states — When(StyleTrigger.Disabled)
    // -------------------------------------------------------------------------
    private VisualElement BuildPseudoStatesSection() =>
        Card(
            "Pseudo-States — When(StyleTrigger.*)",
            "HoverBackground/PressedBackground on buttons; When(Disabled) dims via Opacity(0.38).",
            new HStack()
                .Spacing(12f)
                .Children(
                    BtnCol("Normal",   new Button().Text("Hover me").Classes("primary")      .Width(120f)),
                    BtnCol("Disabled", new Button().Text("Disabled").Classes("disabled-demo").Width(120f).IsEnabled(false)),
                    BtnCol("Danger",   new Button().Text("Delete")  .Classes("danger")       .Width(120f)),
                    BtnCol("Ghost",    new Button().Text("Cancel")  .Classes("ghost")        .Width(120f))
                )
        );

    private static VisualElement BtnCol(string caption, VisualElement btn) =>
        new VStack()
            .Spacing(8f)
            .Children(
                new Label(caption).Classes("muted").Foreground(new Color(130, 130, 155)).FontSize(11f),
                btn
            );

    // -------------------------------------------------------------------------
    // 4. Hover on containers — When(StyleTrigger.Hover) on Frame + Label
    // -------------------------------------------------------------------------
    private VisualElement BuildHoverItemsSection() =>
        Card(
            "Hover on Containers — When(StyleTrigger.Hover)",
            "StyleApplier subscribes to IsHovered; re-runs Apply() on change. Base setters reset, hover setters override.",
            new HStack()
                .Spacing(12f)
                .Children(
                    HoverItem("Hover me",  "Background shifts on enter"),
                    HoverItem("And me",    "Label text brightens too"),
                    HoverItem("Me too",    "All via a single Style<Frame> rule")
                )
        );

    private static VisualElement HoverItem(string title, string sub) =>
        new Frame()
            .Classes("hover-item")
            .Background(new Color(34, 34, 46))
            .BorderRadius(6f)
            .Padding(new Thickness(16f, 12f))
            .Width(200f)
            .Content(
                new VStack()
                    .Spacing(4f)
                    .Children(
                        new Label(title)
                            .Classes("hover-label")
                            .Foreground(new Color(130, 130, 155))
                            .FontSize(14f),
                        new Label(sub)
                            .Classes("muted")
                            .Foreground(new Color(130, 130, 155))
                            .FontSize(11f)
                    )
            );

    // -------------------------------------------------------------------------
    // 5. StyleScope.Local
    // -------------------------------------------------------------------------
    private VisualElement BuildIsolatedSection() =>
        Card(
            "StyleScope.Local — Isolated Component",
            "IsolatedCard overrides StyleScope => StyleScope.Local. Its internals have no matching style classes so global rules don't apply.",
            new HStack()
                .Spacing(20f)
                .Children(
                    new IsolatedCard().Width(280f),
                    new VStack()
                        .Spacing(10f)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Children(
                            new Label("How it works:")
                                .Classes("section-title")
                                .Foreground(new Color(230, 230, 245))
                                .FontSize(14f),
                            BulletLabel("Override StyleScope => StyleScope.Local"),
                            BulletLabel("Global theme walk stops at this boundary"),
                            BulletLabel("The component's own BuildStyles() still apply"),
                            BulletLabel("Internal elements have no matching style classes")
                        )
                )
        );

    private static Label BulletLabel(string text) =>
        new Label("• " + text)
            .Classes("muted")
            .Foreground(new Color(130, 130, 155))
            .FontSize(12f);

    // -------------------------------------------------------------------------
    // Shared helpers for the new sections
    // -------------------------------------------------------------------------

    // Small dimmed caption label used above sub-demos within a section.
    private static Label Caption(string text) =>
        new Label(text)
            .Classes("muted")
            .Foreground(new Color(130, 130, 155))
            .FontSize(11f);

    // Pill-shaped chip whose appearance is driven entirely by style classes.
    private static VisualElement OsChip(string chipClass, string labelClass) =>
        new Frame()
            .Classes(chipClass)
            .Padding(new Thickness(16f, 10f))
            .BorderRadius(20f)
            .Content(
                new Label("---")
                    .Classes(labelClass)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
            );

    // Vertical label+content pair used inside the Important demo.
    private static VisualElement LabeledItem(string caption, VisualElement content) =>
        new VStack()
            .Spacing(8f)
            .Children(
                new Label(caption)
                    .Classes("muted")
                    .Foreground(new Color(130, 130, 155))
                    .FontSize(11f),
                content
            );

    // -------------------------------------------------------------------------
    // 6. Responsive breakpoints — named tiers + predicate conditions
    // -------------------------------------------------------------------------
    private VisualElement BuildResponsiveSection() =>
        Card(
            "Responsive — When(Breakpoint.*) and When(Func<float,bool>)",
            "Resize the window to see both bars update live. Named tiers fire at fixed px boundaries; predicates re-evaluate on every pixel.",
            Caption("Named breakpoints — colour and text change at XSmall / Small / Medium / Large / XLarge:"),
            new Frame()
                .Classes("bp-bar")
                .HorizontalAlignment(HorizontalAlignment.Stretch),
            new Label("---")
                .Classes("bp-label"),
            Caption("Custom-predicate breakpoints — bar width grows as the window widens:"),
            new Frame()
                .Classes("pred-bar"),
            new Label("---")
                .Classes("pred-label")
        );

    // -------------------------------------------------------------------------
    // 7. OS conditions — platform, color scheme, orientation
    // -------------------------------------------------------------------------
    private VisualElement BuildOsConditionsSection() =>
        Card(
            "OS Conditions — Platform / Color Scheme / Orientation",
            "When(PlatformType.*) is evaluated once at startup. When(ColorScheme.*) polls the OS every ~5 s. When(Orientation.*) reacts to every window resize.",
            new HStack()
                .Spacing(12f)
                .Children(
                    new VStack()
                        .Spacing(8f)
                        .Children(
                            Caption("Platform"),
                            OsChip("platform-chip", "platform-label")
                        ),
                    new VStack()
                        .Spacing(8f)
                        .Children(
                            Caption("OS Color Scheme"),
                            OsChip("scheme-chip", "scheme-label")
                        ),
                    new VStack()
                        .Spacing(8f)
                        .Children(
                            Caption("Window Orientation"),
                            OsChip("orient-chip", "orient-label")
                        )
                )
        );

    // -------------------------------------------------------------------------
    // 8. Advanced selectors — multi-class, Not(), FirstChild / LastChild
    // -------------------------------------------------------------------------
    private VisualElement BuildAdvancedSelectorsSection() =>
        Card(
            "Advanced Selectors — Multi-class, Not(), FirstChild / LastChild",
            ".adv-btn.accent requires BOTH classes (AND logic, spec 21 > 11). Not() excludes tagged rows. First/LastChild target position.",
            Caption("Multi-class — .adv-btn alone gets secondary colour; .adv-btn.accent promotes to primary:"),
            new HStack()
                .Spacing(12f)
                .Children(
                    new Button().Text(".adv-btn")       .Classes("adv-btn")       .Width(140f),
                    new Button().Text(".adv-btn.accent").Classes("adv-btn accent").Width(140f),
                    new Button().Text(".adv-btn")       .Classes("adv-btn")       .Width(140f)
                ),
            Caption("Not(.excluded) + FirstChild / LastChild on list rows:"),
            new VStack()
                .Spacing(4f)
                .Children(
                    new Label("First row   (FirstChild rule — primary colour)").Classes("list-row"),
                    new Label("Normal row  (Not(.excluded) rule — success colour)").Classes("list-row"),
                    new Label("Excluded row  (has .excluded — Not() skips it)").Classes("list-row excluded"),
                    new Label("Another normal row  (Not(.excluded) — success)").Classes("list-row"),
                    new Label("Last row   (LastChild rule — danger colour)").Classes("list-row")
                )
        );

    // -------------------------------------------------------------------------
    // 9. Important() — override specificity order
    // -------------------------------------------------------------------------
    private VisualElement BuildImportantSection() =>
        Card(
            "Important() — Override Specificity",
            ".imp-base (spec 11) carries Important(). It is applied AFTER .imp-base.imp-override (spec 21), so the lower-specificity rule wins.",
            Caption("Both rules match every element below — Important() inverts the normal winner:"),
            new HStack()
                .Spacing(32f)
                .Children(
                    LabeledItem(
                        "Only .imp-base  (spec 11, Important)",
                        new Label("Danger colour — Important wins")
                            .Classes("imp-base")
                            .FontSize(14f)),
                    LabeledItem(
                        ".imp-base + .imp-override  (spec 21 + 11, Important on 11)",
                        new Label("Still danger — Important overrides the higher-spec green rule")
                            .Classes("imp-base imp-override")
                            .FontSize(14f))
                ),
            Caption("Without Important() the spec-21 rule (.imp-base.imp-override) would render the text green.")
        );
}
