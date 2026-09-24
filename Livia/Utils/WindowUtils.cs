using System;
using Livia.Native;

namespace Livia.Utils;

public static class WindowUtils
{
    public static bool ForceFocusWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        if (User32.IsIconic(hwnd))
            User32.ShowWindow(hwnd, User32.SW_RESTORE);
        else
            User32.ShowWindow(hwnd, User32.SW_SHOW);

        return User32.SetForegroundWindow(hwnd);
    }

    public static bool ForceFocusWindow(long hwnd)
    {
        return ForceFocusWindow((IntPtr)hwnd);
    }
}