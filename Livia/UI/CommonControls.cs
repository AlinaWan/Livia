using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Livia.UI;

public static class CommonControls
{
    private static ControlTemplate CreateInputTemplate()
    {
        var template = new ControlTemplate(typeof(TextBox));

        var border = new FrameworkElementFactory(typeof(Border));

        border.SetValue(
            Border.BackgroundProperty,
            new TemplateBindingExtension(
                Control.BackgroundProperty));

        border.SetValue(
            Border.BorderBrushProperty,
            new TemplateBindingExtension(
                Control.BorderBrushProperty));

        border.SetValue(
            Border.BorderThicknessProperty,
            new TemplateBindingExtension(
                Control.BorderThicknessProperty));

        border.SetValue(
            Border.CornerRadiusProperty,
            new CornerRadius(6));

        var scrollViewer = new FrameworkElementFactory(
            typeof(ScrollViewer));

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

    public static UIElement CreateInputRow(
        string label,
        int currentValue,
        Action<int> onValueValid,
        CommonTheme theme)
    {
        ArgumentNullException.ThrowIfNull(onValueValid);

        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 14)
        };

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Auto
            });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(
                    1,
                    GridUnitType.Star)
            });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Auto
            });

        var labelText = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(
                theme.SecondaryText),

            VerticalAlignment =
                VerticalAlignment.Center,

            Margin = new Thickness(0, 0, 8, 0),
            FontSize = 12
        };

        Grid.SetColumn(labelText, 0);
        grid.Children.Add(labelText);

        var line = new Rectangle
        {
            Height = 1,
            Fill = new SolidColorBrush(theme.Border),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 1, 8, 0)
        };

        Grid.SetColumn(line, 1);
        grid.Children.Add(line);

        var input = new TextBox
        {
            Text = currentValue.ToString(),
            Width = 65,
            Height = 26,

            Background = new SolidColorBrush(
        theme.InputBackground),

            Foreground = Brushes.White,

            BorderThickness = new Thickness(1),

            BorderBrush = new SolidColorBrush(
        theme.InputBorder),

            VerticalContentAlignment =
        VerticalAlignment.Center,

            HorizontalContentAlignment =
        HorizontalAlignment.Center,

            Template = CreateInputTemplate()
        };

        input.TextChanged += (_, _) =>
        {
            bool valid =
                int.TryParse(input.Text, out int value) &&
                value > 0;

            if (valid)
                onValueValid(value);

            input.Foreground = new SolidColorBrush(
                valid
                    ? Colors.White
                    : theme.Error);
        };

        Grid.SetColumn(input, 2);
        grid.Children.Add(input);

        return grid;
    }

    public static UIElement CreateHelpStep(
        string number,
        string text,
        CommonTheme theme)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 10)
        };

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Auto
            });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(
                    1,
                    GridUnitType.Star)
            });

        var badge = new Border
        {
            Width = 20,
            Height = 20,

            CornerRadius = new CornerRadius(10),

            Background = new SolidColorBrush(
                theme.Accent),

            Margin = new Thickness(0, 0, 10, 0)
        };

        badge.Child = new TextBlock
        {
            Text = number,
            FontSize = 10,
            FontWeight = FontWeights.Bold,

            Foreground = new SolidColorBrush(
                theme.AccentText),

            HorizontalAlignment =
                HorizontalAlignment.Center,

            VerticalAlignment =
                VerticalAlignment.Center
        };

        Grid.SetColumn(badge, 0);
        grid.Children.Add(badge);

        var description = new TextBlock
        {
            Text = text,
            FontSize = 12,

            Foreground = new SolidColorBrush(
                theme.PrimaryText),

            VerticalAlignment =
                VerticalAlignment.Center,

            TextWrapping = TextWrapping.Wrap
        };

        Grid.SetColumn(description, 1);
        grid.Children.Add(description);

        return grid;
    }
}