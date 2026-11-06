using System;
using System.Runtime.InteropServices;
using Ferrum.Interop.Loading;
using Ferrum.Interop.Platforms;

namespace Ferrum.Interop;

public sealed class NativeModule
{
    private readonly IntPtr handle;

    internal NativeModule(IntPtr handle) => this.handle = handle;

    internal static bool IsWindows => NativePlatform.IsWindows;

    internal static string PlatformName => NativePlatform.Name;

    internal static NativeModule Open(string path) => NativeModuleRegistry.Open(path);

    internal T GetExport<T>(string name) where T : Delegate
    {
        IntPtr symbol = NativeLibraryLoader.GetExport(handle, name);
        return Marshal.GetDelegateForFunctionPointer<T>(symbol);
    }
}
