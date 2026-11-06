using System;
using Ferrum.Interop.Marshalling;

namespace Ferrum.Interop.Memory;

/// <summary>Releases Rust output through the same allocator, including error and decoding paths.</summary>
public sealed class OwnedNativeBuffer : IDisposable
{
    private readonly FreeBuffer free;
    private bool disposed;

    // Passed by out to the ABI; ownership is held only by this scope.
    internal NativeBuffer Value;

    internal OwnedNativeBuffer(FreeBuffer free) => this.free = free;

    internal string Read() => Utf8Marshaller.Decode(Value.Data, Value.Length);

    public void Dispose()
    {
        if (disposed)
            return;

        NativeStatus released = free(ref Value);
        if (released != NativeStatus.Ok)
            throw new NativeBridgeException(released, "Rust rejected its output buffer during cleanup.");

        disposed = true;
    }
}
