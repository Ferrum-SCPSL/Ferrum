using System;
using Ferrum.Interop;
using Ferrum.Loader.Features.Diagnostics;
using Ferrum.Loader.Features.Logging;
using Ferrum.Loader.Features.Paths;
using LabApi.Events.Handlers;

namespace Ferrum.Loader.Features;

/// <summary>Owns the bridge for one enabled LabAPI plugin instance.</summary>
public sealed class BridgeLoader
{
    private NativeBridge? bridge;

    internal void Enable()
    {
        if (bridge != null)
            return;

        NativeBridge? candidate = null;
        try
        {
            FerrumPaths.EnsureDirectories();
            candidate = new NativeBridge(NativePaths.LibraryPath);
            BridgeProbe.Run(candidate);
            bridge = candidate;
            ServerEvents.PluginsEnabled += OnPluginsEnabled;
        }
        catch (Exception error)
        {
            candidate?.Dispose();
            bridge = null;
            Log.Error($"Bridge initialization failed; native bridge is unavailable. {error}");
        }
    }

    internal void Disable()
    {
        ServerEvents.PluginsEnabled -= OnPluginsEnabled;
        bridge?.Dispose();
        bridge = null;
        Log.Info("Disabled.");
    }

    private void OnPluginsEnabled()
    {
        ServerEvents.PluginsEnabled -= OnPluginsEnabled;
        if (bridge != null)
            StartupBanner.Show();
    }
}
