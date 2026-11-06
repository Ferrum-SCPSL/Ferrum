using System;
using Ferrum.Interop.Contracts;
using Ferrum.Interop.Invocation;
using Ferrum.Interop.Marshalling;
using Ferrum.Interop.Platforms;
using Ferrum.Interop.Memory;

namespace Ferrum.Interop;

/// <summary>Synchronous ABI v2 facade. Dispose disables calls; it does not unload Rust.</summary>
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

    /// <summary>Rust copies both UTF-8 locale identifiers before returning; no managed pointer is retained.</summary>
    public void ConfigureLocales(string logLanguage, string pluginLanguage)
    {
        lock (gate)
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeBridge));
            using var log = new PinnedUtf8Input(logLanguage);
            using var plugin = new PinnedUtf8Input(pluginLanguage);
            using var output = new OwnedNativeBuffer(api.Free);
            NativeStatus status = api.SetLocales(log.Data, log.Length, plugin.Data, plugin.Length, out output.Value);
            string diagnostic = output.Read();
            if (status != NativeStatus.Ok) throw new NativeBridgeException(status, diagnostic);
        }
    }

    internal static string ReadUtf8(IntPtr data, UIntPtr length) => Utf8Marshaller.Decode(data, length);

    public void Dispose()
    {
        lock (gate)
            disposed = true;
    }
}
