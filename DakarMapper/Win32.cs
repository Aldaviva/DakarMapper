using System;
using System.Runtime.InteropServices;

namespace DakarMapper {

    internal static class Win32 {

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [Flags]
        internal enum ProcessAccessFlags: uint {

            ALL                       = 0x001F0FFF,
            TERMINATE                 = 0x00000001,
            CREATE_THREAD             = 0x00000002,
            VIRTUAL_MEMORY_OPERATION  = 0x00000008,
            VIRTUAL_MEMORY_READ       = 0x00000010,
            VIRTUAL_MEMORY_WRITE      = 0x00000020,
            DUPLICATE_HANDLE          = 0x00000040,
            CREATE_PROCESS            = 0x00000080,
            SET_QUOTA                 = 0x00000100,
            SET_INFORMATION           = 0x00000200,
            QUERY_INFORMATION         = 0x00000400,
            QUERY_LIMITED_INFORMATION = 0x00001000,
            SYNCHRONIZE               = 0x00100000

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out long lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out IntPtr lpBuffer, int dwSize, out long lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hProcess);

    }

}