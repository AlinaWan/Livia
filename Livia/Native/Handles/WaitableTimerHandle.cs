using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace Livia.Native.Handles;

internal sealed class WaitableTimerHandle : IDisposable
{
    private readonly SafeWaitHandle _handle;

    private WaitableTimerHandle(SafeWaitHandle handle)
    {
        _handle = handle;
    }

    internal static WaitableTimerHandle Create()
    {
        SafeWaitHandle handle = Kernel32.CreateWaitableTimerExW(
            IntPtr.Zero,
            null,
            0,
            Kernel32.TIMER_ALL_ACCESS);

        if (handle.IsInvalid)
        {
            handle.Dispose();
            throw new Win32Exception();
        }

        return new WaitableTimerHandle(handle);
    }

    internal bool Set(DateTime targetUtc)
    {
        Kernel32.FILETIME dueTime =
            new(targetUtc.ToFileTimeUtc());

        return Kernel32.SetWaitableTimer(
            _handle,
            ref dueTime,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            false);
    }

    internal bool Cancel()
    {
        return Kernel32.CancelWaitableTimer(_handle);
    }

    internal uint Wait()
    {
        return Kernel32.WaitForSingleObject(
            _handle,
            Kernel32.INFINITE);
    }

    internal SafeWaitHandle Handle => _handle;

    public void Dispose()
    {
        _handle.Dispose();
    }
}