using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop.Native;

public static class WindowsNativeMethods
{
    // LOAD_WITH_ALTERED_SEARCH_PATH: callers must supply an absolute library path.
    internal const uint LoadWithAlteredSearchPath = 0x00000008;

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    internal static extern IntPtr GetProcAddress(IntPtr module, string name);
}
