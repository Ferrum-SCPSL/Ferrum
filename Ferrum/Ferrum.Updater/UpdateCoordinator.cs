using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ferrum.Contracts;

namespace Ferrum.Updater;

public sealed class UpdateCoordinator : IDisposable
{
    private readonly RuntimeContext context;
    private readonly string currentVersion;
    private readonly Action<string, object[]> info;
    private readonly Action<string, Exception?> warning;
    private readonly Action<string> debug;
    private readonly CancellationTokenSource cancellation = new();
    private Task? loop;
    private int ready;
    public bool Ready => Volatile.Read(ref ready) != 0;

    public UpdateCoordinator(RuntimeContext context, string version, Action<string, object[]> info, Action<string, Exception?> warning, Action<string> debug)
    { this.context = context; currentVersion = version; this.info = info; this.warning = warning; this.debug = debug; }

    public void Start()
    {
        if (!context.Config.EnableAutoUpdates || loop != null) return;
        loop = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                try { await CheckAsync(cancellation.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return; }
                catch (Exception error) { warning("update.failed", error); }
                await Task.Delay(TimeSpan.FromMinutes(context.Config.UpdateCheckIntervalMinutes), cancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }

    private async Task CheckAsync(CancellationToken token)
    {
        debug("update.checking");
        var store = new RuntimeStateStore(context.StateDirectory);
        RuntimeState before = store.Read();
        if (!string.IsNullOrEmpty(before.Pending)) { Interlocked.Exchange(ref ready, 1); return; }
        using var client = new GitHubClient();
        GitHubRelease? release = await client.FindReleaseAsync("latest", context.Config.UpdateChannel, token).ConfigureAwait(false);
        if (release == null || ReleaseVersion.Parse(release.Tag).CompareTo(ReleaseVersion.Parse(currentVersion)) <= 0
            || ReleaseVersion.Parse(release.Tag).Value == before.Rejected) { debug("update.none"); return; }
        string version = ReleaseVersion.Parse(release.Tag).Value;
        info("update.downloading", new object[] { version });
        using var package = await PreparedPackage.DownloadAsync(client, release, context.Platform, context.StateDirectory, token).ConfigureAwait(false);
        if (package.Manifest.Bootstrap > 1 || package.Manifest.Contracts > 1)
        { info("update.bootstrap_required", new object[] { version }); return; }
        token.ThrowIfCancellationRequested();
        using (var gate = new InstallationLock(context.Root))
        {
            package.InstallRuntime(context.Root);
            store.Change(state =>
            {
                if (state.Active != currentVersion || !string.IsNullOrEmpty(state.Attempt)) throw new InvalidOperationException("Runtime state changed.");
                state.Pending = version;
            });
        }
        Interlocked.Exchange(ref ready, 1);
        info("update.prepared", new object[] { version });
    }

    public void Dispose()
    {
        cancellation.Cancel();
        // Never block the game thread on network IO. The loop owns the cancellation source until completion.
        if (loop != null) _ = loop.ContinueWith(_ => cancellation.Dispose(), TaskScheduler.Default);
        else cancellation.Dispose();
    }
}
