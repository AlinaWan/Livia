using System;
using System.Runtime.InteropServices;

namespace Livia.Native;

internal static class User32
{
    internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;

    internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    internal const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    internal const uint WM_QUIT = 0x0012;
    internal const uint WM_HOTKEY = 0x0312;

    // SendInput Constants
    internal const int INPUT_MOUSE = 0;
    internal const int INPUT_KEYBOARD = 1;

    internal const uint MOUSEEVENTF_MOVE = 0x0001;
    internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    internal const uint MOUSEEVENTF_LEFTUP = 0x0004;
    internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    internal const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    internal const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    internal const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    internal const uint MOUSEEVENTF_WHEEL = 0x0800;

    internal const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const uint KEYEVENTF_SCANCODE = 0x0008;
    internal const uint MAPVK_VK_TO_VSC = 0;
    internal const uint MAPVK_VK_TO_VSC_EX = 4;

    internal const int SM_CXSCREEN = 0;
    internal const int SM_CYSCREEN = 1;

    internal const int SW_SHOW = 5;
    internal const int SW_RESTORE = 9;

    internal delegate void WinEventDelegate(
        IntPtr hook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate callback,
        uint processId,
        uint threadId,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWinEvent(
        IntPtr hook);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(
        IntPtr hwnd,
        out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostThreadMessage(
        uint threadId,
        uint message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostThreadMessageW(
        uint threadId,
        uint msg,
        UIntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetMessageW(
        out MSG lpMsg,
        IntPtr hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TranslateMessage(
        ref MSG lpMsg);

    [DllImport("user32.dll")]
    internal static extern IntPtr DispatchMessageW(
        ref MSG lpMsg);

    [DllImport("user32.dll")]
    internal static extern int GetMessage(
        out MSG message,
        IntPtr hwnd,
        uint filterMin,
        uint filterMax);

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(
    IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(
        IntPtr hwnd,
        int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(
        IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PointX;
        public int PointY;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;

        public MOUSEINPUT(int dx, int dy, uint mouseData, uint dwFlags, uint time, IntPtr dwExtraInfo)
        {
            Dx = dx;
            Dy = dy;
            MouseData = mouseData;
            DwFlags = dwFlags;
            Time = time;
            DwExtraInfo = dwExtraInfo;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;

        public KEYBDINPUT(ushort vk, ushort scan, uint flags, uint time, IntPtr extraInfo)
        {
            Vk = vk;
            Scan = scan;
            Flags = flags;
            Time = time;
            ExtraInfo = extraInfo;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint Msg;
        public ushort ParamL;
        public ushort ParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT Mi;
        [FieldOffset(0)] public KEYBDINPUT Ki;
        [FieldOffset(0)] public HARDWAREINPUT Hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint Type;
        public INPUTUNION Un;
    }
}
