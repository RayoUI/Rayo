using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class DialogPage : Component
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Dialog", "Modal windows for messages and custom content"),

                Helper.CreateExampleSection("Message Dialogs",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Simple modal dialogs with title, message and automatic overlay.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateDialogButton(
                                        "About",
                                        ColorDefault.Primary,
                                        () => Dialog.Show(
                                            "About Rayo",
                                            "Version 1.0\n\nBuilt with Rayo Framework\n.NET 10 + OpenGL")),

                                    CreateDialogButton(
                                        "Success",
                                        ColorDefault.Success,
                                        () => Dialog.Show(
                                            "Build completed",
                                            "The Gallery sample compiled successfully.\n\n0 errors\n0 warnings")),

                                    CreateDialogButton(
                                        "Warning",
                                        ColorDefault.Warning,
                                        () => Dialog.Show(
                                            "Unsaved changes",
                                            "You have local changes that have not been saved yet.\n\nSave your work before switching pages.")),

                                    CreateDialogButton(
                                        "Error",
                                        ColorDefault.Danger,
                                        () => Dialog.Show(
                                            "Connection failed",
                                            "The remote service did not respond.\n\nCheck your network connection and try again."))
                                )
                        )
                ),

                Helper.CreateExampleSection("Custom Content",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Dialogs can host any VisualElement as body content.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateDialogButton(
                                        "Profile Card",
                                        new Color(59, 130, 246),
                                        () => ShowCustomDialog("User profile", CreateProfileContent())),

                                    CreateDialogButton(
                                        "Preferences",
                                        new Color(34, 197, 94),
                                        () => ShowCustomDialog("Notification preferences", CreatePreferencesContent())),

                                    CreateDialogButton(
                                        "Deployment",
                                        new Color(168, 85, 247),
                                        () => ShowCustomDialog("Deployment status", CreateDeploymentContent()))
                                )
                        )
                ),

                Helper.CreateExampleSection("Cancel and Validation",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Use Cancel for dismissing work, and validation to keep the dialog open until input is valid.")
                                .FontSize(14)
                                .Foreground(ColorDefault.Secondary),

                            new HStack()
                                .Spacing(12)
                                .Children(
                                    CreateDialogButton(
                                        "Cancelable Dialog",
                                        new Color(100, 116, 139),
                                        () => Dialog.Show(
                                            "Delete draft?",
                                            "This action can be canceled before anything changes.",
                                            showCancelButton: true,
                                            okText: "Delete",
                                            cancelText: "Cancel")),

                                    CreateDialogButton(
                                        "Validation",
                                        new Color(234, 88, 12),
                                        ShowValidationDialog)
                                )
                        )
                ),

                Helper.CreateExampleSection("Layout Variations",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            CreateDialogButton(
                                "Long Message",
                                new Color(20, 184, 166),
                                () => Dialog.Show(
                                    "Release notes",
                                    "This dialog demonstrates a longer\n" +
                                    "multiline message.\n\n" +
                                    "Dialogs are centered over a dimmed\n" +
                                    "overlay, while the current page stays\n" +
                                    "visible behind them.\n\n" +
                                    "Use this pattern for confirmations,\n" +
                                    "short explanations, alerts and focused\n" +
                                    "tasks.")),

                            CreateDialogButton(
                                "Checklist Summary",
                                new Color(249, 115, 22),
                                () => ShowCustomDialog("Review before publishing", CreateChecklistContent()))
                        )
                ),

                Helper.CreateExampleSection("Features",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            CreateFeatureItem("Modal overlay with dimmed background"),
                            CreateFeatureItem("Centered dialog frame with title, content and OK action"),
                            CreateFeatureItem("Optional Cancel action for dismissing without accepting"),
                            CreateFeatureItem("Optional validation hook can block OK until input is valid"),
                            CreateFeatureItem("Supports simple text messages and custom VisualElement content"),
                            CreateFeatureItem("Works with multiline content and nested layouts"),
                            CreateFeatureItem("Overlay is removed automatically when the OK button is tapped")
                        )
                )
            );
    }

    private static Button CreateDialogButton(string text, Color background, System.Action onTapped)
    {
        return new Button()
            .Text(text)
            .Background(background)
            .HoverBackground(Lighten(background, 24))
            .PressedBackground(Darken(background, 24))
            .TextColor(Color.White)
            .BorderThickness(0)
            .BorderRadius(6)
            .Padding(new Thickness(16, 10, 16, 10))
            .OnTapped(onTapped);
    }

    private static void ShowCustomDialog(string title, VisualElement content)
    {
        Dialog.Show(title, content);
    }

    private static VisualElement CreateProfileContent()
    {
        return new VStack()
            .Spacing(12)
            .Children(
                new HStack()
                    .Spacing(12)
                    .Alignment(Alignment.Center)
                    .Children(
                        new Frame()
                            .Width(48)
                            .Height(48)
                            .BorderRadius(24)
                            .Background(new Color(59, 130, 246))
                            .Content(
                                new Label("AR")
                                    .FontSize(16)
                                    .Foreground(Color.White)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center)),

                        new VStack()
                            .Spacing(4)
                            .Children(
                                new Label("Ana Ramos")
                                    .FontSize(16)
                                    .Foreground(Color.White),
                                new Label("Product designer")
                                    .FontSize(13)
                                    .Foreground(ColorDefault.Secondary)
                            )
                    ),

                new Label("Last active: 4 minutes ago\nRole: Editor\nStatus: Ready for review")
                    .FontSize(14)
                    .Foreground(new Color(205, 210, 220))
            );
    }

    private static VisualElement CreatePreferencesContent()
    {
        return new VStack()
            .Spacing(12)
            .Children(
                new Label("Choose which updates should appear as modal prompts.")
                    .FontSize(14)
                    .Foreground(ColorDefault.Secondary),

                new Checkbox("Build failures"),
                new Checkbox("Deployment approvals"),
                new Checkbox("Weekly usage summary"),

                new Entry("team@rayoui.dev")
                    .Height(40)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
            );
    }

    private static VisualElement CreateDeploymentContent()
    {
        return new VStack()
            .Spacing(12)
            .Children(
                new Label("Publishing Gallery preview")
                    .FontSize(15)
                    .Foreground(Color.White),

                new ProgressBar()
                    .Value(72)
                    .ForegroundColor(ColorDefault.Success)
                    .BarHeight(8)
                    .CornerRadius(4),

                new HStack()
                    .Spacing(8)
                    .Children(
                        CreateStatusBadge("Assets", ColorDefault.Success),
                        CreateStatusBadge("Tests", ColorDefault.Success),
                        CreateStatusBadge("Upload", ColorDefault.Warning)
                    ),

                new Label("Current step: uploading packaged assets.")
                    .FontSize(13)
                    .Foreground(ColorDefault.Secondary)
            );
    }

    private static VisualElement CreateChecklistContent()
    {
        return new VStack()
            .Spacing(10)
            .Children(
                CreateFeatureItem("Title and message are readable"),
                CreateFeatureItem("Primary action closes the overlay"),
                CreateFeatureItem("Custom content remains aligned inside the dialog"),
                CreateFeatureItem("Background page remains visible but inactive")
            );
    }

    private static void ShowValidationDialog()
    {
        var projectName = new Signal<string>("");
        var error = new Signal<string>("Enter at least 3 characters.");

        var content = new VStack()
            .Spacing(12)
            .Children(
                new Label("Project name")
                    .FontSize(14)
                    .Foreground(Color.White),

                new Entry()
                    .Height(40)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Placeholder("Example: Rayo Gallery")
                    .Text(projectName.Value)
                    .OnTextChanged(text =>
                    {
                        projectName.Value = text;
                        error.Value = text.Trim().Length >= 3
                            ? "Ready to create."
                            : "Enter at least 3 characters.";
                    }),

                new Label()
                    .Text(error)
                    .FontSize(13)
                    .Foreground(error.Map(message =>
                        message == "Ready to create." ? ColorDefault.Success : ColorDefault.Danger)),

                new Label("Press Create with an empty or short\nname to see validation block the dialog.")
                    .FontSize(12)
                    .Foreground(ColorDefault.Secondary)
            );

        Dialog.Show(
            "Create project",
            content,
            showCancelButton: true,
            onAccepted: () => Dialog.Show(
                "Project created",
                "The project name was validated\nsuccessfully."),
            validate: () =>
            {
                if (projectName.Value.Trim().Length >= 3)
                {
                    return true;
                }

                error.Value = "Use at least 3 characters.";
                return false;
            },
            okText: "Create",
            cancelText: "Cancel");
    }

    private static VisualElement CreateStatusBadge(string text, Color color)
    {
        var bytes = color.ToBytes();

        return new Frame()
            .Background(new Color(bytes.R, bytes.G, bytes.B, (byte)45))
            .BorderRadius(6)
            .Padding(new Thickness(10, 6, 10, 6))
            .Content(
                new Label(text)
                    .FontSize(12)
                    .Foreground(color)
            );
    }

    private static VisualElement CreateFeatureItem(string text)
    {
        return new HStack()
            .Spacing(8)
            .Children(
                new Label("*")
                    .FontSize(14)
                    .Foreground(ColorDefault.Primary),
                new Label(text)
                    .FontSize(14)
                    .Foreground(new Color(180, 185, 195))
            );
    }

    private static Color Lighten(Color color, byte amount)
    {
        var bytes = color.ToBytes();

        return new Color(
            ClampToByte(bytes.R + amount),
            ClampToByte(bytes.G + amount),
            ClampToByte(bytes.B + amount),
            bytes.A);
    }

    private static Color Darken(Color color, byte amount)
    {
        var bytes = color.ToBytes();

        return new Color(
            ClampToByte(bytes.R - amount),
            ClampToByte(bytes.G - amount),
            ClampToByte(bytes.B - amount),
            bytes.A);
    }

    private static byte ClampToByte(int value)
    {
        return (byte)System.Math.Clamp(value, 0, 255);
    }
}
