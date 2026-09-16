using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Common;

public static class CommonTab
{
    public static TabItem Create(
        string header,
        CommonTheme theme)
    {
        var tab = new TabItem
        {
            Header = header,
            Foreground = new SolidColorBrush(
                theme.SecondaryText),

            Padding = new Thickness(16, 6, 16, 6),
            Margin = new Thickness(3, 0, 3, 0),
            Cursor = Cursors.Hand
        };

        var template = new ControlTemplate(typeof(TabItem));

        var border = new FrameworkElementFactory(
            typeof(Border));

        border.Name = "TabBorder";

        border.SetValue(
            Border.CornerRadiusProperty,
            new CornerRadius(4));

        border.SetValue(
            Border.BackgroundProperty,
            new SolidColorBrush(theme.Surface));

        border.SetValue(
            Border.PaddingProperty,
            new TemplateBindingExtension(
                TabItem.PaddingProperty));

        var content = new FrameworkElementFactory(
            typeof(ContentPresenter));

        content.SetValue(
            ContentPresenter.ContentSourceProperty,
            "Header");

        content.SetValue(
            FrameworkElement.HorizontalAlignmentProperty,
            HorizontalAlignment.Center);

        content.SetValue(
            FrameworkElement.VerticalAlignmentProperty,
            VerticalAlignment.Center);

        border.AppendChild(content);

        template.VisualTree = border;

        var selected = new Trigger
        {
            Property = TabItem.IsSelectedProperty,
            Value = true
        };

        selected.Setters.Add(
            new Setter(
                TabItem.ForegroundProperty,
                new SolidColorBrush(theme.AccentText),
                "TabBorder"));

        selected.Setters.Add(
            new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(theme.Accent),
                "TabBorder"));

        template.Triggers.Add(selected);

        tab.Template = template;

        return tab;
    }
}