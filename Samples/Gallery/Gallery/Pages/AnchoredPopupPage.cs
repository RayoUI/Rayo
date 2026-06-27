using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;
using Rayo.Styling;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class AnchoredPopupPage : Component
{
    private AnchoredPopup? _activePopup;

    protected override void OnDispose()
    {
        _activePopup?.Close();
        base.OnDispose();
    }

    public override VisualElement Build()
    {
        var autoButton = CreateTrigger("Open automatic popup");
        autoButton.OnTapped(() => OpenExample(autoButton, "Automatic placement"));

        var belowButton = CreateTrigger("Below");
        belowButton.OnTapped(() => OpenExample(
            belowButton,
            "Forced below",
            AnchoredPopupPlacement.Below));

        var aboveButton = CreateTrigger("Above");
        aboveButton.OnTapped(() => OpenExample(
            aboveButton,
            "Forced above",
            AnchoredPopupPlacement.Above));

        var startButton = CreateTrigger("Start");
        startButton.OnTapped(() => OpenExample(
            startButton,
            "Start aligned",
            alignment: AnchoredPopupAlignment.Start));

        var centerButton = CreateTrigger("Center");
        centerButton.OnTapped(() => OpenExample(
            centerButton,
            "Centered",
            alignment: AnchoredPopupAlignment.Center));

        var endButton = CreateTrigger("End");
        endButton.OnTapped(() => OpenExample(
            endButton,
            "End aligned",
            alignment: AnchoredPopupAlignment.End));

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader(
                    "AnchoredPopup",
                    "Reusable overlay content positioned relative to any visual element"),

                Helper.CreateExampleSection(
                    "Automatic Placement",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("The popup opens below the trigger and flips above when space is limited.")
                                .FontSize(13)
                                .Foreground(ColorDefault.Secondary),
                            autoButton)),

                Helper.CreateExampleSection(
                    "Placement",
                    new HStack()
                        .Spacing(12)
                        .Wrap(true)
                        .Children(belowButton, aboveButton)),

                Helper.CreateExampleSection(
                    "Anchor Alignment",
                    new HStack()
                        .Spacing(12)
                        .Wrap(true)
                        .Children(startButton, centerButton, endButton))
            );
    }

    private void OpenExample(
        VisualElement anchor,
        string title,
        AnchoredPopupPlacement placement = AnchoredPopupPlacement.Auto,
        AnchoredPopupAlignment alignment = AnchoredPopupAlignment.Start)
    {
        _activePopup?.Close();

        var closeButton = new Button()
            .Text("Close")
            .Width(90)
            .Height(34)
            .Variant(ButtonVariant.Primary);
        closeButton.OnTapped(() => _activePopup?.Close());

        var content = new Frame()
            .Width(240)
            .Background(RayoThemes.Current.Colors.Surface)
            .BorderBrush(RayoThemes.Current.Colors.Border)
            .BorderThickness(1)
            .BorderRadius(new CornerRadius(10))
            .Padding(new Thickness(14))
            .Content(
                new VStack()
                    .Spacing(10)
                    .Children(
                        new Label(title)
                            .FontSize(15)
                            .Foreground(RayoThemes.Current.Colors.OnSurface),
                        new Label("This can contain any Rayo visual tree.")
                            .FontSize(12)
                            .Foreground(RayoThemes.Current.Colors.OnDisabled),
                        closeButton));

        _activePopup = AnchoredPopup.Show(anchor, content, popup =>
        {
            popup.Placement = placement;
            popup.AnchorAlignment = alignment;
            popup.Gap = 8;
        });
    }

    private static Button CreateTrigger(string text)
    {
        return new Button()
            .Text(text)
            .Width(170)
            .Height(38)
            .Variant(ButtonVariant.Secondary);
    }
}
