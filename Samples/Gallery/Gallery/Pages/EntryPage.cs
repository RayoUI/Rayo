using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class EntryPage : UserControl
{
    public override VisualElement Build()
    {
        var username = new Signal<string>("");
        var email = new Signal<string>("");
        var password = new Signal<string>("");
        var phone = new Signal<string>("");
        var readOnlyText = new Signal<string>("This is read-only text");
        
        // OnCompleted example state
        var submitText = new Signal<string>("");
        var statusText = new Signal<string>("Type something and press Enter...");

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Entry", "Single-line text input control (MAUI-compatible)"),

                Helper.CreateExampleSection("Basic Entry",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Enter your name:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Type your name here...")
                                .Text(username.Value)
                                .OnTextChanged(text => username.Value = text),

                            new Label()
                                .Text(username.Map(t =>
                                    string.IsNullOrEmpty(t) ? "No input yet" : $"Hello, {t}!"))
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("Password Entry",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Password:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Enter password")
                                .IsPassword(true)
                                .Text(password.Value)
                                .OnTextChanged(text => password.Value = text),

                            new Label()
                                .Text(password.Map(p =>
                                    string.IsNullOrEmpty(p) ? "Password not entered" :
                                    $"Password length: {p.Length} characters"))
                                .Foreground(ColorDefault.Secondary)
                        )
                ),

                Helper.CreateExampleSection("Email Entry",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Email address:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("name@example.com")
                                .Text(email.Value)
                                .OnTextChanged(text => email.Value = text),

                            new Label()
                                .Text(email.Map(e =>
                                {
                                    if (string.IsNullOrEmpty(e)) return "Enter an email";
                                    bool valid = e.Contains("@") && e.Contains(".");
                                    return valid ? "? Valid email format" : "? Invalid email format";
                                }))
                                .Foreground(email.Map(e =>
                                {
                                    if (string.IsNullOrEmpty(e)) return ColorDefault.Secondary;
                                    bool valid = e.Contains("@") && e.Contains(".");
                                    return valid ? ColorDefault.Success : ColorDefault.Danger;
                                }))
                        )
                ),

                Helper.CreateExampleSection("MaxLength Entry",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Phone number (max 15 digits):")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .MaxLength(15)
                                .Width(350)
                                .Placeholder("Enter phone number...")
                                .Text(phone.Value)
                                .OnTextChanged(text => phone.Value = text),

                            new Label()
                                .Text(phone.Map(p => $"{p.Length}/15 characters"))
                                .Foreground(ColorDefault.Info)
                        )
                ),

                Helper.CreateExampleSection("Read-Only Entry",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Read-only field:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .IsReadOnly(true)
                                .Width(350)
                                .Text(readOnlyText.Value)
                                .Background(new Color(35, 35, 40))
                                .TextColor(new Color(180, 180, 190)),

                            new Label("You cannot edit or delete this text")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160))
                        )
                ),

                Helper.CreateExampleSection("Entry Styles",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Default style:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Default appearance"),

                            new Label("Custom colors:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Blue theme...")
                                .Background(new Color(20, 30, 50))
                                .TextColor(new Color(200, 220, 255))
                                .PlaceholderColor(new Color(100, 120, 160))
                                .BorderColor(ColorDefault.Primary),

                            new Label("Larger font (18px):")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Larger text for better readability")
                                .FontSize(18),

                            new Label("No border:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Minimal style...")
                                .BorderWidth(0)
                                .Background(new Color(30, 30, 35))
                        )
                ),

                Helper.CreateExampleSection("Form Example",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Login Form:")
                                .FontSize(16)
                                .Foreground(Color.White),

                            new VStack()
                                .Spacing(10)
                                .Children(
                                    new Label("Username:")
                                        .Foreground(ColorDefault.Secondary),

                                    new Entry()
                                        .Width(350)
                                        .Placeholder("Enter username"),

                                    new Label("Password:")
                                        .Foreground(ColorDefault.Secondary),

                                    new Entry()
                                        .Width(350)
                                        .Placeholder("Enter password")
                                        .IsPassword(true),

                                    new HStack()
                                        .Spacing(10)
                                        .Children(
                                            new Button()
                                                .Text("Login")
                                                .Background(ColorDefault.Primary)
                                                .HoverBackground(new Color(41, 98, 255))
                                                .Padding(new Thickness(20, 10, 20, 10)),

                                            new Button()
                                                .Text("Cancel")
                                                .Background(ColorDefault.Secondary)
                                                .HoverBackground(new Color(90, 90, 100))
                                                .Padding(new Thickness(20, 10, 20, 10))
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("OnCompleted Event",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Press Enter to submit:")
                                .Foreground(ColorDefault.Secondary),

                            new Entry()
                                .Width(350)
                                .Placeholder("Type and press Enter...")
                                .Text(submitText.Value)
                                .OnTextChanged(text => submitText.Value = text)
                                .OnEnter(() =>
                                {
                                    if (!string.IsNullOrEmpty(submitText.Value))
                                    {
                                        statusText.Value = $"? Submitted: '{submitText.Value}' at {DateTime.Now:HH:mm:ss}";
                                        submitText.Value = ""; // Clear after submit
                                    }
                                }),

                            new Label()
                                .Text(statusText)
                                .Foreground(ColorDefault.Success)
                                .FontSize(12)
                        )
                )
            );
    }
}
