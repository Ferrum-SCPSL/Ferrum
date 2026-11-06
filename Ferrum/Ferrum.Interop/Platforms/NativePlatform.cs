using System;

namespace Ferrum.Interop.Platforms;

public static class NativePlatform
{
    internal static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

    internal static string LibraryFileName => IsWindows ? "ferrum_native.dll" : "libferrum_native.so";

    internal static string Name
    {
        get
        {
            if (IntPtr.Size != 8)
                throw new PlatformNotSupportedException("Ferrum requires a 64-bit process.");
            if (IsWindows)
                return "win-x64";
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return "linux-x64";

            throw new PlatformNotSupportedException("Ferrum supports Windows x64 and Linux x64.");
        }
    }
}
