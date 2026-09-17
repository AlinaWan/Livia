using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Livia.UI;

internal static class CommonRow
{
    public static Grid Create(
        string label,
        CommonTheme theme)
    {
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
                Width = new GridLength(1, GridUnitType.Star)
            });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = GridLength.Auto
            });

        var labelText = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(theme.SecondaryText),
            VerticalAlignment = VerticalAlignment.Center,
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

        return grid;
    }

    public static void SetControl(
        Grid row,
        UIElement control)
    {
        Grid.SetColumn(control, 2);
        row.Children.Add(control);
    }
}