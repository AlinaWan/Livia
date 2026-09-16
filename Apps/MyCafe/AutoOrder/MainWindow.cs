using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Common;
using WindowsInput;
using WindowsInput.Native;

namespace AutoOrder;

public sealed class MainWindow : CommonWindow
{
    // -------------------------------------------------------------------------
    // Win32 Native Imports
    // -------------------------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    // -------------------------------------------------------------------------
    // Native Constants
    // -------------------------------------------------------------------------

    private const int HotkeyId = 9000;
    private const uint VkF6 = 0x75;

    // -------------------------------------------------------------------------
    // Input Simulator Instance
    // -------------------------------------------------------------------------

    private readonly InputSimulator _inputSim = new();

    // -------------------------------------------------------------------------
    // Application State
    // -------------------------------------------------------------------------

    private int _loopCount = 10;
    private int _pressDelay = 100;

    private CancellationTokenSource? _cts;
    private bool _isRunning;

    // -------------------------------------------------------------------------
    // Application UI
    // -------------------------------------------------------------------------

    private TextBox _debugLog = null!;

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    public MainWindow()
        : base(CreateWindowOptions())
    {
        Body = BuildContent();

        StatusBar.SetStatus(
            "Status: Suspended (Press F6 to toggle)",
            Color.FromRgb(50, 50, 50));
    }

    private static CommonWindowOptions CreateWindowOptions()
    {
        return new CommonWindowOptions
        {
            Title = "My Cafe | Auto Order",
            Header = "Auto Order",
            HeaderIcon = "☕",
            Author = "by angelina",

            Width = 420,
            Height = 410,
            Topmost = true,

            Theme = new CommonTheme
            {
                TitleBar = Color.FromRgb(48, 26, 15),

                HeaderTop = Color.FromRgb(48, 26, 15),
                HeaderBottom = Color.FromRgb(32, 17, 10),

                Background = Color.FromRgb(16, 9, 3),

                HeaderBorder =
                    Color.FromArgb(60, 220, 180, 140),

                PrimaryText =
                    Color.FromRgb(235, 200, 165),

                SecondaryText =
                    Color.FromRgb(190, 190, 190),

                MutedText =
                    Color.FromRgb(130, 110, 95),

                Surface =
                    Color.FromRgb(30, 30, 30),

                SurfaceDark =
                    Color.FromRgb(18, 18, 18),

                Border =
                    Color.FromRgb(45, 45, 45),

                InputBackground =
                    Color.FromRgb(36, 36, 36),

                InputBorder =
                    Color.FromRgb(55, 55, 55),

                Accent =
                    Color.FromRgb(61, 35, 20),

                AccentText =
                    Color.FromRgb(230, 200, 170),

                Success =
                    Color.FromRgb(40, 90, 50),

                Error =
                    Color.FromRgb(180, 40, 50)
            }
        };
    }

    // -------------------------------------------------------------------------
    // Window Lifetime
    // -------------------------------------------------------------------------

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        var source = HwndSource.FromHwnd(helper.Handle);

        source?.AddHook(HwndHook);

        if (!RegisterHotKey(
                helper.Handle,
                HotkeyId,
                0,
                VkF6))
        {
            Log("WARNING: Failed to register F6 hotkey.");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);

        UnregisterHotKey(
            helper.Handle,
            HotkeyId);

        _cts?.Cancel();
        _cts?.Dispose();

