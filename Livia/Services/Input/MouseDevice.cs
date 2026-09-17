using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Livia.Native;

namespace Livia.Services.Input;

public class MouseDevice
{
    /// <summary>
    /// Moves mouse using normalized absolute screen coordinates (0 to 65535).
    /// </summary>
    public void MoveMouseToPositionOnVirtualDesktop(int x, int y)
    {
        User32.INPUT input = new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Un = new User32.INPUTUNION
            {
                Mi = new User32.MOUSEINPUT(
                    x,
                    y,
                    0,
                    User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_ABSOLUTE,
                    0,
                    IntPtr.Zero)
            }
        };

        User32.SendInput(1, new[] { input }, Marshal.SizeOf<User32.INPUT>());
    }

    /// <summary>
    /// Relative mouse movement.
    /// </summary>
    public void MoveMouseBy(int x, int y)
    {
        User32.INPUT input = new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Un = new User32.INPUTUNION
            {
                Mi = new User32.MOUSEINPUT(
                    x,
                    y,
                    0,
                    User32.MOUSEEVENTF_MOVE,
                    0,
                    IntPtr.Zero)
            }
        };

        User32.SendInput(1, new[] { input }, Marshal.SizeOf<User32.INPUT>());
    }

    /// <summary>
    /// Vertical scroll where 1 unit = 120 wheel delta (scroll up).
    /// </summary>
    public void VerticalScroll(int scrollAmountInClicks)
    {
        int scrollDelta = scrollAmountInClicks * 120;

        User32.INPUT input = new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Un = new User32.INPUTUNION
            {
                Mi = new User32.MOUSEINPUT(
                    0,
                    0,
                    (uint)scrollDelta,
                    User32.MOUSEEVENTF_WHEEL,
                    0,
                    IntPtr.Zero)
            }
        };

        User32.SendInput(1, new[] { input }, Marshal.SizeOf<User32.INPUT>());
    }

    public void LeftClick()
    {
        SendClick(User32.MOUSEEVENTF_LEFTDOWN, User32.MOUSEEVENTF_LEFTUP);
    }

    public void RightClick()
    {
        SendClick(User32.MOUSEEVENTF_RIGHTDOWN, User32.MOUSEEVENTF_RIGHTUP);
    }

    private void SendClick(uint downFlag, uint upFlag)
    {
        User32.INPUT[] inputs = new[]
        {
        new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Un = new User32.INPUTUNION { Mi = new User32.MOUSEINPUT(0, 0, 0, downFlag, 0, IntPtr.Zero) }
        },
        new User32.INPUT
        {
            Type = User32.INPUT_MOUSE,
            Un = new User32.INPUTUNION { Mi = new User32.MOUSEINPUT(0, 0, 0, upFlag, 0, IntPtr.Zero) }
        }
    };

        User32.SendInput(2, inputs, Marshal.SizeOf<User32.INPUT>());
    }
}
