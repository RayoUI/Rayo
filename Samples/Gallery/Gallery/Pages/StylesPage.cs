using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class StylesPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Styles", "CSS-like styling system: StyleSheet, selectors, tokens, pseudo-states"),

                Helper.CreateInfoCard(
                    "StyleSheet",
                    "StyleEngine.Apply(sheet, root) walks the element tree and applies matching rules. " +
                    "Supports type, id (#) and class (.) selectors — similar to CSS specificity."
                ),

                // ── Type Selector ─────────────────────────────────────────────────
                Helper.CreateExampleSection("Type Selector — Style<T>()",
                    Styled(
                        [
                            new Style<Button>()
                                .Background(new Color(59, 130, 246))
                                .Set(b => b.TextColor = Color.White)
                                .Set(b => b.Height = 36)
                                .Set(b => b.BorderRadius = new CornerRadius(8)),

                            new Style<Label>()
                                .Set(l => l.FontSize = 13)
                                .Set(l => l.Foreground = new Color(180, 185, 200)),
                        ],
                        new VStack()
                            .Spacing(10)
                            .Children(
                                new Label("All buttons get Primary color · all labels get 13px secondary tone:"),
                                new HStack()
                                    .Spacing(8)
                                    .Children(
                                        new Button().Text("Save"),
                                        new Button().Text("Cancel"),
                                        new Button().Text("Delete")
                                    )
                            )
                    )
                ),

                // ── ID Selector ────────────────────────────────────────────────────
                Helper.CreateExampleSection("ID Selector — #id",
                    Styled(
                        [
                            new Style<Button>()
                                .Background(new Color(50, 52, 65))
                                .Set(b => b.TextColor = new Color(180, 185, 200))
                                .Set(b => b.Height = 36)
                                .Set(b => b.BorderRadius = new CornerRadius(8)),

                            Style.Id<Button>("submit")
                                .Background(new Color(34, 197, 94))
                                .Set(b => b.TextColor = Color.White),

                            Style.Id<Button>("delete")
                                .Background(new Color(239, 68, 68))
                                .Set(b => b.TextColor = Color.White),
                        ],
                        new HStack()
                            .Spacing(8)
                            .Children(
                                new Button().Text("Back").Id("back"),
                                new Button().Text("Submit").Id("submit"),
                                new Button().Text("Delete").Id("delete")
                            )
                    )
                ),

                // ── Class Selector ─────────────────────────────────────────────────
                Helper.CreateExampleSection("Class Selector — .className",
                    Styled(
                        [
                            new Style<Label>()
                                .Set(l => l.FontSize = 12)
                                .Set(l => l.Foreground = new Color(200, 203, 215))
                                .Set(l => l.Padding = new Thickness(10, 5))
                                .Set(l => l.BorderRadius = new CornerRadius(6)),

                            Style.Class<Label>("primary")
                                .Background(new Color(59, 130, 246))
                                .Set(l => l.Foreground = Color.White)
                                .Set(l => l.FontWeight = FontWeight.SemiBold),

                            Style.Class<Label>("success")
                                .Background(new Color(34, 197, 94))
                                .Set(l => l.Foreground = Color.White),

                            Style.Class<Label>("danger")
                                .Background(new Color(239, 68, 68))
                                .Set(l => l.Foreground = Color.White)
                                .Set(l => l.TextDecorations = TextDecorations.Strikethrough),

                            Style.Class<Label>("muted")
                                .Background(new Color(50, 52, 65))
                                .Set(l => l.Foreground = new Color(120, 125, 140)),
                        ],
                        new HStack()
                            .Spacing(8)
                            .Alignment(Alignment.Center)
                            .Children(
                                new Label("Default"),
                                new Label("Primary").Classes("primary"),
                                new Label("Success").Classes("success"),
                                new Label("Danger").Classes("danger"),
                                new Label("Muted").Classes("muted")
                            )
                    )
                ),

                // ── Design Tokens ─────────────────────────────────────────────────
                Helper.CreateExampleSection("Design Tokens — StyleTokens",
                    BuildTokensDemo()
                ),

                // ── Pseudo-states ──────────────────────────────────────────────────
                Helper.CreateExampleSection("Pseudo-state Triggers — When(StyleTrigger.*)",
                    Styled(
                        [
                            new Style<Button>()
                                .Background(new Color(50, 52, 65))
                                .Set(b => b.TextColor = Color.White)
                                .Set(b => b.Height = 40)
                                .Set(b => b.BorderRadius = new CornerRadius(8))
                                .When(StyleTrigger.Hover,    s => s.Background(new Color(70, 73, 90)))
                                .When(StyleTrigger.Pressed,  s => s.Background(new Color(59, 130, 246)))
                                .When(StyleTrigger.Disabled, s => s.Background(new Color(35, 37, 48))
                                                                   .Set(b => b.TextColor = new Color(80, 83, 95))),
                        ],
                        new VStack()
                            .Spacing(8)
                            .Children(
                                new Label("Hover → lighter · Press → blue · Disabled → dimmed")
                                    .FontSize(12).Foreground(ColorDefault.Secondary),
                                new HStack()
                                    .Spacing(8)
                                    .Children(
                                        new Button().Text("Hover me"),
                                        new Button().Text("Press me"),
                                        new Button().Text("Disabled").IsEnabled(false)
                                    )
                            )
                    )
                ),

                // ── Extend ────────────────────────────────────────────────────────
                Helper.CreateExampleSection("Style Inheritance — Extend(baseStyle)",
                    BuildExtendDemo()
                ),

                // ── ChildOf combinator ─────────────────────────────────────────────
                Helper.CreateExampleSection("Structural Selectors — ChildOf<T> / DescendantOf<T>",
                    BuildCombinatorsDemo()
                ),

                // ── StyleScope ─────────────────────────────────────────────────────
                Helper.CreateExampleSection("StyleScope — Global vs Local",
                    BuildScopesDemo()
                )
            );
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a StyleSheet to the given container and returns it.
    /// </summary>
    private static VisualElement Styled(StyleSheet sheet, VisualElement container)
    {
        StyleEngine.Apply(sheet, container);
        return container;
    }

    // ── Design Tokens demo ────────────────────────────────────────────────────────

    private static VisualElement BuildTokensDemo()
    {
        // Define a token set — like CSS custom properties
        var tokens = new StyleTokens()
            .Set("--color-accent",  new Color(139, 92, 246))
            .Set("--color-surface", new Color(40, 42, 55))
            .Set("--radius",        8f)
            .Set("--spacing",       12f);

        StyleSheet sheet =
        [
            new Style<Button>()
                .Background(tokens.Get<Color>("--color-accent"))
                .Set(b => b.BorderRadius = new CornerRadius(tokens.Get<float>("--radius")))
                .Set(b => b.Height = 36)
                .Set(b => b.TextColor = Color.White),

            new Style<Frame>()
                .Background(tokens.Get<Color>("--color-surface"))
                .Set(f => f.BorderRadius = new CornerRadius(tokens.Get<float>("--radius")))
                .Set(f => f.Padding = new Thickness(tokens.Get<float>("--spacing"))),
        ];

        var container = new VStack()
            .Spacing(8)
            .Children(
                new Label("Tokens: --color-accent=purple · --radius=8 · --spacing=12")
                    .FontSize(11).Foreground(ColorDefault.Secondary),
                new Frame()
                    .Content(
                        new HStack()
                            .Spacing(8)
                            .Children(
                                new Label("Token-styled card").Foreground(Color.White),
                                new Button().Text("Action")
                            )
                    )
            );

        StyleEngine.Apply(sheet, container);
        return container;
    }

    // ── Extend demo ───────────────────────────────────────────────────────────────

    private static VisualElement BuildExtendDemo()
    {
        var btnBase = new Style<Button>()
            .Set(b => b.Height = 36)
            .Set(b => b.BorderRadius = new CornerRadius(8))
            .Set(b => b.TextColor = Color.White);

        StyleSheet sheet =
        [
            new Style<Button>(".primary")
                .Extend(btnBase)
                .Background(new Color(59, 130, 246)),

            new Style<Button>(".success")
                .Extend(btnBase)
                .Background(new Color(34, 197, 94)),

            new Style<Button>(".warning")
                .Extend(btnBase)
                .Background(new Color(245, 158, 11)),

            new Style<Button>(".danger")
                .Extend(btnBase)
                .Background(new Color(239, 68, 68)),
        ];

        var container = new HStack()
            .Spacing(8)
            .Children(
                new Button().Text("Primary").Classes("primary"),
                new Button().Text("Success").Classes("success"),
                new Button().Text("Warning").Classes("warning"),
                new Button().Text("Danger").Classes("danger")
            );

        StyleEngine.Apply(sheet, container);
        return container;
    }

    // ── Combinators demo ──────────────────────────────────────────────────────────

    private static VisualElement BuildCombinatorsDemo()
    {
        // Labels inside the "toolbar" Frame get a different style than those outside
        var toolbarFrame = new Frame()
            .Id("toolbar")
            .Background(new Color(35, 38, 50))
            .BorderRadius(8)
            .Padding(new Thickness(12, 8));

        var outsideLabel  = new Label("Label outside toolbar");
        var insideLabel   = new Label("Label inside toolbar (DescendantOf<Frame>)");
        var outsideLabel2 = new Label("Another label outside");

        toolbarFrame.Content(insideLabel);

        StyleSheet sheet =
        [
            new Style<Label>()
                .Set(l => l.FontSize = 13)
                .Set(l => l.Foreground = new Color(160, 165, 180)),

            new Style<Label>()
                .DescendantOf<Frame>()
                .Set(l => l.FontSize = 13)
                .Set(l => l.Foreground = new Color(59, 130, 246))
                .Set(l => l.FontWeight = FontWeight.SemiBold),
        ];

        var container = new VStack()
            .Spacing(8)
            .Children(outsideLabel, toolbarFrame, outsideLabel2);

        StyleEngine.Apply(sheet, container);
        return container;
    }

    // ── Scope demo ────────────────────────────────────────────────────────────────

    private static VisualElement BuildScopesDemo()
    {
        var globalSheet = new StyleSheet();
        globalSheet.Add(new Style<Label>()
            .Set(l => l.Foreground = new Color(59, 130, 246))
            .Set(l => l.FontWeight = FontWeight.SemiBold));

        var localSheet = new StyleSheet();
        localSheet.Add(new Style<Label>()
            .Set(l => l.Foreground = new Color(34, 197, 94))
            .Set(l => l.FontWeight = FontWeight.SemiBold));

        var globalRoot = new VStack()
            .Spacing(4)
            .Children(
                new Label("StyleScope.Global").FontSize(11).Foreground(ColorDefault.Secondary),
                new Label("Label A (global blue)"),
                new Label("Label B (global blue)")
            );

        var localRoot = new VStack()
            .Spacing(4)
            .Children(
                new Label("StyleScope.Local").FontSize(11).Foreground(ColorDefault.Secondary),
                new Label("Label C (local green)"),
                new Label("Label D (local green)")
            );

        StyleEngine.Apply(globalSheet, globalRoot, StyleScope.Global);
        StyleEngine.Apply(localSheet,  localRoot,  StyleScope.Local);

        return new HStack()
            .Spacing(24)
            .Children(
                new Border()
                    .CornerRadius(new CornerRadius(8))
                    .Background(new Color(35, 38, 50))
                    .BorderBrush(new Color(60, 63, 78))
                    .BorderThickness(new Thickness(1))
                    .Padding(new Thickness(14, 10))
                    .Content(globalRoot),
                new Border()
                    .CornerRadius(new CornerRadius(8))
                    .Background(new Color(35, 38, 50))
                    .BorderBrush(new Color(60, 63, 78))
                    .BorderThickness(new Thickness(1))
                    .Padding(new Thickness(14, 10))
                    .Content(localRoot)
            );
    }
}
