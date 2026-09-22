using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace AutoRejoin;

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

    private int _rejoinRetries = 3;
    private bool _antiAfk = true;
    private bool _rejoinFailShutdown = true;
    private string _rejoinUrl = "";

    private CancellationTokenSource? _cts;
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
            Title = "General | Auto-Rejoin",
            Header = "Auto-Rejoin",
            HeaderIcon = "🔄",
            Author = "by angelina",

            Width = 420,
            Height = 420,
            Topmost = true
        };
    }

    // -------------------------------------------------------------------------
    // Window Lifetime
    // -------------------------------------------------------------------------

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _logMonitor = new RobloxLogMonitor();
        _logMonitor.Start(new Dictionary<string, Action<Match>>
        {
            [@"\[FLog::Network\] Time to disconnect replication data: ([\d.]+)"] = OnRobloxDisconnected
        });
    }

    protected override void OnClosed(EventArgs e)
    {
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

        var panel = new FrameworkElementFactory(typeof(DockPanel));

        var headerPanel = new FrameworkElementFactory(typeof(StackPanel));
        headerPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        headerPanel.SetValue(DockPanel.DockProperty, Dock.Top);
        headerPanel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        headerPanel.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 10));
        headerPanel.SetValue(StackPanel.IsItemsHostProperty, true);

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(DockPanel.DockProperty, Dock.Bottom);
        content.SetValue(ContentPresenter.ContentSourceProperty, "SelectedContent");

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
        var tab = CommonTab.Create("Main", Theme);

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
            CommonStringInput.CreateRow(
                "Rejoin URL:",
                _rejoinUrl,
                value =>
                {
                    _rejoinUrl = value;
                    Log($"Rejoin URL set to: {_rejoinUrl}");
                },
                Theme));

        stack.Children.Add(
            CommonIntegerInput.CreateRow(
                "Max Rejoin Retries:",
                _rejoinRetries,
                value =>
                {
                    _rejoinRetries = value;
                    Log($"Max rejoin retries set to: {_rejoinRetries}");
                },
                Theme,
                validator: val => val > 0));

        stack.Children.Add(
            CommonSegmentedToggle.CreateRow(
                "Anti-AFK:",
                _antiAfk,
                isEnabled =>
                {
                    _antiAfk = isEnabled;
                    Log($"Anti-AFK: {_antiAfk}");
                },
                Theme));

        stack.Children.Add(
            CommonSegmentedToggle.CreateRow(
                "Shutdown on Rejoin Failed:",
                _rejoinFailShutdown,
                isEnabled =>
                {
                    _rejoinFailShutdown = isEnabled;
                    Log($"Shutdown on rejoin failed: {_rejoinFailShutdown}");
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
        var tab = CommonTab.Create("Debug", Theme);

        var card = new Border
        {
            Background = new SolidColorBrush(Theme.SurfaceDark),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            BorderBrush = new SolidColorBrush(Theme.Border),
            BorderThickness = new Thickness(1)
        };

        CommonScrollBar.Apply(card, Theme);

        _debugLog = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 180)),
            FontFamily = new FontFamily("Consolas, Courier New, Monospace"),
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
        var tab = CommonTab.Create("Help", Theme);

        var card = new Border
        {
            Background = new SolidColorBrush(Theme.Surface),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15),
            BorderBrush = new SolidColorBrush(Theme.Border),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel();

        stack.Children.Add(
            CommonHelpStep.Create("1", "Enter a private server URL.", Theme));

        stack.Children.Add(
            CommonHelpStep.Create("2", "Press 'F6' to Start / Stop macro.", Theme));

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

    private void SetStatus(string text, Color color)
    {
        StatusBar.SetStatus(text, color);
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            if (_debugLog == null)
                return;

            var time = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{time}] {message}{Environment.NewLine}";

            _debugLog.Text = line + _debugLog.Text;

            if (_debugLog.Text.Length > 3000)
            {
                _debugLog.Text = _debugLog.Text[..3000];
            }
        });
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    private void OnRobloxDisconnected(Match match)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            if (!_isRunning || _isRejoining)
                return;

            _isRejoining = true;
            Log("Disconnect detected. Triggering rejoin sequence...");

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            await HandleRejoinAsync(_cts.Token);
        });
    }

    private async Task HandleRejoinAsync(CancellationToken token)
    {
        _isRejoining = true;
        SetStatus("Status: Reconnecting to Roblox...", Theme.Warning);

        int maxAttempts = _rejoinRetries;
        TimeSpan detectionTimeout = TimeSpan.FromSeconds(60);
        Process? newProcess = null;

        try
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                Log($"Rejoin attempt {attempt} of {maxAttempts}...");

                int oldPid = GetRobloxProcessId();

                if (string.IsNullOrWhiteSpace(_rejoinUrl))
                {
                    Log("Error: Rejoin URL is empty!");
                    break;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _rejoinUrl,
                    UseShellExecute = true
                });

                Log("Waiting for Roblox process to reload...");

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
                    break;
                }

                Log($"Attempt {attempt} timed out waiting for new process.");
            }

            if (newProcess == null)
            {
                Log($"Failed to detect new Roblox instance after {maxAttempts} attempts.");
                SetStatus("Status: Rejoin Failed", Theme.Error);

                if (_rejoinFailShutdown)
                {
                    SystemUtils.InitiateSystemShutdown(30, "Shutting down due to rejoin failure. Press Esc to abort.");
                }
                return;
            }

            Log($"New Roblox instance detected (PID: {newProcess.Id}). Waiting for game load...");

            await Task.Delay(20000, token);

            if (!WindowUtils.ForceFocusWindow(newProcess.MainWindowHandle))
            {
                Log("Failed to focus Roblox window.");
            }
            else
            {
                Log("Roblox window focused.");
            }

            await Task.Delay(3000, token);

            Log("Rejoin completed successfully. Restarting routine.");

            _isRejoining = false;
            _isRunning = false;

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

            if (_rejoinFailShutdown)
            {
                SystemUtils.InitiateSystemShutdown(30, "Shutting down due to rejoin failure. Press Esc to abort.");
            }
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
                if (_antiAfk)
                {
                    // Move mouse back to center at the start of every loop
                    _inputSim.Mouse.MoveMouseToPositionOnVirtualDesktop(32767, 32767);

                    // 1px relative circular movement pattern
                    _inputSim.Mouse.MoveMouseBy(0, -1);
                    await Task.Delay(250, token);

                    _inputSim.Mouse.MoveMouseBy(1, 0);
                    await Task.Delay(250, token);

                    _inputSim.Mouse.MoveMouseBy(0, 1);
                    await Task.Delay(250, token);

                    _inputSim.Mouse.MoveMouseBy(-1, 0);
                    await Task.Delay(250, token);
                }
                else
                {
                    await Task.Delay(1000, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopped
        }
        finally
        {
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