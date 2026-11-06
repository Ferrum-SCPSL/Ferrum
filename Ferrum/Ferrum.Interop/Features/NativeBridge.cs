using System;
using Ferrum.Interop.Contracts;
using Ferrum.Interop.Invocation;
using Ferrum.Interop.Marshalling;
using Ferrum.Interop.Platforms;

namespace Ferrum.Interop;

/// <summary>Synchronous ABI v1 facade. Dispose disables calls; it does not unload Rust.</summary>
public sealed class NativeBridge : IDisposable
{
    private readonly NativeApi api;
    private readonly object gate = new();
    private bool disposed;

    public static string PlatformName => NativePlatform.Name;

    public static string LibraryFileName => NativePlatform.LibraryFileName;

    public NativeBridge(string libraryPath) => api = new NativeApi(libraryPath);

    public string RoundTrip(string value, Action<string> log)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        if (log is null)
            throw new ArgumentNullException(nameof(log));

        lock (gate)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(NativeBridge));

            return NativeInvocation.Execute(api, value, log);
        }
    }

    internal static string ReadUtf8(IntPtr data, UIntPtr length) => Utf8Marshaller.Decode(data, length);

    public void Dispose()
    {
        lock (gate)
            disposed = true;
    }
}
