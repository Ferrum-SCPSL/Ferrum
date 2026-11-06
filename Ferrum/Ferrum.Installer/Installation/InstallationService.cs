using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ferrum.Contracts;
using Ferrum.Updater;

namespace Ferrum.Installer;

public sealed class InstallationService
{
    private readonly ServerInstallation installation;
    private readonly Action<string, object[]> log;
    public InstallationService(ServerInstallation installation, Action<string, object[]> log) { this.installation = installation; this.log = log; }

    public async Task InstallAsync(InstallerOptions options, CancellationToken token)
    {
        installation.RequireStopped();
        InstallPaths.EnsureDataDirectories(installation.FerrumPath);
        using var running = new InstallationLock(installation.StateDirectory, "running.lock");
        using var gate = new InstallationLock(installation.FerrumPath);
        var store = new RuntimeStateStore(installation.StateDirectory);
        if (options.Rollback)
        {
            RuntimeState state = store.Read();
            if (string.IsNullOrEmpty(state.Previous)) throw new InvalidOperationException("No previous runtime is available.");
            PackageIntegrity.VerifyRuntime(InstallPaths.Runtime(installation.FerrumPath, state.Previous, InstallPaths.Platform), InstallPaths.Platform, state.Previous);
            state = store.Change(current => { string old = current.Active; current.Active = current.Previous; current.Previous = old; current.Pending = string.Empty; current.Attempt = string.Empty; current.Rejected = string.Empty; });
            log("install.rollback", new object[] { state.Active }); return;
        }
        using var client = new GitHubClient();
        GitHubRelease? release = await client.FindReleaseAsync(options.Version, options.Channel, token).ConfigureAwait(false);
        if (release == null) throw new InstallerException("install.no_releases");
        using var package = await PreparedPackage.DownloadAsync(client, release, InstallPaths.Platform, installation.StateDirectory, token).ConfigureAwait(false);
        if (package.Manifest.Bootstrap > 1 || package.Manifest.Contracts > 1) throw new InvalidDataException("This installer is too old for the bootstrap contract.");
        package.InstallRuntime(installation.FerrumPath);
        using var transaction = new FileTransaction(installation.StateDirectory);
        foreach (string name in new[] { "Ferrum.Loader", "Ferrum.Contracts" })
        {
            string category = name == "Ferrum.Loader" ? "plugins" : "dependencies";
            transaction.Replace(Path.Combine(package.DirectoryPath, "bootstrap", name + ".dll"), Path.Combine(installation.LabApiPath, category, "global", name + ".dll"));
        }
        // Only known legacy Ferrum binaries are moved to the transaction backup; user plugins are untouched.
        transaction.Remove(Path.Combine(installation.LabApiPath, "dependencies", "global", "Ferrum.Interop.dll"));
        transaction.Remove(Path.Combine(installation.LabApiPath, "dependencies", "global", "Ferrum.Interop.pdb"));
        transaction.Remove(Path.Combine(installation.FerrumPath, "Plugins", "Dependencies", "ferrum_native.dll"));
        transaction.Remove(Path.Combine(installation.FerrumPath, "Plugins", "Dependencies", "libferrum_native.so"));
        foreach (string language in new[] { "en", "ru", "uk", "zh-CN" })
        {
            string destination = Path.Combine(installation.FerrumPath, "Localizations", language + ".csv");
            if (!File.Exists(destination)) transaction.Replace(Path.Combine(package.DirectoryPath, "localizations", language + ".csv"), destination);
        }
        var config = new FerrumConfig { FerrumDirectoryPath = installation.FerrumPath };
        transaction.CreateText(installation.ConfigPath, ConfigTemplate.Create(config));
        store.Change(state =>
        {
            if (state.Active != package.Manifest.Version) state.Previous = state.Active;
            state.Active = package.Manifest.Version; state.Pending = string.Empty; state.Attempt = string.Empty; state.Rejected = string.Empty;
        });
        transaction.Commit();
        log("install.done", new object[] { package.Manifest.Version });
    }
}
