using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Livia.UI.Controls;

public static class CommonSegmentedToggle
{
    private static ControlTemplate CreateTemplate(
    string offLabel,
    string onLabel,
    Color activeColor,
    Color inactiveColor,
    bool invertOrder = false)
    {
        var template = new ControlTemplate(typeof(ToggleButton));

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

        var grid = new FrameworkElementFactory(typeof(Grid));

        var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));

        var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));

        grid.AppendChild(col1);
        grid.AppendChild(col2);

        // Left Text Block
        var leftText = new FrameworkElementFactory(typeof(TextBlock));
        leftText.Name = "PART_LeftText";
        leftText.SetValue(TextBlock.TextProperty, invertOrder ? onLabel : offLabel);
        leftText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        leftText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        leftText.SetValue(TextBlock.FontSizeProperty, 11.0);
        leftText.SetValue(Grid.ColumnProperty, 0);

        // Right Text Block
        var rightText = new FrameworkElementFactory(typeof(TextBlock));
        rightText.Name = "PART_RightText";
        rightText.SetValue(TextBlock.TextProperty, invertOrder ? offLabel : onLabel);
        rightText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        rightText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        rightText.SetValue(TextBlock.FontSizeProperty, 11.0);
        rightText.SetValue(Grid.ColumnProperty, 1);

        grid.AppendChild(leftText);
        grid.AppendChild(rightText);
        border.AppendChild(grid);

        template.VisualTree = border;

        var offBrush = new SolidColorBrush(inactiveColor);
        var onBrush = new SolidColorBrush(activeColor);

        // Default (Unchecked / IsChecked = false)
        var uncheckedTrigger = new Trigger
        {
            Property = ToggleButton.IsCheckedProperty,
            Value = false
        };

        // Checked (IsChecked = true)
        var checkedTrigger = new Trigger
        {
            Property = ToggleButton.IsCheckedProperty,
            Value = true
        };

        if (invertOrder)
        {
            // Inverted: Left is ON, Right is OFF
            uncheckedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, offBrush, "PART_LeftText"));
            uncheckedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, onBrush, "PART_RightText"));

            checkedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, onBrush, "PART_LeftText"));
            checkedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, offBrush, "PART_RightText"));
        }
        else
        {
            // Standard: Left is OFF, Right is ON
            uncheckedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, onBrush, "PART_LeftText"));
            uncheckedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, offBrush, "PART_RightText"));

            checkedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, offBrush, "PART_LeftText"));
            checkedTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, onBrush, "PART_RightText"));
        }

        template.Triggers.Add(uncheckedTrigger);
        template.Triggers.Add(checkedTrigger);

        return template;
    }

    public static UIElement CreateRow(
        string label,
        bool initialValue,
        Action<bool> onValueChanged,
        CommonTheme theme,
        string offLabel = "⭘",
        string onLabel = "⏽",
        Color? activeColor = null,
        Color? inactiveColor = null,
        bool invertOrder = false)
    {
        ArgumentNullException.ThrowIfNull(onValueChanged);

        var row = CommonRow.Create(label, theme);

        Color active = activeColor ?? theme.SecondaryText;
        Color inactive = inactiveColor ?? theme.MutedText;

        var toggle = new ToggleButton
        {
            IsChecked = initialValue,
            Width = 65,
            Height = 26,
            Background = new SolidColorBrush(theme.InputBackground),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(theme.InputBorder),
            Template = CreateTemplate(offLabel, onLabel, active, inactive, invertOrder),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        toggle.Click += (_, _) =>
        {
            bool newState = toggle.IsChecked ?? false;
            onValueChanged(newState);
        };

        CommonRow.SetControl(row, toggle);

        return row;
    }
}