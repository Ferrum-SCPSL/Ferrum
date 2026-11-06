using System;
using Ferrum.Loader.Features;
using LabApi.Loader.Features.Plugins;

namespace Ferrum.Loader;

/// <summary>LabAPI entry point; lifecycle and bridge implementation live in Features.</summary>
public sealed class Loader : Plugin
{
    private readonly BridgeLoader loader = new();

    public override string Name => "Ferrum";
    public override string Description => "Experimental synchronous C# / Rust bridge PoC.";
    public override string Author => "Ferrum SCP:SL Team";
    public override Version RequiredApiVersion => new(1, 1, 7);

    public override void Enable() => loader.Enable();

    public override void Disable() => loader.Disable();
}
