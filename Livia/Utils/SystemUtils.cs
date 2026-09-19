using System;
using Livia.Native;

namespace Livia.Utils;

public static class SystemUtils
{
    public static bool InitiateSystemShutdown(uint timeoutSec = 30, string message = "Shutting down.")
    {
        return WithShutdownPrivilege(() =>
            AdvApi32.InitiateSystemShutdownExW(
                string.Empty,
                message,
                timeoutSec,
                bForceAppsClosed: true,
                bRebootAfterShutdown: false,
                dwReason: 0));
    }

    public static bool AbortSystemShutdown()
    {
        return WithShutdownPrivilege(() =>
            AdvApi32.AbortSystemShutdownW(string.Empty));
    }

    private static bool WithShutdownPrivilege(Func<bool> action)
    {
        if (!AdvApi32.OpenProcessToken(
            Kernel32.GetCurrentProcess(),
            AdvApi32.TOKEN_ADJUST_PRIVILEGES | AdvApi32.TOKEN_QUERY,
            out IntPtr tokenHandle))
        {
            return false;
        }

        bool privilegeAdjusted = false;
        AdvApi32.TOKEN_PRIVILEGES previousState = default;

        try
        {
            if (!AdvApi32.LookupPrivilegeValueW(string.Empty, AdvApi32.SE_SHUTDOWN_NAME, out AdvApi32.LUID luid))
                return false;

            AdvApi32.TOKEN_PRIVILEGES newState = new AdvApi32.TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privilege = new AdvApi32.LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = AdvApi32.SE_PRIVILEGE_ENABLED
                }
            };

            uint returnLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<AdvApi32.TOKEN_PRIVILEGES>();

            // Enable privilege and save previous state
            privilegeAdjusted = AdvApi32.AdjustTokenPrivileges(
                tokenHandle,
                false,
                ref newState,
                returnLength,
                ref previousState,
                out _);

            if (!privilegeAdjusted)
                return false;

            return action();
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
            {
                // Restore previous privilege state if we modified it
                if (privilegeAdjusted)
                {
                    AdvApi32.AdjustTokenPrivileges(
                        tokenHandle,
                        false,
                        ref previousState,
                        0,
                        IntPtr.Zero,
                        out _);
                }

                // Immediately release process token handle
                Kernel32.CloseHandle(tokenHandle);
            }
        }
    }
}