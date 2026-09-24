using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Livia;
using Livia.Services;
using Livia.Services.Input;
using Livia.UI;
using Livia.UI.Controls;
using Livia.Utils;

namespace AutoOrder;

public sealed class MainWindow : CommonWindow
{
    // -------------------------------------------------------------------------
    // Input Simulator Instance and Hotkey Instance
    // -------------------------------------------------------------------------

    private readonly InputSimulationService _inputSim = new();
    private readonly HotkeyService _hotkeys;

    // -------------------------------------------------------------------------
    // Application State
    // -------------------------------------------------------------------------

    private int _loopCount = 14;
    private int _pressDelay = 100;
    private bool _stopOnUnfocus = true;
    private bool _rejoinOnDisconnect = false;
    private string _rejoinUrl = "";

    private CancellationTokenSource? _cts;
    private WindowFocusMonitor? _focusMonitor;
    private RobloxLogMonitor? _logMonitor;
    private bool _isRunning;
    private bool _isRejoining;

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

        _hotkeys = new HotkeyService(this);

        _hotkeys.Register(
            VirtualKeys.F6,
            ModifierKeys.ModNoRepeat,
            ToggleMacro);

        _hotkeys.Register(
            VirtualKeys.Escape,
            ModifierKeys.None,
            () => SystemUtils.AbortSystemShutdown());
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
            Height = 460,
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

        _focusMonitor = new WindowFocusMonitor("RobloxPlayerBeta.exe");
        _focusMonitor.Unfocused += OnTargetWindowUnfocused;
        _focusMonitor.Start();

