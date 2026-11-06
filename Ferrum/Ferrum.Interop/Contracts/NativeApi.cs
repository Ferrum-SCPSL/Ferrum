using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop.Contracts;

/// <summary>Validates ABI v2 and binds its complete function table once.</summary>
public sealed class NativeApi
{
    private const uint ExpectedVersion = 2;

    internal ProcessUtf8 Process { get; }

    internal FreeBuffer Free { get; }

    internal SetLocalesUtf8 SetLocales { get; }

    internal NativeApi(string libraryPath)
    {
        if (Marshal.SizeOf<NativeBuffer>() != 16 || Marshal.OffsetOf<NativeBuffer>(nameof(NativeBuffer.Length)).ToInt32() != 8)
            throw new PlatformNotSupportedException("Unexpected FerrumBuffer ABI layout.");

        NativeModule module = NativeModule.Open(libraryPath);
        uint version = module.GetExport<AbiVersion>("ferrum_abi_version")();
        if (version != ExpectedVersion)
            throw new NativeBridgeException(NativeStatus.InternalError,
                $"Incompatible Ferrum ABI: expected {ExpectedVersion}, received {version}.");

        Process = module.GetExport<ProcessUtf8>("ferrum_process_utf8");
        Free = module.GetExport<FreeBuffer>("ferrum_buffer_free");
        SetLocales = module.GetExport<SetLocalesUtf8>("ferrum_set_locales_utf8");
    }
}
