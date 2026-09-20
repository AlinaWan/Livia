using System;
using System.Runtime.InteropServices;

namespace Livia.Native.ScreenCapture;

internal static class DxgiFrameCapture
{
    [DllImport(
        "Livia.Native.DxgiFrameCapture.dll",
        CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr InitContext();

    [DllImport(
        "Livia.Native.DxgiFrameCapture.dll",
        CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr GrabFramePointer(
        IntPtr context,
        int left,
        int top,
        int right,
        int bottom,
        out int rowPitch);

    [DllImport(
        "Livia.Native.DxgiFrameCapture.dll",
        CallingConvention = CallingConvention.Cdecl)]
    internal static extern void UnlockFramePointer(
        IntPtr context);

    [DllImport(
        "Livia.Native.DxgiFrameCapture.dll",
        CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CloseContext(
        IntPtr context);
}