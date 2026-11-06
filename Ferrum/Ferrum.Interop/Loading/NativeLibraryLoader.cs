using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Ferrum.Interop.Native;
using Ferrum.Interop.Platforms;

namespace Ferrum.Interop.Loading;

/// <summary>OS-specific library and export resolution. This PoC deliberately has no unload operation.</summary>
public static class NativeLibraryLoader
{
    internal static IntPtr Load(string absolutePath)
    {
        IntPtr handle = NativePlatform.IsWindows
            ? WindowsNativeMethods.LoadLibraryEx(absolutePath, IntPtr.Zero, WindowsNativeMethods.LoadWithAlteredSearchPath)
            : LinuxNativeMethods.Dlopen(absolutePath, LinuxNativeMethods.RtldNow);

        if (handle != IntPtr.Zero)
            return handle;

        string detail = NativePlatform.IsWindows
            ? new Win32Exception(Marshal.GetLastWin32Error()).Message
            : Marshal.PtrToStringAnsi(LinuxNativeMethods.Dlerror()) ?? "unknown dlopen error";

        throw new NativeBridgeException(NativeStatus.InternalError,
            $"Cannot load '{absolutePath}' for {NativePlatform.Name}. Check architecture and native dependencies: {detail}");
    }

    internal static IntPtr GetExport(IntPtr handle, string name)
    {
        IntPtr symbol = NativePlatform.IsWindows
            ? WindowsNativeMethods.GetProcAddress(handle, name)
            : LinuxNativeMethods.Dlsym(handle, name);

        if (symbol == IntPtr.Zero)
            throw new NativeBridgeException(NativeStatus.InternalError, $"Missing Ferrum native export: {name}");

        return symbol;
    }
}
