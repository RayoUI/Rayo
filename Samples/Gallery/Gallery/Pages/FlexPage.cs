using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Rendering;

namespace Gallery.Pages;

public class FlexPage : UserControl
{
    public override VisualElement Build()
    {
        return
            new VStack()
                .Spacing(20)
                .Padding(new Thickness(20))
                .Children(
                    Helper.CreatePageHeader("Flex", "CSS Flexbox-style layout with flexible rows and columns"),
                    CreateDirectionSection(),
                    CreateWrapSection(),
                    CreateJustifyContentSection(),
                    CreateAlignItemsSection(),
                    CreateGrowShrinkSection(),
                    CreateComplexExample()
                );
    }

    private VisualElement CreateDirectionSection()
    {
        var flex1 = new Flex().Direction(FlexDirection.Row).Gap(8);
        flex1.AddChild(CreateFlexItem("1", new Color(59, 130, 246), 60, 60));
        flex1.AddChild(CreateFlexItem("2", new Color(34, 197, 94), 60, 60));
        flex1.AddChild(CreateFlexItem("3", new Color(234, 179, 8), 60, 60));

        var flex2 = new Flex().Direction(FlexDirection.Column).Gap(8);
        flex2.AddChild(CreateFlexItem("1", new Color(59, 130, 246), 60, 50));
        flex2.AddChild(CreateFlexItem("2", new Color(34, 197, 94), 60, 50));
        flex2.AddChild(CreateFlexItem("3", new Color(234, 179, 8), 60, 50));

        var flex3 = new Flex().Direction(FlexDirection.RowReverse).Gap(8);
        flex3.AddChild(CreateFlexItem("1", new Color(59, 130, 246), 60, 60));
        flex3.AddChild(CreateFlexItem("2", new Color(34, 197, 94), 60, 60));
        flex3.AddChild(CreateFlexItem("3", new Color(234, 179, 8), 60, 60));

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Direction"),
                Helper.CreateExampleSection("Row (left to right)",
                    new Frame().Height(80).Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex1)
                ),
                Helper.CreateExampleSection("Column (top to bottom)",
                    new Frame().Height(200).Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex2)
                ),
                Helper.CreateExampleSection("Row Reverse",
                    new Frame().Height(80).Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex3)
                )
            );
    }

    private VisualElement CreateWrapSection()
    {
        var flex1 = new Flex().Direction(FlexDirection.Row).Wrap(FlexWrap.NoWrap).Gap(8).AlignItems(Alignment.Start);
        flex1.AddChild(CreateFlexItem("1", new Color(59, 130, 246), 60, 60));
        flex1.AddChild(CreateFlexItem("2", new Color(34, 197, 94), 60, 60));
        flex1.AddChild(CreateFlexItem("3", new Color(234, 179, 8), 60, 60));
        flex1.AddChild(CreateFlexItem("4", new Color(239, 68, 68), 60, 60));
        flex1.AddChild(CreateFlexItem("5", new Color(168, 85, 247), 60, 60));
        flex1.AddChild(CreateFlexItem("6", new Color(236, 72, 153), 60, 60));
        flex1.AddChild(CreateFlexItem("7", new Color(59, 130, 246), 60, 60));
        flex1.AddChild(CreateFlexItem("8", new Color(34, 197, 94), 60, 60));
        flex1.AddChild(CreateFlexItem("9", new Color(234, 179, 8), 60, 60));
        flex1.AddChild(CreateFlexItem("10", new Color(239, 68, 68), 60, 60));

        var flex2 = new Flex().Direction(FlexDirection.Row).Wrap(FlexWrap.Wrap).Gap(8).RowGap(8).AlignItems(Alignment.Start);
        flex2.AddChild(CreateFlexItem("1", new Color(59, 130, 246), 60, 60));
        flex2.AddChild(CreateFlexItem("2", new Color(34, 197, 94), 60, 60));
        flex2.AddChild(CreateFlexItem("3", new Color(234, 179, 8), 60, 60));
        flex2.AddChild(CreateFlexItem("4", new Color(239, 68, 68), 60, 60));
        flex2.AddChild(CreateFlexItem("5", new Color(168, 85, 247), 60, 60));
        flex2.AddChild(CreateFlexItem("6", new Color(236, 72, 153), 60, 60));
        flex2.AddChild(CreateFlexItem("7", new Color(59, 130, 246), 60, 60));
        flex2.AddChild(CreateFlexItem("8", new Color(34, 197, 94), 60, 60));
        flex2.AddChild(CreateFlexItem("9", new Color(234, 179, 8), 60, 60));
        flex2.AddChild(CreateFlexItem("10", new Color(239, 68, 68), 60, 60));
        flex2.AddChild(CreateFlexItem("11", new Color(59, 130, 246), 60, 60));

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Wrap"),
                Helper.CreateExampleSection("NoWrap (overflow)",
                    new Frame().Height(80).Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex1)
                ),
                Helper.CreateExampleSection("Wrap (multiple lines)",
                    new Frame().Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex2)
                )
            );
    }

    private VisualElement CreateJustifyContentSection()
    {
        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("JustifyContent"),
                Helper.CreateExampleSection("Start", CreateJustifyExample(JustifyContent.Start)),
                Helper.CreateExampleSection("Center", CreateJustifyExample(JustifyContent.Center)),
                Helper.CreateExampleSection("End", CreateJustifyExample(JustifyContent.End)),
                Helper.CreateExampleSection("SpaceBetween", CreateJustifyExample(JustifyContent.SpaceBetween)),
                Helper.CreateExampleSection("SpaceAround", CreateJustifyExample(JustifyContent.SpaceAround)),
                Helper.CreateExampleSection("SpaceEvenly", CreateJustifyExample(JustifyContent.SpaceEvenly))
            );
    }

    private VisualElement CreateJustifyExample(JustifyContent justify)
    {
        var flex = new Flex().Direction(FlexDirection.Row).JustifyContent(justify);
        flex.AddChild(CreateFlexItem("A", new Color(59, 130, 246), 60, 60));
        flex.AddChild(CreateFlexItem("B", new Color(34, 197, 94), 60, 60));
        flex.AddChild(CreateFlexItem("C", new Color(234, 179, 8), 60, 60));

        return new Frame()
            .Height(80)
            .Background(new Color(30, 30, 35))
            .Padding(new Thickness(10))
            .Content(flex);
    }

    private VisualElement CreateAlignItemsSection()
    {
        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("AlignItems"),
                Helper.CreateExampleSection("Start", CreateAlignExample(Rayo.Layout.Alignment.Start)),
                Helper.CreateExampleSection("Center", CreateAlignExample(Rayo.Layout.Alignment.Center)),
                Helper.CreateExampleSection("End", CreateAlignExample(Rayo.Layout.Alignment.End)),
                Helper.CreateExampleSection("Stretch", CreateAlignExample(Rayo.Layout.Alignment.Stretch))
            );
    }

    private VisualElement CreateAlignExample(Rayo.Layout.Alignment align)
    {
        var flex = new Flex().Direction(FlexDirection.Row).AlignItems(align).Gap(8);
        flex.AddChild(CreateFlexItem("40", new Color(59, 130, 246), 60, 40));
        flex.AddChild(CreateFlexItem("60", new Color(34, 197, 94), 60, 60));
        flex.AddChild(CreateFlexItem("80", new Color(234, 179, 8), 60, 80));

        return new Frame()
            .Height(120)
            .Background(new Color(30, 30, 35))
            .Padding(new Thickness(10))
            .Content(flex);
    }

    private VisualElement CreateGrowShrinkSection()
    {
        var flex1 = new Flex().Direction(FlexDirection.Row).Gap(8);
        var item1 = CreateFlexItem("Grow: 0", new Color(59, 130, 246), 100, 60);
        var item2 = CreateFlexItem("Grow: 1", new Color(34, 197, 94), 100, 60);
        var item3 = CreateFlexItem("Grow: 2", new Color(234, 179, 8), 100, 60);
        flex1.AddChild(item1);
        flex1.Grow(item1, 0);
        flex1.AddChild(item2);
        flex1.Grow(item2, 1);
        flex1.AddChild(item3);
        flex1.Grow(item3, 2);

        var flex2 = new Flex().Direction(FlexDirection.Row).Gap(8);
        var orderItem1 = CreateFlexItem("Order: 3", new Color(59, 130, 246), 80, 60);
        var orderItem2 = CreateFlexItem("Order: 1", new Color(34, 197, 94), 80, 60);
        var orderItem3 = CreateFlexItem("Order: 2", new Color(234, 179, 8), 80, 60);
        flex2.AddChild(orderItem1);
        flex2.Order(orderItem1, 3);
        flex2.AddChild(orderItem2);
        flex2.Order(orderItem2, 1);
        flex2.AddChild(orderItem3);
        flex2.Order(orderItem3, 2);

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Grow & Shrink"),
                Helper.CreateExampleSection("Flex Grow (0, 1, 2)",
                    new Frame().Height(80).Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex1)
                ),
                Helper.CreateExampleSection("Flex Order (visual order: 2, 3, 1)",
                    new Frame().Height(80).Background(new Color(30, 30, 35)).Padding(new Thickness(10)).Content(flex2)
                )
            );
    }

    private VisualElement CreateComplexExample()
    {
        var headerFlex = new Flex()
            .Direction(FlexDirection.Row)
            .JustifyContent(JustifyContent.SpaceBetween)
            .AlignItems(Rayo.Layout.Alignment.Center)
            .Padding(new Thickness(15));

        headerFlex.AddChild(new Label("Flex App").FontSize(18).Foreground(Color.White));
        headerFlex.AddChild(
            new HStack().Spacing(8).Children(
                CreateIconButton("⚙", new Color(100, 100, 120)),
                CreateIconButton("👤", new Color(100, 100, 120))
            )
        );

        var contentFlex = new Flex()
            .Direction(FlexDirection.Row)
            .Wrap(FlexWrap.Wrap)
            .Gap(12)
            .RowGap(12)
            .Padding(new Thickness(15));

        for (int i = 1; i <= 6; i++)
        {
            var card = CreateCard($"Card {i}", new Color(45, 55, 72));
            contentFlex.AddChild(card);
            contentFlex.Grow(card, 1);
            contentFlex.Basis(card, 150);
        }

        return new VStack()
            .Spacing(15)
            .Children(
                Helper.CreateSectionTitle("Complex Example"),
                Helper.CreateExampleSection("Responsive Card Layout",
                    new Frame()
                        .Height(300)
                        .Background(new Color(30, 30, 35))
                        .Content(
                            new VStack().Children(
                                new Frame().Background(new Color(20, 20, 25)).Height(60).Content(headerFlex),
                                new ScrollView(contentFlex).Background(new Color(30, 30, 35))
                            )
                        )
                )
            );
    }

    private VisualElement CreateFlexItem(string text, Color color, float width, float height)
    {
        return new Frame()
            .Width(width)
            .Height(height)
            .Background(color)
            .BorderRadius(6)
            .Content(
                new Label(text)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(Color.White)
                    .FontSize(14)
            );
    }

    private VisualElement CreateCard(string title, Color color)
    {
        return new Frame()
            .Width(150)
            .Height(100)
            .Background(color)
            .BorderRadius(8)
            .Padding(new Thickness(15))
            .Content(
                new VStack().Spacing(8).Children(
                    new Label(title).FontSize(14).Foreground(Color.White),
                    new Label("Lorem ipsum dolor sit amet").FontSize(11).Foreground(new Color(200, 200, 200))
                )
            );
    }

    private VisualElement CreateIconButton(string icon, Color color)
    {
        return new Frame()
            .Width(36)
            .Height(36)
            .Background(color)
            .BorderRadius(18)
            .Content(
                new Label(icon)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .TextHorizontalAlignment(HorizontalAlignment.Center)
                    .TextVerticalAlignment(VerticalAlignment.Center)
                    .Foreground(Color.White)
                    .FontSize(16)
            );
    }
}
