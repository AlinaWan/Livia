using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Livia.UI;

public static class CommonHelpStep
{
    public static UIElement Create(
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
            Background =
                new SolidColorBrush(theme.Accent),
            Margin = new Thickness(0, 0, 10, 0)
        };

        badge.Child = new TextBlock
        {
            Text = number,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground =
                new SolidColorBrush(theme.AccentText),
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
            Foreground =
                new SolidColorBrush(theme.PrimaryText),
            VerticalAlignment =
                VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        Grid.SetColumn(description, 1);
        grid.Children.Add(description);

        return grid;
    }
}