using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Livia.UI.Controls;

public static class CommonActionInput
{
    private static ControlTemplate CreateButtonTemplate(CommonTheme theme)
    {
        var template = new ControlTemplate(typeof(Button));

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
            new CornerRadius(0));

        var content = new FrameworkElementFactory(typeof(ContentPresenter));

        content.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            HorizontalAlignment.Center);

        content.SetValue(
            ContentPresenter.VerticalAlignmentProperty,
            VerticalAlignment.Center);

        content.SetValue(
            ContentPresenter.RecognizesAccessKeyProperty,
            true);

        border.AppendChild(content);
        template.VisualTree = border;

        var hoverTrigger = new Trigger
        {
            Property = Button.IsMouseOverProperty,
            Value = true
        };

        hoverTrigger.Setters.Add(
            new Setter(
                Control.BackgroundProperty,
                new SolidColorBrush(theme.InputBorder)));

        var pressedTrigger = new Trigger
        {
            Property = Button.IsPressedProperty,
            Value = true
        };

        pressedTrigger.Setters.Add(
            new Setter(
                Control.BackgroundProperty,
                new SolidColorBrush(theme.InputBackground)));

        var disabledTrigger = new Trigger
        {
            Property = Button.IsEnabledProperty,
            Value = false
        };

        disabledTrigger.Setters.Add(
            new Setter(
                Control.ForegroundProperty,
                new SolidColorBrush(theme.MutedText)));

        template.Triggers.Add(hoverTrigger);
        template.Triggers.Add(pressedTrigger);
        template.Triggers.Add(disabledTrigger);

        return template;
    }

    public static UIElement CreateRow(
        string label,
        string currentValue,
        Func<string?> getValue,
        CommonTheme theme,
        string buttonText = "Get",
        string loadingText = "...",
        bool invertOrder = false)
    {
        ArgumentNullException.ThrowIfNull(getValue);

        return CreateRow(
            label,
            currentValue,
            () => Task.Run(getValue),
            theme,
            buttonText,
            loadingText,
            invertOrder);
    }

    public static UIElement CreateRow(
        string label,
        string currentValue,
        Func<Task<string?>> getValueAsync,
        CommonTheme theme,
        string buttonText = "Get",
        string loadingText = "...",
        bool invertOrder = false)
    {
        ArgumentNullException.ThrowIfNull(getValueAsync);
        ArgumentException.ThrowIfNullOrEmpty(buttonText);
        ArgumentException.ThrowIfNullOrEmpty(loadingText);

        var row = CommonRow.Create(label, theme);

        var border = new Border
        {
            Width = 65,
            Height = 26,
            Background = new SolidColorBrush(theme.InputBackground),
            BorderBrush = new SolidColorBrush(theme.InputBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        };

        var grid = new Grid();

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });

        var button = new Button
        {
            Content = buttonText,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(theme.SecondaryText),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Template = CreateButtonTemplate(theme)
        };

        var divider = new Border
        {
            Width = 1,
            Background = new SolidColorBrush(theme.InputBorder),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsHitTestVisible = false
        };

        Panel.SetZIndex(divider, 10);

        var input = new TextBox
        {
            Text = currentValue,
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            CaretBrush = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(5, 0, 5, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
        };

        Grid.SetColumn(button, invertOrder ? 1 : 0);
        Grid.SetColumn(divider, invertOrder ? 1 : 0);
        Grid.SetColumn(input, invertOrder ? 0 : 1);

        divider.HorizontalAlignment = invertOrder
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Right;

        grid.Children.Add(button);
        grid.Children.Add(divider);
        grid.Children.Add(input);

        border.Child = grid;

        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            button.Content = loadingText;

            try
            {
                string? value = await getValueAsync();

                if (value is not null)
                    input.Text = value;
            }
            finally
            {
                button.Content = buttonText;
                button.IsEnabled = true;
            }
        };

        CommonRow.SetControl(row, border);

        return row;
    }
}