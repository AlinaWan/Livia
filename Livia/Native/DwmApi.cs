using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace Livia.Native;

internal static class DwmApi
{
    internal const uint DWMWA_CAPTION_COLOR = 35;

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        uint attribute,
        ref int value,
        int valueSize);
}