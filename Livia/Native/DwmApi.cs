using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace Livia.Native;

internal static partial class DwmApi
{
    internal const uint DWMWA_CAPTION_COLOR = 35;

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        uint attribute,
        ref int value,
        int valueSize);
}