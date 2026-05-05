using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Rendering;
using Rayo.Styling;
using Orientation = Rayo.Styling.Orientation;

namespace StyleDemo;

// ---------------------------------------------------------------------------
// Design tokens — the single source of truth for each theme's visual values.
// All style rules reference tokens instead of hard-coded colors.
// ---------------------------------------------------------------------------
public static class AppThemes
{
    // -----------------------------------------------------------------------
    // Token names (constants avoid typos across themes and rules)
    // -----------------------------------------------------------------------
    public const string BgApp         = "--bg-app";
    public const string BgCard        = "--bg-card";
    public const string BgHeader      = "--bg-header";
    public const string Primary       = "--color-primary";
    public const string PrimaryHv     = "--color-primary-hover";
    public const string PrimaryPr     = "--color-primary-pressed";
    public const string Secondary     = "--color-secondary";
    public const string Danger        = "--color-danger";
    public const string DangerHv      = "--color-danger-hover";
    public const string Success       = "--color-success";
    public const string TextMain      = "--text-main";
    public const string TextMuted     = "--text-muted";
    public const string TextOnPrimary = "--text-on-primary";
    public const string Radius        = "--radius";
    public const string RadiusSm      = "--radius-sm";

    // -----------------------------------------------------------------------
    // Dark theme
    // -----------------------------------------------------------------------
    public static StyleSheet Dark()
    {
        var t = new StyleTokens()
            .Set(BgApp,          new Color(22,  22,  30))
            .Set(BgCard,         new Color(34,  34,  46))
            .Set(BgHeader,       new Color(16,  16,  24))
            .Set(Primary,        new Color(99,  130, 246))
            .Set(PrimaryHv,      new Color(122, 151, 255))
            .Set(PrimaryPr,      new Color(75,  103, 210))
            .Set(Secondary,      new Color(55,  55,  72))
            .Set(Danger,         new Color(220, 60,  60))
            .Set(DangerHv,       new Color(240, 85,  85))
            .Set(Success,        new Color(34,  197, 94))
            .Set(TextMain,       new Color(230, 230, 245))
            .Set(TextMuted,      new Color(130, 130, 155))
            .Set(TextOnPrimary,  new Color(255, 255, 255))
            .Set(Radius,         10f)
            .Set(RadiusSm,       6f);

        return BuildSheet(t);
    }

    // -----------------------------------------------------------------------
    // Light theme
    // -----------------------------------------------------------------------
    public static StyleSheet Light()
    {
        var t = new StyleTokens()
            .Set(BgApp,          new Color(242, 242, 248))
            .Set(BgCard,         new Color(255, 255, 255))
            .Set(BgHeader,       new Color(225, 225, 235))
            .Set(Primary,        new Color(59,  98,  220))
            .Set(PrimaryHv,      new Color(45,  80,  200))
            .Set(PrimaryPr,      new Color(30,  60,  175))
            .Set(Secondary,      new Color(210, 210, 225))
            .Set(Danger,         new Color(200, 40,  40))
            .Set(DangerHv,       new Color(225, 60,  60))
            .Set(Success,        new Color(22,  160, 75))
            .Set(TextMain,       new Color(25,  25,  40))
            .Set(TextMuted,      new Color(100, 100, 125))
            .Set(TextOnPrimary,  new Color(255, 255, 255))
            .Set(Radius,         10f)
            .Set(RadiusSm,       6f);

        return BuildSheet(t);
    }

    // -----------------------------------------------------------------------
    // Neon theme
    // -----------------------------------------------------------------------
    public static StyleSheet Neon()
    {
        var t = new StyleTokens()
            .Set(BgApp,          new Color(8,   8,   18))
            .Set(BgCard,         new Color(16,  16,  32))
            .Set(BgHeader,       new Color(5,   5,   15))
            .Set(Primary,        new Color(0,   240, 200))
            .Set(PrimaryHv,      new Color(40,  255, 215))
            .Set(PrimaryPr,      new Color(0,   200, 165))
            .Set(Secondary,      new Color(30,  20,  55))
            .Set(Danger,         new Color(255, 30,  90))
            .Set(DangerHv,       new Color(255, 60,  115))
            .Set(Success,        new Color(0,   255, 150))
            .Set(TextMain,       new Color(220, 220, 255))
            .Set(TextMuted,      new Color(100, 100, 150))
            .Set(TextOnPrimary,  new Color(5,   5,   15))
            .Set(Radius,         4f)
            .Set(RadiusSm,       2f);

        return BuildSheet(t);
    }

