using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using static Rayo.Core.UIHelpers;
using Rayo;

namespace Gallery.Pages;

public class EditorPage : UserControl
{
    public override VisualElement Build()
    {
        var notes = new Signal<string>("This is a multi-line editor.\nYou can type multiple lines here.\n\nTry adding more content!");
        var readOnlyText = new Signal<string>("This editor is read-only.\nYou cannot edit this content.");
        var characterCount = new Computed<int>(() => notes.Value.Length);
        var lineCount = new Computed<int>(() => notes.Value.Split('\n').Length);
        var wordCount = new Computed<int>(() => 
            notes.Value.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length
        );

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("Editor", "Multi-line text input control (MAUI-compatible)"),

                Helper.CreateExampleSection("Basic Editor",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Notes:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(500)
                                .Height(150)
                                .Placeholder("Enter your notes here...")
                                .Text(notes.Value)
                                .WordWrap(true)
                                .OnTextChanged(text => notes.Value = text),

                            new Frame()
                                .Background(new Color(40, 40, 50))
                                .BorderRadius(6)
                                .Padding(new Thickness(12))
                                .Content(
                                    new HStack()
                                        .Spacing(20)
                                        .Children(
                                            new Label()
                                                .Text(characterCount.Map(c => $"Characters: {c}"))
                                                .Foreground(ColorDefault.Info),
                                            
                                            new Label()
                                                .Text(lineCount.Map(l => $"Lines: {l}"))
                                                .Foreground(ColorDefault.Info),
                                            
                                            new Label()
                                                .Text(wordCount.Map(w => $"Words: {w}"))
                                                .Foreground(ColorDefault.Info)
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("Editor Sizes",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Small (100px height):")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(400)
                                .Height(100)
                                .Placeholder("Compact editor..."),

                            new Label("Medium (200px height):")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(400)
                                .Height(200)
                                .IsMultiline(true)
                                .Placeholder("Standard editor..."),

                            new Label("Large (300px height):")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(400)
                                .Height(300)
                                .Placeholder("Large editor for extensive content...")
                        )
                ),

                Helper.CreateExampleSection("Read-Only Editor",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Read-only content display:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .IsReadOnly(true)
                                .Text(readOnlyText.Value)
                                .Width(500)
                                .Height(120)
                                .Background(new Color(35, 35, 40))
                                .TextColor(new Color(180, 180, 190))
                        )
                ),

                Helper.CreateExampleSection("Styled Editors",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Custom colors:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(500)
                                .Height(120)
                                .Placeholder("Dark blue theme...")
                                .Background(new Color(20, 30, 50))
                                .TextColor(new Color(200, 220, 255))
                                .PlaceholderColor(new Color(100, 120, 160))
                                .BorderColor(ColorDefault.Primary),

                            new Label("Large font:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(500)
                                .Height(120)
                                .Placeholder("Larger text for better readability...")
                                .FontSize(16),

                            new Label("Minimal border:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(500)
                                .Height(120)
                                .Placeholder("No border style...")
                                .BorderWidth(0)
                                .Background(new Color(30, 30, 35))
                        )
                ),

                Helper.CreateExampleSection("MaxLength Example",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Limited to 100 characters:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .SetMaxLength(100)
                                .Width(500)
                                .Height(120)
                                .Placeholder("Type up to 100 characters..."),

                            new Label()
                                .Text("Useful for input validation and limiting content size")
                                .FontSize(12)
                                .Foreground(new Color(140, 145, 160))
                        )
                ),

                Helper.CreateExampleSection("Code/Log Display",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Monospace-like display (code/logs):")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .Width(600)
                                .Height(200)
                                .Text("function calculateSum(a, b) {\n    return a + b;\n}\n\nconst result = calculateSum(5, 3);\nconsole.log(result); // Output: 8")
                                .Background(new Color(20, 20, 25))
                                .TextColor(new Color(100, 255, 100))
                                .FontSize(13)
                                .BorderColor(new Color(60, 60, 70))
                        )
                ),

                Helper.CreateExampleSection("Form Example",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("Feedback Form:")
                                .FontSize(16)
                                .Foreground(Color.White),

                            new VStack()
                                .Spacing(10)
                                .Children(
                                    new Label("Your feedback:")
                                        .Foreground(ColorDefault.Secondary),

                                    new Editor()
                                        .Width(500)
                                        .Height(150)
                                        .Placeholder("Please share your thoughts, suggestions, or report any issues..."),

                                    new HStack()
                                        .Spacing(10)
                                        .Children(
                                            new Button()
                                                .Text("Submit")
                                                .Background(ColorDefault.Primary)
                                                .HoverBackground(new Color(41, 98, 255))
                                                .Padding(new Thickness(20, 10, 20, 10))
                                                .OnTapped(() => { /* Submit feedback logic */ }),

                                            new Button()
                                                .Text("Clear")
                                                .Background(ColorDefault.Secondary)
                                                .HoverBackground(new Color(90, 90, 100))
                                                .Padding(new Thickness(20, 10, 20, 10))
                                        )
                                )
                        )
                ),

                Helper.CreateExampleSection("AutoSize Feature (Future)",
                    new VStack()
                        .Spacing(12)
                        .Children(
                            new Label("AutoSize automatically adjusts height based on content:")
                                .Foreground(ColorDefault.Secondary),

                            new Editor()
                                .AutoSize(true)
                                .Width(500)
                                .Placeholder("This editor will grow as you type (when AutoSize is implemented)...")
                                .Background(new Color(35, 35, 40)),

                            new Label()
                                .Text("Note: AutoSize feature may require additional implementation")
                                .FontSize(11)
                                .Foreground(ColorDefault.Warning)
                        )
                )
            );
    }
}
