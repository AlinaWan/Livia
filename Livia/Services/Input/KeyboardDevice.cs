using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Livia.Native;

namespace Livia.Services.Input;

public class KeyboardDevice
{
    private static bool IsExtendedKey(VirtualKeys vkCode)
    {
        return vkCode is
            VirtualKeys.Menu or
            VirtualKeys.RightMenu or
            VirtualKeys.Control or
            VirtualKeys.RightControl or
            VirtualKeys.Insert or
            VirtualKeys.Delete or
            VirtualKeys.Home or
            VirtualKeys.End or
            VirtualKeys.Prior or
            VirtualKeys.Next or
            VirtualKeys.Right or
            VirtualKeys.Up or
            VirtualKeys.Left or
            VirtualKeys.Down or
            VirtualKeys.NumLock or
            VirtualKeys.Cancel or
            VirtualKeys.Snapshot or
            VirtualKeys.Divide;
    }

    public void KeyDown(VirtualKeys vkCode)
    {
        SendKey(vkCode, isDown: true);
    }

    public void KeyUp(VirtualKeys vkCode)
    {
        SendKey(vkCode, isDown: false);
    }

    public void KeyPress(VirtualKeys vkCode)
    {
        SendKey(vkCode, isDown: true);
        SendKey(vkCode, isDown: false);
    }

    private void SendKey(VirtualKeys vkCode, bool isDown)
    {
        uint mappedScanCode = User32.MapVirtualKeyW(
            (uint)vkCode,
            User32.MAPVK_VK_TO_VSC_EX);

        if (mappedScanCode == 0)
        {
            throw new ArgumentException(
                $"Virtual key 0x{(ushort)vkCode:X2} could not be mapped to a scan code.",
                nameof(vkCode));
        }

        uint flags = User32.KEYEVENTF_SCANCODE;

        if (IsExtendedKey(vkCode))
        {
            flags |= User32.KEYEVENTF_EXTENDEDKEY;
        }

        if (!isDown)
        {
            flags |= User32.KEYEVENTF_KEYUP;
        }

        User32.INPUT input = new()
        {
            Type = User32.INPUT_KEYBOARD,
            Un = new User32.INPUTUNION
            {
                Ki = new User32.KEYBDINPUT(
                    vk: 0,
                    scan: (ushort)(mappedScanCode & 0xFF),
                    flags: flags,
                    time: 0,
                    extraInfo: IntPtr.Zero)
            }
        };

        uint result = User32.SendInput(
            1,
            new[] { input },
            Marshal.SizeOf<User32.INPUT>());

        if (result != 1)
        {
            throw new InvalidOperationException(
                $"SendInput failed for VK 0x{(ushort)vkCode:X2}. " +
                $"LastError={Marshal.GetLastWin32Error()}");
        }
    }
}
