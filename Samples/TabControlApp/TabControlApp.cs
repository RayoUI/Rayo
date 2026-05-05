using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Core.Platform;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;

namespace FluentExamples;

/// <summary>
/// Demonstrates the reactive features added to TabControl:
///   • Items driven by SignalList&lt;TabItem&gt; (dynamic add / remove)
///   • BindSelectedIndex — two-way Signal&lt;int&gt; binding
///   • WithTabHeaderTemplate — fully custom header elements
///   • TabItem.IsEnabled — per-tab enable / disable
/// </summary>
public class TabControlApp : UserControl
{
    // ── Reactive state ────────────────────────────────────────────────────────

    // Two-way binding: changes here switch the active tab; tab clicks update this.
    private readonly Signal<int> _selectedTab = new(0);

    // All tabs are held in a SignalList so adding / removing one
    // automatically rebuilds the tab headers without any manual refresh call.
    private readonly SignalList<TabItem> _tabs = new();

    // Counter used when the user adds new tabs at runtime.
    private int _tabCounter = 0;

    // ── Build ─────────────────────────────────────────────────────────────────

    public override VisualElement Build()
    {
        // Seed the initial tabs.
        _tabs.Add(MakeTab("🏠  Home",     BuildHomeContent()));
        _tabs.Add(MakeTab("⚙  Settings", BuildSettingsContent()));
        _tabs.Add(MakeTab("📊  Data",     BuildDataContent()));
        _tabs.Add(MakeTab("🔒  Locked",   BuildLockedContent(), isEnabled: false));

        _tabCounter = _tabs.Count;

        // Computed label that re-renders whenever _selectedTab or _tabs.Count changes.
        var selectionInfo = new Computed<string>(
            () => $"Active tab: {_selectedTab.Value + 1} / {_tabs.Count}");

        return new VStack()
            .Spacing(0)
            .Children(

                // ── Title bar ─────────────────────────────────────────────────
                new Frame()
                    .Background(new Color(18, 18, 22))
                    .Padding(new Thickness(16, 12))
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Content(
                        new Label()
                            .Text("TabControl — Reactive Features Demo")
                            .FontSize(16)
                            .Foreground(new Color(200, 200, 210))
                    ),

                // ── TabControl (fills remaining space) ────────────────────────
                new TabControl()
                    .Items(_tabs)
                    .ShowTabCloseButtons(true)
                    .BindSelectedIndex(_selectedTab)
                    .WithTabHeaderTemplate((tab, index, isSelected) =>
                        new HStack()
                            .Spacing(4)
                            .Padding(new Thickness(10, 0))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Children(
                                new Label()
                                    .Text(tab.Title)
                                    .FontSize(12)
                                    .Foreground(
                                        !tab.IsEnabled  ? new Color(70, 70, 75)  :
                                        isSelected      ? new Color(255, 255, 255) :
                                                          new Color(150, 150, 160))
                            ))
                    .TabDropIndicatorColor(Color.Red)
                    .TabBackground(new Color(30, 30, 36))
                    .TabActiveBackground(new Color(22, 22, 28))
                    .TabAccentColor(Color.Red)
                    .TabHoverBackground(new Color(50, 50, 55))
                    .TabWidth(130)
                    .TabHeight(40)
                    .ContentBackground(new Color(22, 22, 28))
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch),

                // ── Control panel ─────────────────────────────────────────────
                new Frame()
                    .Background(new Color(15, 15, 20))
                    .Padding(16)
                    .VerticalAlignment(VerticalAlignment.Bottom)
                    .Content(
                        new VStack()
                            .Spacing(10)
                            .Children(

                                // Row 1: dynamic tab management
                                new HStack()
                                    .Spacing(8)
                                    .Children(
                                        new Label()
                                            .Text("Tabs:")
                                            .Foreground(new Color(120, 120, 130))
                                            .FontSize(12)
                                            .VerticalAlignment(VerticalAlignment.Center),

                                        new Button()
                                            .Text("＋ Add Tab")
                                            .Height(30)
                                            .OnTapped(() =>
                                            {
                                                _tabCounter++;
                                                _tabs.Add(MakeTab(
                                                    $"📄  Tab {_tabCounter}",
                                                    BuildDynamicContent(_tabCounter)));
                                            }),

                                        new Button()
                                            .Text("✕ Remove Last")
                                            .Height(30)
                                            .OnTapped(() =>
                                            {
                                                if (_tabs.Count > 1)
                                                    _tabs.RemoveAt(_tabs.Count - 1);
                                            }),

                                        new Button()
                                            .Text("Toggle 🔒 Locked")
                                            .Height(30)
                                            .OnTapped(() =>
                                            {
                                                // Replace the item so SignalList sees the change
                                                // and triggers a header rebuild.
                                                int lockedIdx = IndexOfLocked();
                                                if (lockedIdx < 0) return;
                                                var old = _tabs[lockedIdx];
                                                _tabs[lockedIdx] = MakeTab(
                                                    old.Title, old.Content,
                                                    isEnabled: !old.IsEnabled);
                                            })
                                    ),

                                // Row 2: programmatic navigation (drives _selectedTab directly)
                                new HStack()
                                    .Spacing(8)
                                    .Children(
                                        new Label()
                                            .Text("Navigate:")
                                            .Foreground(new Color(120, 120, 130))
                                            .FontSize(12)
                                            .VerticalAlignment(VerticalAlignment.Center),

                                        new Button()
                                            .Text("◀ Prev")
                                            .Height(30)
                                            .OnTapped(() =>
                                            {
                                                if (_selectedTab.Value > 0)
                                                    _selectedTab.Value--;
                                            }),

                                        // Reactive label — updates automatically via Computed<string>.
                                        new Label()
                                            .Text(selectionInfo)
                                            .FontSize(12)
                                            .Foreground(new Color(100, 180, 255))
                                            .VerticalAlignment(VerticalAlignment.Center),

                                        new Button()
                                            .Text("Next ▶")
                                            .Height(30)
                                            .OnTapped(() =>
                                            {
                                                if (_selectedTab.Value < _tabs.Count - 1)
                                                    _selectedTab.Value++;
                                            })
                                    )
                            )
                    )
            );
    }

    // ── Tab content builders ──────────────────────────────────────────────────

    private static VisualElement BuildHomeContent() =>
        new VStack()
            .Spacing(16)
            .Padding(24)
            .Children(
                new Label()
                    .Text("Home")
                    .FontSize(22)
                    .Foreground(new Color(220, 220, 230)),
                new Label()
                    .Text(
                        "This tab was added via SignalList<TabItem>.\n" +
                        "The header uses WithTabHeaderTemplate — a custom\n" +
                        "HStack with selection-aware text color.")
                    .FontSize(13)
                    .Foreground(new Color(150, 150, 165))
            );

    private static VisualElement BuildSettingsContent() =>
        new VStack()
            .Spacing(16)
            .Padding(24)
            .Children(
                new Label()
                    .Text("Settings")
                    .FontSize(22)
                    .Foreground(new Color(220, 220, 230)),
                new Label()
                    .Text(
                        "Clicking ◀ Prev / Next ▶ in the control panel\n" +
                        "sets _selectedTab.Value directly.\n" +
                        "BindSelectedIndex wires the Signal<int> to\n" +
                        "the control in both directions.")
                    .FontSize(13)
                    .Foreground(new Color(150, 150, 165))
            );

    private static VisualElement BuildDataContent() =>
        new VStack()
            .Spacing(16)
            .Padding(24)
            .Children(
                new Label()
                    .Text("Data")
                    .FontSize(22)
                    .Foreground(new Color(220, 220, 230)),
                new Label()
                    .Text(
                        "The status label below the tabs is a\n" +
                        "Computed<string> that re-evaluates automatically\n" +
                        "whenever the selected index or the tab count changes.")
                    .FontSize(13)
                    .Foreground(new Color(150, 150, 165))
            );

    private static VisualElement BuildLockedContent() =>
        new VStack()
            .Spacing(16)
            .Padding(24)
            .Children(
                new Label()
                    .Text("🔒  Locked")
                    .FontSize(22)
                    .Foreground(new Color(220, 220, 230)),
                new Label()
                    .Text(
                        "This tab starts disabled (TabItem.IsEnabled = false).\n" +
                        "Click 'Toggle 🔒 Locked' to enable or disable it.\n" +
                        "The item is replaced in the SignalList so the\n" +
                        "header template re-runs and updates the text color.")
                    .FontSize(13)
                    .Foreground(new Color(150, 150, 165))
            );

    private static VisualElement BuildDynamicContent(int n) =>
        new VStack()
            .Spacing(16)
            .Padding(24)
            .Children(
                new Label()
                    .Text($"Tab {n}")
                    .FontSize(22)
                    .Foreground(new Color(220, 220, 230)),
                new Label()
                    .Text(
                        $"Dynamically added tab #{n}.\n" +
                        "Clicking '✕ Remove Last' called _tabs.RemoveAt();\n" +
                        "the SignalList subscription triggered a header\n" +
                        "rebuild with no manual refresh needed.")
                    .FontSize(13)
                    .Foreground(new Color(150, 150, 165))
            );

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TabItem MakeTab(string title, VisualElement content, bool isEnabled = true) =>
        new(title, content) { IsEnabled = isEnabled };

    private int IndexOfLocked()
    {
        for (int i = 0; i < _tabs.Count; i++)
            if (_tabs[i].Title.StartsWith("🔒"))
                return i;
        return -1;
    }
}
