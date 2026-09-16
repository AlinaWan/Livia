using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace Common.Native;

internal static class WindowChrome
{
    private const int DWMWA_CAPTION_COLOR = 35;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int valueSize);

    public static void SetTitleBarColor(
        WindowInteropHelper window,
        Color color)
    {
        IntPtr hwnd = window.EnsureHandle();

        int bgrColor =
            (color.B << 16) |
            (color.G << 8) |
            color.R;

        DwmSetWindowAttribute(
            hwnd,
            DWMWA_CAPTION_COLOR,
            ref bgrColor,
            sizeof(int));
    }
}