        _logMonitor = new RobloxLogMonitor();
        _logMonitor.Start(new Dictionary<string, Action<Match>>
        {
            [@"\[FLog::Network\] Time to disconnect replication data: ([\d.]+)"] = OnRobloxDisconnected
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_focusMonitor != null)
        {
            _focusMonitor.Unfocused -= OnTargetWindowUnfocused;
            _focusMonitor.Dispose();
        }

        _hotkeys.Dispose();

        _cts?.Cancel();
        _cts?.Dispose();

        base.OnClosed(e);
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
            Foreground = new SolidColorBrush(Theme.MutedText),
            Margin = new Thickness(0, 5, 0, 15)
        });

        stack.Children.Add(
            CommonIntegerInput.CreateRow(
                "Item Count:",
                _loopCount,
                value =>
                {
                    _loopCount = value;
                    Log($"Loop count set to: {_loopCount}");
                },
                Theme,
                validator: val => val > 0));

        stack.Children.Add(
            CommonIntegerInput.CreateRow(
                "Press Delay (ms):",
                _pressDelay,
                value =>
                {
                    _pressDelay = value;
                    Log($"Delay set to: {_pressDelay} ms");
                },
                Theme));

        stack.Children.Add(
            CommonSegmentedToggle.CreateRow(
                "Stop on Unfocus:",
                _stopOnUnfocus,
                isEnabled =>
                {
                    _stopOnUnfocus = isEnabled;
                    Log($"Stop on unfocus: {_stopOnUnfocus}");
                },
                Theme));

        stack.Children.Add(
            CommonSegmentedToggle.CreateRow(
                "Rejoin on Disconnect:",
                _rejoinOnDisconnect,
                isEnabled =>
                {
                    _rejoinOnDisconnect = isEnabled;
                    Log($"Rejoin on disconnect: {_rejoinOnDisconnect}");
                },
                Theme));

        stack.Children.Add(
            CommonActionInput.CreateRow(
                "Rejoin URL:",
                _rejoinUrl,
                GetRejoinURL,
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

        CommonScrollBar.Apply(card, Theme);

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
            CommonHelpStep.Create(
                "1",
                "Set Item Count to total Drinks + Toppings.",
                Theme));

        stack.Children.Add(
            CommonHelpStep.Create(
                "2",
                "Open Order tab in main menu.",
                Theme));

        stack.Children.Add(
            CommonHelpStep.Create(
                "3",
                "Press '\\' to enable UI navigation.",
                Theme));

        stack.Children.Add(
            CommonHelpStep.Create(
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
    // Event Handlers
    // -------------------------------------------------------------------------
    private async Task<string?> GetRejoinURL()
    {
        var securityToken = await Task.Run(() =>
            BrowserUtils.GetCookieValue(
                ".roblox.com",
                ".ROBLOSECURITY"));

        if (string.IsNullOrEmpty(securityToken))
        {
            return null;
        }

        return await RobloxServerUtils.GetPrivateServerJoinLinkAsync(
            "https://www.roblox.com/games/133345376331809",
            null,
            securityToken);
    }

    private void OnTargetWindowUnfocused(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            // Do NOT stop if we are currently executing the rejoin sequence
            if (_stopOnUnfocus && _isRunning && !_isRejoining)
            {
                Log("Roblox focus lost. Suspending macro.");
                _cts?.Cancel();
            }
        });
    }
    private void OnRobloxDisconnected(Match match)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            if (!_isRunning || _isRejoining)
                return;

            // Set flag immediately
            _isRejoining = true;

            Log("Disconnect detected.");

            if (_rejoinOnDisconnect)
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                await HandleRejoinAsync(_cts.Token);
            }
            else
            {
                _isRejoining = false;
                _cts?.Cancel();
            }
        });
    }

    private async Task HandleRejoinAsync(CancellationToken token)
    {
        _isRejoining = true;
        SetStatus("Status: Reconnecting to Roblox...", Theme.Warning);
        Log("Initiating auto-rejoin...");

        const int maxAttempts = 3;
        TimeSpan detectionTimeout = TimeSpan.FromSeconds(60); // Timeout per attempt
        Process? newProcess = null;

        try
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                Log($"Rejoin attempt {attempt} of {maxAttempts}...");

                // Capture existing PID before attempting launch
                int oldPid = GetRobloxProcessId();

                // Open rejoin link in browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = _rejoinUrl,
                    UseShellExecute = true
                });

                Log("Waiting for Roblox process to reload...");

                // Poll for a new PID until detectionTimeout is reached
                DateTime startTime = DateTime.UtcNow;
                while (newProcess == null && (DateTime.UtcNow - startTime) < detectionTimeout)
                {
                    await Task.Delay(1000, token);

                    var process = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
                    if (process != null && process.Id != oldPid)
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            newProcess = process;
                        }
                    }
                }

                if (newProcess != null)
                {
                    break; // Successfully detected new PID
                }

                Log($"Attempt {attempt} timed out waiting for new process.");
            }

            if (newProcess == null)
            {
                Log("Failed to detect new Roblox instance after 3 attempts.");
                SetStatus("Status: Rejoin Failed", Theme.Error);
                SystemUtils.InitiateSystemShutdown(30, "Shutting down due to rejoin failure. Press Esc to abort.");
                return;
            }

            Log($"New Roblox instance detected (PID: {newProcess.Id}). Waiting for game load...");

            // Give Roblox time to load into the map/UI
            await Task.Delay(20000, token);

            // Force focus the Roblox window
            if (!WindowUtils.ForceFocusWindow(newProcess.MainWindowHandle))
            {
                Log("Failed to focus Roblox window.");
            }
            else
            {
                Log("Roblox window focused.");
            }

            await Task.Delay(5000, token); // Additional wait to let the window focus

            Log("Rejoin completed successfully. Restarting macro.");

            // This will skip the loading screen if it still hasn't loaded after the wait
            _inputSim.Mouse.MoveMouseToPositionOnVirtualDesktop(32767, 32767);
            _inputSim.Mouse.MoveMouseBy(1, 1);
            _inputSim.Mouse.LeftClick();

            // Enable UI navigation mode (press '\')
            _inputSim.Keyboard.KeyPress(VirtualKeys.OEM5);

            // Now we need to navigate to the main menu and re-enter the order tab.
            // At this point we should be on the top right button row on the very rightmost button
            for (int i = 0; i < 25; i++)
            {
                _inputSim.Keyboard.KeyPress(VirtualKeys.Up);
                await Task.Delay(1, token);

                _inputSim.Keyboard.KeyPress(VirtualKeys.Right);
                await Task.Delay(1, token);
            }
            await Task.Delay(_pressDelay, token);

            // Move to the main menu button on to top middle button row
            for (int i = 0; i < 5; i++)
            {
                _inputSim.Keyboard.KeyPress(VirtualKeys.Left);
                await Task.Delay(_pressDelay, token);
            }

            // Enter main menu
            _inputSim.Keyboard.KeyPress(VirtualKeys.Return);
            await Task.Delay(_pressDelay, token);

            // Navigate to the order tab (2nd button in the left button row)
            _inputSim.Keyboard.KeyPress(VirtualKeys.Down);
            await Task.Delay(_pressDelay, token);

            _inputSim.Keyboard.KeyPress(VirtualKeys.Left);
            await Task.Delay(_pressDelay, token);

            for (int i = 0; i < 2; i++)
            {
                _inputSim.Keyboard.KeyPress(VirtualKeys.Down);
                await Task.Delay(_pressDelay, token);
            }

            _inputSim.Keyboard.KeyPress(VirtualKeys.Return);
            await Task.Delay(_pressDelay, token);

            // Reset state flags and restart macro execution
            _isRejoining = false;
            _isRunning = false; // Reset flag so ToggleMacro can start clean

            ToggleMacro();
        }
        catch (OperationCanceledException)
        {
            Log("Rejoin cancelled.");
            _isRejoining = false;
            _isRunning = false;
            SetStatus("F6: Start/Stop | Status: Suspended", Color.FromRgb(50, 50, 50));
        }
        catch (Exception ex)
        {
            Log($"Rejoin failed: {ex.Message}");
            _isRejoining = false;
            _isRunning = false;

            SetStatus("Status: Rejoin Failed", Theme.Error);
            SystemUtils.InitiateSystemShutdown(30, "Shutting down due to rejoin failure. Press Esc to abort.");
        }
    }

    private int GetRobloxProcessId()
    {
        var proc = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
        return proc?.Id ?? 0;
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

        if (_stopOnUnfocus && _focusMonitor != null && !_focusMonitor.IsFocused && !_isRejoining)
        {
            Log("Roblox is not focused. Cannot start macro.");
            return;
        }

        _isRunning = true;
        _cts = new CancellationTokenSource();

        SetStatus("F6: Start/Stop | Status: Running", Theme.Success);
        Log("Started");

        _ = Task.Run(() => DoWorkAsync(_cts.Token));
    }

    private async Task DoWorkAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Return UI navigation position.
                // We should be at any of the top 3 buttons in the top middle button row.
                for (int i = 0; i < 50; i++)
                {
                    _inputSim.Keyboard.KeyPress(VirtualKeys.Up);
                    await Task.Delay(1, token);
                }

                // Center mouse using normalized absolute screen coordinates (0 to 65535).
                _inputSim.Mouse.MoveMouseToPositionOnVirtualDesktop(32767, 32767);

                // Relative mouse movement.
                _inputSim.Mouse.MoveMouseBy(1, 1);

                // Return order listbox (Scroll up).
                for (int i = 0; i < _loopCount * 2; i++)
                {
                    _inputSim.Mouse.VerticalScroll(1);
                    await Task.Delay(3, token);
                }

                await Task.Delay(_pressDelay, token);

                // Navigate back to the top middle button row
                // Here, it can highlight any of the top 4 buttons. After update 1 which added the 4th button, this is
                // a problem, because the 1st button in the dock is no longer aligned with the order listbox.
                // Lets just use the starting reference to the top right as we do in the rejoin sequence.
                for (int i = 0; i < 20; i++)
                {
                    _inputSim.Keyboard.KeyPress(VirtualKeys.Up);
                    await Task.Delay(1, token);

                    _inputSim.Keyboard.KeyPress(VirtualKeys.Right);
                    await Task.Delay(1, token);
                }
                await Task.Delay(_pressDelay, token);

                // 3 left presses should get us to 4th button in the top middle button row, which seems the safest
                for (int i = 0; i < 3; i++)
                {
                    _inputSim.Keyboard.KeyPress(VirtualKeys.Left);
                    await Task.Delay(_pressDelay, token);
                }

                // Navigate down to the menu
                // This should highlight either the Drinks or Toppings button
                // The next 2 down presses will highlight the first item in the list
                _inputSim.Keyboard.KeyPress(VirtualKeys.Down);
                await Task.Delay(1, token);

                // Main execution loop.
                for (int i = 0; i < _loopCount; i++)
                {
                    for (int d = 0; d < 2; d++)
                    {
                        _inputSim.Keyboard.KeyPress(VirtualKeys.Down);
                        await Task.Delay(_pressDelay, token);
                    }

                    // Purchase this item.
                    for (int e = 0; e < 10; e++)
                    {
                        _inputSim.Keyboard.KeyPress(VirtualKeys.Return);
                        await Task.Delay(1, token);
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
            // Only update "Stopped" UI state if we are not actively rejoining
            if (!_isRejoining)
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

    // Call on Window Closing / Shutdown
    public void Cleanup()
    {
        _logMonitor?.Dispose();
    }
}