    // -----------------------------------------------------------------------
    // Shared rule builder — consumes tokens, emits a StyleSheet.
    //
    // Demonstrates:
    //   • Design tokens referenced from style rules
    //   • @extend  (btnBase extended by .primary, .secondary, .danger, .ghost)
    //   • Specificity (type=1 < class=11 < multi-class=21 < id=101)
    //   • When(StyleTrigger.*) — hover / pressed / disabled pseudo-states
    //   • When(Breakpoint.*)   — named responsive tiers
    //   • When(Func<float,bool>) — custom-predicate responsive conditions
    //   • When(PlatformType.*) — OS platform conditions
    //   • When(ColorScheme.*)  — OS dark/light preference
    //   • When(Orientation.*)  — portrait / landscape
    //   • Not() / FirstChild() / LastChild() structural selectors
    //   • Multi-class selector (.adv-btn.accent)
    //   • Important()          — override specificity
    // -----------------------------------------------------------------------
    private static StyleSheet BuildSheet(StyleTokens t)
    {
        // Shared base style — @extended by all primary button variants
        var btnBase = new Style<Button>()
            .Height(40f)
            .BorderRadius(t.Get<float>(Radius))
            .FontSize(14f)
            .When(StyleTrigger.Disabled, s => s.Opacity(0.38f));

        return
        [
            // ----------------------------------------------------------------
            // Root app background
            // ----------------------------------------------------------------
            new Style<Frame>(".app-root")
                .Background(t.Get<Color>(BgApp)),

            // ----------------------------------------------------------------
            // Header bar
            // ----------------------------------------------------------------
            new Style<Frame>(".header")
                .Background(t.Get<Color>(BgHeader)),

            // ----------------------------------------------------------------
            // Section cards
            // ----------------------------------------------------------------
            new Style<Frame>(".card")
                .Background(t.Get<Color>(BgCard))
                .BorderRadius(t.Get<float>(Radius)),

            // ----------------------------------------------------------------
            // Labels
            // ----------------------------------------------------------------
            new Style<Label>(".title")
                .Foreground(t.Get<Color>(TextMain))
                .FontSize(22f),

            new Style<Label>(".section-title")
                .Foreground(t.Get<Color>(TextMain))
                .FontSize(15f),

            new Style<Label>(".muted")
                .Foreground(t.Get<Color>(TextMuted))
                .FontSize(13f),

            new Style<Label>(".badge")
                .Foreground(t.Get<Color>(TextOnPrimary))
                .Background(t.Get<Color>(Primary))
                .FontSize(12f),

            // ----------------------------------------------------------------
            // Button variants — btnBase @extended, then each class adds colour
            // ----------------------------------------------------------------
            new Style<Button>(".primary")
                .Extend(btnBase)
                .Background(t.Get<Color>(Primary))
                .HoverBackground(t.Get<Color>(PrimaryHv))
                .PressedBackground(t.Get<Color>(PrimaryPr))
                .TextColor(t.Get<Color>(TextOnPrimary)),

            new Style<Button>(".secondary")
                .Extend(btnBase)
                .Background(t.Get<Color>(Secondary))
                .HoverBackground(t.Get<Color>(BgHeader))
                .PressedBackground(t.Get<Color>(BgApp))
                .TextColor(t.Get<Color>(TextMain)),

            new Style<Button>(".danger")
                .Extend(btnBase)
                .Background(t.Get<Color>(Danger))
                .HoverBackground(t.Get<Color>(DangerHv))
                .PressedBackground(t.Get<Color>(Danger))
                .TextColor(t.Get<Color>(TextOnPrimary)),

            new Style<Button>(".success")
                .Extend(btnBase)
                .Background(t.Get<Color>(Success))
                .HoverBackground(t.Get<Color>(Success))
                .PressedBackground(t.Get<Color>(Success))
                .TextColor(t.Get<Color>(TextOnPrimary))
                .Opacity(0.9f),

            new Style<Button>(".ghost")
                .Extend(btnBase)
                .Background(new Color(0, 0, 0, 0))
                .HoverBackground(t.Get<Color>(BgHeader))
                .PressedBackground(t.Get<Color>(Secondary))
                .TextColor(t.Get<Color>(Primary))
                .BorderWidth(1f)
                .BorderColor(t.Get<Color>(Primary)),

            new Style<Button>(".disabled-demo")
                .Extend(btnBase)
                .Background(t.Get<Color>(Primary))
                .TextColor(t.Get<Color>(TextOnPrimary)),

            // ----------------------------------------------------------------
            // Theme-switcher buttons in the header
            // ----------------------------------------------------------------
            new Style<Button>(".theme-btn")
                .Height(32f)
                .FontSize(12f)
                .BorderRadius(t.Get<float>(RadiusSm))
                .Background(t.Get<Color>(Secondary))
                .HoverBackground(t.Get<Color>(Primary))
                .PressedBackground(t.Get<Color>(PrimaryPr))
                .TextColor(t.Get<Color>(TextMain)),

            // id selector (spec 101) overrides the .theme-btn class rule (spec 11)
            new Style<Button>("#theme-active")
                .Background(t.Get<Color>(Primary))
                .TextColor(t.Get<Color>(TextOnPrimary)),

            // ----------------------------------------------------------------
            // Hover-card: When(StyleTrigger.Hover) on Frame + Label
            // ----------------------------------------------------------------
            new Style<Frame>(".hover-item")
                .Background(t.Get<Color>(BgCard))
                .BorderRadius(t.Get<float>(RadiusSm))
                .When(StyleTrigger.Hover, s =>
                    s.Background(t.Get<Color>(Secondary))),

            new Style<Label>(".hover-label")
                .Foreground(t.Get<Color>(TextMuted))
                .FontSize(13f)
                .When(StyleTrigger.Hover, s =>
                    s.Foreground(t.Get<Color>(TextMain))),

            // ----------------------------------------------------------------
            // SECTION 6 — Named breakpoints
            // Re-applied automatically when the window crosses a px threshold.
            // ----------------------------------------------------------------
            new Style<Frame>(".bp-bar")
                .Height(16f)
                .BorderRadius(t.Get<float>(RadiusSm))
                .Background(t.Get<Color>(Danger))
                .When(Breakpoint.Small,  s => s.Background(t.Get<Color>(TextMuted)))
                .When(Breakpoint.Medium, s => s.Background(t.Get<Color>(Primary)))
                .When(Breakpoint.Large,  s => s.Background(t.Get<Color>(Success)))
                .When(Breakpoint.XLarge, s => s.Background(t.Get<Color>(PrimaryHv))),

            new Style<Label>(".bp-label")
                .Foreground(t.Get<Color>(Danger))
                .FontSize(13f)
                .Text("XSmall  (< 480 px)")
                .When(Breakpoint.Small,  s => s.Foreground(t.Get<Color>(TextMuted)).Text("Small  (480–767 px)"))
                .When(Breakpoint.Medium, s => s.Foreground(t.Get<Color>(Primary)).Text("Medium  (768–1023 px)"))
                .When(Breakpoint.Large,  s => s.Foreground(t.Get<Color>(Success)).Text("Large  (1024–1439 px)"))
                .When(Breakpoint.XLarge, s => s.Foreground(t.Get<Color>(PrimaryHv)).Text("XLarge  (≥ 1440 px)")),

            // ----------------------------------------------------------------
            // SECTION 6 — Predicate breakpoints
            // Re-evaluated on every pixel change, not just at named thresholds.
            // ----------------------------------------------------------------
            new Style<Frame>(".pred-bar")
                .Height(16f)
                .Width(60f)
                .BorderRadius(t.Get<float>(RadiusSm))
                .Background(t.Get<Color>(Danger))
                .When(w => w >= 500,  s => s.Background(t.Get<Color>(Primary)).Width(160f))
                .When(w => w >= 800,  s => s.Background(t.Get<Color>(Success)).Width(280f))
                .When(w => w >= 1200, s => s.Background(t.Get<Color>(PrimaryHv))
                                            .Width(float.NaN)
                                            .HorizontalAlignment(HorizontalAlignment.Stretch)),

            new Style<Label>(".pred-label")
                .Foreground(t.Get<Color>(Danger))
                .FontSize(13f)
                .Text("Compact  (< 500 px)")
                .When(w => w >= 500,  s => s.Foreground(t.Get<Color>(Primary)).Text("Medium  (≥ 500 px)"))
                .When(w => w >= 800,  s => s.Foreground(t.Get<Color>(Success)).Text("Wide  (≥ 800 px)"))
                .When(w => w >= 1200, s => s.Foreground(t.Get<Color>(PrimaryHv)).Text("Full  (≥ 1200 px)")),

            // ----------------------------------------------------------------
            // SECTION 7 — Platform
            // Evaluated once at startup; the OS never changes at runtime.
            // ----------------------------------------------------------------
            new Style<Frame>(".platform-chip")
                .BorderRadius(20f)
                .Padding(new Thickness(14f, 8f))
                .Background(t.Get<Color>(Secondary))
                .When(PlatformType.Windows, s => s.Background(new Color(0,   120, 212)))
                .When(PlatformType.MacOS,   s => s.Background(t.Get<Color>(Success)))
                .When(PlatformType.Linux,   s => s.Background(t.Get<Color>(Danger)))
                .When(PlatformType.Android, s => s.Background(new Color(61,  220, 132)))
                .When(PlatformType.iOS,     s => s.Background(new Color(90,  90,  110))),

            new Style<Label>(".platform-label")
                .Foreground(t.Get<Color>(TextMuted))
                .FontSize(13f)
                .Text("Unknown")
                .When(PlatformType.Windows, s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("Windows"))
                .When(PlatformType.MacOS,   s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("macOS"))
                .When(PlatformType.Linux,   s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("Linux"))
                .When(PlatformType.Android, s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("Android"))
                .When(PlatformType.iOS,     s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("iOS")),

            // ----------------------------------------------------------------
            // SECTION 7 — OS color scheme
            // Polled every ~5 s; re-applies when dark/light preference changes.
            // ----------------------------------------------------------------
            new Style<Frame>(".scheme-chip")
                .BorderRadius(20f)
                .Padding(new Thickness(14f, 8f))
                .Background(t.Get<Color>(Secondary))
                .When(ColorScheme.Dark,  s => s.Background(new Color(30,  30,  55)))
                .When(ColorScheme.Light, s => s.Background(new Color(225, 230, 255))),

            new Style<Label>(".scheme-label")
                .Foreground(t.Get<Color>(TextMuted))
                .FontSize(13f)
                .Text("Detecting…")
                .When(ColorScheme.Dark,  s => s.Foreground(new Color(140, 160, 255)).Text("OS: Dark mode"))
                .When(ColorScheme.Light, s => s.Foreground(new Color(40,  70,  200)).Text("OS: Light mode")),

            // ----------------------------------------------------------------
            // SECTION 7 — Orientation
            // Re-evaluated on every resize; Portrait when height > width.
            // ----------------------------------------------------------------
            new Style<Frame>(".orient-chip")
                .BorderRadius(20f)
                .Padding(new Thickness(14f, 8f))
                .Background(t.Get<Color>(Secondary))
                .When(Orientation.Portrait,  s => s.Background(t.Get<Color>(Success)))
                .When(Orientation.Landscape, s => s.Background(t.Get<Color>(Primary))),

            new Style<Label>(".orient-label")
                .Foreground(t.Get<Color>(TextMuted))
                .FontSize(13f)
                .Text("Landscape")
                .When(Orientation.Portrait,  s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("Portrait  (h > w)"))
                .When(Orientation.Landscape, s => s.Foreground(t.Get<Color>(TextOnPrimary)).Text("Landscape  (w ≥ h)")),

            // ----------------------------------------------------------------
            // SECTION 8 — Multi-class selector
            // .adv-btn.accent requires BOTH classes to match (AND logic).
            // Specificity: .adv-btn = 11, .adv-btn.accent = 21 → wins on conflict.
            // ----------------------------------------------------------------
            new Style<Button>(".adv-btn")
                .Height(40f)
                .BorderRadius(t.Get<float>(RadiusSm))
                .FontSize(14f)
                .Background(t.Get<Color>(Secondary))
                .TextColor(t.Get<Color>(TextMain))
                .When(StyleTrigger.Hover, s => s.Background(t.Get<Color>(BgHeader))),

            new Style<Button>(".adv-btn.accent")      // both classes required — spec 21
                .Background(t.Get<Color>(Primary))
                .TextColor(t.Get<Color>(TextOnPrimary))
                .Height(52f)
                .BorderRadius(t.Get<float>(Radius))
                .When(StyleTrigger.Hover, s => s.Background(t.Get<Color>(PrimaryHv))),

            // ----------------------------------------------------------------
            // SECTION 8 — Not() + FirstChild / LastChild
            // ----------------------------------------------------------------
            // Base style for all list rows
            new Style<Label>(".list-row")
                .Foreground(t.Get<Color>(TextMuted))
                .FontSize(13f),

            // Green for every .list-row that does NOT also have .excluded
            new Style<Label>(".list-row").Not(".excluded")
                .Foreground(t.Get<Color>(Success)),

            // First child overrides the Not() green with primary blue (spec 11, applied later)
            new Style<Label>(".list-row").FirstChild()
                .Foreground(t.Get<Color>(Primary))
                .FontSize(14f),

            // Last child overrides with danger red
            new Style<Label>(".list-row").LastChild()
                .Foreground(t.Get<Color>(Danger)),

            // ----------------------------------------------------------------
            // SECTION 9 — Important()
            // .imp-base (spec 11) is marked Important() so it is applied AFTER
            // .imp-base.imp-override (spec 21). The lower-specificity rule wins.
            // ----------------------------------------------------------------
            new Style<Label>(".imp-base")
                .Foreground(t.Get<Color>(Danger))
                .Important(),

            new Style<Label>(".imp-base.imp-override")  // spec 21 — normally wins, but doesn't here
                .Foreground(t.Get<Color>(Success)),
        ];
    }
}
