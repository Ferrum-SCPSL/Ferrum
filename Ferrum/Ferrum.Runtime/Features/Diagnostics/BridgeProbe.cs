using System;
using System.IO;
using Ferrum.Interop;

namespace Ferrum.Runtime.Features.Diagnostics;

/// <summary>The startup PoC is kept separate from plugin lifecycle management.</summary>
public static class BridgeProbe
{
    private const string Probe = "Ferrum: Привет из C# — Rust 🦀";

    public static string Run(NativeBridge bridge)
    {
        string? callbackMessage = null;
        int callbackCount = 0;
        string result = bridge.RoundTrip(Probe, text =>
        {
            callbackMessage = text;
            callbackCount++;
        });

        if (!string.Equals(Probe, result, StringComparison.Ordinal))
            throw new InvalidDataException("Rust returned a different probe string.");

        if (callbackCount != 1 || !string.Equals(callbackMessage, "Received UTF-8: " + Probe, StringComparison.Ordinal))
            throw new InvalidDataException("Rust logging callback did not preserve the probe string.");

        return result;
    }
}
