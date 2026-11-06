using System;

namespace Ferrum.Interop;

/// <summary>An error returned by Rust or encountered while loading its ABI.</summary>
public sealed class NativeBridgeException : Exception
{
    public NativeStatus Status { get; }

    public NativeBridgeException(NativeStatus status, string message, Exception? inner = null)
        : base(message, inner) => Status = status;
}
