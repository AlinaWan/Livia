using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Common.Theme;

namespace Common.Controls;

public sealed class CommonStatusBar : Border
{
    private readonly TextBlock _label;

    public CommonStatusBar(CommonTheme theme)
    {
        Height = 32;
        Padding = new Thickness(10, 0, 10, 0);

        _label = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,

            HorizontalAlignment =
                HorizontalAlignment.Center,

            VerticalAlignment =
                VerticalAlignment.Center,

            Foreground = new SolidColorBrush(
                theme.PrimaryText)
        };

        Child = _label;
    }

    public void SetStatus(string text, Color background)
    {
        _label.Text = text;
        Background = new SolidColorBrush(background);
    }
}