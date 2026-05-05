using Rayo.Controls;
using Rayo.Core;
using Rayo.DevTool.Shared.Protocol;
using Rayo.Layout;
using Rayo.Reactivity;
using Rayo.Rendering;
using Rayo.Rendering.Brushes;
using static Rayo.Core.UIHelpers;
using System.Collections.Generic;
using System.Linq;

namespace Rayo.DevTool.Frames;

public class PropertyFrame : UserControl
{
    private readonly DevToolState _state;

    // Width of the left (name) column in property rows, matching VS Properties panel style
    private const float LabelColumnWidth = 130f;

    public PropertyFrame(DevToolState state)
    {
        _state = state;
    }

    public override VisualElement Build()
    {
        return new Frame()
            .Background(new Color(28, 28, 32))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Content(
                new ScrollView()
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(BuildPropertyList())
            );
    }

    // -------------------------------------------------------------------------
    // Property list
    // -------------------------------------------------------------------------

    private VisualElement BuildPropertyList()
    {
        var container = new VStack()
            .Spacing(0)
            .Padding(new Thickness(4))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Top);

        _state.Properties.Subscribe(props =>
        {
            container.ClearChildren();

            if (props.Count == 0)
            {
                container.AddChild(
                    new Label("Select an element to view properties")
                        .Foreground(ColorDefault.Secondary)
                        .FontSize(12)
                );
                container.MarkNeedsLayout();
                return;
            }

            string? currentCategory = null;

            foreach (var prop in props)
            {
                if (prop.Category != currentCategory)
                {
                    currentCategory = prop.Category;
                    container.AddChild(BuildCategoryHeader(currentCategory));
                }

                container.AddChild(BuildPropertyRow(prop));
                container.AddChild(BuildSeparator());
            }

            container.MarkNeedsLayout();
        });

