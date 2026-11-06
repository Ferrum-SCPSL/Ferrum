using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop.Native;

public static class LinuxNativeMethods
{
    internal const int RtldNow = 2; // RTLD_NOW | RTLD_LOCAL on glibc x64.

    [DllImport("libdl.so.2", EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libdl.so.2", EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Dlsym(IntPtr module, string name);

    [DllImport("libdl.so.2", EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Dlerror();
}
