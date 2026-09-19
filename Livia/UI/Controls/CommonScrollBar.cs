using System;
using System.Windows;
using System.Windows.Media;

namespace Livia.UI.Controls;

/// <summary>
/// Provides the common Livia scrollbar style.
/// </summary>
public static class CommonScrollBar
{
    private const string ResourceUri =
        "pack://application:,,,/Livia;component/UI/Controls/CommonScrollBar.xaml";

    /// <summary>
    /// Applies the common Livia scrollbar style using the default options.
    /// </summary>
    /// <param name="element">
    /// The element whose resource hierarchy should contain the style.
    /// </param>
    /// <param name="theme">
    /// The theme used for scrollbar colors.
    /// </param>
    public static void Apply(
        FrameworkElement element,
        CommonTheme theme)
    {
        Apply(
            element,
            theme,
            new CommonScrollBarOptions());
    }

    /// <summary>
    /// Applies the common Livia scrollbar style using the specified options.
    /// </summary>
    /// <param name="element">
    /// The element whose resource hierarchy should contain the style.
    /// </param>
    /// <param name="theme">
    /// The theme used for scrollbar colors.
    /// </param>
    /// <param name="options">
    /// The scrollbar appearance and behavior options.
    /// </param>
    public static void Apply(
        FrameworkElement element,
        CommonTheme theme,
        CommonScrollBarOptions options)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(options);

        var resources = new ResourceDictionary
        {
            Source = new Uri(ResourceUri)
        };

        resources["CommonScrollBarBackgroundBrush"] =
            new SolidColorBrush(
                theme.ScrollBarBackground);

        resources["CommonScrollBarThumbBrush"] =
            new SolidColorBrush(
                theme.ScrollBarThumb);

        resources["CommonScrollBarWidth"] =
            options.Width;

        resources["CommonScrollBarHeight"] =
            options.Height;

        resources["CommonScrollBarCornerRadius"] =
            new CornerRadius(
                options.CornerRadius);

        resources["CommonScrollBarTrackMargin"] =
            new Thickness(
                options.TrackMargin);

        resources["CommonScrollBarArrowVisibility"] =
            options.ShowArrows
                ? Visibility.Visible
                : Visibility.Collapsed;

        element.Resources.MergedDictionaries.Add(
            resources);
    }
}