        return container;
    }

    private static VisualElement BuildCategoryHeader(string? category) =>
        new Frame()
            .Background(new Color(36, 36, 42))
            .Padding(new Thickness(6, 5, 4, 3))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new Label(category ?? "")
                    .FontSize(10)
                    .Foreground(new Color(150, 150, 170))
            );

    private static VisualElement BuildSeparator() =>
        new Frame()
            .Height(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Background(new Color(45, 45, 52));

    // -------------------------------------------------------------------------
    // Property row — two-column layout: fixed label | stretching value
    // -------------------------------------------------------------------------

    private VisualElement BuildPropertyRow(PropertyInfo prop)
    {
        var editor = BuildPropertyEditor(prop);
        editor.HorizontalAlignment = HorizontalAlignment.Stretch;

        return new Grid()
            .Rows(GridLength.Auto)
            .Columns(GridLength.Pixels(LabelColumnWidth), GridLength.Star)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .AddChild(
                // Left column: property name — fixed width, clipped
                new Label(prop.Name)
                    .Padding(new Thickness(6, 4))
                    .Foreground(new Color(156, 220, 254))
                    .FontSize(11),
                0, 0)
            .AddChild(
                // Right column: editor — stretch child fills remaining space
                editor,
                0, 1);
    }

    // -------------------------------------------------------------------------
    // Editor dispatch
    // -------------------------------------------------------------------------

    private VisualElement BuildPropertyEditor(PropertyInfo prop)
    {
        var elementId = _state.SelectedElementId.Value;

        if (prop.IsReadOnly)
            return BuildReadOnlyLabel(prop.Value);

        return prop.Editor switch
        {
            "boolean"      => BuildBooleanEditor(prop, elementId!),
            "number"       => BuildNumberEditor(prop, elementId!),
            "text"         => BuildTextEditor(prop, elementId!),
            "color"        => BuildColorEditor(prop, elementId!),
            "brushcolor"   => BuildBrushColorEditor(prop, elementId!),
            "enum"         => BuildEnumEditor(prop, elementId!),
            "thickness"    => BuildThicknessEditor(prop, elementId!),
            "cornerradius" => BuildCornerRadiusEditor(prop, elementId!),
            _ => BuildReadOnlyLabel(prop.Value)
        };
    }

    // -------------------------------------------------------------------------
    // Individual editors
    // -------------------------------------------------------------------------

    private static VisualElement BuildReadOnlyLabel(object? value)
    {
        if (IsNullValue(value))
        {
            return new Entry()
                .IsReadOnly(true)
                .Text("")
                .Placeholder("null")
                .FontSize(11)
                .HorizontalAlignment(HorizontalAlignment.Stretch);
        }

        return new Label(FormatValue(value))
            .FontSize(11)
            .Foreground(new Color(180, 180, 190))
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    private static bool IsNullValue(object? value) =>
        value == null ||
        (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Null);

    private VisualElement BuildBooleanEditor(PropertyInfo prop, string elementId) =>
        new Checkbox()
            .IsChecked(GetValueAsBool(prop.Value))
            .OnChanged(async checkedState =>
            {
                await _state.Client.SetPropertyAsync(elementId, prop.Name, checkedState);
            });

    private VisualElement BuildNumberEditor(PropertyInfo prop, string elementId)
    {
        var bounds = GetNumericBounds(prop.Name);
        bool isNull      = IsNullValue(prop.Value);
        var  displayValue = isNull ? "" : FormatNumberValue(prop.Value);
        var  entry        = new Entry
        {
            IsNumericOnly = true,
            IsDecimalAllowed = true,
            IsNegativeAllowed = bounds.MinValue is null || bounds.MinValue < 0f
        };
        entry.Text(displayValue)
             .Placeholder(isNull ? "null" : "")
             .HorizontalAlignment(HorizontalAlignment.Stretch)
             .FontSize(11)
             .OnTextChanged(async text =>
             {
                 if (float.TryParse(text,
                         System.Globalization.NumberStyles.Any,
                         System.Globalization.CultureInfo.InvariantCulture,
                         out var v))
                 {
                     v = ClampToBounds(v, bounds);
                     await _state.Client.SetPropertyAsync(elementId, prop.Name, v);
                 }
             });
        return entry;
    }

    private VisualElement BuildTextEditor(PropertyInfo prop, string elementId)
    {
        bool isNull = IsNullValue(prop.Value);
        return new Entry()
            .Text(isNull ? "" : prop.Value?.ToString() ?? "")
            .Placeholder(isNull ? "null" : "")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .FontSize(11)
            .OnTextChanged(async text =>
            {
                await _state.Client.SetPropertyAsync(elementId, prop.Name, text);
            });
    }

    private VisualElement BuildColorEditor(PropertyInfo prop, string elementId)
    {
        var hex = prop.Value?.ToString() ?? "#FFFFFFFF";
        ParseHexColor(hex, out var color);

        return new HStack()
            .Spacing(6)
            .Children(
                new Frame()
                    .Size(16)
                    .Background(color)
                    .BorderRadius(new CornerRadius(2)),
                new Entry()
                    .Text(hex)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .FontSize(11)
                    .OnTextChanged(async text =>
                    {
                        if (ParseHexColor(text, out var newColor))
                            await _state.Client.SetPropertyAsync(elementId, prop.Name, newColor);
                    })
            );
    }

    private VisualElement BuildBrushColorEditor(PropertyInfo prop, string elementId)
    {
        var hex = prop.Value?.ToString() ?? "#FFFFFFFF";
        ParseHexColor(hex, out var color);

        var swatch = new Frame()
            .Size(16)
            .Background(new SolidColorBrush(color))
            .BorderRadius(new CornerRadius(2));

        var valueLabel = new Label(hex)
            .FontSize(11)
            .Foreground(new Color(180, 180, 190))
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center);

        var button = new Button()
            .Text("...")
            .Width(40)
            .Height(26)
            .FontSize(10)
            .Background(new Color(40, 40, 46))
            .HoverBackground(new Color(55, 55, 62))
            .BorderColor(new Color(60, 60, 68))
            .BorderWidth(1)
            .BorderRadius(new CornerRadius(2));

        button.OnTapped(() =>
        {
            ColorPicker.ShowDialog(
                color,
                selectedColor =>
                {
                    color = selectedColor;
                    var nextHex = ToHexColor(selectedColor);
                    swatch.Background(new SolidColorBrush(selectedColor));
                    valueLabel.Text(nextHex);
                    _ = _state.Client.SetPropertyAsync(elementId, prop.Name, selectedColor);
                });
        });

        return new HStack()
            .Spacing(6)
            .Alignment(Alignment.Center)
            .Children(
                swatch,
                valueLabel,
                button
            );
    }

    private VisualElement BuildEnumEditor(PropertyInfo prop, string elementId)
    {
        var currentValue = prop.Value?.ToString() ?? "";
        var enumValues = prop.EnumValues ?? new List<string>();

        // Find the initial index so the ComboBox shows the correct current value
        int initialIndex = enumValues.IndexOf(currentValue);

        var comboBox = new ComboBox()
            .Items(enumValues)
            .ItemTextAlignment(HorizontalAlignment.Left)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Background(new Color(40, 40, 46))
            .BorderColor(new Color(60, 60, 68))
            .TextColor(Color.White)
            .HoverColor(new Color(55, 55, 62))
            .SelectedColor(ColorDefault.Primary);

        // Set initial selection index (after Items is assigned so count is correct)
        if (initialIndex >= 0 && initialIndex < enumValues.Count)
            comboBox.SelectedIndex = initialIndex;

        comboBox.SelectionChanged += async (selectedIndex) =>
        {
            if (selectedIndex >= 0 && selectedIndex < enumValues.Count)
                await _state.Client.SetPropertyAsync(elementId, prop.Name, enumValues[selectedIndex]);
        };

        return comboBox;
    }

    private VisualElement BuildThicknessEditor(PropertyInfo prop, string elementId)
    {
        var t = ParseThickness(prop.Value);

        return new HStack()
            .Spacing(3)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                FloatEntry("L", t.Left, false, v => { t = new Thickness(v, t.Top, t.Right, t.Bottom);   _ = _state.Client.SetPropertyAsync(elementId, prop.Name, t); }),
                FloatEntry("T", t.Top, false, v => { t = new Thickness(t.Left, v, t.Right, t.Bottom);   _ = _state.Client.SetPropertyAsync(elementId, prop.Name, t); }),
                FloatEntry("R", t.Right, false, v => { t = new Thickness(t.Left, t.Top, v, t.Bottom);     _ = _state.Client.SetPropertyAsync(elementId, prop.Name, t); }),
                FloatEntry("B", t.Bottom, false, v => { t = new Thickness(t.Left, t.Top, t.Right, v);      _ = _state.Client.SetPropertyAsync(elementId, prop.Name, t); })
            );
    }

    private VisualElement BuildCornerRadiusEditor(PropertyInfo prop, string elementId)
    {
        var r = ParseCornerRadius(prop.Value);

        return new HStack()
            .Spacing(3)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Children(
                FloatEntry("TL", r.TopLeft, false, v => { r = new CornerRadius(v, r.TopRight, r.BottomRight, r.BottomLeft);     _ = _state.Client.SetPropertyAsync(elementId, prop.Name, r); }),
                FloatEntry("TR", r.TopRight, false, v => { r = new CornerRadius(r.TopLeft, v, r.BottomRight, r.BottomLeft);      _ = _state.Client.SetPropertyAsync(elementId, prop.Name, r); }),
                FloatEntry("BR", r.BottomRight, false, v => { r = new CornerRadius(r.TopLeft, r.TopRight, v, r.BottomLeft);         _ = _state.Client.SetPropertyAsync(elementId, prop.Name, r); }),
                FloatEntry("BL", r.BottomLeft, false, v => { r = new CornerRadius(r.TopLeft, r.TopRight, r.BottomRight, v);        _ = _state.Client.SetPropertyAsync(elementId, prop.Name, r); })
            );
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Labeled mini float entry used in Thickness / CornerRadius editors.</summary>
    private static VisualElement FloatEntry(string label, float initialValue, bool allowNegative, Action<float> onChange)
    {
        var miniEntry = new Entry { IsNumericOnly = true, IsDecimalAllowed = true, IsNegativeAllowed = allowNegative };
        miniEntry.Text(FormatFloat(initialValue))
                 .FontSize(11)
                 .HorizontalAlignment(HorizontalAlignment.Stretch)
                 .OnTextChanged(text =>
                 {
                     if (float.TryParse(text,
                             System.Globalization.NumberStyles.Any,
                             System.Globalization.CultureInfo.InvariantCulture,
                             out var v))
                     {
                         onChange(allowNegative ? v : Math.Max(0f, v));
                     }
                 });

        return new VStack()
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Alignment(Alignment.Center)
            .Children(
                new Label(label)
                    .FontSize(9)
                    .Foreground(new Color(110, 110, 130))
                    .HorizontalAlignment(HorizontalAlignment.Center),
                miniEntry
            );
    }

    /// <summary>Format a float to at most 4 significant digits, no trailing zeros.</summary>
    private static string FormatFloat(float value) =>
        value.ToString("G4", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Format any property value for display.
    /// Numbers get at most 4 significant digits; strings are returned as-is.
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null) return "null";

        if (value is System.Text.Json.JsonElement json)
        {
            return json.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number =>
                    json.TryGetDouble(out var d) ? FormatFloat((float)d) : json.ToString(),
                System.Text.Json.JsonValueKind.String => json.GetString() ?? "",
                System.Text.Json.JsonValueKind.True   => "true",
                System.Text.Json.JsonValueKind.False  => "false",
                _ => json.ToString()
            };
        }

        if (value is float f)  return FormatFloat(f);
        if (value is double d2) return FormatFloat((float)d2);
        return value.ToString() ?? "";
    }

    /// <summary>Format a property value for numeric Entry fields.</summary>
    private static string FormatNumberValue(object? value)
    {
        if (value == null) return "0";

        if (value is System.Text.Json.JsonElement json && json.ValueKind == System.Text.Json.JsonValueKind.Number)
            return json.TryGetDouble(out var d) ? FormatFloat((float)d) : "0";

        if (value is float f)  return FormatFloat(f);
        if (value is double d2) return FormatFloat((float)d2);
        if (value is int i)   return i.ToString();
        return value.ToString() ?? "0";
    }

    private static bool GetValueAsBool(object? value)
    {
        if (value is bool b) return b;
        if (value is string s) return bool.TryParse(s, out var r) && r;
        if (value is System.Text.Json.JsonElement j)
        {
            if (j.ValueKind == System.Text.Json.JsonValueKind.True)  return true;
            if (j.ValueKind == System.Text.Json.JsonValueKind.False) return false;
        }
        return false;
    }

    private static Thickness ParseThickness(object? value)
    {
        if (value is Thickness t) return t;

        string? str = null;
        if (value is string s)
            str = s;
        else if (value is System.Text.Json.JsonElement json &&
                 json.ValueKind == System.Text.Json.JsonValueKind.String)
            str = json.GetString();

        if (!string.IsNullOrEmpty(str))
        {
            var parts = str.Split(',');
            if (parts.Length == 4 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tp) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r) &&
                float.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bt))
            {
                return new Thickness(l, tp, r, bt);
            }
        }
        return new Thickness();
    }

    private static CornerRadius ParseCornerRadius(object? value)
    {
        if (value is CornerRadius cr) return cr;

        string? str = null;
        if (value is string s)
            str = s;
        else if (value is System.Text.Json.JsonElement json &&
                 json.ValueKind == System.Text.Json.JsonValueKind.String)
            str = json.GetString();

        if (!string.IsNullOrEmpty(str))
        {
            var parts = str.Split(',');
            if (parts.Length == 4 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tl) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tr) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var br) &&
                float.TryParse(parts[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bl))
            {
                return new CornerRadius(tl, tr, br, bl);
            }
        }
        return new CornerRadius(0);
    }

    private static bool ParseHexColor(string hex, out Color color)
    {
        color = Color.White;
        if (string.IsNullOrEmpty(hex)) return false;
        hex = hex.TrimStart('#');

        try
        {
            if (hex.Length == 6)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                color = new Color(r, g, b);
                return true;
            }
            else if (hex.Length == 8)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                color = new Color(r, g, b, a);
                return true;
            }
            return false;
        }
        catch { return false; }
    }

    private static string ToHexColor(Color color)
    {
        return $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}{(int)(color.A * 255):X2}";
    }

    private static (float? MinValue, float? MaxValue) GetNumericBounds(string propertyName)
    {
        return propertyName switch
        {
            "Opacity" => (0f, 1f),
            "Scale" => (0f, null),
            "Width" or "Height" or "MinWidth" or "MinHeight" or "MaxWidth" or "MaxHeight" or
            "BorderWidth" or "StrokeWidth" or "StrokeThickness" or
            "FontSize" or "Spacing" or "ItemSpacing" or "RowSpacing" or "ColumnSpacing" or
            "Gap" or "RowGap" or "Padding" or "Margin" or
            "Radius" or "RadiusX" or "RadiusY" or "InnerRadius" or
            "BlurRadius" or "ContactRadius" or "BadgeSize" or "IconSize" or
            "ItemHeight" or "RowHeight" or "HeaderHeight" or "DrawerHeight" or "BarHeight" or
            "TrackHeight" or "TrackWidth" or "SplitterSize" or "ThumbSize" or "CircleSize" or
            "ButtonWidth" or "TabWidth" or "VerticalTabWidth" or "TabHeight" or "VerticalTabHeight" or
            "LineHeight" or "MaxDropdownHeight" or "IndentSize" or "AnimationDuration" or "TransitionDuration" or
            "CornerRadius" or "BorderRadius"
                => (0f, null),
            _ => (null, null)
        };
    }

    private static float ClampToBounds(float value, (float? MinValue, float? MaxValue) bounds)
    {
        if (bounds.MinValue.HasValue && value < bounds.MinValue.Value)
            value = bounds.MinValue.Value;
        if (bounds.MaxValue.HasValue && value > bounds.MaxValue.Value)
            value = bounds.MaxValue.Value;
        return value;
    }
}
