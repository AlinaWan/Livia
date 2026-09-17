using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Livia.UI;

public static class CommonInput
{
    private static ControlTemplate CreateTemplate()
    {
        var template = new ControlTemplate(typeof(TextBox));
        var border = new FrameworkElementFactory(typeof(Border));

        border.SetValue(
            Border.BackgroundProperty,
            new TemplateBindingExtension(Control.BackgroundProperty));

        border.SetValue(
            Border.BorderBrushProperty,
            new TemplateBindingExtension(Control.BorderBrushProperty));

        border.SetValue(
            Border.BorderThicknessProperty,
            new TemplateBindingExtension(Control.BorderThicknessProperty));

        border.SetValue(
            Border.CornerRadiusProperty,
            new CornerRadius(6));

        var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
        scrollViewer.Name = "PART_ContentHost";

        scrollViewer.SetValue(
            ScrollViewer.HorizontalScrollBarVisibilityProperty,
            ScrollBarVisibility.Hidden);

        scrollViewer.SetValue(
            ScrollViewer.VerticalScrollBarVisibilityProperty,
            ScrollBarVisibility.Hidden);

        border.AppendChild(scrollViewer);
        template.VisualTree = border;

        return template;
    }

    public static UIElement CreateRow(
        string label,
        int currentValue,
        Action<int> onValueValid,
        CommonTheme theme,
        Func<int, bool>? validator = null)
    {
        ArgumentNullException.ThrowIfNull(onValueValid);

        // Default validator: accepts full integer range (negative to positive)
        validator ??= _ => true;

        var row = CommonRow.Create(label, theme);

        var input = new TextBox
        {
            Text = currentValue.ToString(),
            Width = 65,
            Height = 26,
            Background = new SolidColorBrush(theme.InputBackground),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(theme.InputBorder),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Template = CreateTemplate()
        };

        input.TextChanged += (_, _) =>
        {
            // Validates that it's a valid integer AND passes the custom predicate
            bool valid = int.TryParse(input.Text, out int value) && validator(value);

            if (valid)
                onValueValid(value);

            input.Foreground = new SolidColorBrush(
                valid ? Colors.White : theme.Error);
        };

        CommonRow.SetControl(row, input);

        return row;
    }
}