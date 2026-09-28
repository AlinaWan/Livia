using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Livia.UI.Controls;

public sealed class CommonListInput : UserControl
{
    private readonly CommonTheme _theme;
    private readonly StackPanel _itemsPanel;
    private readonly TextBox _input;

    /// <summary>
    /// Gets the values currently contained in the list.
    /// </summary>
    public ObservableCollection<string> Items
    {
        get;
    }

    /// <summary>
    /// Gets or sets whether duplicate values are allowed.
    /// </summary>
    public bool AllowDuplicates
    {
        get;
        set;
    }

    /// <summary>
    /// Occurs when a value is added through the input.
    /// </summary>
    public event EventHandler<string>? ItemAdded;

    /// <summary>
    /// Occurs when a value is removed through the control.
    /// </summary>
    public event EventHandler<string>? ItemRemoved;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommonListInput"/> class.
    /// </summary>
    /// <param name="theme">The theme used to style the control.</param>
    /// <param name="items">
    /// The initial values to add to the list.
    /// </param>
    public CommonListInput(
        CommonTheme theme,
        IEnumerable<string>? items = null)
    {
        ArgumentNullException.ThrowIfNull(theme);

        _theme = theme;

        Items = new ObservableCollection<string>();

        _itemsPanel = new StackPanel();

        _input = CreateInput();

        _input.KeyDown += Input_KeyDown;

        Items.CollectionChanged +=
            Items_CollectionChanged;

        Build();

        if (items != null)
        {
            foreach (string item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    Items.Add(item);
                }
            }
        }
    }

    private TextBox CreateInput()
    {
        var input = new TextBox
        {
            Height = 26,

            Background =
                new SolidColorBrush(
                    _theme.InputBackground),

            Foreground = Brushes.White,

            BorderThickness =
                new Thickness(1),

            BorderBrush =
                new SolidColorBrush(
                    _theme.InputBorder),

            VerticalContentAlignment =
                VerticalAlignment.Center,

            Padding =
                new Thickness(
                    7,
                    0,
                    7,
                    0),

            Template =
                CreateInputTemplate()
        };

        return input;
    }

    private static ControlTemplate CreateInputTemplate()
    {
        var template =
            new ControlTemplate(
                typeof(TextBox));

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

        var scrollViewer =
            new FrameworkElementFactory(
                typeof(ScrollViewer));

        scrollViewer.Name =
            "PART_ContentHost";

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

    private void Build()
    {
        var layout = new Grid();

        layout.RowDefinitions.Add(
            new RowDefinition
            {
                Height =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });

        layout.RowDefinitions.Add(
            new RowDefinition
            {
                Height =
                    GridLength.Auto
            });

        var listBorder = new Border
        {
            Background =
                new SolidColorBrush(
                    _theme.InputBackground),

            BorderBrush =
                new SolidColorBrush(
                    _theme.InputBorder),

            BorderThickness =
                new Thickness(1),

            CornerRadius =
                new CornerRadius(6),

            Padding =
                new Thickness(3)
        };

        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility =
                ScrollBarVisibility.Auto,

            HorizontalScrollBarVisibility =
                ScrollBarVisibility.Disabled,

            Content = _itemsPanel
        };

        CommonScrollBar.Apply(
            listScroll,
            _theme);

        listBorder.Child = listScroll;

        Grid.SetRow(
            listBorder,
            0);

        layout.Children.Add(
            listBorder);

        var addGrid = new Grid
        {
            Margin =
                new Thickness(
                    0,
                    4,
                    0,
                    0)
        };

        addGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });

        addGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });

        Grid.SetColumn(
            _input,
            0);

        _input.Margin = new Thickness(
            0,
            0,
            4,
            0);

        addGrid.Children.Add(
            _input);

        var addButton = CreateButton("＋");

        addButton.Click += (_, _) =>
        {
            AddInputValue();
        };

        Grid.SetColumn(
            addButton,
            1);

        addGrid.Children.Add(
            addButton);

        Grid.SetRow(
            addGrid,
            1);

        layout.Children.Add(
            addGrid);

        Content = layout;
    }

    private void Items_CollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        RebuildItems();
    }

    private void RebuildItems()
    {
        _itemsPanel.Children.Clear();

        foreach (string item in Items)
        {
            _itemsPanel.Children.Add(
                CreateItem(item));
        }
    }

    private FrameworkElement CreateItem(
        string value)
    {
        var row = new Grid
        {
            Height = 26,

            Margin =
                new Thickness(
                    0,
                    1,
                    0,
                    1)
        };

        row.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });

        row.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    GridLength.Auto
            });

        var text = new TextBlock
        {
            Text = value,

            Foreground =
                new SolidColorBrush(
                    _theme.PrimaryText),

            VerticalAlignment =
                VerticalAlignment.Center,

            Margin =
                new Thickness(
                    7,
                    0,
                    7,
                    0),

            TextTrimming =
                TextTrimming.CharacterEllipsis
        };

        Grid.SetColumn(
            text,
            0);

        row.Children.Add(
            text);

        var removeButton =
            CreateButton("✕");

        removeButton.Click += (_, _) =>
        {
            if (Items.Remove(value))
            {
                ItemRemoved?.Invoke(
                    this,
                    value);
            }
        };

        Grid.SetColumn(
            removeButton,
            1);

        row.Children.Add(
            removeButton);

        return row;
    }

    private Button CreateButton(
        string content)
    {
        var button = new Button
        {
            Content = content,

            Width = 26,
            Height = 26,

            Padding =
                new Thickness(0),

            Background =
                new SolidColorBrush(
                    _theme.InputBackground),

            Foreground =
                new SolidColorBrush(
                    _theme.SecondaryText),

            BorderThickness =
                new Thickness(1),

            BorderBrush =
                new SolidColorBrush(
                    _theme.InputBorder),

            Cursor =
                Cursors.Hand,

            VerticalContentAlignment =
                VerticalAlignment.Center,

            HorizontalContentAlignment =
                HorizontalAlignment.Center
        };

        button.Template =
            CreateButtonTemplate();

        return button;
    }

    private static ControlTemplate CreateButtonTemplate()
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

        var presenter =
            new FrameworkElementFactory(
                typeof(ContentPresenter));

        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            HorizontalAlignment.Center);

        presenter.SetValue(
            ContentPresenter.VerticalAlignmentProperty,
            VerticalAlignment.Center);

        border.AppendChild(
            presenter);

        template.VisualTree = border;

        return template;
    }

    private void Input_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        AddInputValue();

        e.Handled = true;
    }

    private void AddInputValue()
    {
        string value =
            _input.Text.Trim();

        if (value.Length == 0)
        {
            return;
        }

        if (!AllowDuplicates &&
            Items.Contains(value))
        {
            _input.SelectAll();
            _input.Focus();

            return;
        }

        Items.Add(value);

        _input.Clear();
        _input.Focus();

        ItemAdded?.Invoke(
            this,
            value);
    }
}
