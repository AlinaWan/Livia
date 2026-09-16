using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Livia.Native;

namespace Livia.UI;

public class CommonWindow : Window
{
    private readonly CommonWindowOptions _options;
    private readonly ContentPresenter _bodyPresenter;
    protected CommonStatusBar StatusBar
    {
        get;
    }

    public CommonTheme Theme => _options.Theme;

    public UIElement? Body
    {
        get => _bodyPresenter.Content as UIElement;
        set => _bodyPresenter.Content = value;
    }

    public CommonWindow(CommonWindowOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _bodyPresenter = new ContentPresenter();
        StatusBar = new CommonStatusBar(options.Theme);

        Title = options.Title;
        Width = options.Width;
        Height = options.Height;
        Topmost = options.Topmost;
        WindowStartupLocation = options.StartupLocation;
        Background = new SolidColorBrush(options.Theme.Background);

        BuildWindow();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        SetTitleBarColor(
            new WindowInteropHelper(this),
            Theme.TitleBar);
    }

    public static void SetTitleBarColor(
        WindowInteropHelper window,
        Color color)
    {
        IntPtr hwnd = window.EnsureHandle();

        int bgrColor =
            (color.B << 16) |
            (color.G << 8) |
            color.R;

        // Delegates P/Invoke execution to Common.Native
        DwmApi.DwmSetWindowAttribute(
            hwnd,
            DwmApi.DWMWA_CAPTION_COLOR,
            ref bgrColor,
            sizeof(int));
    }

    private static string GetAppVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;

        return version != null
            ? $"version {version.Major}.{version.Minor}.{version.Build}, revision {version.Revision}"
            : "unknown version";
    }

    private static string GetEngineVersion()
    {
        var version = typeof(CommonWindow).Assembly.GetName().Version;

        return version != null
            ? $"livia {version.Major}.{version.Minor}.{version.Build}, revision {version.Revision}"
            : "unknown engine version";
    }

    private void BuildWindow()
    {
        var root = new Grid();

        root.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            });

        root.RowDefinitions.Add(
            new RowDefinition
            {
                Height = new GridLength(
                    1,
                    GridUnitType.Star)
            });

        root.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            });

        if (!string.IsNullOrWhiteSpace(_options.Header))
        {
            var header = CreateHeader();

            Grid.SetRow(header, 0);
            root.Children.Add(header);
        }

        Grid.SetRow(_bodyPresenter, 1);
        root.Children.Add(_bodyPresenter);

        var bottom = new Grid();

        bottom.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            });

        bottom.RowDefinitions.Add(
            new RowDefinition
            {
                Height = GridLength.Auto
            });

        var versionText = new TextBlock
        {
            Text =
                $"{GetAppVersion()}\n{GetEngineVersion()}",

            FontSize = 9,

            Foreground = new SolidColorBrush(
                Theme.MutedText),

            HorizontalAlignment =
                HorizontalAlignment.Right,

            Margin = new Thickness(0, 0, 12, 4)
        };

        Grid.SetRow(versionText, 0);
        bottom.Children.Add(versionText);

        Grid.SetRow(StatusBar, 1);
        bottom.Children.Add(StatusBar);

        Grid.SetRow(bottom, 2);
        root.Children.Add(bottom);

        Content = root;
    }

    private Border CreateHeader()
    {
        const double headerHeight = 61;

        var headerBorder = new Border
        {
            Height = headerHeight,

            Background = new LinearGradientBrush(
                Theme.HeaderTop,
                Theme.HeaderBottom,
                new Point(0.5, 0),
                new Point(0.5, 1)),

            BorderBrush = new SolidColorBrush(
                Theme.HeaderBorder),

            BorderThickness = new Thickness(0, 0, 0, 1),

            ClipToBounds = true,

            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 3,
                Opacity = 0.4,
                BlurRadius = 8
            }
        };

        var headerGrid = new Grid();

        // Normal header content.
        var headerStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 8, 20, 8)
        };

        var titleText = new TextBlock
        {
            Text = _options.Header,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,

            Foreground = new SolidColorBrush(
                Theme.PrimaryText),

            HorizontalAlignment =
                HorizontalAlignment.Center
        };

        headerStack.Children.Add(titleText);

        if (!string.IsNullOrWhiteSpace(_options.Author))
        {
            headerStack.Children.Add(new TextBlock
            {
                Text = _options.Author,
                FontSize = 16,

                FontFamily = new FontFamily(
                    "Brush Script MT, Segoe Script, Corsiva, Cursive"),

                Foreground = new SolidColorBrush(
                    Theme.MutedText),

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                Margin = new Thickness(0, 1, 0, 0)
            });
        }

        Panel.SetZIndex(headerStack, 1);
        headerGrid.Children.Add(headerStack);

        // Decorative icon layer.
        if (!string.IsNullOrWhiteSpace(_options.HeaderIcon))
        {
            var icon = new TextBlock
            {
                Text = _options.HeaderIcon,
                FontSize = 90,

                Foreground = new SolidColorBrush(
                    Theme.MutedText),

                Opacity = 0.3,

                HorizontalAlignment =
                    HorizontalAlignment.Left,

                // Align to the top of the header grid
                VerticalAlignment =
                    VerticalAlignment.Top,

                Margin = new Thickness(-45, -20, 0, 0),

                IsHitTestVisible = false
            };

            Panel.SetZIndex(icon, 2);
            headerGrid.Children.Add(icon);
        }

        headerBorder.Child = headerGrid;

        return headerBorder;
    }
}