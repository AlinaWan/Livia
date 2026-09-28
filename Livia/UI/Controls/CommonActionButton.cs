using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Livia.UI.Controls;

public static class CommonActionButton
{
    private static ControlTemplate CreateButtonTemplate(
        CommonTheme theme)
    {
        var template =
            new ControlTemplate(
                typeof(Button));

        var border =
            new FrameworkElementFactory(
                typeof(Border));

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

        var content =
            new FrameworkElementFactory(
                typeof(ContentPresenter));

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
            Property =
                Button.IsMouseOverProperty,

            Value = true
        };

        hoverTrigger.Setters.Add(
            new Setter(
                Control.BackgroundProperty,
                new SolidColorBrush(
                    theme.InputBorder)));

        var pressedTrigger = new Trigger
        {
            Property =
                Button.IsPressedProperty,

            Value = true
        };

        pressedTrigger.Setters.Add(
            new Setter(
                Control.BackgroundProperty,
                new SolidColorBrush(
                    theme.InputBackground)));

        var disabledTrigger = new Trigger
        {
            Property =
                Button.IsEnabledProperty,

            Value = false
        };

        disabledTrigger.Setters.Add(
            new Setter(
                Control.ForegroundProperty,
                new SolidColorBrush(
                    theme.MutedText)));

        template.Triggers.Add(
            hoverTrigger);

        template.Triggers.Add(
            pressedTrigger);

        template.Triggers.Add(
            disabledTrigger);

        return template;
    }

    /// <summary>
    /// Creates a button that executes an asynchronous action.
    /// </summary>
    /// <param name="text">
    /// The text displayed when the action is idle.
    /// </param>
    /// <param name="action">
    /// The asynchronous action to execute.
    /// </param>
    /// <param name="theme">
    /// The theme used to style the button.
    /// </param>
    /// <param name="loadingText">
    /// The text displayed while the action is running.
    /// </param>
    /// <returns>
    /// A configured action button.
    /// </returns>
    public static Button Create(
        string text,
        Func<Task> action,
        CommonTheme theme,
        string loadingText = "...")
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentException.ThrowIfNullOrEmpty(loadingText);

        var button = new Button
        {
            Content = text,

            Height = 26,

            MinWidth = 65,

            Padding =
                new Thickness(
                    10,
                    0,
                    10,
                    0),

            Background =
                new SolidColorBrush(
                    theme.InputBackground),

            Foreground =
                new SolidColorBrush(
                    theme.SecondaryText),

            BorderBrush =
                new SolidColorBrush(
                    theme.InputBorder),

            BorderThickness =
                new Thickness(1),

            Cursor =
                System.Windows.Input.Cursors.Hand,

            Template =
                CreateButtonTemplate(theme)
        };

        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            button.Content = loadingText;

            try
            {
                await action();
            }
            finally
            {
                button.Content = text;
                button.IsEnabled = true;
            }
        };

        return button;
    }
}