        base.OnClosed(e);
    }

    private IntPtr HwndHook(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        const int WmHotkey = 0x0312;

        if (msg == WmHotkey &&
            wParam.ToInt32() == HotkeyId)
        {
            ToggleMacro();
            handled = true;
        }

        return IntPtr.Zero;
    }

    // -------------------------------------------------------------------------
    // UI Construction
    // -------------------------------------------------------------------------

    private UIElement BuildContent() => BuildTabs();

    private TabControl BuildTabs()
    {
        var tabs = new TabControl
        {
            Margin = new Thickness(12, 10, 12, 10),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        tabs.Template = CreateTabControlTemplate();

        tabs.Items.Add(BuildMainTab());
        tabs.Items.Add(BuildDebugTab());
        tabs.Items.Add(BuildHelpTab());
        tabs.Items.Add(BuildCreditsTab());

        return tabs;
    }

    private static ControlTemplate CreateTabControlTemplate()
    {
        var template = new ControlTemplate(typeof(TabControl));

        var panel = new FrameworkElementFactory(
            typeof(DockPanel));

        var headerPanel = new FrameworkElementFactory(
            typeof(StackPanel));

        headerPanel.SetValue(
            StackPanel.OrientationProperty,
            Orientation.Horizontal);

        headerPanel.SetValue(
            DockPanel.DockProperty,
            Dock.Top);

        headerPanel.SetValue(
            FrameworkElement.HorizontalAlignmentProperty,
            HorizontalAlignment.Center);

        headerPanel.SetValue(
            FrameworkElement.MarginProperty,
            new Thickness(0, 0, 0, 10));

        headerPanel.SetValue(
            StackPanel.IsItemsHostProperty,
            true);

        var content = new FrameworkElementFactory(
            typeof(ContentPresenter));

        content.SetValue(
            DockPanel.DockProperty,
            Dock.Bottom);

        content.SetValue(
            ContentPresenter.ContentSourceProperty,
            "SelectedContent");

        panel.AppendChild(headerPanel);
        panel.AppendChild(content);

        template.VisualTree = panel;

        return template;
    }

    // -------------------------------------------------------------------------
    // Main Tab
    // -------------------------------------------------------------------------

    private TabItem BuildMainTab()
    {
        var tab = CommonTab.Create(
            "Main",
            Theme);

        var stack = new StackPanel
        {
            Margin = new Thickness(5)
        };

        stack.Children.Add(new TextBlock
        {
            Text = "CONFIGURATION",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,

            Foreground = new SolidColorBrush(
                Theme.MutedText),

            Margin = new Thickness(0, 5, 0, 15)
        });

        stack.Children.Add(
            CommonControls.CreateInputRow(
                "Item Count:",
                _loopCount,
                value =>
                {
                    _loopCount = value;
                    Log($"Loop count set to: {_loopCount}");
                },
                Theme));

        stack.Children.Add(
            CommonControls.CreateInputRow(
                "Press Delay (ms):",
                _pressDelay,
                value =>
                {
                    _pressDelay = value;
                    Log($"Delay set to: {_pressDelay} ms");
                },
                Theme));

        tab.Content = stack;

        return tab;
    }

    // -------------------------------------------------------------------------
    // Debug Tab
    // -------------------------------------------------------------------------

    private TabItem BuildDebugTab()
    {
        var tab = CommonTab.Create(
            "Debug",
            Theme);

        var card = new Border
        {
            Background = new SolidColorBrush(
                Theme.SurfaceDark),

            CornerRadius = new CornerRadius(6),

            Padding = new Thickness(6),

            BorderBrush = new SolidColorBrush(
                Theme.Border),

            BorderThickness = new Thickness(1)
        };

        _debugLog = new TextBox
        {
            IsReadOnly = true,

            TextWrapping = TextWrapping.Wrap,

            VerticalScrollBarVisibility =
                ScrollBarVisibility.Auto,

            Background = Brushes.Transparent,

            Foreground =
                new SolidColorBrush(
                    Color.FromRgb(180, 200, 180)),

            FontFamily =
                new FontFamily(
                    "Consolas, Courier New, Monospace"),

            FontSize = 11,

            BorderThickness = new Thickness(0),

            Padding = new Thickness(4)
        };

        card.Child = _debugLog;

        tab.Content = card;

        return tab;
    }

    // -------------------------------------------------------------------------
    // Help Tab
    // -------------------------------------------------------------------------

    private TabItem BuildHelpTab()
    {
        var tab = CommonTab.Create(
            "Help",
            Theme);

        var card = new Border
        {
            Background = new SolidColorBrush(
                Theme.Surface),

            CornerRadius = new CornerRadius(6),

            Padding = new Thickness(15),

            BorderBrush = new SolidColorBrush(
                Theme.Border),

            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel();

        stack.Children.Add(
            CommonControls.CreateHelpStep(
                "1",
                "Set Item Count to total Drinks + Toppings.",
                Theme));

        stack.Children.Add(
            CommonControls.CreateHelpStep(
                "2",
                "Open Order tab in main menu.",
                Theme));

        stack.Children.Add(
            CommonControls.CreateHelpStep(
                "3",
                "Press '\\' to enable UI navigation.",
                Theme));

        stack.Children.Add(
            CommonControls.CreateHelpStep(
                "4",
                "Press 'F6' to Start / Stop macro.",
                Theme));

        card.Child = stack;

        tab.Content = card;

        return tab;
    }

    // -------------------------------------------------------------------------
    // Credits Tab
    // -------------------------------------------------------------------------

    private TabItem BuildCreditsTab()
    {
        var tab = CommonTab.Create(
            "Credits",
            Theme);

        var card = new Border
        {
            Background = new SolidColorBrush(
                Theme.Surface),

            CornerRadius = new CornerRadius(6),

            Padding = new Thickness(15),

            BorderBrush = new SolidColorBrush(
                Theme.Border),

            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel();

        stack.Children.Add(new TextBlock
        {
            Text = "DEVELOPMENT",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,

            Foreground = new SolidColorBrush(
                Theme.MutedText),

            Margin = new Thickness(0, 0, 0, 4)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Created by Angelina <github.com/AlinaWan>",
            FontSize = 12,

            Margin = new Thickness(0, 0, 0, 15)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "SPECIAL THANKS",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,

            Foreground = new SolidColorBrush(
                Theme.MutedText),

            Margin = new Thickness(0, 0, 0, 4)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Contributors & Testers",
            FontSize = 12,

            Margin = new Thickness(0, 0, 0, 15)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "LICENSE",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,

            Foreground = new SolidColorBrush(
                Theme.MutedText),

            Margin = new Thickness(0, 0, 0, 4)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "MIT | Copyright (c) 2026 Angelina",
            FontSize = 12
        });

        card.Child = stack;

        tab.Content = card;

        return tab;
    }

    // -------------------------------------------------------------------------
    // Status / Logging
    // -------------------------------------------------------------------------

    private void SetStatus(
        string text,
        Color color)
    {
        StatusBar.SetStatus(
            text,
            color);
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            if (_debugLog == null)
                return;

            var time = DateTime.Now.ToString("HH:mm:ss");

            var line =
                $"[{time}] {message}{Environment.NewLine}";

            _debugLog.Text =
                line + _debugLog.Text;

            if (_debugLog.Text.Length > 3000)
            {
                _debugLog.Text =
                    _debugLog.Text[..3000];
            }
        });
    }

    // -------------------------------------------------------------------------
    // Macro Control
    // -------------------------------------------------------------------------

    private void ToggleMacro()
    {
        if (_isRunning)
        {
            _cts?.Cancel();
            return;
        }

        _isRunning = true;

        _cts = new CancellationTokenSource();

        SetStatus(
            "F6: Start/Stop | Status: Running",
            Theme.Success);

        Log("Started");

        _ = Task.Run(
            () => DoWorkAsync(_cts.Token));
    }

    private async Task DoWorkAsync(
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Center mouse using normalized absolute screen coordinates (0 to 65535).
                _inputSim.Mouse.MoveMouseToPositionOnVirtualDesktop(32767, 32767);

                // Relative mouse movement.
                _inputSim.Mouse.MoveMouseBy(1, 1);

                // Return order listbox (Scroll up).
                for (int i = 0;
                     i < _loopCount * 2;
                     i++)
                {
                    // 1 click up corresponds to a delta of 120 (1 unit in InputSimulator).
                    _inputSim.Mouse.VerticalScroll(1);

                    await Task.Delay(
                        3,
                        token);
                }

                await Task.Delay(
                    _pressDelay,
                    token);

                // Return UI navigation position.
                // This should return the highlighter to either of the 3 buttons on the top dock.
                for (int i = 0; i < 50; i++)
                {
                    _inputSim.Keyboard.KeyPress(VirtualKeyCode.UP);

                    await Task.Delay(
                        1,
                        token);
                }

                // Move to order listbox.
                // This should move the highlighter to either the Drinks or Toppings button immediately above the listbox.
                // At this point the next 2 down movements should move the highligher to the first item.
                _inputSim.Keyboard.KeyPress(VirtualKeyCode.DOWN);

                await Task.Delay(
                    _pressDelay,
                    token);

                // Main execution loop.
                for (int i = 0;
                     i < _loopCount;
                     i++)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        _inputSim.Keyboard.KeyPress(VirtualKeyCode.DOWN);

                        await Task.Delay(
                            _pressDelay,
                            token);
                    }

                    // Purchase this item.
                    for (int e = 0; e < 10; e++)
                    {
                        _inputSim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                        await Task.Delay(
                            1,
                            token);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the macro is stopped.
        }
        finally
        {
            _isRunning = false;

            await Dispatcher.InvokeAsync(() =>
            {
                SetStatus(
                    "F6: Start/Stop | Status: Suspended",
                    Color.FromRgb(50, 50, 50));

                Log("Stopped");
            });
        }
    }
}