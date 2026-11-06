using System;
using System.Runtime.InteropServices;
using Ferrum.Interop.Marshalling;

namespace Ferrum.Interop.Memory;

/// <summary>Owns the GCHandle for one borrowed input, including the empty/null convention.</summary>
public sealed class PinnedUtf8Input : IDisposable
{
    private GCHandle pinned;

    internal IntPtr Data { get; }

    internal UIntPtr Length { get; }

    internal PinnedUtf8Input(string value)
    {
        byte[] bytes = Utf8Marshaller.Encode(value);
        Length = new UIntPtr((uint)bytes.Length);
        if (bytes.Length == 0)
            return;

        pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        Data = pinned.AddrOfPinnedObject();
    }

    public void Dispose()
    {
        if (pinned.IsAllocated)
            pinned.Free();
    }
}
