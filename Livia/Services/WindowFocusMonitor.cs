using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Livia.Native;

namespace Livia.Services;

public sealed class WindowFocusMonitor : IDisposable
{
    private readonly string _executableName;

    private readonly object _sync = new();

    private Thread? _thread;
    private uint _threadId;

    private User32.WinEventDelegate? _callback;
    private IntPtr _hook;

    private bool _running;
    private bool _isFocused;

    private IntPtr _focusedWindow;

    public bool IsFocused
    {
        get
        {
            lock (_sync)
            {
                return _isFocused;
            }
        }
    }

    public event EventHandler? Focused;

    public event EventHandler? Unfocused;

    public WindowFocusMonitor(string executableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            executableName);

        _executableName =
            Path.GetFileName(executableName);
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_running)
                return;

            _running = true;
        }

        _thread = new Thread(MonitorThread)
        {
            IsBackground = true,
            Name = $"Livia Window Focus Monitor ({_executableName})"
        };

        _thread.Start();
    }

    public void Stop()
    {
        Thread? thread;

        lock (_sync)
        {
            if (!_running)
                return;

            _running = false;
            thread = _thread;
        }

        if (_threadId != 0)
        {
            User32.PostThreadMessage(
                _threadId,
                User32.WM_QUIT,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        if (thread != null &&
            thread != Thread.CurrentThread)
        {
            thread.Join();
        }

        lock (_sync)
        {
            _thread = null;
            _threadId = 0;
            _hook = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void MonitorThread()
    {
        _threadId =
            User32.GetCurrentThreadId();

        _callback =
            HandleWinEvent;

        _hook = User32.SetWinEventHook(
            User32.EVENT_SYSTEM_FOREGROUND,
            User32.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _callback,
            0,
            0,
            User32.WINEVENT_OUTOFCONTEXT |
            User32.WINEVENT_SKIPOWNPROCESS);

        if (_hook == IntPtr.Zero)
        {
            lock (_sync)
            {
                _running = false;
            }

            return;
        }

        UpdateFocus(
            User32.GetForegroundWindow());

        while (User32.GetMessage(
                   out _,
                   IntPtr.Zero,
                   0,
                   0) > 0)
        {
        }

        User32.UnhookWinEvent(_hook);

        _hook = IntPtr.Zero;
        _callback = null;
    }

    private void HandleWinEvent(
        IntPtr hook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime)
    {
        if (eventType !=
            User32.EVENT_SYSTEM_FOREGROUND)
        {
            return;
        }

        UpdateFocus(hwnd);
    }

    private void UpdateFocus(IntPtr hwnd)
    {
        bool focused =
            IsTargetWindow(hwnd);

        bool changed;

        lock (_sync)
        {
            changed =
                focused != _isFocused;

            if (!changed)
                return;

            _isFocused = focused;
            _focusedWindow =
                focused
                    ? hwnd
                    : IntPtr.Zero;
        }

        if (focused)
        {
            Focused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Unfocused?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool IsTargetWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        User32.GetWindowThreadProcessId(
            hwnd,
            out uint processId);

        if (processId == 0)
            return false;

        try
        {
            using var process =
                Process.GetProcessById(
                    (int)processId);

            string? path =
                process.MainModule?.FileName;

            if (string.IsNullOrWhiteSpace(path))
                return false;

            return string.Equals(
                Path.GetFileName(path),
                _executableName,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}