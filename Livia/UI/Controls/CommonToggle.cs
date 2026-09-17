using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Livia.UI.Controls;

public static class CommonToggle
{
    private static ControlTemplate CreateTemplate()
    {
        var template =
            new ControlTemplate(typeof(ToggleButton));

        var border =
            new FrameworkElementFactory(typeof(Border));

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

        var presenter =
            new FrameworkElementFactory(
                typeof(ContentPresenter));

        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            HorizontalAlignment.Center);

        presenter.SetValue(
            ContentPresenter.VerticalAlignmentProperty,
            VerticalAlignment.Center);

        border.AppendChild(presenter);

        template.VisualTree = border;

        return template;
    }

    public static UIElement CreateRow(
        string label,
        bool initialValue,
        Action<bool> onValueChanged,
        CommonTheme theme,
        string offIcon = "⭘",
        string onIcon = "⏽",
        Color? offColor = null,
        Color? onColor = null)
    {
        ArgumentNullException.ThrowIfNull(onValueChanged);

        var row = CommonRow.Create(label, theme);

        Color activeOffColor =
            offColor ?? theme.SecondaryText;

        Color activeOnColor =
            onColor ?? theme.SecondaryText;

        var toggle = new ToggleButton
        {
            IsChecked = initialValue,
            Width = 65,
            Height = 26,
            Background = new SolidColorBrush(
                theme.InputBackground),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(
                theme.InputBorder),
            Template = CreateTemplate(),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        void UpdateVisuals(bool isChecked)
        {
            toggle.Content =
                isChecked ? onIcon : offIcon;

            toggle.Foreground =
                new SolidColorBrush(
                    isChecked
                        ? activeOnColor
                        : activeOffColor);
        }

        UpdateVisuals(initialValue);

        toggle.Click += (_, _) =>
        {
            bool newState =
                toggle.IsChecked ?? false;

            UpdateVisuals(newState);
            onValueChanged(newState);
        };

        CommonRow.SetControl(row, toggle);

        return row;
    }
}