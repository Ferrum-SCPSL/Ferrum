using System;
using Ferrum.Interop.Callbacks;
using Ferrum.Interop.Contracts;
using Ferrum.Interop.Memory;

namespace Ferrum.Interop.Invocation;

public static class NativeInvocation
{
    internal static string Execute(NativeApi api, string value, Action<string> log)
    {
        using var input = new PinnedUtf8Input(value);
        using var callback = new NativeLogCallback(log);
        using var output = new OwnedNativeBuffer(api.Free);

        NativeStatus status = api.Process(input.Data, input.Length, callback.Function, out output.Value);
        string result = output.Read();
        if (status != NativeStatus.Ok)
            throw new NativeBridgeException(status, $"Rust {status}: {result}", callback.Error);

        return result;
    }
}
