using System;
using Ferrum.Interop.Marshalling;

namespace Ferrum.Interop.Callbacks;

/// <summary>Roots the managed callback and contains every managed exception before returning to Rust.</summary>
public sealed class NativeLogCallback : IDisposable
{
    private readonly Action<string> log;

    internal LogCallback Function { get; }

    internal Exception? Error { get; private set; }

    internal NativeLogCallback(Action<string> log)
    {
        this.log = log;
        Function = Invoke;
    }

    private int Invoke(IntPtr data, UIntPtr length)
    {
        try
        {
            log(Utf8Marshaller.Decode(data, length));
            return 0;
        }
        catch (Exception error)
        {
            Error = error;
            return 1;
        }
    }

    public void Dispose() => GC.KeepAlive(Function);
}
