using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Livia.Native;

namespace Livia.Services.Input;

public sealed class HotkeyService : IDisposable
{
    private readonly List<RegisteredHotkey> _hotkeys = [];
    private readonly object _sync = new();

    private readonly Window? _window;
    private HwndSource? _source;

    private Thread? _thread;
    private uint _threadId;

    private bool _started;
    private bool _disposed;

    private int _nextId = 1;

    /// <summary>
    /// Creates a framework-independent hotkey service.
    /// Call <see cref="Start"/> after registering the hotkeys.
    /// </summary>
    public HotkeyService()
    {
    }

    /// <summary>
    /// Creates a hotkey service associated with a WPF window.
    /// Hotkeys are registered when the window's HWND is initialized.
    /// </summary>
    /// <param name="window">The WPF window to associate with the hotkeys.</param>
    public HotkeyService(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _window = window;

        window.SourceInitialized += OnSourceInitialized;
        window.Closed += OnWindowClosed;
    }

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="key">The virtual key to register.</param>
    /// <param name="modifiers">The modifier keys required.</param>
    /// <param name="callback">The callback to invoke when the hotkey is pressed.</param>
    public void Register(
        VirtualKeys key,
        ModifierKeys modifiers,
        Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(callback);

        lock (_sync)
        {
            if (_started)
            {
                throw new InvalidOperationException(
                    "Hotkeys cannot be registered after the service has started.");
            }

            int id = _nextId++;

            _hotkeys.Add(
                new RegisteredHotkey(
                    id,
                    modifiers,
                    key,
                    callback));
        }
    }

    /// <summary>
    /// Starts the framework-independent hotkey message loop.
    /// This method is not required when the service is associated with a WPF window.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_window is not null)
        {
            throw new InvalidOperationException(
                "Start cannot be called when the service is associated with a WPF window.");
        }

        lock (_sync)
        {
            if (_started)
                return;

            if (_hotkeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one hotkey must be registered before starting.");
            }

            _started = true;

            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Livia Hotkey Service"
            };

            _thread.Start();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (_window is null)
            return;

        lock (_sync)
        {
            if (_started)
                return;

            if (_hotkeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one hotkey must be registered before the window is initialized.");
            }

            var helper = new WindowInteropHelper(_window);

            _source = HwndSource.FromHwnd(helper.Handle);

            if (_source is null)
            {
                throw new InvalidOperationException(
                    "Failed to obtain the WPF window's HwndSource.");
            }

            _source.AddHook(HwndHook);

            try
            {
                RegisterHotkeys(helper.Handle);
                _started = true;
            }
            catch
            {
                _source.RemoveHook(HwndHook);
                _source = null;
                throw;
            }
        }
    }

    private IntPtr HwndHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message == User32.WM_HOTKEY)
        {
            HandleHotkey(unchecked((int)wParam));
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void Run()
    {
        _threadId = Kernel32.GetCurrentThreadId();

        try
        {
            RegisterHotkeys(IntPtr.Zero);

            if (!HasRegisteredHotkeys())
                return;

            while (true)
            {
                int result = User32.GetMessageW(
                    out User32.MSG message,
                    IntPtr.Zero,
                    0,
                    0);

                if (result == -1 || result == 0)
                    break;

                if (message.Message == User32.WM_HOTKEY)
                {
                    HandleHotkey(unchecked((int)message.WParam));
                }

                User32.TranslateMessage(ref message);
                User32.DispatchMessageW(ref message);
            }
        }
        finally
        {
            UnregisterHotkeys(IntPtr.Zero);
        }
    }

    private void RegisterHotkeys(IntPtr hwnd)
    {
        lock (_sync)
        {
            foreach (RegisteredHotkey hotkey in _hotkeys)
            {
                if (User32.RegisterHotKey(
                        hwnd,
                        hotkey.Id,
                        (uint)hotkey.Modifiers,
                        (uint)hotkey.Key))
                {
                    hotkey.Registered = true;
                    continue;
                }

                int error = Marshal.GetLastWin32Error();

                throw new Win32Exception(
                    error,
                    $"Failed to register hotkey {hotkey.Key}.");
            }
        }
    }

    private void UnregisterHotkeys(IntPtr hwnd)
    {
        lock (_sync)
        {
            foreach (RegisteredHotkey hotkey in _hotkeys)
            {
                if (!hotkey.Registered)
                    continue;

                User32.UnregisterHotKey(
                    hwnd,
                    hotkey.Id);

                hotkey.Registered = false;
            }
        }
    }

    private bool HasRegisteredHotkeys()
    {
        lock (_sync)
        {
            foreach (RegisteredHotkey hotkey in _hotkeys)
            {
                if (hotkey.Registered)
                    return true;
            }

            return false;
        }
    }

    private void HandleHotkey(int id)
    {
        Action? callback = null;

        lock (_sync)
        {
            foreach (RegisteredHotkey hotkey in _hotkeys)
            {
                if (hotkey.Id != id)
                    continue;

                callback = hotkey.Callback;
                break;
            }
        }

        callback?.Invoke();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_window is not null)
        {
            _window.SourceInitialized -= OnSourceInitialized;
            _window.Closed -= OnWindowClosed;

            if (_source is not null)
            {
                UnregisterHotkeys(_source.Handle);
                _source.RemoveHook(HwndHook);
                _source = null;
            }

            return;
        }

        Thread? thread;

        lock (_sync)
        {
            thread = _thread;
        }

        if (thread is null || !thread.IsAlive)
            return;

        uint threadId = _threadId;

        if (threadId != 0)
        {
            User32.PostThreadMessageW(
                threadId,
                User32.WM_QUIT,
                UIntPtr.Zero,
                IntPtr.Zero);
        }

        thread.Join(TimeSpan.FromSeconds(1));
    }

    private sealed class RegisteredHotkey
    {
        internal int Id
        {
            get;
        }

        internal ModifierKeys Modifiers
        {
            get;
        }

        internal VirtualKeys Key
        {
            get;
        }

        internal Action Callback
        {
            get;
        }

        internal bool Registered
        {
            get; set;
        }

        internal RegisteredHotkey(
            int id,
            ModifierKeys modifiers,
            VirtualKeys key,
            Action callback)
        {
            Id = id;
            Modifiers = modifiers;
            Key = key;
            Callback = callback;
        }
    }
}