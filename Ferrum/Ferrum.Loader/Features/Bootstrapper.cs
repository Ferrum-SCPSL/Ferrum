using System;
using System.IO;
using Ferrum.Contracts;
using Ferrum.Loader.Verification;

namespace Ferrum.Loader.Features;

public sealed class Bootstrapper : IDisposable
{
    private RuntimeResolver? resolver;
    private InstallationLock? running;
    private IRuntime? runtime;
    private RuntimeStateStore? store;
    private string candidate = string.Empty;
    private bool ready;

    public void Enable(RuntimeContext context, Action<string> rollback)
    {
        InstallPaths.EnsureDataDirectories(context.Root);
        running = new InstallationLock(context.StateDirectory, "running.lock");
        store = new RuntimeStateStore(context.StateDirectory);
        RuntimeState state = store.Change(current =>
        {
            if (!string.IsNullOrEmpty(current.Attempt))
            {
                current.Rejected = current.Attempt;
                if (current.Attempt == current.Active && !string.IsNullOrEmpty(current.Previous)) current.Active = current.Previous;
                current.Attempt = string.Empty; current.Pending = string.Empty;
                rollback(current.Active);
            }
            if (string.IsNullOrEmpty(current.Active)) current.Active = "0.1.0";
            candidate = string.IsNullOrEmpty(current.Pending) ? current.Active : current.Pending;
            if (candidate == current.Rejected) throw new InvalidOperationException("Rejected runtime. Reinstall or roll back.");
            current.Attempt = candidate;
        });
        string directory = InstallPaths.Runtime(context.Root, candidate, context.Platform);
        PackageIntegrity.VerifyRuntime(directory, context.Platform, candidate);
        ReleaseManifest manifest = JsonFile.Read<ReleaseManifest>(Path.Combine(directory, "manifest.json"));
        if (manifest.Bootstrap > 1 || manifest.Contracts > 1) throw new InvalidDataException("New bootstrap required.");
        context.RuntimeDirectory = directory;
        context.MarkReady = MarkReady;
        resolver = new RuntimeResolver(directory);
        Type entry = resolver.LoadRuntime().GetType("Ferrum.Runtime.FrameworkRuntime", true)!;
        runtime = (IRuntime)Activator.CreateInstance(entry)!;
        runtime.Initialize(context);
    }

    public void PluginsEnabled() => runtime?.PluginsEnabled();
    public void RoundEnding() => runtime?.RoundEnding();

    private void MarkReady()
    {
        store!.Change(state =>
        {
            if (state.Attempt != candidate) throw new InvalidOperationException("Unexpected runtime attempt.");
            if (state.Active != candidate) state.Previous = state.Active;
            state.Active = candidate; state.Pending = string.Empty; state.Attempt = string.Empty;
        });
        ready = true;
    }

    public void Fail()
    {
        if (store == null || ready || candidate.Length == 0) return;
        store.Change(state =>
        {
            state.Rejected = candidate; state.Attempt = string.Empty; state.Pending = string.Empty;
            if (state.Active == candidate && !string.IsNullOrEmpty(state.Previous)) state.Active = state.Previous;
        });
    }

    public void Dispose()
    {
        try { runtime?.Dispose(); }
        finally { runtime = null; resolver?.Dispose(); resolver = null; running?.Dispose(); running = null; }
    }
}
